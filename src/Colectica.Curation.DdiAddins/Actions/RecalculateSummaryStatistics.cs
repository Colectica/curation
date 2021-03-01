using Colectica.Curation.Contracts;
using Colectica.Curation.Data;
using Colectica.Curation.Web.Utility;
using log4net;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Colectica.Curation.DdiAddins.Actions
{
    [Export(typeof(IApplyMetadataUpdatesAction))]
    public class RecalculateSummaryStatistics : IApplyMetadataUpdatesAction
    {
        ILog logger;

        public string Name { get { return "Apply Metadata Updates to Stata File"; } }

        public RecalculateSummaryStatistics()
        {
            logger = LogManager.GetLogger("Curation");
        }

        public bool CanApplyMetadataUpdates(ManagedFile file)
        {
            return file.IsStatisticalDataFile();
        }

        public bool ApplyMetadataUpdates(CatalogRecord record, ManagedFile file, ApplicationUser user, string userId, ApplicationDbContext db, string processingDirectory)
        {
            try
            {
                string agencyId = record.Organization.AgencyID;
                string fullProcessingDirectory = System.IO.Path.Combine(processingDirectory, record.Id.ToString());
                var piUpdater = new CreatePhysicalInstances();
                piUpdater.Run(record, file, user, db, fullProcessingDirectory, agencyId);
            }
            catch (Exception ex)
            {
                logger.Error("Error while recalculating summary statistics", ex);
                return false;
            }

            return true;
        }
    }
}
