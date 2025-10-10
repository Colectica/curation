using Colectica.Curation.Common.ViewModels;
using Colectica.Curation.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace Colectica.Curation.Web.Utility
{
    public class CatalogRecordChangeDetector
    {
        public static string GetChangeSummary(CatalogRecord record, CatalogRecordGeneralViewModel model)
        {
            var builder = new StringBuilder();

            ManagedFileChangeDetector.CheckProperty("Title", record.Title, model.Title, builder);
            ManagedFileChangeDetector.CheckProperty("Study ID", record.StudyId, model.StudyId, builder);
            ManagedFileChangeDetector.CheckProperty("Number", record.Number, model.Number, builder);
            ManagedFileChangeDetector.CheckProperty("Authors", record.AuthorsText, model.Authors, builder);
            ManagedFileChangeDetector.CheckProperty("Description", record.Description, model.Description, builder);
            ManagedFileChangeDetector.CheckProperty("Keywords", record.Keywords, model.Keywords, builder);
            ManagedFileChangeDetector.CheckProperty("PersistentId", record.PersistentId, model.PersistentId, builder);
            ManagedFileChangeDetector.CheckProperty("Funding", record.Funding, model.Funding, builder);
            ManagedFileChangeDetector.CheckProperty("Access Statement", record.AccessStatement, model.AccessStatement, builder);
            ManagedFileChangeDetector.CheckProperty("Confidential", record.IsRestrictedConfidential.ToString(), model.IsRestrictedConfidential.ToString(), builder);
            ManagedFileChangeDetector.CheckProperty("Embargo", record.IsRestrictedEmbargo.ToString(), model.IsRestrictedEmbargo.ToString(), builder);
            ManagedFileChangeDetector.CheckProperty("Other Restriction", record.IsRestrictedOther.ToString(), model.IsRestrictedOther.ToString(), builder);
            ManagedFileChangeDetector.CheckProperty("Terms of Use", record.TermsOfUse, model.TermsOfUse, builder);
            ManagedFileChangeDetector.CheckProperty("Related Database", record.RelatedDatabase, model.RelatedDatabase, builder);
            ManagedFileChangeDetector.CheckProperty("Related Publications", record.RelatedPublications, model.RelatedPublications, builder);
            ManagedFileChangeDetector.CheckProperty("Related Projects", record.RelatedProjects, model.RelatedProjects, builder);
            ManagedFileChangeDetector.CheckProperty("Review Type", record.ReviewType, model.ReviewType, builder);

            return builder.ToString();
        }

        public static string GetChangeSummary(CatalogRecord record, CatalogRecordMethodsViewModel model)
        {
            var builder = new StringBuilder();

            ManagedFileChangeDetector.CheckProperty("Research Design", record.ResearchDesign, model.ResearchDesign, builder);
            ManagedFileChangeDetector.CheckProperty("Mode of Data Collection", record.ModeOfDataCollection, model.ModeOfDataCollection, builder);
            ManagedFileChangeDetector.CheckProperty("Field Dates", record.FieldDates, JsonConvert.SerializeObject(model.FieldDates), builder);
            ManagedFileChangeDetector.CheckProperty("Study Time Period", record.StudyTimePeriod, JsonConvert.SerializeObject(model.StudyTimePeriod), builder);
            ManagedFileChangeDetector.CheckProperty("Location", record.Location, model.Location, builder);
            ManagedFileChangeDetector.CheckProperty("Location Details", record.LocationDetails, model.LocationDetails, builder);
            ManagedFileChangeDetector.CheckProperty("Unit of Observation", record.UnitOfObservation, model.UnitOfObservation, builder);
            ManagedFileChangeDetector.CheckProperty("Sample Size", record.SampleSize, model.SampleSize, builder);
            ManagedFileChangeDetector.CheckProperty("Inclusion / Exclusion Criteria", record.InclusionExclusionCriteria, model.InclusionExclusionCriteria, builder);
            ManagedFileChangeDetector.CheckProperty("Randomization Procedure", record.RandomizationProcedure, model.RandomizationProcedure, builder);
            ManagedFileChangeDetector.CheckProperty("Unit of Randomization", record.UnitOfRandomization, model.UnitOfRandomization, builder);
            ManagedFileChangeDetector.CheckProperty("Treatment", record.Treatment, model.Treatment, builder);
            ManagedFileChangeDetector.CheckProperty("Treatment Administration", record.TreatmentAdministration, model.TreatmentAdministration, builder);
            ManagedFileChangeDetector.CheckProperty("Outcome Measures", record.OutcomeMeasures, model.OutcomeMeasures, builder);
            ManagedFileChangeDetector.CheckProperty("Data Type", record.DataType, model.CatalogRecordDataType, builder);
            ManagedFileChangeDetector.CheckProperty("Data Source", record.DataSource, model.CatalogRecordDataSource, builder);
            ManagedFileChangeDetector.CheckProperty("Data Source Information", record.DataSourceInformation, model.CatalogRecordDataSourceInformation, builder);

            return builder.ToString();
        }
    }
}