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
    public class Organization
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string Name { get; set; }
        
        [Url]
        public string ImageUrl { get; set; }
        
        public string Hostname { get; set; }

        public string AgencyID { get; set; }

        public string IngestDirectory { get; set; }

        public string ProcessingDirectory { get; set; }

        public string ArchiveDirectory { get; set; }

        public string ContactInformation { get; set; }

        public string LoginPageText { get; set; }

        public string DepositAgreement { get; set; }

        public string TermsOfService { get; set; }

        public string OrganizationPolicy { get; set; }

        public string ReplyToAddress { get; set; }

        public string NotificationEmailClosing { get; set; }

        public bool IsAnonymousRegistrationAllowed { get; set; }

        public string IPAddressesAllowedToDownloadFiles { get; set; }

        public virtual ICollection<ApplicationUser> ApplicationUsers { get; set; }

        public string HandleServerEndpoint { get; set; }
        public string HandleGroupName { get; set; }
        public string HandleUserName { get; set; }
        public string HandlePassword { get; set; }

        public Organization()
        {
            ApplicationUsers = new List<ApplicationUser>();
        }
    }
}
