using Colectica.Curation.Contracts;
using Colectica.Curation.Data;
using Colectica.Curation.Dataverse;
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
    public class PublishToDataverse
    {
        ILog logger;

        public string Name => "Publish to Dataverse";

        public PublishToDataverse()
        {
            logger = LogManager.GetLogger("Curation");
        }

        public void PublishRecord(CatalogRecord record, ApplicationUser user, ApplicationDbContext db, string ProcessingDirectory)
        {
            string dataverseUrl = "";
            string dataverseName = "";
            string apiToken = "";
            string publishedDataDirectory = "";
            string debugDir = ";";
            DataversePublisher dataversePublisher = new DataversePublisher(dataverseUrl, dataverseName, apiToken, publishedDataDirectory, debugDir);

        }
    }
}
