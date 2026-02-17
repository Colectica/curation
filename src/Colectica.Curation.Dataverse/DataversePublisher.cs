using Colectica.Curation.Data;
using Polly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Colectica.Curation.Dataverse
{
    public class DataversePublisher
    {
        private readonly string apiToken;
        private readonly string publishedDataDirectory;
        private readonly string dataverseUrl;
        private readonly string dataverseName;
        private readonly string debugDir;

        private readonly HttpClient httpClient;
        private readonly IAsyncPolicy<HttpResponseMessage> retryPolicy;
        private readonly JsonSerializerOptions jsonOptions;
        private Dictionary<CatalogRecord, string> datasetIdMap = new Dictionary<CatalogRecord, string>();
        private readonly Action<string, string, Exception> log;

        private void LogDebug(string message) => log?.Invoke("Debug", message, null);
        private void LogInfo(string message) => log?.Invoke("Info", message, null);
        private void LogWarn(string message, Exception ex = null) => log?.Invoke("Warn", message, ex);
        private void LogError(string message, Exception ex = null) => log?.Invoke("Error", message, ex);

        public DataversePublisher(string dataverseUrl, string dataverseName, string apiToken, string publishedDataDirectory, string debugDir,
            Action<string, string, Exception> log = null)
        {
            // Configure JsonSerializer to ignore null values and use camelCase
            jsonOptions = new JsonSerializerOptions
            {
                IgnoreNullValues = true,
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

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
                        string url = context.TryGetValue("url", out var urlValue) ? urlValue?.ToString() : "unknown";
                        string reason = outcome.Exception?.Message ?? outcome.Result?.ReasonPhrase ?? "Unknown";
                        LogWarn($"Retry {retryCount} for {url} in {duration.TotalMilliseconds}ms. Reason: {reason}");
                    });

            // Create HttpClient with timeout
            httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromMinutes(30);
            httpClient.DefaultRequestHeaders.Add("X-Dataverse-key", apiToken);
            httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            this.apiToken = apiToken;
            this.publishedDataDirectory = publishedDataDirectory;
            this.dataverseUrl = dataverseUrl;
            this.dataverseName = dataverseName;
            this.debugDir = debugDir;
            this.log = log;
        }

        public async Task<string> PublishRecord(CatalogRecord record)
        {
            LogDebug($"Processing record {record.Number} {record.Id} {record.Title}");

            // Check to see if the record is already in Dataverse.
            string checkUrl = $"{dataverseUrl}/api/search?q={record.Number}&type=dataset&metadata_fields=otherIdValue";
            string checkResultJson = await GetFromApiAsync(checkUrl, apiToken);

            string existingPersistentId = GetIdFromSearchResult(checkResultJson);

            // Add or update the new record in Dataverse.
            DatasetDto datasetDto = DatasetDto.FromCatalogRecord(record);

            string datasetResponse = "";
            ApiResponseDto datasetApiResponse = null;
            try
            {
                if (existingPersistentId != null)
                {
                    LogInfo("Updating existing dataset");
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
                    LogInfo("Creating new dataset");

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
                    LogError($"Failed to deserialize API response. Response: {datasetResponse}");
                    return null;
                }

                if (datasetApiResponse.Status != "OK")
                {
                    LogError($"Failed to create or update dataset {record.Number}. Message: {datasetApiResponse.MessageText}. Response: {datasetResponse}");
                    return null;
                }

                LogDebug("Success");
            }
            catch (Exception ex)
            {
                LogError($"Failed to create dataset {existingPersistentId}. Response: {datasetResponse}", ex);
                return null;
            }

            string datasetDoi = datasetApiResponse.Data?.PersistentId;
            if (datasetDoi == null)
            {
                datasetDoi = datasetApiResponse.Data?.DatasetPersistentId;
            }

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
                LogWarn($"No dataset ID returned in response for record {record.Number}. Response: {datasetResponse}");
            }

            // Use the ispsArchiveDate field as the date in the citation.
            try
            {
                string setCitationDateUrl = $"{dataverseUrl}/api/datasets/:persistentId/citationdate?persistentId={datasetDoi}";
                var citationDateResponse = await PutToApiAsync(setCitationDateUrl, apiToken, "ispsArchiveDate", datasetDoi);
            }
            catch (Exception ex)
            {
                LogError($"Failed to set citation date for dataset {datasetDoi}", ex);
                return datasetDoi;
            }

            return datasetDoi;
        }

        public async Task PublishFilesForRecord(CatalogRecord record, string datasetDoi, int currentNumber, int totalNumber)
        {
            LogInfo($"Publishing files for record {record.Number} ({currentNumber}/{totalNumber}) to dataset {datasetDoi}");

            // Add all files.
            string addFileUrl = $"{dataverseUrl}/api/datasets/:persistentId/add?persistentId={datasetDoi}";

            if (!datasetIdMap.TryGetValue(record, out string datasetId) || string.IsNullOrWhiteSpace(datasetId))
            {
                LogError($"No dataset ID found for record {record.Number}. Cannot publish files.");
                return;
            }

            ExistingFilesDto existingFiles = null;
            try
            {
                string getFilesUrl = $"{dataverseUrl}/api/datasets/{datasetId}/versions/:latest/files";
                string getFilesJson = await GetFromApiAsync(getFilesUrl, apiToken);
                existingFiles = JsonSerializer.Deserialize<ExistingFilesDto>(getFilesJson, jsonOptions);
            }
            catch (Exception ex)
            {
                LogError($"Failed to retrieve existing files for record {record.Number}.", ex);
                return;
            }

            int totalFileCount = record.Files.Count(f => f.IsPublicAccess && f.Status == FileStatus.Accepted);
            int fileCount = 0;
            foreach (var file in record.Files)
            {
                if (!IsFileToBePublished(record, file))
                {
                    LogInfo($"Skipping file. {record.Number}, {Path.GetFileName(file.Name)}");
                    continue;
                }

                fileCount++;

                LogDebug($"Processing file {fileCount} of {totalFileCount} - {file.Name}");

                // Create a new file, and upload it.
                string filePath = Path.Combine(
                    publishedDataDirectory,
                    record.Id.ToString(),
                    file.Name);
                if (!System.IO.File.Exists(filePath))
                {
                    LogError($"File not found for record {record.Number}: {filePath}");
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
                    LogError($"Multiple existing files found with label {file.Name} in record {record.Number}. Using the first one.");

                    foreach (var existingFile in existingFileList)
                    {
                        LogError($" - Existing file ID: {existingFile.DataFile.Id}, Label: {existingFile.Label}, MD5: {existingFile.DataFile.Md5}");
                    }

                }

                string existingFileId = existingFileList?.FirstOrDefault()?.DataFile.Id.ToString();
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

                        using (var multipartContent = new MultipartFormDataContent { { fileMetadataContent, "jsonData" } })
                        {
                            string fileResponseStr = await PostToApiAsync(updateFileUrl, apiToken, multipartContent);
                            if (fileResponseStr.StartsWith("File Metadata update has been completed:"))
                            {
                                LogDebug($"File metadata updated successfully: {file.Name}");
                            }
                            else
                            {
                                LogError($"Failed to update file metadata in record {record.Number}. Response: {fileResponseStr}");
                                continue;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogError($"Exception while updating file metadata in record {record.Number}. {file.Name}", ex);
                    }

                }
                else
                {
                    string fileJson = JsonSerializer.Serialize(fileDto, jsonOptions);
                    StringContent fileMetadataContent = new StringContent(fileJson, Encoding.UTF8, "application/json");


                    try
                    {
                        using (var multipartContent = new MultipartFormDataContent())
                        {
                            multipartContent.Add(fileMetadataContent, "jsonData");
                            multipartContent.Add(new StreamContent(System.IO.File.OpenRead(filePath)), "file", file.Name);
                            // First, see if the dataset is locked. If so, wait a bit and try again.
                            string lockUrl = $"{dataverseUrl}/api/datasets/{datasetId}/locks";

                            bool isLockOkay = false;
                            int lockCheckCount = 0;
                            string lockResponse = "";
                            while (!isLockOkay)
                            {
                                if (lockCheckCount % 20 == 0)
                                {
                                    LogDebug($"Checking lock status for dataset {datasetId} for record {record.Number}, attempt {lockCheckCount + 1}");
                                }

                                lockResponse = await GetFromApiAsync(lockUrl, apiToken);
                                if (lockResponse == "{\"status\":\"OK\",\"data\":[]}")
                                {
                                    isLockOkay = true;
                                    break;
                                }

                                await Task.Delay(5000);
                                lockCheckCount++;
                            }

                            if (!isLockOkay)
                            {
                                LogError($"Dataset {record.Number} is locked after multiple attempts. Skipping file upload for record. {lockResponse}");
                                continue;
                            }


                            string fileResponseStr = await PostToApiAsync(addFileUrl, apiToken, multipartContent);
                            var fileResponseObj = JsonSerializer.Deserialize<ApiResponseDto>(fileResponseStr, jsonOptions);

                            if (fileResponseObj == null)
                            {
                                LogError($"Failed to deserialize API response. Response: {fileResponseStr}");
                                continue;
                            }

                            if (fileResponseObj.Status != "OK")
                            {
                                LogError($"Failed to create file. Message: {fileResponseObj.MessageText}. Response: {fileResponseStr}");
                                continue;
                            }
                            LogInfo($"File uploaded successfully: {file.Name}");
                        }
                    }
                    catch (Exception ex)
                    {
                        LogError($"Failed to upload file: {file.Name}", ex);
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

            // Delete files from Dataverse that are no longer marked for publication.
            if (existingFiles?.Data != null)
            {
                List<int> fileIdsToDelete = new List<int>();
                foreach (var file in record.Files)
                {
                    if (!IsFileToBePublished(record, file))
                    {
                        ExistingFileItemDto existingMatch = existingFiles.Data
                            .FirstOrDefault(ef => ef.Label == file.Name);
                        if (existingMatch != null)
                        {
                            fileIdsToDelete.Add(existingMatch.DataFile.Id);
                            LogInfo($"File '{file.Name}' is no longer published. Queuing for deletion from Dataverse (ID: {existingMatch.DataFile.Id}).");
                        }
                    }
                }

                if (fileIdsToDelete.Count > 0)
                {
                    try
                    {
                        string deleteUrl = $"{dataverseUrl}/api/datasets/{datasetId}/deleteFiles";
                        string deleteJson = JsonSerializer.Serialize(fileIdsToDelete);
                        StringContent deleteContent = new StringContent(deleteJson, Encoding.UTF8, "application/json");
                        string deleteResponse = await PutJsonToApiAsync(deleteUrl, apiToken, deleteContent);
                        LogInfo($"Deleted {fileIdsToDelete.Count} unpublished file(s) from Dataverse for record {record.Number}. Response: {deleteResponse}");
                    }
                    catch (Exception ex)
                    {
                        LogError($"Failed to delete unpublished files from Dataverse for record {record.Number}.", ex);
                    }
                }
            }
        }

        private string GetFileIdFromSearchResult(string json, string fileNumber, CatalogRecord record)
        {
            try
            {
                using (JsonDocument checkDoc = JsonDocument.Parse(json))
                {
                    if (checkDoc.RootElement.TryGetProperty("data", out JsonElement dataElement) &&
                        dataElement.TryGetProperty("items", out JsonElement itemsElement) &&
                        itemsElement.GetArrayLength() > 0)
                    {
                        if (itemsElement.GetArrayLength() > 1)
                        {
                            LogError($"Record {record.Number} has multiple files found with file number {fileNumber}. Using the first one.");
                        }

                        JsonElement firstItem = itemsElement[0];
                        if (firstItem.TryGetProperty("file_id", out JsonElement idElement))
                        {
                            string id = idElement.ToString();
                            LogDebug($"Found existing file with ID: {id}");
                            return id;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"Failed to parse search result JSON: {json}", ex);
            }

            return null;
        }

        private string GetIdFromSearchResult(string searchResultJson)
        {
            try
            {
                using (JsonDocument checkDoc = JsonDocument.Parse(searchResultJson))
                {

                    if (checkDoc.RootElement.TryGetProperty("data", out JsonElement dataElement) &&
                        dataElement.TryGetProperty("items", out JsonElement itemsElement) &&
                        itemsElement.GetArrayLength() > 0)
                    {
                        JsonElement firstItem = itemsElement[0];
                        if (firstItem.TryGetProperty("global_id", out JsonElement idElement))
                        {
                            string id = idElement.ToString();
                            LogDebug($"Found existing dataset with ID: {id}");
                            return id;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogWarn($"Failed to parse search result JSON: {searchResultJson}", ex);
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

        private async Task<string> PutToApiAsync(string url, string apiToken, string value, string doi)
        {
            var context = new Context(url) { ["url"] = url };
            byte[] contentBytes = Encoding.UTF8.GetBytes(value);

            HttpResponseMessage response = await retryPolicy.ExecuteAsync(async (ctx) =>
            {
                ByteArrayContent retryContent = new ByteArrayContent(contentBytes);
                retryContent.Headers.TryAddWithoutValidation("Content-Type", "application/x-www-form-urlencoded; charset=utf-8");
                return await httpClient.PutAsync(url, retryContent);
            }, context);

            string responseBody = await response.Content.ReadAsStringAsync();
            return responseBody;
        }

        public async Task<string> PutJsonToApiAsync(string url, string apiToken, HttpContent content)
        {
            var context = new Context(url) { ["url"] = url };
            byte[] contentBytes = await content.ReadAsByteArrayAsync();
            string contentType = content.Headers.ContentType?.ToString();

            HttpResponseMessage response = await retryPolicy.ExecuteAsync(async (ctx) =>
            {
                ByteArrayContent retryContent = new ByteArrayContent(contentBytes);
                if (contentType != null)
                {
                    retryContent.Headers.TryAddWithoutValidation("Content-Type", contentType);
                }
                return await httpClient.PutAsync(url, retryContent);
            }, context);

            string responseBody = await response.Content.ReadAsStringAsync();
            return responseBody;
        }

        public async Task<string> PostToApiAsync(string url, string apiToken, HttpContent content)
        {
            var context = new Context(url) { ["url"] = url };
            byte[] contentBytes = await content.ReadAsByteArrayAsync();
            string contentType = content.Headers.ContentType?.ToString();

            HttpResponseMessage response = await retryPolicy.ExecuteAsync(async (ctx) =>
            {
                ByteArrayContent retryContent = new ByteArrayContent(contentBytes);
                if (contentType != null)
                {
                    retryContent.Headers.TryAddWithoutValidation("Content-Type", contentType);
                }
                return await httpClient.PostAsync(url, retryContent);
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

        public static bool IsFileToBePublished(CatalogRecord record, ManagedFile file)
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

            if (file.Name.EndsWith("ddi32.xml") || file.Name.EndsWith("ddi33.xml"))
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

        public void Dispose()
        {
            if (httpClient != null)
            {
                httpClient.Dispose();
            }
        }
    }
}
