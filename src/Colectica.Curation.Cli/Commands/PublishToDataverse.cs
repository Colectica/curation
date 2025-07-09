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

            foreach (var record in publishedRecords.Take(1))
            {
                await PublishRecord(record);
            }
        }

        private async Task PublishRecord(CatalogRecord record)
        {
            Log.Debug("Processing record {recordId} {recordTitle}", record.Id, record.Title);

            // TODO Check to see if the record is already in Dataverse.


            // Add the record to Dataverse.
            string createDatasetUrl = $"{dataverseUrl}/api/dataverses/{dataverseName}/datasets?doNotValidate=true";
            DatasetDto datasetDto = DatasetDto.FromCatalogRecord(record);
            
            // Configure JsonSerializer to ignore null values and use camelCase
            var options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            string datasetJson = JsonSerializer.Serialize(datasetDto, options);
            StringContent datasetContent = new StringContent(datasetJson, Encoding.UTF8, "application/json");

            if (debugDir != null)
            {
                File.WriteAllText(Path.Combine(debugDir, record.Id.ToString() + ".json"), datasetJson);
            }

            string datasetResponse = "";
            ApiResponseDto? datasetApiResponse = null;
            try
            {
                datasetResponse = await PostToApiAsync(createDatasetUrl, apiToken, datasetContent);
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
                File.WriteAllText("/home/jeremy/tmp/test.json", datasetJson);
                return;
            }

            string? datasetDoi = datasetApiResponse.Data?.PersistentId;
            string addFileUrl = $"{dataverseUrl}/api/datasets/:persistentId/add?persistentId={datasetDoi}";

            foreach (var file in record.Files)
            {
                if (!file.IsPublicAccess)
                {
                    continue;
                }

                Log.Debug("Processing file {file}", file.Name);

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

                FileDto fileDto = FileDto.FromManagedFile(file);
                string fileJson = JsonSerializer.Serialize(fileDto, options);

                using var multipartContent = new MultipartFormDataContent();
                multipartContent.Add(new StringContent(fileJson, Encoding.UTF8, "application/json"), "jsonData");
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

        public async Task<string> PostToApiAsync(string url, string apiToken, HttpContent content)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("X-Dataverse-key", apiToken);
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            HttpResponseMessage response = await client.PostAsync(url, content);
            string responseBody = await response.Content.ReadAsStringAsync();

            response.EnsureSuccessStatusCode();

            return responseBody;
        }

    }
}
