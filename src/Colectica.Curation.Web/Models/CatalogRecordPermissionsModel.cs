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
using System.Text;

namespace Colectica.Curation.Web.Models
{
    public class CatalogRecordPermissionsModel : ICatalogRecordNavigator
    {
        public CatalogRecord CatalogRecord { get; set; }

        public List<CatalogRecordPermissionsUserModel> Users { get; set; } = new List<CatalogRecordPermissionsUserModel>();

        public Guid CatalogRecordId
        {
            get { return CatalogRecord.Id; }
        }

        public bool IsUserCurator { get; set; }

        public bool IsUserApprover { get; set; }

        public bool CanAssignCurators { get; set; }

        public int TaskCount { get; set; }

    }

    public class SetCatalogRecordPermissionsModel
    {
        public Guid CatalogRecordId { get; set; }

        public List<CatalogRecordPermissionsUserModel> Users { get; set; } = new List<CatalogRecordPermissionsUserModel>();

    }

    public class CatalogRecordPermissionsUserModel
    {
        public string UserName { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public bool IsCurator { get; set;  } 
        public bool IsApprover { get; set; }
    }
}
