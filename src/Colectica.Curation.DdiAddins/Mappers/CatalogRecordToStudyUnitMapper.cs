// Copyright 2014 - 2018 Colectica.
// 
// This file is part of the Colectica Curation Tools.
// 
// The Colectica Curation Tools are free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your option)
// any later version.
// 
// The Colectica Curation Tools are distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
// FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for
// more details.
// 
// You should have received a copy of the GNU Affero General Public License along
// with Colectica Curation Tools. If not, see <https://www.gnu.org/licenses/>.

﻿using Algenta.Colectica.Model.Ddi;
using Colectica.Curation.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Colectica.Curation.Addins.Editors.Utility;
using Colectica.Curation.ViewModel.Utility;

namespace Colectica.Curation.Common.Mappers
{
    public class CatalogRecordToStudyUnitMapper
    {
        public void Map(CatalogRecord record, StudyUnit study)
        {
            if (record == null)
            {
                throw new ArgumentNullException("record");
            }

            if (study == null)
            {
                throw new ArgumentNullException("study");
            }

            if (record.LastUpdatedDate.HasValue)
            {
                study.VersionDate = record.LastUpdatedDate.Value;
            }

            study.SetUserAttribute("CreatedDate", record.CreatedDate);
            study.SetUserAttribute("ArchiveDate", record.ArchiveDate);
            study.SetUserAttribute("PublishDate", record.PublishDate);
            study.DublinCoreMetadata.Date = record.PublishDate;
            study.SetUserAttribute("CertifiedDate", record.CertifiedDate);

            study.SetUserAttribute("DepositAgreement", record.DepositAgreement);
            study.SetUserAttribute("AccessStatement", record.AccessStatement);
            study.SetUserAttribute("ConfidentialityStatement", record.ConfidentialityStatement);

            study.SetUserAttribute("RelatedDatabase", record.RelatedDatabase);
            study.SetUserAttribute("RelatedPublications", record.RelatedPublications);
            study.SetUserAttribute("RelatedProjects", record.RelatedProjects);

            study.SetUserAttribute("ReviewType", record.ReviewType);
            study.SetUserAttribute("OrganizationName", record.Organization?.Name);


            study.DublinCoreMetadata.Title.Current = record.Title;
            study.SetUserId("StudyId", record.StudyId);
            study.SetUserId("StudyNumber", record.Number?.ToString());

            study.DublinCoreMetadata.Creator.Current = record.AuthorsText;
            study.DublinCoreMetadata.Description.Current = record.Description;

            if (record.Keywords != null)
            {
                string[] keywords = record.Keywords.Split(new char[] { ',' });
                foreach (string kw in keywords)
                {
                    study.Coverage.TopicalCoverage.Keywords.Add(kw);
                }
            }

            if (record.CreatedBy != null)
            {
                study.DublinCoreMetadata.Contributor.Current = record.CreatedBy.FullName;
            }

            if (record.Owner != null)
            {
                study.DublinCoreMetadata.Publisher.Current = record.Owner.FullName;
            }

            study.SetUserId("Handle", record.PersistentId);

            var fundingInfo = new FundingInformation();
            fundingInfo.Description.Current = record.Funding;
            study.FundingSources.Add(fundingInfo);

            if (!string.IsNullOrWhiteSpace(record.EmbargoStatement))
            {
                var embargo = new Embargo();
                embargo.Description.Current = record.EmbargoStatement;
                study.Embargos.Add(embargo);
            }

            study.SetMethodology(record.ResearchDesign);
            study.SetModeOfDataCollection(record.ModeOfDataCollection);
            study.SetDataCollectionDate(record.FieldDates);

            var dateSpec = FormMappingHelper.GetDateFromJson(record.StudyTimePeriod);            
            if (dateSpec != null)
            {
                study.Coverage.TemporalCoverage.Dates.Add(dateSpec);
            }
            else
            {
                study.Coverage.TemporalCoverage.Dates.Clear();
            }

            study.Coverage.SpatialCoverage.HighestLevel = record.Location;
            study.Coverage.SpatialCoverage.Description.Current = record.LocationDetails;
            study.AnalysisUnit = record.UnitOfObservation;
            study.SetUserAttribute("SampleSize", record.SampleSize);
            study.SetSamplingProcedure(record.InclusionExclusionCriteria);
            study.SetUserAttribute("RandomizationProcedure", record.RandomizationProcedure);
            study.SetUserAttribute("UnitOfRandomization", record.UnitOfRandomization);
            study.SetUserAttribute("Treatment", record.Treatment);
            study.SetUserAttribute("TreatmentAdministration", record.TreatmentAdministration);
            study.SetUserAttribute("OutcomeMeasures", record.OutcomeMeasures);

        }
    }
}
