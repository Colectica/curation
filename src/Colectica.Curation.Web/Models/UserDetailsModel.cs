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
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Colectica.Curation.Web.Models
{
    public class UserDetailsModel
    {
        public string Id { get; set; }

        public ApplicationUser User { get; set; }

        public List<UserInOrganizationModel> Organizations { get; protected set; }

        public List<HistoryEventModel> Events { get; protected set; }

        #region Edit

        [Display(Name = "User name")]
        public string UserName { get; set; }

        [EmailAddress]
        public string Email { get; set; }

        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        public string Affiliation { get; set; }

        [Display(Name = "Contact Information")]
        public string ContactInformation { get; set; }

        [Display(Name = "ORCID")]
        public string Orcid { get; set; }

        #endregion

        #region Permissions

        public bool IsEditingUserSiteAdministrator { get; set; }

        public string OrganizationName { get; set; }

        public bool IsSiteAdministrator { get; set; }

        public bool CanAssignRights { get; set; }

        public bool CanViewAllCatalogRecords { get; set; }

        public bool CanAssignCurator { get; set; }

        public bool CanEditOrganization { get; set; }

        public bool CanApprove { get; set; }
        public bool CanEditUser { get; set; }
        public bool CanEditPermissions { get; set; }
        public bool IsOrganizationAmbiguous { get; internal set; }

        #endregion

        public UserDetailsModel()
        {
            Organizations = new List<UserInOrganizationModel>();
            Events = new List<HistoryEventModel>();
        }
    }

    public class UserInOrganizationModel
    {
        public Guid OrganizationId { get; set; }
        public string OrganizationName { get; set; }

        public bool CanAssignRights { get; set; }
        public bool CanViewAllCatalogRecords { get; set; }
        public bool CanAssignCurators { get; set; }

        public bool HasAnySpecialRights
        {
            get
            {
                return CanAssignRights ||
                    CanViewAllCatalogRecords ||
                    CanAssignCurators;
            }
        }

    }
}
