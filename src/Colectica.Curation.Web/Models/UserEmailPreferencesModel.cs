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
using System.Web;

namespace Colectica.Curation.Web.Models
{
    public class UserEmailPreferencesModel
    {
        public string Id { get; set; }

        [EmailAddress]
        public string Email { get; set; }

        public bool NotifyOnCatalogRecordCreated { get; set; }
        public bool NotifyOnCatalogRecordSubmittedForCuration { get; set; }
        public bool NotifyOnAssignedToCatalogRecord { get; set; }
        public bool NotifyOnCatalogRecordPublishRequested { get; set; }
        public bool NotifyOnCatalogRecordPublishedOrRejected { get; set; }
    }
}
