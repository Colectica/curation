using Algenta.Colectica.Model.Ddi;
using Algenta.Colectica.Model.Repository;
using Algenta.Colectica.Model.Serialization;
using Algenta.Colectica.Repository.Client;
using Colectica.Curation.Data;
using Colectica.Curation.Dataverse;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Linq;

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
        private readonly JsonSerializerOptions jsonOptions;
        private readonly RestRepositoryClient repositoryClient;

        public PublishToDataverse(string dataverseUrl, IConfiguration config)
        {
            this.dataverseUrl = dataverseUrl;
            this.config = config;

            connectionString = config["Data:DefaultConnection:ConnectionString"] ?? "";
            apiToken = config["Dataverse:ApiToken"] ?? "";
            dataverseName = config["Dataverse:DataverseName"] ?? "";
            debugDir = config["Data:DebugDirectory"] ?? "";

            // Configure JsonSerializer to ignore null values and use camelCase
            jsonOptions = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            RepositoryConnectionInfo connectionInfo = new()
            {
                TransportMethod = RepositoryTransportMethod.REST,
                AuthenticationMethod = RepositoryAuthenticationMethod.UserName,
                Url = config["Repository:Url"],
                UserName = config["Repository:UserName"],
                Password = config["Repository:Password"]
            };
            repositoryClient = new RestRepositoryClient(connectionInfo);
        }

        public async Task Publish()
        {
            Log.Logger.Information("Publishing to {destination}", dataverseUrl);

            using var db = new ApplicationDbContext(connectionString);

            var publishedRecords = db.CatalogRecords
                .Include(x => x.Owner)
                .Where(x => x.Status == CatalogRecordStatus.Published)
                .ToList();
            if (!publishedRecords.Any())
            {
                Log.Logger.Information("No published records found. Exiting.");
                return;
            }

            Dictionary<CatalogRecord, string> datasetDoiMap = [];
            var recordsToPublish = publishedRecords;
            foreach (var record in recordsToPublish)
            {
                string? doi = await PublishRecord(record);
                if (!string.IsNullOrWhiteSpace(doi))
                {
                    datasetDoiMap.Add(record, doi);
                }
            }

            foreach (var record in recordsToPublish)
            {
                datasetDoiMap.TryGetValue(record, out string? doi);
                if (string.IsNullOrWhiteSpace(doi))
                {
                    Log.Error("Record with no known DOI {number}", record.Number);
                    continue;
                }

                await PublishFilesForRecord(record, doi);
            }
        }


        private async Task<string?> PublishRecord(CatalogRecord record)
        {
            Log.Debug("Processing record {recordNumber} {recordId} {recordTitle}", record.Number, record.Id, record.Title);

            // Check to see if the record is already in Dataverse.
            string checkUrl = $"{dataverseUrl}/api/search?q={record.Number}&type=dataset&metadata_fields=otherIdValue";
            string checkResultJson = await GetFromApiAsync(checkUrl, apiToken);

            string? existingPersistentId = GetIdFromSearchResult(checkResultJson);

            // Add or update the new record in Dataverse.
            DatasetDto datasetDto = DatasetDto.FromCatalogRecord(record);

            string datasetResponse = "";
            ApiResponseDto? datasetApiResponse = null;
            try
            {
                if (existingPersistentId != null)
                {
                    Log.Information("Updating existing dataset");
                    // Update the existing dataset.
                    string datasetJson = JsonSerializer.Serialize(datasetDto.DatasetVersion, jsonOptions);
                    StringContent datasetContent = new StringContent(datasetJson, Encoding.UTF8, "application/json");

                    if (debugDir != null)
                    {
                        File.WriteAllText(Path.Combine(debugDir, record.Id.ToString() + ".json"), datasetJson);
                    }

                    string updateDatasetUrl = $"{dataverseUrl}/api/datasets/:persistentId/versions/:draft?persistentId={existingPersistentId}";
                    datasetResponse = await PutJsonToApiAsync(updateDatasetUrl, apiToken, datasetContent);
                }
                else
                {
                    Log.Information("Creating new dataset");

                    // Create a new dataset.
                    string datasetJson = JsonSerializer.Serialize(datasetDto, jsonOptions);
                    StringContent datasetContent = new StringContent(datasetJson, Encoding.UTF8, "application/json");

                    if (debugDir != null)
                    {
                        File.WriteAllText(Path.Combine(debugDir, record.Id.ToString() + ".json"), datasetJson);
                    }

                    string createDatasetUrl = $"{dataverseUrl}/api/dataverses/{dataverseName}/datasets?doNotValidate=true";
                    datasetResponse = await PostToApiAsync(createDatasetUrl, apiToken, datasetContent);
                }

                datasetApiResponse = JsonSerializer.Deserialize<ApiResponseDto>(datasetResponse, jsonOptions);

                if (datasetApiResponse == null)
                {
                    Log.Error("Failed to deserialize API response. Response: {response}", datasetResponse);
                    return null;
                }

                if (datasetApiResponse.Status != "OK")
                {
                    Log.Error("Failed to create or update dataset {id}. Message: {message}. Response: {response}", existingPersistentId, datasetApiResponse.MessageText, datasetResponse);
                    return null;
                }

                Log.Debug("Success");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to create dataset {id}. Response: {response}", existingPersistentId, datasetResponse);
                return null;
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
                return datasetDoi;
            }

            return datasetDoi;
        }

        private async Task PublishFilesForRecord(CatalogRecord record, string datasetDoi)
        {
            // Add all files.
            string addFileUrl = $"{dataverseUrl}/api/datasets/:persistentId/add?persistentId={datasetDoi}";

            int totalFileCount = record.Files.Count(f => f.IsPublicAccess);
            int fileCount = 0;
            foreach (var file in record.Files)
            {
                if (!file.IsPublicAccess)
                {
                    continue;
                }

                fileCount++;

                Log.Debug("Processing file {count} of {total} - {file}", fileCount, totalFileCount, file.Name);

                // Check for an existing file.
                string fileCheckUrl = $"{dataverseUrl}/api/search?q={file.Number}&type=file";
                string fileCheckResultJson = await GetFromApiAsync(fileCheckUrl, apiToken);
                string? existingFileId = GetFileIdFromSearchResult(fileCheckResultJson, file.Name);

                FileDto fileDto = FileDto.FromManagedFile(file);

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

                if (existingFileId != null)
                {
                    // Update the existing file.
                    try
                    {
                        string updateFileUrl = $"{dataverseUrl}/api/files/{existingFileId}/metadata";

                        fileDto.DataFileId = existingFileId;
                        string fileJson = JsonSerializer.Serialize(fileDto, jsonOptions);
                        StringContent fileMetadataContent = new StringContent(fileJson, Encoding.UTF8, "application/json");

                        using var multipartContent = new MultipartFormDataContent
                        {
                            { fileMetadataContent, "jsonData" }
                        };

                        string fileResponseStr = await PostToApiAsync(updateFileUrl, apiToken, multipartContent);
                        if (fileResponseStr.StartsWith("File Metadata update has been completed:"))
                        {
                            Log.Information("File metadata updated successfully: {file}", file.Name);
                        }
                        else
                        {
                            Log.Error("Failed to update file metadata. Response: {response}", fileResponseStr);
                            continue;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Failed to update file metadata: {file}", file.Name);
                    }

                }
                else
                {
                    string fileJson = JsonSerializer.Serialize(fileDto, jsonOptions);
                    StringContent fileMetadataContent = new StringContent(fileJson, Encoding.UTF8, "application/json");

                    using var multipartContent = new MultipartFormDataContent();
                    multipartContent.Add(fileMetadataContent, "jsonData");

                    multipartContent.Add(new StreamContent(System.IO.File.OpenRead(filePath)), "file", file.Name);

                    try
                    {
                        string fileResponseStr = await PostToApiAsync(addFileUrl, apiToken, multipartContent);
                        Log.Debug("File upload response: {response}", fileResponseStr);
                        var fileResponseObj = JsonSerializer.Deserialize<ApiResponseDto>(fileResponseStr, jsonOptions);

                        if (fileResponseObj == null)
                        {
                            Log.Error("Failed to deserialize API response. Response: {response}", fileResponseStr);
                            return;
                        }

                        if (fileResponseObj.Status != "OK")
                        {
                            Log.Error("Failed to create file. Message: {message}. Response: {response}", fileResponseObj.MessageText, fileResponseStr);
                            return;
                        }
                        Log.Information("File uploaded successfully: {file}", file.Name);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Failed to upload file: {file}", file.Name);
                        continue;
                    }
                }

                // If this is a data file and we have a PhysicalInstance, make some DDI 2 XML and send that to Dataverse.
                // if (file.Name.EndsWith(".dta") ||
                //     file.Name.EndsWith(".sav") ||
                //     file.Name.EndsWith(".csv") ||
                //     file.Name.EndsWith(".rda") ||
                //     file.Name.EndsWith(".rdata"))
                // {
                //     try
                //     {
                //         // Try to get the PhysicalInstance.
                //         if (repositoryClient.GetLatestItem(file.Id, "us.yale.isps") is PhysicalInstance physicalInstance)
                //         {
                //             SetPopulator populator = new(repositoryClient);
                //             physicalInstance.Accept(populator);

                //             DdiInstance ddiInstance = new();
                //             StudyUnit studyUnit = new();
                //             studyUnit.PhysicalInstances.Add(physicalInstance);
                //             ddiInstance.StudyUnits.Add(studyUnit);

                //             // Serialize as DDI Codebook
                //             Ddi2Serializer serializer = new();
                //             XDocument doc = serializer.GetDdi2(ddiInstance);

                //             // Write the full XML document to debug directory
                //             if (debugDir != null)
                //             {
                //                 string xmlFileName = $"{file.Id}_{file.Name}_ddi.xml";
                //                 string xmlFilePath = Path.Combine(debugDir, xmlFileName);
                //                 File.WriteAllText(xmlFilePath, doc.ToString());
                //             }
                            
                //             XNamespace ddiNamespace = "ddi:codebook:2_5";
                //             XElement? dataDscr = doc.Descendants(ddiNamespace + "dataDscr")?.FirstOrDefault();
                //             if (dataDscr != null)
                //             {
                //                 // Send the dataDscr section as the root.
                //                 // string xml = dataDscr?.ToString() ?? "";
                //                 string xml = File.ReadAllText("/home/jeremy/Downloads/dct.xml");
                //                 if (!string.IsNullOrWhiteSpace(xml))
                //                 {
                //                     string ddiUrl = $"{dataverseUrl}/api/edit/{existingFileId}";
                //                     StringContent ddiContent = new StringContent(xml, Encoding.UTF8, "application/xml");
                //                     string ddiResponseStr = await PutJsonToApiAsync(ddiUrl, apiToken, ddiContent);

                //                 }
                //             }
                //         }
                //         else
                //         {
                //             Log.Error("No PhysicalInstance exists for {id} {file}", file.Id, file.Name);
                //         }
                //     }
                //     catch (Exception ex)
                //     {
                //         Log.Error(ex, "Error while sending DDI to Dataverse");
                //     }
                // }
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
                    if (itemsElement.GetArrayLength() > 1)
                    {
                        Log.Error("Multiple files found with label {label}. Using the first one.", fileLabel);
                    }

                    JsonElement firstItem = itemsElement[0];
                    if (firstItem.TryGetProperty("file_id", out JsonElement idElement))
                    {
                        string? id = idElement.ToString();
                        Log.Debug("Found existing file with ID: {id}", id);
                        return id;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to parse search result JSON: {json}", json);
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
            return responseBody;
        }

        private async Task<string> PutToApiAsync(string url, string apiToken, string value, string? doi)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("X-Dataverse-key", apiToken);

            StringContent content = new StringContent(value, Encoding.UTF8, "application/x-www-form-urlencoded");

            HttpResponseMessage response = await client.PutAsync(url, content);
            string responseBody = await response.Content.ReadAsStringAsync();
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
            return responseBody;
        }

        public async Task<string> PostToApiAsync(string url, string apiToken, HttpContent content)
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromMinutes(30);
            client.DefaultRequestHeaders.Add("X-Dataverse-key", apiToken);
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            HttpResponseMessage response = await client.PostAsync(url, content);
            string responseBody = await response.Content.ReadAsStringAsync();
            return responseBody;
        }

    }

}
