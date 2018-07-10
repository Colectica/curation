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
    [Authorize]
    public class DashboardController : CurationControllerBase
    {
        public ActionResult Index()
        {
            using (var db = ApplicationDbContext.Create())
            {
                var model = new DashboardViewModel();

                var thisUser = db.Users.Where(x => x.UserName == User.Identity.Name)
                    .Include(x => x.Organizations)
                    .FirstOrDefault();

                var org = OrganizationHelper.GetOrganizationByHost(Request, db);
                if (org == null)
                {
                    model.IsOrganizationAmbiguous = true;
                    model.Organizations = db.Organizations.ToList();
                }
                else
                {
                    model.IsAdmin = OrganizationHelper.DoesUserHaveRight(db, User, org.Id, Right.CanViewAllCatalogRecords);

                    // Restrict queries to files and users to which the current user shared an organization.
                    // Maybe it would be nicer to do per-organization dashboards, or to allow filtering
                    // of this information dynamically.
                    IEnumerable<CatalogRecord> userRecords = null;
                    IEnumerable<ManagedFile> userFiles = null;
                    IEnumerable<ApplicationUser> userUsers = null;


                    if (model.IsAdmin)
                    {
                        userRecords = db.CatalogRecords.Where(x => x.Organization.Id == org.Id);

                        userFiles = db.Files.Where(x => x.CatalogRecord.Organization.Id == org.Id);
                        userUsers = db.Users
                            .Where(u => u.Organizations.Any(o => o.Id == org.Id));
                    }
                    else
                    {
                        userRecords = db.CatalogRecords.Where(x => x.Organization.Id == org.Id &&
                            x.CreatedBy.UserName == thisUser.UserName ||
                            x.Owner.UserName == thisUser.UserName ||
                            x.Curators.Any(u => u.UserName == thisUser.UserName) ||
                            x.Authors.Any(u => u.UserName == thisUser.UserName));

                        userFiles = db.Files.Where(x => x.CatalogRecord.Organization.Id == org.Id &&
                           x.CatalogRecord.CreatedBy.UserName == thisUser.UserName ||
                           x.CatalogRecord.Owner.UserName == thisUser.UserName ||
                           x.CatalogRecord.Curators.Any(u => u.UserName == thisUser.UserName) ||
                           x.CatalogRecord.Authors.Any(u => u.UserName == thisUser.UserName));
                    }


                    // CatalogRecords
                    model.NewCatalogRecordCount = userRecords
                        .Where(x => x.Status == CatalogRecordStatus.New)
                        .Count();

                    model.ProcessingCatalogRecordCount = userRecords
                        .Where(x => x.Status == CatalogRecordStatus.Processing ||
                            x.Status == CatalogRecordStatus.PublicationRequested ||
                            x.Status == CatalogRecordStatus.PublicationApproved)
                        .Count();

                    model.PublishedCatalogRecordCount = userRecords
                        .Where(x => x.Status == CatalogRecordStatus.Published)
                        .Count();

                    // Files
                    model.FileCount = userFiles.Where(x => x.Status == FileStatus.Accepted).Count();
                    model.FileSize = userFiles.Where(x => x.Status == FileStatus.Accepted).Select(x => x.Size).DefaultIfEmpty(0).Sum();

                    // Users
                    if (model.IsAdmin)
                    {
                        model.UserCount = userUsers.Count();
                    }

                }

                return View(model);
            }
        }
    }
}
