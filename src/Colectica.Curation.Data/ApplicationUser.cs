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

﻿using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Colectica.Curation.Data
{
    // You can add profile data for the user by adding more properties to your ApplicationUser class, please visit http://go.microsoft.com/fwlink/?LinkID=317594 to learn more.
    public class ApplicationUser : IdentityUser
    {
        public virtual ICollection<Organization> Organizations { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        [NotMapped]
        public string FullName
        {
            get
            {
                return FirstName + " " + LastName;
            }
        }

        public string Affiliation { get; set; }

        public string ContactInformation { get; set; }

        public string Orcid { get; set; }

        public bool IsOrcidConfirmed { get; set; }

        public bool IsPlaceholder { get; set; }

        public bool IsAdministrator { get; set; }

        public bool NotifyOnCatalogRecordCreated { get; set; }
        public bool NotifyOnCatalogRecordSubmittedForCuration { get; set; }
        public bool NotifyOnAssignedToCatalogRecord { get; set; }
        public bool NotifyOnCatalogRecordPublishRequested { get; set; }
        public bool NotifyOnCatalogRecordPublishedOrRejected { get; set; }

        public virtual ICollection<CatalogRecord> AuthorFor { get; set; }

        public virtual ICollection<CatalogRecord> CuratorFor { get; set; }

        public virtual ICollection<CatalogRecord> ApproverFor { get; set; }

        public virtual ICollection<Permission> Permissions { get; set; }


        public ApplicationUser()
        {
            Organizations = new HashSet<Organization>();
            AuthorFor = new HashSet<CatalogRecord>();
            CuratorFor = new HashSet<CatalogRecord>();

            NotifyOnCatalogRecordCreated = true;
            NotifyOnCatalogRecordSubmittedForCuration = true;
            NotifyOnAssignedToCatalogRecord = true;
            NotifyOnCatalogRecordPublishRequested = true;
            NotifyOnCatalogRecordPublishedOrRejected = true;
        }

        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager)
        {
            // Note the authenticationType must match the one defined in CookieAuthenticationOptions.AuthenticationType
            var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);
            // Add custom user claims here
            return userIdentity;
        }
    }
}
