using Colectica.Curation.Data;
using Colectica.Curation.Dataverse;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Colectica.Curation.Cli.Commands
{
    public class PublishToDataverse
    {
        private readonly string dataverseUrl;
        private readonly IConfiguration config;
        private readonly string? connectionString;
        private readonly string apiToken;
        private readonly string dataverseName;
        private readonly string? debugDir;

        public PublishToDataverse(string dataverseUrl, IConfiguration config)
        {
            this.dataverseUrl = dataverseUrl;
            this.config = config;

            connectionString = config["Data:DefaultConnection:ConnectionString"] ?? "";
            apiToken = config["Dataverse:ApiToken"] ?? "";
            dataverseName = config["Dataverse:DataverseName"] ?? "";
            debugDir = config["Data:DebugDirectory"] ?? "";

        }

        public async Task Publish()
        {
            Log.Logger.Information("Publishing to {destination}", dataverseUrl);

            using var db = new ApplicationDbContext(connectionString);

            var publishedRecords = db.CatalogRecords.Where(x => x.Status == CatalogRecordStatus.Published).ToList();
            if (!publishedRecords.Any())
            {
                Log.Logger.Information("No published records found. Exiting.");
                return;
            }

            foreach (var record in publishedRecords)
            {
                await PublishRecord(record);
            }
        }

        private async Task PublishRecord(CatalogRecord record)
        {
            Log.Debug("Processing record {recordId} {recordTitle}", record.Id, record.Title);

            // Check to see if the record is already in Dataverse.
            string checkUrl = $"{dataverseUrl}/api/search?q={record.Number}&type=dataset&metadata_fields=otherIdValue";
            string checkResultJson = await GetFromApiAsync(checkUrl, apiToken);

            string? existingPersistentId = GetIdFromSearchResult(checkResultJson);

            // Add or update the new record in Dataverse.
            DatasetDto datasetDto = DatasetDto.FromCatalogRecord(record);
            
            // Configure JsonSerializer to ignore null values and use camelCase
            var options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            

            string datasetResponse = "";
            ApiResponseDto? datasetApiResponse = null;
            try
            {
                if (existingPersistentId != null)
                {
                    // Update the existing dataset.
                    string datasetJson = JsonSerializer.Serialize(datasetDto.DatasetVersion, options);
                    StringContent datasetContent = new StringContent(datasetJson, Encoding.UTF8, "application/json");
                    string updateDatasetUrl = $"{dataverseUrl}/api/datasets/:persistentId/versions/:draft?persistentId={existingPersistentId}";
                    datasetResponse = await PutJsonToApiAsync(updateDatasetUrl, apiToken, datasetContent);
                }
                else
                {
                    // Create a new dataset.
                    string datasetJson = JsonSerializer.Serialize(datasetDto, options);
                    StringContent datasetContent = new StringContent(datasetJson, Encoding.UTF8, "application/json");

                    if (debugDir != null)
                    {
                        File.WriteAllText(Path.Combine(debugDir, record.Id.ToString() + ".json"), datasetJson);
                    }

                    string createDatasetUrl = $"{dataverseUrl}/api/dataverses/{dataverseName}/datasets?doNotValidate=true";
                    datasetResponse = await PostToApiAsync(createDatasetUrl, apiToken, datasetContent);
                }

                datasetApiResponse = JsonSerializer.Deserialize<ApiResponseDto>(datasetResponse, options);

                if (datasetApiResponse == null)
                {
                    Log.Error("Failed to deserialize API response. Response: {response}", datasetResponse);
                    return;
                }

                if (datasetApiResponse.Status != "OK")
                {
                    Log.Error("Failed to create dataset. Response: {response}", datasetResponse);
                    return;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to create dataset. Response: {response}", datasetResponse);
                return;
            }

            string? datasetDoi = datasetApiResponse.Data?.PersistentId;
            datasetDoi ??= datasetApiResponse.Data?.DatasetPersistentId;

            // Use the ispsArchiveDate field as the date in the citation.
            try
            {
                string setCitationDateUrl = $"{dataverseUrl}/api/datasets/:persistentId/citationdate?persistentId={datasetDoi}";
                var citationDateResponse = await PutToApiAsync(setCitationDateUrl, apiToken, "ispsArchiveDate", datasetDoi);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to set citation date for dataset {datasetDoi}", datasetDoi);
                return;
            }


            // Add all files.
            string addFileUrl = $"{dataverseUrl}/api/files/:persistentId/add?persistentId={datasetDoi}";

            foreach (var file in record.Files)
            {
                if (!file.IsPublicAccess)
                {
                    continue;
                }

                // For now, skip this file if it is over 5 MB.
                if (file.Size > 1 * 1024 * 256)
                {
                    Log.Debug("Skipping file {file} because it is larger than the set limit", file.Name);
                    continue;
                }

                Log.Debug("Processing file {file}", file.Name);

                // Check for an existing file.
                string fileCheckUrl = $"{dataverseUrl}/api/search?q={file.Number}&type=file";
                string fileCheckResultJson = await GetFromApiAsync(fileCheckUrl, apiToken);
                string? existingFileId = GetFileIdFromSearchResult(fileCheckResultJson, file.Name);

                FileDto fileDto = FileDto.FromManagedFile(file);

                if (existingFileId != null)
                {
                    // Update metadata for the existing file.

                    try
                    {
                        string updateFileUrl = $"{dataverseUrl}/api/files/{existingFileId}/metadata";

                        fileDto.DataFileId = existingFileId;
                        string fileJson = JsonSerializer.Serialize(fileDto, options);
                        StringContent fileMetadataContent = new StringContent(fileJson, Encoding.UTF8, "application/json");

                        string fileResponse = await PostToApiAsync(updateFileUrl, apiToken, fileMetadataContent);
                        Log.Information("File metadata updated successfully: {file}", file.Name);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Failed to update file metadata: {file}", file.Name);
                    }

                }
                else
                {
                    // Create a new file, and upload it.
                    string publishedDataDirectory = config["Curation:PublishedDataDirectory"] ?? "";
                    string filePath = Path.Combine(
                        publishedDataDirectory,
                        record.Id.ToString(),
                        file.Name);
                    if (!System.IO.File.Exists(filePath))
                    {
                        Log.Error("File not found: {filePath}", filePath);
                        continue;
                    }

                    string fileJson = JsonSerializer.Serialize(fileDto, options);
                    StringContent fileMetadataContent = new StringContent(fileJson, Encoding.UTF8, "application/json");

                    using var multipartContent = new MultipartFormDataContent();
                    multipartContent.Add(fileMetadataContent, "jsonData");
                    multipartContent.Add(new StreamContent(System.IO.File.OpenRead(filePath)), "file", file.Name);

                    try
                    {
                        string fileResponse = await PostToApiAsync(addFileUrl, apiToken, multipartContent);
                        Log.Information("File uploaded successfully: {file}", file.Name);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Failed to upload file: {file}", file.Name);
                    }
                }

            }

        }

        private string? GetFileIdFromSearchResult(string json, string fileLabel)
        {
            try
            {
                using JsonDocument checkDoc = JsonDocument.Parse(json);
                if (checkDoc.RootElement.TryGetProperty("data", out JsonElement dataElement) &&
                    dataElement.TryGetProperty("items", out JsonElement itemsElement) &&
                    itemsElement.GetArrayLength() > 0)
                {
                    JsonElement firstItem = itemsElement[0];
                    if (firstItem.TryGetProperty("file_id", out JsonElement idElement))
                    {
                        string? id = idElement.ToString();
                        Log.Debug("Found existing dataset with ID: {id}", id);
                        return id;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to parse search result JSON: {json}", json);
            }

            return null;           
        }

        private string? GetIdFromSearchResult(string searchResultJson)
        {
            try
            {
                using JsonDocument checkDoc = JsonDocument.Parse(searchResultJson);
                if (checkDoc.RootElement.TryGetProperty("data", out JsonElement dataElement) &&
                    dataElement.TryGetProperty("items", out JsonElement itemsElement) &&
                    itemsElement.GetArrayLength() > 0)
                {
                    JsonElement firstItem = itemsElement[0];
                    if (firstItem.TryGetProperty("global_id", out JsonElement idElement))
                    {
                        string? id = idElement.ToString();
                        Log.Debug("Found existing dataset with ID: {id}", id);
                        return id;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to parse search result JSON: {json}", searchResultJson);
            }

            return null;
        }

        private async Task<string> GetFromApiAsync(string url, string apiToken)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("X-Dataverse-key", apiToken);

            HttpResponseMessage response = await client.GetAsync(url);
            string responseBody = await response.Content.ReadAsStringAsync();

            response.EnsureSuccessStatusCode();

            return responseBody;
        }

        private async Task<string> PutToApiAsync(string url, string apiToken, string value, string? doi)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("X-Dataverse-key", apiToken);

            StringContent content = new StringContent(value, Encoding.UTF8, "application/x-www-form-urlencoded");

            HttpResponseMessage response = await client.PutAsync(url, content);
            string responseBody = await response.Content.ReadAsStringAsync();

            response.EnsureSuccessStatusCode();

            return responseBody;
        }

        public async Task<string> PutJsonToApiAsync(string url, string apiToken, HttpContent content)
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromMinutes(5);
            client.DefaultRequestHeaders.Add("X-Dataverse-key", apiToken);
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            HttpResponseMessage response = await client.PutAsync(url, content);
            string responseBody = await response.Content.ReadAsStringAsync();

            response.EnsureSuccessStatusCode();

            return responseBody;
        }

        public async Task<string> PostToApiAsync(string url, string apiToken, HttpContent content)
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromMinutes(5);
            client.DefaultRequestHeaders.Add("X-Dataverse-key", apiToken);
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            HttpResponseMessage response = await client.PostAsync(url, content);
            string responseBody = await response.Content.ReadAsStringAsync();

            response.EnsureSuccessStatusCode();

            return responseBody;
        }

    }
}
