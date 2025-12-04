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
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Linq;
using Polly;
using Polly.Extensions.Http;

namespace Colectica.Curation.Cli.Commands
{
    public class PublishToDataverse
    {
        private readonly string dataverseUrl;
        private readonly IConfiguration config;
        private readonly string catalogRecordNumber;
        private readonly string? connectionString;
        private readonly string apiToken;
        private readonly string dataverseName;
        private readonly string? debugDir;
        private readonly JsonSerializerOptions jsonOptions;
        private readonly RestRepositoryClient repositoryClient;
        private readonly HttpClient httpClient;
        private readonly IAsyncPolicy<HttpResponseMessage> retryPolicy;
        private Dictionary<CatalogRecord, string> datasetIdMap = [];

        public PublishToDataverse(string dataverseUrl, IConfiguration config, string catalogRecordNumber)
        {
            this.dataverseUrl = dataverseUrl;
            this.config = config;
            this.catalogRecordNumber = catalogRecordNumber;
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

            // Configure retry policy
            retryPolicy = Policy
                .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                .Or<HttpRequestException>()
                .Or<TaskCanceledException>()
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // Exponential backoff
                    onRetry: (outcome, duration, retryCount, context) =>
                    {
                        Log.Warning("Retry {retryCount} for {url} in {duration}ms. Reason: {reason}",
                            retryCount,
                            context.GetValueOrDefault("url", "unknown"),
                            duration.TotalMilliseconds,
                            outcome.Exception?.Message ?? outcome.Result?.ReasonPhrase ?? "Unknown");
                    });

            // Create HttpClient with timeout
            httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromMinutes(30);
            httpClient.DefaultRequestHeaders.Add("X-Dataverse-key", apiToken);
            httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task Publish()
        {
            Log.Logger.Information("Publishing to {destination}", dataverseUrl);

            using var db = new ApplicationDbContext(connectionString);

            var recordsQuery = db.CatalogRecords
                .Include(x => x.Owner)
                .Where(x => x.Status == CatalogRecordStatus.Published && x.Organization.Id == new Guid("ad826be3-ba74-4719-b6eb-c626d061cf07"));

            if (!string.IsNullOrWhiteSpace(catalogRecordNumber))
            {
                recordsQuery = recordsQuery
                    .Where(x => x.Number == catalogRecordNumber);
            }

            var recordsToPublish = recordsQuery.OrderBy(x => x.Number).ToList();
            if (!recordsToPublish.Any())
            {
                Log.Logger.Information("No published records found. Exiting.");
                return;
            }

            Dictionary<CatalogRecord, string> datasetDoiMap = [];
            foreach (var record in recordsToPublish)
            {
                string? doi = await PublishRecord(record);
                if (!string.IsNullOrWhiteSpace(doi))
                {
                    datasetDoiMap.Add(record, doi);
                }
            }

            int expectedFileCount = recordsToPublish.Sum(r => r.Files.Count(f => IsFileToBePublished(r, f)));
            Log.Information("Publishing files for {recordCount} records. Expected file count: {fileCount}", recordsToPublish.Count, expectedFileCount);

            int recordNumber = 0;
            foreach (var record in recordsToPublish)
            {
                recordNumber++;
                datasetDoiMap.TryGetValue(record, out string? doi);
                if (string.IsNullOrWhiteSpace(doi))
                {
                    Log.Error("Record with no known DOI {number}", record.Number);
                    continue;
                }

                await PublishFilesForRecord(record, doi, recordNumber, recordsToPublish.Count);
            }
        }

        private bool IsFileToBePublished(CatalogRecord record, ManagedFile file)
        {
            // Only publish files that are marked as public access.
            if (!file.IsPublicAccess)
            {
                return false;
            }

            if (file.Status != FileStatus.Accepted)
            {
                return false;
            }

            if (file.Name.ToLower().EndsWith(".zip"))
            {
                return false;
            }

            string nameOnly = Path.GetFileNameWithoutExtension(file.Name);
            if (file.Name.EndsWith(".csv") && 
                record.Files.Any(f => f.Name == nameOnly + ".dta"))
            {
                return false;
            }

            return true;
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
                    Log.Error("Failed to create or update dataset {id}. Message: {message}. Response: {response}", record.Number, datasetApiResponse.MessageText, datasetResponse);
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

            if (datasetApiResponse.Data?.Id != null && datasetApiResponse.Data?.DatasetId == null)
            {
                datasetIdMap.Add(record, datasetApiResponse.Data?.Id.ToString() ?? "");
            }
            else if (datasetApiResponse.Data?.DatasetId != null)
            {
                datasetIdMap.Add(record, datasetApiResponse.Data?.DatasetId?.ToString() ?? "");
            }
            else
            {
                Log.Warning("No dataset ID returned in response for record {recordNumber}. Response: {response}", record.Number, datasetResponse);
            }

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

        private async Task PublishFilesForRecord(CatalogRecord record, string datasetDoi, int currentNumber, int totalNumber)
        {
            Log.Information("Publishing files for record {recordNumber} ({currentNumber}/{totalNumber}) to dataset {datasetDoi}", record.Number, currentNumber, totalNumber, datasetDoi);

            // Add all files.
            string addFileUrl = $"{dataverseUrl}/api/datasets/:persistentId/add?persistentId={datasetDoi}";

            if (!datasetIdMap.TryGetValue(record, out string? datasetId) || string.IsNullOrWhiteSpace(datasetId))
            {
                Log.Error("No dataset ID found for record {recordNumber}. Cannot publish files.", record.Number);
                return;
            }

            ExistingFilesDto? existingFiles = null;
            try
            {
                string getFilesUrl = $"{dataverseUrl}/api/datasets/{datasetId}/versions/:latest/files";
                string getFilesJson = await GetFromApiAsync(getFilesUrl, apiToken);
                existingFiles = JsonSerializer.Deserialize<ExistingFilesDto>(getFilesJson, jsonOptions);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to retrieve existing files for record {recordNumber}.", record.Number);
                return;
            }

            int totalFileCount = record.Files.Count(f => f.IsPublicAccess && f.Status == FileStatus.Accepted);
            int fileCount = 0;
            foreach (var file in record.Files)
            {
                if (!IsFileToBePublished(record, file))
                {
                    Log.Information("Skipping file. {recordNumber}, {fileName}", record.Number, Path.GetFileName(file.Name));
                    continue;
                }

                fileCount++;

                Log.Debug("Processing file {count} of {total} - {file}", fileCount, totalFileCount, file.Name);

                // Create a new file, and upload it.
                string publishedDataDirectory = config["Curation:PublishedDataDirectory"] ?? "";
                string filePath = Path.Combine(
                    publishedDataDirectory,
                    record.Id.ToString(),
                    file.Name);
                if (!System.IO.File.Exists(filePath))
                {
                    Log.Error("File not found for record {recordNumber}: {filePath}", record.Number, filePath);
                    continue;
                }

                // Compute MD5 checksum of local file
                string localMd5 = ComputeMd5Checksum(filePath);

                // Check for an existing file by label or MD5 match.
                var existingFileList = existingFiles?.Data?.Where(f => 
                    f.Label == file.Name ||
                    (!string.IsNullOrWhiteSpace(f.DataFile.Md5) && f.DataFile.Md5.Equals(localMd5, StringComparison.OrdinalIgnoreCase)));
                if (existingFileList?.Count() > 1)
                {
                    Log.Error("Multiple existing files found with label {label} in record {recordNumber}. Using the first one.", file.Name, record.Number);

                    foreach (var existingFile in existingFileList)
                    {
                        Log.Error(" - Existing file ID: {id}, Label: {label}, MD5: {md5}", existingFile.DataFile.Id, existingFile.Label, existingFile.DataFile.Md5);
                    }

                }

                string? existingFileId = existingFileList?.FirstOrDefault()?.DataFile.Id.ToString();
                FileDto fileDto = FileDto.FromManagedFile(file);

                if (existingFileId != null)
                {
                    // Update the existing file.
                    try
                    {
                        string updateFileUrl = $"{dataverseUrl}/api/files/{existingFileId}/metadata";

                        fileDto.DataFileId = existingFileId;
                        string fileJson = JsonSerializer.Serialize(fileDto, jsonOptions);
                        StringContent fileMetadataContent = new StringContent(fileJson, Encoding.UTF8, "application/json");

                        if (debugDir != null)
                        {
                            File.WriteAllText(Path.Combine(debugDir, record.Number.ToString() + "_" + file.Number?.ToString() + "_" + ".json"), fileJson);
                        }

                        using var multipartContent = new MultipartFormDataContent
                        {
                            { fileMetadataContent, "jsonData" }
                        };

                        string fileResponseStr = await PostToApiAsync(updateFileUrl, apiToken, multipartContent);
                        if (fileResponseStr.StartsWith("File Metadata update has been completed:"))
                        {
                            Log.Debug("File metadata updated successfully: {file}", file.Name);
                        }
                        else
                        {
                            Log.Error("Failed to update file metadata in record {recordNumber}. Response: {response}", record.Number, fileResponseStr);
                            continue;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Exception while updating file metadata in record {recordNumber}. {file}", record.Number, file.Name);
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
                        // First, see if the dataset is locked. If so, wait a bit and try again.
                        string lockUrl = $"{dataverseUrl}/api/datasets/{datasetId}/locks";

                        bool isLockOkay = false;
                        int lockCheckCount = 0;
                        string lockResponse = "";
                        while (!isLockOkay)
                        {
                            if (lockCheckCount % 20 == 0)
                            {
                                Log.Debug("Checking lock status for dataset {datasetId} for record {recordNumber}, attempt {attempt}", datasetId, record.Number, lockCheckCount + 1);
                            }

                            lockResponse = await GetFromApiAsync(lockUrl, apiToken);
                            if (lockResponse == """{"status":"OK","data":[]}""")
                            {
                                isLockOkay = true;
                                break;
                            }

                            Task.Delay(5000).Wait();
                            lockCheckCount++;
                        }

                        if (!isLockOkay)
                        {
                            Log.Error("Dataset {number} is locked after multiple attempts. Skipping file upload for record. {response}", record.Number, lockResponse);
                            continue;
                        }


                        string fileResponseStr = await PostToApiAsync(addFileUrl, apiToken, multipartContent);
                        var fileResponseObj = JsonSerializer.Deserialize<ApiResponseDto>(fileResponseStr, jsonOptions);

                        if (fileResponseObj == null)
                        {
                            Log.Error("Failed to deserialize API response. Response: {response}", fileResponseStr);
                            continue;
                        }

                        if (fileResponseObj.Status != "OK")
                        {
                            Log.Error("Failed to create file. Message: {message}. Response: {response}", fileResponseObj.MessageText, fileResponseStr);
                            continue;
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

        private string? GetFileIdFromSearchResult(string json, string fileNumber, CatalogRecord record)
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
                        Log.Error("Record {recordNumber} has multiple files found with file number {fileNumber}. Using the first one.", record.Number, fileNumber);
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
            var context = new Context(url) { ["url"] = url };
            
            HttpResponseMessage response = await retryPolicy.ExecuteAsync(async (ctx) =>
            {
                return await httpClient.GetAsync(url);
            }, context);

            string responseBody = await response.Content.ReadAsStringAsync();
            return responseBody;
        }

        private async Task<string> PutToApiAsync(string url, string apiToken, string value, string? doi)
        {
            var context = new Context(url) { ["url"] = url };
            StringContent content = new StringContent(value, Encoding.UTF8, "application/x-www-form-urlencoded");

            HttpResponseMessage response = await retryPolicy.ExecuteAsync(async (ctx) =>
            {
                return await httpClient.PutAsync(url, content);
            }, context);

            string responseBody = await response.Content.ReadAsStringAsync();
            return responseBody;
        }

        public async Task<string> PutJsonToApiAsync(string url, string apiToken, HttpContent content)
        {
            var context = new Context(url) { ["url"] = url };

            HttpResponseMessage response = await retryPolicy.ExecuteAsync(async (ctx) =>
            {
                return await httpClient.PutAsync(url, content);
            }, context);

            string responseBody = await response.Content.ReadAsStringAsync();
            return responseBody;
        }

        public async Task<string> PostToApiAsync(string url, string apiToken, HttpContent content)
        {
            var context = new Context(url) { ["url"] = url };

            HttpResponseMessage response = await retryPolicy.ExecuteAsync(async (ctx) =>
            {
                return await httpClient.PostAsync(url, content);
            }, context);

            string responseBody = await response.Content.ReadAsStringAsync();
            return responseBody;
        }

        private string ComputeMd5Checksum(string filePath)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filePath))
                {
                    byte[] hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        public void Dispose()
        {
            httpClient?.Dispose();
        }
    }
}
