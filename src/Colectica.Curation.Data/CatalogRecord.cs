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

﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Colectica.Curation.Data
{
    public class CatalogRecord
    {
        [Key]
        public Guid Id { get; set; }

        #region General

        [Required]
        public string Title { get; set; }

        public string StudyId { get; set; }

        public string Number { get; set; }

        [InverseProperty("AuthorFor")]
        public virtual ICollection<ApplicationUser> Authors { get; set; }
        public string AuthorsText { get; set; }

        public string Description { get; set; }

        public virtual Organization Organization { get; set; }

        public string Keywords { get; set; }

        public virtual ApplicationUser CreatedBy { get; set; }

        public DateTime? CreatedDate { get; set; }

        public long Version { get; set; }

        public DateTime? LastUpdatedDate { get; set; }

        #endregion

        #region Citation

        public virtual ApplicationUser Owner { get; set; }
        public string OwnerText { get; set; }

        public string PersistentId { get; set; }

        public string Funding { get; set; }
        
        public DateTime? ArchiveDate { get; set; }

        public DateTime? PublishDate { get; set; }

        #endregion

        #region Access

        public string DepositAgreement { get; set; }
        public string AccessStatement { get; set; }
        public bool IsRestrictedConfidential { get; set; }
        public bool IsRestrictedEmbargo { get; set; }
        public bool IsRestrictedOther { get; set; }
        public string ConfidentialityStatement { get; set; }
        public string EmbargoStatement { get; set;}
        public string OtherRestrictionStatement { get; set; }

        #endregion

        #region Other

        public string TermsOfUse { get; set; }
        public string RelatedDatabase { get; set; }

        public string RelatedPublications { get; set; }

        public string RelatedProjects { get; set; }

        #endregion

        #region Methods

        public string ResearchDesign { get; set; }
        public string ModeOfDataCollection { get; set; }
        public string FieldDates { get; set; }
        public string StudyTimePeriod { get; set; }
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
        public string DataType { get; set; }
        public string DataSource { get; set; }
        public string DataSourceInformation { get; set; }

        #endregion

        #region Curation

        public string ReviewType { get; set; }

        public DateTime? CertifiedDate { get; set; }

        #endregion

        #region Non-content

        public Guid? OperationLockId { get; set; }

        public string OperationStatus { get; set; }

        [NotMapped]
        public bool IsLocked { get { return OperationLockId.HasValue; } }

        public virtual ICollection<ManagedFile> Files { get; set; }

        public virtual ICollection<TaskStatus> TaskStatuses { get; set; }

        public CatalogRecordStatus Status { get; set; }

        [InverseProperty("CuratorFor")]
        public virtual ICollection<ApplicationUser> Curators { get; set; }

        [InverseProperty("ApproverFor")]
        public virtual ICollection<ApplicationUser> Approvers { get; set; }

        #endregion

        #region Constructor

        public CatalogRecord()
        {
            Authors = new HashSet<ApplicationUser>();
            Curators = new HashSet<ApplicationUser>();
            Files = new HashSet<ManagedFile>();
        }

        #endregion
    }

    public enum CatalogRecordStatus
    {
        New = 0,
        Rejected = 5,
        Processing = 10,
        PublicationRequested = 20,
        PublicationApproved = 25,
        Published = 30
    }
}
