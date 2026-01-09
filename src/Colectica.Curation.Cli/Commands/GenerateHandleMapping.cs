using Colectica.Curation.Data;
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

            var records = db.CatalogRecords
                .Where(x => x.Status == CatalogRecordStatus.Published)
                .OrderBy(x => x.Number)
                .ToList();

            if (!records.Any())
            {
                Log.Information("No published records found. Exiting.");
                return;
            }

            Log.Information("Found {count} published catalog records", records.Count);

            var mappings = new List<(string Handle, string Target)>();

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

                mappings.Add((record.PersistentId, doi));
                Log.Debug("Mapped {handle} -> {doi}", record.PersistentId, doi);
            }

            // Write CSV file
            var csv = new StringBuilder();
            csv.AppendLine("Handle,Target");
            foreach (var (handle, target) in mappings)
            {
                csv.AppendLine($"{handle},{target}");
            }

            await File.WriteAllTextAsync(filePath, csv.ToString());
            Log.Information("Handle mapping written to {file}. Total mappings: {count}", filePath, mappings.Count);
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
