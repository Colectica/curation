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

﻿using Colectica.Curation.Common.ViewModels;
using Colectica.Curation.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Colectica.Curation.Web.Models
{
    public class DepositFilesViewModel : ICatalogRecordNavigator
    {
        public Guid CatalogRecordId { get; set; }
        public CatalogRecord CatalogRecord { get; set; }

        public List<string> Types { get; set; }
        public List<string> Titles { get; set; }
        public List<string> Sources { get; set; }
        public List<string> SourceInformations { get; set; }
        public List<string> Rights { get; set; }
        public List<string> PublicAccesses { get; set; }
        public List<string> SelectedExistingFileNames { get; set; }
        public List<string> CreationDates { get; set; }

        public string ActionType { get; set; }
        public string Notes { get; set; }


        public string DepositAgreement { get; set; }

        public string TermsOfService { get; set; }

        public string Policy { get; set; }

        public bool IsAgreementChecked { get; set; }

        public int TaskCount { get; set; }

        public List<string> AvailableExistingFileNames { get; protected set; }

        public DepositFilesViewModel()
        {

        }

        public DepositFilesViewModel(CatalogRecord catalogRecord)
        {
            CatalogRecord = catalogRecord;
            CatalogRecordId = catalogRecord.Id;

            if (catalogRecord.OperationLockId.HasValue)
            {
                IsLocked = true;
                OperationStatus = catalogRecord.OperationStatus;
            }

            Types = new List<string>();
            Titles = new List<string>();
            Sources = new List<string>();
            SourceInformations = new List<string>();
            Rights = new List<string>();
            PublicAccesses = new List<string>();
            SelectedExistingFileNames = new List<string>();
            CreationDates = new List<string>();

            AvailableExistingFileNames = new List<string>();
        }

        public bool IsLocked { get; set; }

        public string OperationStatus { get; set; }

        public bool IsUserCurator { get; set; }
        
        public bool CanAssignCurators { get; set; }

        public bool IsUserApprover { get; set; }
    }
}
