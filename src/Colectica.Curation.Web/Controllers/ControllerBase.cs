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
using Colectica.Curation.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using Colectica.Curation.Web.Utility;

namespace Colectica.Curation.Web.Controllers
{
    public class CurationControllerBase : Controller
    {
        [ChildActionOnly]
        public ActionResult UserSpecificNavigation()
        {
            using (var db = ApplicationDbContext.Create())
            {
                var thisUser = db.Users.Where(x => x.UserName == User.Identity.Name)
                    .Include(x => x.Organizations)
                    .FirstOrDefault();

                var org = OrganizationHelper.GetOrganizationByHost(Request, db);
                bool isOrgAdmin = false;

                if (org != null)
                {
                    isOrgAdmin = OrganizationHelper.DoesUserHaveRight(db, User, org.Id, Right.CanEditOrganization);
                }

                var model = new UserInfoModel()
                {
                    IsOrgAdmin = isOrgAdmin,
                    IsSiteAdministrator = thisUser.IsAdministrator,
                    Organization = org
                };

                return PartialView("_UserSpecificNavigation", model);
            }
        }
    }
}
