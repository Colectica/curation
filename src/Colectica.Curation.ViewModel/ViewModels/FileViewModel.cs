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

﻿using AutoMapper;
using Colectica.Curation.Common.ViewModels;
using Colectica.Curation.Contracts;
using Colectica.Curation.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Colectica.Curation.Web.Models
{
    public class FileViewModel : IUserRights, IFileModel
    {
        public ManagedFile File { get; set; }

        public string CatalogRecordName { get; set; }

        public string Name { get; set; }
        
        public string PublicName { get; set; }

        public string Title { get; set; }

        public string Number { get; set; }

        public string PersistentLink { get; set; }

        public DateTime? PersistentLinkDate { get; set; }

        public string Version { get; set; }

        public string Type { get; set; }
        
        public string FormatName { get; set; }

        public string FormatId { get; set; }

        public long Size { get; set; }

        public DateTime? CreationDate { get; set; }

        public string KindOfData { get; set; }

        public string Source { get; set; }

        public string SourceInformation { get; set; }

        public string Rights { get; set; }

        public string IsPublicAccess { get; set; }

        public string OwnerContact { get; set; }

        public string TermsOfUse { get; set; }

        public string ExternalDatabase { get; set; }

        public string Software { get; set; }

        public string SoftwareVersion { get; set; }

        public string Hardware { get; set; }

        public string Checksum { get; set; }

        public string ChecksumMethod { get; set; }

        public DateTime? ChecksumDate { get; set; }

        public string Owner { get; set; }
       
        public string Contributor { get; set; }

        public List<NoteViewModel> Notes { get; set; }
        
        public FileStatusModel TaskStatus { get; set; }

        public bool IsLocked { get; set; }

        public bool IsReadOnly
        {
            get
            {
                if (this.File == null)
                {
                    return false;
                }

                if (!IsUserCurator)
                {
                    // If it is new, the depositor can still edit it.
                    if (this.File.CatalogRecord.Status != CatalogRecordStatus.New)
                    {
                        return true;
                    }
                }

                return false;
            }
        }


        public bool IsUserCurator { get; set; }
        public bool IsUserApprover { get; set; }
        public bool IsUserAdmin { get; set; }

        public string OperationStatus { get; set; }

        public bool HasAllDataTasks { get; set; }
        public bool HasAllCodeTasks { get; set; }

        public FileViewModel()
        {
            Notes = new List<NoteViewModel>();
        }

        public FileViewModel(ManagedFile file)
        {
            Notes = new List<NoteViewModel>();

            File = file;

            Id = file.Id;
            Name = file.Name;
            Type = file.Type;
            Status = file.Status;
            Number = file.Number;

            IsPublicAccess = file.IsPublicAccess ? "Yes" : "No";

            CatalogRecordName = file.CatalogRecord.Title;

            if (file.CatalogRecord.IsLocked)
            {
                this.IsLocked = true;
                this.OperationStatus = file.CatalogRecord.OperationStatus;
            }
        }

        public Guid Id { get; set; }

        public Data.FileStatus Status { get; set; }

        public bool IsFileTypeAutoDetected { get; set; }

    }
}
