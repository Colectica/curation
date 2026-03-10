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
        private readonly string publishedDataDirectory;
        private readonly string? debugDir;
        private readonly RestRepositoryClient repositoryClient;

        public PublishToDataverse(IConfiguration config, string environment, string catalogRecordNumber)
        {
            this.config = config;
            this.catalogRecordNumber = catalogRecordNumber;
            connectionString = config["Data:DefaultConnection:ConnectionString"] ?? "";
            
            string envPrefix = $"Dataverse:Environments:{environment}";
            dataverseUrl = config[$"{envPrefix}:Url"] ?? "";
            apiToken = config[$"{envPrefix}:ApiToken"] ?? "";
            dataverseName = config[$"{envPrefix}:DataverseName"] ?? "";
            publishedDataDirectory = config["Curation:PublishedDataDirectory"] ?? "";
            
            if (string.IsNullOrWhiteSpace(dataverseUrl))
            {
                throw new ArgumentException($"Dataverse environment '{environment}' is not configured or has no Url specified.");
            }
            
            debugDir = config["Data:DebugDirectory"] ?? "";

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

            DataversePublisher dataversePublisher = new DataversePublisher(dataverseUrl, dataverseName, apiToken, publishedDataDirectory, debugDir,
             (type, message, ex) =>
             {
                 if (ex != null)
                 {
                     Log.Error(ex, "{type}: {message}", type, message);
                 }
                 else
                 {
                     Log.Information("{type}: {message}", type, message);
                 }
            });

            Dictionary<CatalogRecord, string> datasetDoiMap = [];
            foreach (var record in recordsToPublish)
            {
                string? doi = await dataversePublisher.PublishRecord(record);
                if (!string.IsNullOrWhiteSpace(doi))
                {
                    datasetDoiMap.Add(record, doi);
                }
            }

            int expectedFileCount = recordsToPublish.Sum(r => r.Files.Count(f => DataversePublisher.IsFileToBePublished(r, f)));
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

                await dataversePublisher.PublishFilesForRecord(record, doi, recordNumber, recordsToPublish.Count);
            }
        }




    }
}
