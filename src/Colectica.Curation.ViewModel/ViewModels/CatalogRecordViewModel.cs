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

﻿using Colectica.Curation.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Colectica.Curation.Common.ViewModels
{
    public class CatalogRecordViewModelBase : ICatalogRecordNavigator
    {
        public Guid CatalogRecordId { get; set; }

        public CatalogRecord CatalogRecord { get; set; }

        public bool IsLocked { get; set; }
        
        public string OperationStatus { get; set; }

        public List<string> AuthorIds { get; set; }

        public int FileCount { get; set; }

        public int TaskCount { get; set; }

        public List<NoteViewModel> Notes { get; set; }

        public bool IsUserCurator { get; set; }

        public bool IsUserApprover { get; set; }

        public bool CanAssignCurators { get; set; }

        public bool IsCatalogRecordCreated { get; set; }
        
        public bool IsReadOnly
        {
            get
            {
                if (!IsUserCurator &&
                    !IsUserApprover &&
                    CatalogRecord.Status != CatalogRecordStatus.New)
                {
                    return true;
                }

                return false;
            }
        }

        public CatalogRecordViewModelBase()
        {
            this.CatalogRecord = new Data.CatalogRecord();

            Notes = new List<NoteViewModel>();
            AuthorIds = new List<string>();
        }

        public CatalogRecordViewModelBase(CatalogRecord catalogRecord)
        {
            CatalogRecord = catalogRecord;
            CatalogRecordId = catalogRecord.Id;
            IsCatalogRecordCreated = true;

            if (catalogRecord.OperationLockId.HasValue)
            {
                IsLocked = true;
                OperationStatus = catalogRecord.OperationStatus;
            }

            Notes = new List<NoteViewModel>();
            AuthorIds = new List<string>();
        }

        public string CustomEditorName { get; set; }

        public string[] ResearchDesignChoices = new string[]
        {
            "Field experiment",
            "Natural experiment",
            "Lab experiment",
            "Survey experiment",
            "Regression discontinuity design",
            "Matching",
            "Observational",
            "Metaanalysis",
            "Other",
            "Multiple"
        };

        public string[] TreatmentAdministrationChoices = new string[]
        {
            "Mail",
            "Door to door",
            "Web delivered",
            "NGO-adminstered program",
            "Radio",
            "Television",
            "Mobile technology / Text messages",
            "Other"
        };


        public string[] UnitOfObservationChoices = new string[]
        {
            "Individual",
            "Organization",
            "Family",
            "Household: unit",
            "Household: family",
            "Housing unit",
            "Geo: village",
            "Geo: district",
            "Geo: school",
            "Geo: DMA",
            "Geo: region",
            "Geo: country",
            "Geo: Census track",
            "Event/process",
            "Other"
        };



    }

    public class CatalogRecordGeneralViewModel : CatalogRecordViewModelBase
    {
        [Required]
        public string Title { get; set; }
        public string StudyId { get; set; }
        public string Number { get; set; }
        public string Authors { get; set; }
        public string Description { get; set; }
        public string Organization { get; set; }
        public string Keywords { get; set; }
        public long Version { get; set; }
        public string LastUpdatedDate { get; set; }

        public string Owner { get; set; }
        public string OwnerContact { get; set; }

        [Display(Name = "Suggested Citation")]
        public string TermsOfUse { get; set; }
        public string PersistentId { get; set; }

        [Display(Name = "Funder / Sponsor")]
        public string Funding { get; set; }
        public string ArchiveDate { get; set; }
        public string PublishDate { get; set; }

        public string AccessStatement { get; set; }
        public bool IsRestrictedConfidential { get; set; }
        public bool IsRestrictedEmbargo { get; set; }
        public bool IsRestrictedOther { get; set; }
        public string ConfidentialityStatement { get; set; }
        public string EmbargoStatement { get; set; }
        public string OtherRestrictionStatement { get; set; }

        public string RelatedDatabase { get; set; }
        public string RelatedPublications { get; set; }
        public string RelatedProjects { get; set; }

        public string ReviewType { get; set; }

        #region Agreement

        public string DepositAgreement { get; set; }

        public string TermsOfService { get; set; }

        public string Policy { get; set; }
        public int CuratorCount { get; set; }

        #endregion

        public List<string> AvailableKeywords { get; } = new List<string>();

        public CatalogRecordGeneralViewModel()
            : base()
        {

        }

        public CatalogRecordGeneralViewModel(CatalogRecord catalogRecord)
            : base(catalogRecord)
        {
            Title = CatalogRecord.Title;
            Number = catalogRecord.Number;
            StudyId = CatalogRecord.StudyId;
            Description = CatalogRecord.Description;
            Organization = CatalogRecord.Organization.Name;
            Keywords = CatalogRecord.Keywords;
            PersistentId = CatalogRecord.PersistentId;
            Funding = CatalogRecord.Funding;
            ArchiveDate = CatalogRecord.ArchiveDate.HasValue ? CatalogRecord.ArchiveDate.Value.ToShortDateString() : string.Empty;
            PublishDate = CatalogRecord.PublishDate.HasValue ? CatalogRecord.PublishDate.Value.ToShortDateString() : string.Empty;
            Version = CatalogRecord.Version;
            LastUpdatedDate = CatalogRecord.LastUpdatedDate.HasValue ? CatalogRecord.LastUpdatedDate.Value.ToShortDateString() : string.Empty;

            DepositAgreement = CatalogRecord.DepositAgreement;
            AccessStatement = CatalogRecord.AccessStatement;
            IsRestrictedConfidential = CatalogRecord.IsRestrictedConfidential;
            IsRestrictedEmbargo = CatalogRecord.IsRestrictedEmbargo;
            IsRestrictedOther = CatalogRecord.IsRestrictedOther;
            ConfidentialityStatement = CatalogRecord.ConfidentialityStatement;
            EmbargoStatement = CatalogRecord.EmbargoStatement;
            OtherRestrictionStatement = CatalogRecord.OtherRestrictionStatement;

            RelatedDatabase = CatalogRecord.RelatedDatabase;
            RelatedPublications = CatalogRecord.RelatedPublications;
            RelatedProjects = CatalogRecord.RelatedProjects;

            ReviewType = catalogRecord.ReviewType;
            OwnerContact = catalogRecord.Organization.ContactInformation;
            TermsOfUse = catalogRecord.Organization.OrganizationPolicy;
        }

    }

    public class DateModel
    {
        [JsonIgnore]public string Id { get; set; }
        [JsonIgnore]public string Title { get; set; }

        public string dateType { get; set; }
        public string date { get; set; }
        public bool isRange { get; set; }
        public string endDate { get; set; }
    }

    public class CatalogRecordMethodsViewModel : CatalogRecordViewModelBase
    {
        public string ResearchDesign { get; set; }
        public string ModeOfDataCollection { get; set; }

        public DateModel FieldDates { get; set; }
        public DateModel StudyTimePeriod { get; set; }

        public string Location { get; set; }
        public string LocationDetails { get; set; }
        public string UnitOfObservation { get; set; }
        public string SampleSize { get; set; }
        public string InclusionExclusionCriteria { get; set; }
        public string RandomizationProcedure { get; set; }
        public string UnitOfRandomization { get; set; }
        public string Treatment { get; set; }
        public string TreatmentAdministration { get; set; }
        public string OutcomeMeasures { get; set; }

        [AllowHtml]
        public string DataType { get; set; }

        [AllowHtml]
        public string DataSource { get; set; }

        [AllowHtml]
        public string DataSourceInformation { get; set; }

        public string CatalogRecordDataType { get; set; }
        public string CatalogRecordDataSource { get; set; }
        public string CatalogRecordDataSourceInformation { get; set; }


        public string ResearchDesignOtherSpecify { get; set; }
        public string TreatmentAdministrationOtherSpecify { get; set; }
        public string UnitOfObservationOtherSpecify { get; set; }

        public List<string> AvailableOutcomeMeasures { get; } = new List<string>();

        public CatalogRecordMethodsViewModel()
            : base()
        {
        }

        public CatalogRecordMethodsViewModel(CatalogRecord catalogRecord)
            : base(catalogRecord)
        {
            // For ResearchDesign, TreatmentAdministration, and UnitOfObservation,
            // we have two properties: one if the value is from the dropdown choices,
            // and an other-specify if the value is not in the dropdown choices.
            if (ResearchDesignChoices.Contains(CatalogRecord.ResearchDesign))
            {
                ResearchDesign = CatalogRecord.ResearchDesign;
            }
            else
            {
                ResearchDesign = "Other";
                ResearchDesignOtherSpecify = CatalogRecord.ResearchDesign;
            }

            if (TreatmentAdministrationChoices.Contains(CatalogRecord.TreatmentAdministration))
            {
                TreatmentAdministration = CatalogRecord.TreatmentAdministration;
            }
            else
            {
                TreatmentAdministration = "Other";
                TreatmentAdministrationOtherSpecify = CatalogRecord.TreatmentAdministration;
            }

            if (UnitOfObservationChoices.Contains(CatalogRecord.UnitOfObservation))
            {
                UnitOfObservation = CatalogRecord.UnitOfObservation;
            }
            else
            {
                UnitOfObservation = "Other";
                UnitOfObservationOtherSpecify = CatalogRecord.UnitOfObservation;
            }

            ModeOfDataCollection = CatalogRecord.ModeOfDataCollection;

            try
            {
                if (!string.IsNullOrWhiteSpace(CatalogRecord.FieldDates))
                {
                    FieldDates = JsonConvert.DeserializeObject<DateModel>(CatalogRecord.FieldDates);

                }

                if (!string.IsNullOrWhiteSpace(CatalogRecord.StudyTimePeriod))
                {
                    StudyTimePeriod = JsonConvert.DeserializeObject<DateModel>(CatalogRecord.StudyTimePeriod);
                }
            }
            catch { }

            if (FieldDates == null)
            {
                FieldDates = new DateModel();
            }
            if (StudyTimePeriod == null)
            {
                StudyTimePeriod = new DateModel();
            }

            FieldDates.Id = "FieldDates";
            FieldDates.Title = "Field Dates";
            StudyTimePeriod.Id = "StudyTimePeriod";
            StudyTimePeriod.Title = "Study Time Period";

            Location = CatalogRecord.Location;
            LocationDetails = CatalogRecord.LocationDetails;
            SampleSize = CatalogRecord.SampleSize;
            InclusionExclusionCriteria = CatalogRecord.InclusionExclusionCriteria;
            RandomizationProcedure = CatalogRecord.RandomizationProcedure;
            UnitOfRandomization = CatalogRecord.UnitOfRandomization;
            Treatment = CatalogRecord.Treatment;
            OutcomeMeasures = CatalogRecord.OutcomeMeasures;

            GetConcatenatedDataProperties(catalogRecord, out string dataType, out string dataSource, out string dataSourceInformation);
            DataType = dataType;
            DataSource = dataSource;
            DataSourceInformation = dataSourceInformation;

            CatalogRecordDataType = CatalogRecord.DataType;
            CatalogRecordDataSource = CatalogRecord.DataSource;
            CatalogRecordDataSourceInformation = CatalogRecord.DataSourceInformation;

            FileCount = CatalogRecord.Files.Where(x => x.Status != FileStatus.Removed).Count();
        }

        public static void GetConcatenatedDataProperties(CatalogRecord catalogRecord, out string dataType, out string dataSource, out string dataSourceInformation)
        {
            // Set the data type and source properties based on the data files.
            string noFiles = "This catalog record does not contain any data files";
            var dataFiles = catalogRecord.Files.Where(x => x.Status != FileStatus.Removed).Where(x => x.Type == "Data");
            if (dataFiles.Count() == 0)
            {
                dataType = noFiles;
                dataSource = noFiles;
                dataSourceInformation = noFiles;
            }
            else
            {
                dataType = string.Join(Environment.NewLine, dataFiles.Select(x => x.KindOfData).Distinct());
                dataSource = string.Join(Environment.NewLine, dataFiles.Select(x => x.Source).Distinct());
                dataSourceInformation = string.Join(Environment.NewLine, dataFiles.Select(x => x.SourceInformation).Distinct());
            }
        }
    }

    public class CatalogRecordSubmitViewModel : CatalogRecordViewModelBase
    {
        public string DepositAgreement { get; set; }

        public string TermsOfService { get; set; }

        public string Policy { get; set; }


        public bool IsOkayToSubmit { get; set; }

        public List<RequiredInformationModel> Messages { get; set; }

        public CatalogRecordSubmitViewModel(CatalogRecord record)
            : base(record)
        {
            Messages = new List<RequiredInformationModel>();
        }

    }

    public class RequiredInformationModel
    {
        public string Link { get; set; }

        public string Text { get; set; }

        public RequiredInformationModel(string link, string text)
        {
            Link = link;
            Text = text;
        }
    }

    public interface ICatalogRecordNavigator
    {
        CatalogRecord CatalogRecord { get; }

        Guid CatalogRecordId { get; }

        bool IsUserCurator { get; }

        bool IsUserApprover { get; }

        bool CanAssignCurators { get; }

        int TaskCount { get; }
    }
}
