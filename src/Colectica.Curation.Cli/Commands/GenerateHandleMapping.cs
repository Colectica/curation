using Colectica.Curation.Data;
using Colectica.Curation.Dataverse;
using Microsoft.Extensions.Configuration;
using Polly;
using Serilog;
using System.Data.Entity;
using System.Text;
using System.Text.Json;

namespace Colectica.Curation.Cli.Commands
{
    public class GenerateHandleMapping
    {
        private readonly string dataverseUrl;
        private readonly string filePath;
        private readonly IConfiguration config;
        private readonly string? connectionString;
        private readonly string apiToken;
        private readonly HttpClient httpClient;
        private readonly IAsyncPolicy<HttpResponseMessage> retryPolicy;

        public GenerateHandleMapping(string file, IConfiguration config, string environment)
        {
            this.filePath = file;
            this.config = config;
            connectionString = config["Data:DefaultConnection:ConnectionString"] ?? "";
            
            string envPrefix = $"Dataverse:Environments:{environment}";
            dataverseUrl = config[$"{envPrefix}:Url"] ?? "";
            apiToken = config[$"{envPrefix}:ApiToken"] ?? "";
            
            if (string.IsNullOrWhiteSpace(dataverseUrl))
            {
                throw new ArgumentException($"Dataverse environment '{environment}' is not configured or has no Url specified.");
            }

            retryPolicy = Policy
                .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                .Or<HttpRequestException>()
                .Or<TaskCanceledException>()
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (outcome, duration, retryCount, context) =>
                    {
                        Log.Warning("Retry {retryCount} for {url} in {duration}ms. Reason: {reason}",
                            retryCount,
                            context.GetValueOrDefault("url", "unknown"),
                            duration.TotalMilliseconds,
                            outcome.Exception?.Message ?? outcome.Result?.ReasonPhrase ?? "Unknown");
                    });

            httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromMinutes(10);
            httpClient.DefaultRequestHeaders.Add("X-Dataverse-key", apiToken);
            httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task Generate()
        {
            Log.Information("Generating handle mapping from {dataverseUrl} to {file}", dataverseUrl, filePath);

            using var db = new ApplicationDbContext(connectionString);

            var org = db.Organizations.FirstOrDefault(x => x.Hostname == "isps.yard.yale.edu");

            var records = db.CatalogRecords
                .Include(x => x.Files)
                .Where(x => x.Organization.Id == org.Id)
                .Where(x => x.Status == CatalogRecordStatus.Published)
                .OrderBy(x => x.Number)
                .ToList();

            if (!records.Any())
            {
                Log.Information("No published records found. Exiting.");
                return;
            }

            Log.Information("Found {count} published catalog records", records.Count);

            var datasetMappings = new List<(string Number, string Title, string Handle, string Target)>();
            var fileMappings = new List<(string Number, string Title, string Handle, string Target)>();

            foreach (var record in records)
            {
                if (string.IsNullOrWhiteSpace(record.PersistentId))
                {
                    Log.Warning("Record {number} has no PersistentId (Handle). Skipping.", record.Number);
                    continue;
                }

                string? doi = await GetDataverseDoi(record);
                if (string.IsNullOrWhiteSpace(doi))
                {
                    Log.Warning("Record {number} has no corresponding Dataverse DOI. Skipping.", record.Number);
                    continue;
                }

                datasetMappings.Add((record.Number, record.Title.Replace("\"", ""), record.PersistentId, doi));
                Log.Debug("Mapped dataset {handle} -> {doi}", record.PersistentId, doi);

                // Get file mappings for this record
                var recordFileMappings = await GetFileMappings(record, doi);
                fileMappings.AddRange(recordFileMappings);
            }

            // Write dataset mappings CSV file
            var datasetCsv = new StringBuilder();
            datasetCsv.AppendLine("Number,Title,Handle,Target");
            foreach (var (number, title, handle, target) in datasetMappings)
            {
                datasetCsv.AppendLine($"{number},\"{title}\",{handle},{target}");
            }

            await File.WriteAllTextAsync(filePath, datasetCsv.ToString());
            Log.Information("Dataset handle mapping written to {file}. Total mappings: {count}", filePath, datasetMappings.Count);

            // Write file mappings CSV file
            string filesMappingPath = GetFilesMappingPath(filePath);
            var filesCsv = new StringBuilder();
            filesCsv.AppendLine("Number,Title,Handle,Target");
            foreach (var (number, title, handle, target) in fileMappings)
            {
                filesCsv.AppendLine($"{number},\"{title}\",{handle},{target}");
            }

            await File.WriteAllTextAsync(filesMappingPath, filesCsv.ToString());
            Log.Information("File handle mapping written to {file}. Total mappings: {count}", filesMappingPath, fileMappings.Count);
        }

        private string GetFilesMappingPath(string originalPath)
        {
            string directory = Path.GetDirectoryName(originalPath) ?? "";
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalPath);
            string extension = Path.GetExtension(originalPath);
            
            return Path.Combine(directory, $"{fileNameWithoutExtension}-files{extension}");
        }

        private async Task<List<(string Number, string Title, string Handle, string Target)>> GetFileMappings(CatalogRecord record, string datasetDoi)
        {
            var fileMappings = new List<(string Number, string Title, string Handle, string Target)>();

            try
            {
                // Get the dataset ID from the DOI
                string? datasetId = await GetDatasetIdFromDoi(datasetDoi);
                if (string.IsNullOrWhiteSpace(datasetId))
                {
                    Log.Warning("Could not get dataset ID for DOI {doi}. Skipping file mappings.", datasetDoi);
                    return fileMappings;
                }

                // Get the list of files from Dataverse
                string getFilesUrl = $"{dataverseUrl}/api/datasets/{datasetId}/versions/:latest/files";
                string getFilesJson = await GetFromApiAsync(getFilesUrl);
                
                var existingFiles = JsonSerializer.Deserialize<ExistingFilesDto>(getFilesJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                if (existingFiles == null || existingFiles.Data == null || !existingFiles.Data.Any())
                {
                    Log.Debug("No files found in Dataverse for record {number}", record.Number);
                    return fileMappings;
                }

                Log.Debug("Found {count} files in Dataverse for record {number}", existingFiles.Data.Count, record.Number);

                // Match ManagedFiles to Dataverse files and create mappings
                foreach (var managedFile in record.Files)
                {
                    if (!DataversePublisher.IsFileToBePublished(record, managedFile))
                    {
                        Log.Debug("File {fileName} in record {number} is not marked for publication. Skipping.", managedFile.Name, record.Number);
                        continue;
                    }

                    string fileTitle = !string.IsNullOrWhiteSpace(managedFile.Name) ? managedFile.Name.Replace("\"", "") : managedFile.Name;

                    if (string.IsNullOrWhiteSpace(managedFile.PersistentLink))
                    {
                        Log.Debug("File {fileName} in record {number} has no PersistentLink. Skipping.", managedFile.Name, record.Number);
                        fileMappings.Add((managedFile.Number, fileTitle, "NONE", ""));
                        continue;
                    }

                    // Find the matching Dataverse file by filename (label)
                    var dataverseFile = existingFiles.Data.FirstOrDefault(f => f.Label == managedFile.Name || (!string.IsNullOrWhiteSpace(managedFile.Number) && f.Description.Contains(managedFile.Number)));
                    
                    if (dataverseFile == null)
                    {
                        Log.Warning("Could not find Dataverse file for {fileName} in record {number}. Skipping.", managedFile.Name, record.Number);
                        fileMappings.Add((managedFile.Number, fileTitle, managedFile.PersistentLink, "NONE"));
                        continue;
                    }

                    string fileUrl = $"{dataverseUrl}/file.xhtml?fileId={dataverseFile.DataFile.Id}";
                    fileMappings.Add((managedFile.Number, fileTitle, managedFile.PersistentLink, fileUrl));
                    Log.Debug("Mapped file {handle} -> {fileUrl}", managedFile.PersistentLink, fileUrl);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting file mappings for record {number}", record.Number);
            }

            return fileMappings;
        }

        private async Task<string?> GetDatasetIdFromDoi(string doi)
        {
            try
            {
                string getDatasetUrl = $"{dataverseUrl}/api/datasets/:persistentId?persistentId={doi}";
                string datasetJson = await GetFromApiAsync(getDatasetUrl);
                
                using JsonDocument doc = JsonDocument.Parse(datasetJson);
                if (doc.RootElement.TryGetProperty("data", out JsonElement dataElement) &&
                    dataElement.TryGetProperty("id", out JsonElement idElement))
                {
                    return idElement.GetInt32().ToString();
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to get dataset ID for DOI {doi}", doi);
            }

            return null;
        }

        private async Task<string?> GetDataverseDoi(CatalogRecord record)
        {
            string checkUrl = $"{dataverseUrl}/api/search?q={record.Number}&type=dataset&metadata_fields=otherIdValue";
            string checkResultJson = await GetFromApiAsync(checkUrl);

            return GetDoiFromSearchResult(checkResultJson);
        }

        private string? GetDoiFromSearchResult(string searchResultJson)
        {
            try
            {
                using JsonDocument doc = JsonDocument.Parse(searchResultJson);
                if (doc.RootElement.TryGetProperty("data", out JsonElement dataElement) &&
                    dataElement.TryGetProperty("items", out JsonElement itemsElement) &&
                    itemsElement.GetArrayLength() > 0)
                {
                    JsonElement firstItem = itemsElement[0];
                    if (firstItem.TryGetProperty("global_id", out JsonElement idElement))
                    {
                        return idElement.GetString();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to parse search result JSON: {json}", searchResultJson);
            }

            return null;
        }

        private async Task<string> GetFromApiAsync(string url)
        {
            var context = new Context(url) { ["url"] = url };

            HttpResponseMessage response = await retryPolicy.ExecuteAsync(async (ctx) =>
            {
                return await httpClient.GetAsync(url);
            }, context);

            return await response.Content.ReadAsStringAsync();
        }

    }
}
