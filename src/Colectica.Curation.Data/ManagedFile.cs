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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Colectica.Curation.Data
{
    public class ManagedFile
    {
        [Key]
        public Guid Id { get; set; }

        public string Number { get; set; }
        
        public string Title { get; set; }

        [Required]
        public string Name { get; set; }

        public string PublicName { get; set; }

        public string PersistentLink { get; set; }

        public DateTime? PersistentLinkDate { get; set; }

        public long Version { get; set; }
        
        public string Type { get; set; }


        public string FormatName { get; set; }

        public string FormatId { get; set; }

        public long Size { get; set; }

        public DateTime? CreationDate { get; set; }

        public string KindOfData { get; set; }

        public string Source { get; set; }

        public string SourceInformation { get; set; }

        public string Rights { get; set; }

        public bool IsPublicAccess { get; set; }

        public DateTime? UploadedDate { get; set; }

        public string ExternalDatabase { get; set; }

        public string Software { get; set; }

        public string SoftwareVersion { get; set; }

        public string Hardware { get; set; }
        
        public string Checksum { get; set; }
        
        public string ChecksumMethod { get; set; }
        
        public DateTime? ChecksumDate { get; set; }

        public string VirusCheckOutcome { get; set; }
        public string VirusCheckMethod { get; set; }
        public DateTime? VirusCheckDate { get; set; }

        public DateTime? AcceptedDate { get; set; }
        public DateTime? CertifiedDate { get; set; }
        
        public bool HasPendingMetadataUpdates { get; set; }

        public virtual CatalogRecord CatalogRecord { get; set; }

        public FileStatus Status { get; set; }

        public virtual ApplicationUser Owner { get; set; }

        public virtual ICollection<Event> Events { get; set; }
    }

    public enum FileStatus
    {
        Pending = 0,
        Accepted = 1,
        Rejected = 2,
        Removed = 3
    }
}
