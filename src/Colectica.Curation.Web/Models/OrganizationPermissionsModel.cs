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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Colectica.Curation.Web.Models
{
    public class OrganizationPermissionsModel
    {
        public Organization Organization { get; set; }
        
        public string AllOrganizationUsersJson { get; set; }

        public string UsersWithCanAssignRightsJson { get; set; }
        public string UsersWithCanViewAllCatalogRecordsJson { get; set; }
        public string UsersWithCanAssignCuratorsJson { get; set; }
        public string UsersWithCanEditOrganizationJson { get; set; }
        public string UsersWithCanApproveJson { get; set; }

    }
}
