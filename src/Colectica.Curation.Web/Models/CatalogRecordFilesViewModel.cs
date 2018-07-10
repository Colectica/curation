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
using Colectica.Curation.Contracts;
using Colectica.Curation.Data;
using Colectica.Curation.Operations;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Web;

namespace Colectica.Curation.Web.Models
{
    public class CatalogRecordFilesViewModel : ICatalogRecordNavigator
    {
        public CatalogRecord CatalogRecord { get; set; }

        public List<FileViewModel> Files { get; set; }

        [ImportMany]
        public List<IScriptedTask> AllTasks { get; set; }

        public CatalogRecordFilesViewModel(CatalogRecord catalogRecord)
        {
            this.AllTasks = new List<IScriptedTask>();
            foreach (var task in MefConfig.AddinManager.AllTasks)
            {
                AllTasks.Add(task);
            }

            this.CatalogRecord = catalogRecord;

            if (catalogRecord.OperationLockId.HasValue)
            {
                IsLocked = true;
                OperationStatus = catalogRecord.OperationStatus;
            }

            Files = new List<FileViewModel>();
        }

        public bool IsLocked { get; set; }

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


        public string OperationStatus { get; set; }

        public Guid CatalogRecordId
        {
            get
            {
                return CatalogRecord.Id;
            }
        }

        public bool IsUserCurator { get; set; }

        public bool CanAssignCurators { get; set; }

        public bool IsUserApprover { get; set; }

        public int TaskCount { get; set; }
    }
}
