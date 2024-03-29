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
using System.Linq;
using System.Web;

namespace Colectica.Curation.Web.Models
{
    public class SiteSettings
    {
        public string SiteName { get; set; }

        public string StataPath { get; set; }

        public string StatTransferPath { get; set; }

        public string IngestDirectory { get; set; }

        public string ProcessingDirectory { get; set; }

        public string ArchiveDirectory { get; set; }

        public bool IsAnonymousOrganizationRegistrationAllowed { get; set; }

        public string SmtpHost { get; set; }

        public int SmtpPort { get; set; }

        public string SmtpUserName { get; set; }

        public string SmtpPassword { get; set; }

        public string PublishedFilesDirectory { get; set; }
        public string HandleTargetUrlBase { get; set; }
    }
}
