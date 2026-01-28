using Colectica.Curation.Contracts;
using Colectica.Curation.Data;
using Colectica.Curation.Dataverse;
using Colectica.Curation.DdiAddins.Utility;
using log4net;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public void PublishRecord(CatalogRecord record, ApplicationUser user, ApplicationDbContext db, string ProcessingDirectory)
        {
            DataversePublisher dataversePublisher = new DataversePublisher(
                DataverseSettings.DataverseUrl,
                DataverseSettings.DataverseName,
                DataverseSettings.ApiToken,
                DataverseSettings.PublishedDataDirectory,
                DataverseSettings.DebugDirectory);

            string datasetDoi = Task.Run(async () => await dataversePublisher.PublishRecord(record)).GetAwaiter().GetResult();

            if (!string.IsNullOrEmpty(datasetDoi))
            {
                Task.Run(async () => await dataversePublisher.PublishFilesForRecord(record, datasetDoi, 1, 1)).GetAwaiter().GetResult();
            }
        }
    }
}
