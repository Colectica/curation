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
using System.Security.Principal;
using System.Web;
using System.Data.Entity;

namespace Colectica.Curation.Web.Utility
{
    public class OrganizationHelper
    {
        public static Organization GetOrganizationByHost(HttpRequestBase request, ApplicationDbContext db)
        {
            string host = request.Headers["Host"];
            var org = db.Organizations.Where(x => string.Compare(host, x.Hostname, true) == 0)
                .FirstOrDefault();

            return org;
        }

        public static List<Organization> GetAvailableOrganizationsForUser(ApplicationDbContext db, IPrincipal user)
        {
            // TODO Non super-admins should only see their own organizations.
            var thisUser = db.Users.Where(x => x.UserName == user.Identity.Name)
                .Include(x => x.Organizations)
                .FirstOrDefault();

            List<Organization> orgs;
            if (thisUser.IsAdministrator)
            {
                orgs = db.Organizations.OrderBy(x => x.Name).ToList();
            }
            else
            {
                orgs = thisUser.Organizations.OrderBy(x => x.Name).ToList();

            }
            
            return orgs;
        }

        public static bool DoesUserHaveRight(ApplicationDbContext db, IPrincipal user, Guid organizationId, Right right)
        {
            if (!user.Identity.IsAuthenticated)
            {
                return false;
            }

            var thisUser = db.Users.Single(x => x.UserName == user.Identity.Name);
            if (thisUser != null && 
                thisUser.IsAdministrator)
            {
                return true;
            }

            bool hasPermission = db.Permissions
                .Any(x => x.User.UserName == user.Identity.Name &&
                    x.Organization.Id == organizationId &&
                    x.Right == right);

            return hasPermission;
        }

        public static IEnumerable<ApplicationUser> GetUsersWithRight(ApplicationDbContext db, Guid organizationId, Right right)
        {
            return db.Permissions
                .Where(x => x.Organization.Id == organizationId &&
                       x.Right == right)
                .Where(x => x.User != null)
                .Select(x => x.User);
        }

    }
}
