using Colectica.Curation.Contracts;
using Colectica.Curation.Data;
using Colectica.Curation.Dataverse;
using Colectica.Curation.DdiAddins.Utility;
using log4net;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Colectica.Curation.DdiAddins.Actions
{
    [Export(typeof(IPublishAction))]
    public class PublishToDataverse : IPublishAction
    {
        ILog logger;

        public string Name => "Publish to Dataverse";

        public PublishToDataverse()
        {
            logger = LogManager.GetLogger("Curation");
        }

        public async Task PublishRecord(CatalogRecord record, ApplicationUser user, ApplicationDbContext db, string ProcessingDirectory)
        {
            LoadConfiguration();

            DataversePublisher dataversePublisher = new DataversePublisher(
                DataverseSettings.DataverseUrl,
                DataverseSettings.DataverseName,
                DataverseSettings.ApiToken,
                DataverseSettings.PublishedDataDirectory,
                DataverseSettings.DebugDirectory,
                (level, message, ex) =>
                {
                    switch (level)
                    {
                        case "Debug": logger.Debug(message); break;
                        case "Info": logger.Info(message); break;
                        case "Warn": logger.Warn(message, ex); break;
                        case "Error": logger.Error(message, ex); break;
                    }
                });

            logger.Info($"Starting Dataverse publish for record {record?.Title ?? "(null)"}");
            logger.Info($"Dataverse URL: '{DataverseSettings.DataverseUrl}', Name: '{DataverseSettings.DataverseName}'");

            try
            {
                logger.Info("Calling dataversePublisher.PublishRecord...");
                string datasetDoi = await dataversePublisher.PublishRecord(record);
                logger.Info($"Publish completed with DOI: '{datasetDoi ?? "(null)"}'");

                if (!string.IsNullOrEmpty(datasetDoi))
                {
                    logger.Info("Calling dataversePublisher.PublishFilesForRecord...");
                    await dataversePublisher.PublishFilesForRecord(record, datasetDoi, 1, 1);
                    logger.Info("PublishFilesForRecord completed.");
                }
                else
                {
                    logger.Warn("DOI was null or empty - skipping file publish.");
                }
            }
            catch (Exception ex)
            {
                logger.Error("Error publishing to Dataverse", ex);
                throw;
            }
        }

        public void LoadConfiguration()
        {
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            string configPath = Path.Combine(basePath, "dataverse.json");

            if (!File.Exists(configPath))
            {
                return;
            }

            try
            {
                string json = File.ReadAllText(configPath);
                JObject jObject = JObject.Parse(json);
                var dataverseSection = jObject["Dataverse"];

                if (dataverseSection != null)
                {
                    DataverseSettings.DataverseUrl = dataverseSection["Url"]?.Value<string>() ?? "";
                    DataverseSettings.DataverseName = dataverseSection["DataverseName"]?.Value<string>() ?? "";
                    DataverseSettings.ApiToken = dataverseSection["ApiToken"]?.Value<string>() ?? "";
                    DataverseSettings.PublishedDataDirectory = dataverseSection["PublishedDataDirectory"]?.Value<string>() ?? "";
                    DataverseSettings.DebugDirectory = dataverseSection["DebugDirectory"]?.Value<string>() ?? "";

                    logger.Debug($"Dataverse config loaded with URL {DataverseSettings.DataverseUrl}");
                }
            }
            catch (Exception ex)
            {
                logger.Warn("Failed to load Dataverse configuration", ex);
            }
        }
    }

}
