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

﻿using AutoMapper;
using Colectica.Curation.Common.Utility;
using Colectica.Curation.Common.ViewModels;
using Colectica.Curation.Data;
using Colectica.Curation.Web.Models;
using Colectica.Curation.Web.Utility;
using Microsoft.AspNet.Identity.Owin;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Colectica.Curation.Web.Controllers
{
    [Authorize]
    public class OrganizationController : CurationControllerBase
    {
        public ActionResult Index()
        {
            using (var db = ApplicationDbContext.Create())
            {
                if (!User.Identity.IsAuthenticated)
                {
                    throw new HttpException(403, "Forbidden");
                }

                var thisUser = db.Users.Where(x => x.UserName == User.Identity.Name)
                    .Include(x => x.Organizations)
                    .FirstOrDefault();


                List<Organization> orgs = OrganizationHelper.GetAvailableOrganizationsForUser(db, User);

                var model = new OrganizationIndexModel();
                model.IsSiteAdministrator = thisUser.IsAdministrator;

                foreach (var org in orgs)
                {
                    var orgModel = new OrganizationInListModel()
                    {
                        Organization = org,
                        CanUserAssignRights = OrganizationHelper.DoesUserHaveRight(db, User, org.Id, Right.CanAssignRights)
                    };

                    model.Organizations.Add(orgModel);
                }

                return View(model);
            }
        }

        public ActionResult Details(Guid id)
        {
            if (id == Guid.Empty)
            {
                throw new HttpException(400, "Bad Request");
            }

            using (var db = ApplicationDbContext.Create())
            {
                var org = db.Organizations
                    .Where(x => x.Id == id)
                    .Include(x => x.ApplicationUsers)
                    .FirstOrDefault();
                if (org == null)
                {
                    throw new HttpException(404, "Not Found");
                }

                var thisUser = db.Users.Where(x => x.UserName == User.Identity.Name)
                    .Include(x => x.Organizations)
                    .FirstOrDefault();

                var model = new OrganizationDetailsModel();
                model.Organization = org;
                model.IsSiteAdministrator = thisUser.IsAdministrator;

                var existingUsers = org.ApplicationUsers.ToList();
                var addableUsers = db.Users
                    .Where(x => !x.IsPlaceholder)
                    .OrderBy(x => x.Email)
                    .ToList()
                    .Except(existingUsers)
                    .ToList();
                foreach (var user in addableUsers)
                {
                    var sel = new SelectListItem();
                    sel.Text = user.FullName + " - " + user.Email;
                    sel.Value = user.Id;
                    model.AddableUsers.Add(sel);
                }

                return View(model);
            }
        }

        [HttpPost, ValidateInput(false)]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Organization organization)
        {
            if (!ModelState.IsValid)
            {
                return View(organization);
            }

            using (var db = ApplicationDbContext.Create())
            {
                if (!OrganizationHelper.DoesUserHaveRight(db, User, organization.Id, Right.CanEditOrganization))
                {
                    throw new HttpException(403, "Forbidden");
                }

                db.Entry(organization).State = EntityState.Modified;
                db.SaveChanges();

                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddUser(Guid orgId, Guid userId)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction("Details", new { id = orgId });
            }

            using (var db = ApplicationDbContext.Create())
            {
                if (!OrganizationHelper.DoesUserHaveRight(db, User, orgId, Right.CanEditOrganization))
                {
                    throw new HttpException(403, "Forbidden");
                }

                var organization = db.Organizations.Find(orgId);
                var userToAdd = db.Users.FirstOrDefault(x => x.Id == userId.ToString());

                if (organization != null &&
                    userToAdd != null)
                {
                    organization.ApplicationUsers.Add(userToAdd);
                    db.SaveChanges();
                }

                return RedirectToAction("Details", new { id = orgId });
            }
        }

        [AllowAnonymous]
        public ActionResult Create()
        {
            using (var db = ApplicationDbContext.Create())
            {
                EnsureOrganizationCreationIsAllowed(db);

                var model = new CreateOrganizationViewModel();
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public ActionResult Create(CreateOrganizationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            using (var db = ApplicationDbContext.Create())
            {
                EnsureOrganizationCreationIsAllowed(db);

                if (model.HostName.StartsWith("http://"))
                {
                    model.HostName = model.HostName.Substring("http://".Length);
                }
                if (model.HostName.StartsWith("https://"))
                {
                    model.HostName = model.HostName.Substring("https://".Length);
                }


                // Create the organization.
                var org = new Organization()
                {
                    Id = Guid.NewGuid(),
                    Name = model.OrganizationName,
                    Hostname = model.HostName,
                    AgencyID = "int.example"
                };

                db.Organizations.Add(org);


                // Create the user.
                var thisUser = db.Users.Where(x => x.UserName == User.Identity.Name)
                    .FirstOrDefault();
                thisUser.Organizations.Add(org);

                // Make the creating user an administrator, with all rights.
                db.Permissions.Add(CreatePermission(thisUser, org, Right.CanAssignRights));
                db.Permissions.Add(CreatePermission(thisUser, org, Right.CanAssignCurator));
                db.Permissions.Add(CreatePermission(thisUser, org, Right.CanViewAllCatalogRecords));
                db.Permissions.Add(CreatePermission(thisUser, org, Right.CanEditOrganization));
                db.Permissions.Add(CreatePermission(thisUser, org, Right.CanApprove));

                db.SaveChanges();

                return RedirectToAction("Details", new { id = org.Id });
            }
        }

        Permission CreatePermission(ApplicationUser user, Organization org, Right right)
        {
            return new Permission
            {
                User = user,
                Organization = org,
                Right = right
            };
        }

        Organization GetOrganization(Guid id, ApplicationDbContext db)
        {
            if (id == Guid.Empty)
            {
                throw new HttpException(400, "Bad Request");
            }

            var org = db.Organizations
                .Where(x => x.Id == id)
                .FirstOrDefault();
            if (org == null)
            {
                throw new HttpException(404, "Not Found");
            }

            return org;
        }

        Organization GetOrganizationWithusers(Guid id, ApplicationDbContext db)
        {
            if (id == Guid.Empty)
            {
                throw new HttpException(400, "Bad Request");
            }

            var org = db.Organizations
                .Where(x => x.Id == id)
                .Include(x => x.ApplicationUsers)
                .FirstOrDefault();
            if (org == null)
            {
                throw new HttpException(404, "Not Found");
            }

            return org;
        }

        void EnsureOrganizationCreationIsAllowed(ApplicationDbContext db)
        {
            // If the user is not authenticated and the site settings
            // do not allow new Organization creation, don't allow
            // access.
            var settings = SettingsHelper.GetSiteSettings(db);
            if (!settings.IsAnonymousOrganizationRegistrationAllowed)
            {
                if (!User.Identity.IsAuthenticated)
                {
                    throw new HttpException(403, "Forbidden");
                }

                // If the user is authenticated, ensure they are an admin.
                var thisUser = db.Users.Where(x => x.UserName == User.Identity.Name)
                    .FirstOrDefault();
                if (thisUser == null ||
                    !thisUser.IsAdministrator)
                {
                    throw new HttpException(403, "Forbidden");
                }
            }
        }

        void EnsureUserCanAssignRights(ApplicationDbContext db, Guid organizationId)
        {
            if (!OrganizationHelper.DoesUserHaveRight(db, User, organizationId, Right.CanAssignRights))
            {
                throw new HttpException(403, "Forbidden");
            }
        }
    }
}
