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
using Colectica.Curation.Data;
using Colectica.Curation.Web.Models;
using Colectica.Curation.Web.Utility;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Colectica.Curation.Web.Controllers
{
    [Authorize]
    public class UserController : CurationControllerBase
    {
        public ActionResult Index()
        {
            using (var db = ApplicationDbContext.Create())
            {
                var thisUser = db.Users.Where(x => x.UserName == User.Identity.Name)
                    .Include(x => x.Organizations)
                    .FirstOrDefault();

                var org = OrganizationHelper.GetOrganizationByHost(Request, db);
                List<ApplicationUser> users = null;

                if (org != null)
                {
                    bool isOrgAdmin = OrganizationHelper.DoesUserHaveRight(db, User, org.Id, Right.CanAssignRights);

                    // Site administrators can see all users of the current organization.
                    if (thisUser.IsAdministrator)
                    {
                        users = db.Organizations
                            .Where(x => x.Id == org.Id)
                            .Include(x => x.ApplicationUsers)
                            .Include("ApplicationUsers.Organizations")
                            .FirstOrDefault()
                            ?.ApplicationUsers
                            ?.ToList();
                    }
                    else if (isOrgAdmin)
                    {
                        // Organization administrators can see that organization's users.
                        users = db.Organizations
                            .Where(x => x.Id == org.Id)
                            .Include(x => x.ApplicationUsers)
                            .Include("ApplicationUsers.Organizations")
                            .FirstOrDefault()
                            ?.ApplicationUsers
                            ?.ToList();
                    }
                    else
                    {
                        // Regular users should just see their own page.
                        return RedirectToAction("Details", new { id = thisUser.UserName });
                    }
                }
                else
                {
                    // When not on an organization-specific site, show all users.
                    users = db.Users
                        .Include(x => x.Organizations)
                        .OrderBy(x => x.Email)
                        .ToList();
                }

                var model = new UsersViewModel(users);
                return View(model);
            }
        }

        public ActionResult Details(string id)
        {
            if (id == null)
            {
                throw new HttpException(400, "Bad Request");
            }

            using (var db = ApplicationDbContext.Create())
            {
                var user = db.Users
                    .Where(x => x.UserName == id)
                    .Include(x => x.Organizations)
                    .FirstOrDefault();
                if (user == null)
                {
                    throw new HttpException(404, "Not Found");
                }

                // Information about the requesting user.
                var thisUser = db.Users.Where(x => x.UserName == User.Identity.Name)
                    .Include(x => x.Organizations)
                    .FirstOrDefault();


                var model = new UserDetailsModel();
                model.User = user;

                // Get information about each organization the user belongs to.
                foreach (var o in user.Organizations)
                {
                    var orgModel = new UserInOrganizationModel();
                    orgModel.OrganizationId = o.Id;
                    orgModel.OrganizationName = o.Name;
                    model.Organizations.Add(orgModel);

                    // TODO better to do this in one query above.
                    var permissions = db.Permissions
                        .Where(x => x.User.Id == user.Id && x.Organization.Id == o.Id);

                    foreach (var permission in permissions)
                    {
                        switch (permission.Right)
                        {
                            case Right.CanAssignRights:
                                orgModel.CanAssignRights = true;
                                break;
                            case Right.CanViewAllCatalogRecords:
                                orgModel.CanViewAllCatalogRecords = true;
                                break;
                            case Right.CanAssignCurator:
                                orgModel.CanAssignCurators = true;
                                break;
                            default:
                                break;
                        }
                    }
                }

                // Get history information for the user.
                var events = db.Events
                    .Where(x => x.User.UserName == id)
                    .OrderByDescending(x => x.Timestamp)
                    .Include(x => x.RelatedCatalogRecord)
                    .Include(x => x.RelatedManagedFiles);

                foreach (var userEvent in events)
                {
                    var eventModel = HistoryEventModel.FromEvent(userEvent, user);
                    model.Events.Add(eventModel);
                }

                // Ideas for more events to add
                // TODO Show when this user was created?
                // TODO Show when this user creates other users?
                // TODO Show when records, files, and anything else is edited?

                // Can the requesting user edit the user?
                var org = OrganizationHelper.GetOrganizationByHost(Request, db);
                if (org == null)
                {
                    model.IsOrganizationAmbiguous = true;
                }

                bool isOrgAdmin = false;
                if (org != null)
                {
                    isOrgAdmin = OrganizationHelper.DoesUserHaveRight(db, User, org.Id, Right.CanAssignRights);
                }

                model.CanEditUser = thisUser.IsAdministrator ||
                    isOrgAdmin ||
                    thisUser.UserName == id;

                // Permissions
                model.CanEditPermissions = thisUser.IsAdministrator || isOrgAdmin;
                model.IsEditingUserSiteAdministrator = thisUser.IsAdministrator;

                if (org != null)
                {
                    model.OrganizationName = org.Name;
                }
                else
                {
                    model.OrganizationName = string.Join(", ", user.Organizations.Select(x => x.Name));
                }

                model.IsSiteAdministrator = user.IsAdministrator;

                if (org != null)
                {
                    var orgPermissions = user.Permissions.Where(x => x.Organization.Id == org.Id);
                    model.CanAssignRights = orgPermissions.Any(x => x.Right == Right.CanAssignRights);
                    model.CanViewAllCatalogRecords = orgPermissions.Any(x => x.Right == Right.CanViewAllCatalogRecords);
                    model.CanAssignCurator = orgPermissions.Any(x => x.Right == Right.CanAssignCurator);
                    model.CanEditOrganization = orgPermissions.Any(x => x.Right == Right.CanEditOrganization);
                    model.CanApprove = orgPermissions.Any(x => x.Right == Right.CanApprove);
                }

                // Map information from the user object to the view model.
                model.UserName = user.UserName;
                model.FirstName = user.FirstName;
                model.LastName = user.LastName;
                model.Affiliation = user.Affiliation;
                model.ContactInformation = user.ContactInformation;
                model.Orcid = user.Orcid;
                model.Email = user.Email;
                model.PhoneNumber = user.PhoneNumber;


                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(UserDetailsModel model)
        {
            using (var db = ApplicationDbContext.Create())
            {
                // Only allow Admins or the actual user to edit.
                var thisUser = db.Users.Where(x => x.UserName == User.Identity.Name)
                    .Include(x => x.Organizations)
                    .FirstOrDefault();

                var org = OrganizationHelper.GetOrganizationByHost(Request, db);

                bool isOrgAdmin = false;
                if (org != null)
                {
                    isOrgAdmin = OrganizationHelper.DoesUserHaveRight(db, User, org.Id, Right.CanAssignRights);
                }

                if (!thisUser.IsAdministrator &&
                    !isOrgAdmin &&
                    thisUser.UserName != model.UserName)
                {
                    throw new HttpException(403, "Only administrators can edit users");
                }

                if (!ModelState.IsValid)
                {
                    return RedirectToAction("Edit", new { id = thisUser.UserName });
                }

                var user = db.Users.Where(x => x.UserName == model.UserName).FirstOrDefault();
                if (user == null)
                {
                    throw new HttpException(404, "Not Found");
                }

                Mapper.Map(model, user);

                db.SaveChanges();

                return RedirectToAction("Index");
            }
        }

        public ActionResult EmailPreferences(string id)
        {
            using (var db = ApplicationDbContext.Create())
            {
                var model = new UserEmailPreferencesModel();
                var user = db.Users.Where(x => x.UserName == id).FirstOrDefault();
                if (user == null)
                {
                    throw new HttpException(404, "Not Found");
                }

                Mapper.Map(user, model);

                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EmailPreferences(UserEmailPreferencesModel model)
        {
            using (var db = ApplicationDbContext.Create())
            {
                var user = db.Users.Where(x => x.UserName == model.Id).FirstOrDefault();

                Mapper.Map(model, user);

                db.SaveChanges();

                return RedirectToAction("EmailPreferences", new { id = user.UserName });
            }
        }

        public ActionResult Create()
        {
            using (var db = ApplicationDbContext.Create())
            {
                var thisUser = db.Users.Single(x => x.UserName == User.Identity.Name);

                var org = OrganizationHelper.GetOrganizationByHost(Request, db);

                bool isOrgAdmin = false;

                if (org != null)
                {
                    isOrgAdmin = OrganizationHelper.DoesUserHaveRight(db, User, org.Id, Right.CanAssignRights);
                }

                if (thisUser == null ||
                    (!thisUser.IsAdministrator &&
                    !isOrgAdmin))
                {
                    throw new HttpException(403, "Only administrators can create new users.");
                }

                var model = new RegisterViewModel();

                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(RegisterViewModel model)
        {
            using (var db = ApplicationDbContext.Create())
            {
                var thisUser = db.Users.Single(x => x.UserName == User.Identity.Name);

                var org = OrganizationHelper.GetOrganizationByHost(Request, db);

                bool isOrgAdmin = false;

                if (org != null)
                {
                    isOrgAdmin = OrganizationHelper.DoesUserHaveRight(db, User, org.Id, Right.CanAssignRights);
                }

                if (thisUser == null ||
                    (!thisUser.IsAdministrator && !isOrgAdmin))
                {
                    throw new HttpException(403, "Forbidden");
                }

                if (ModelState.IsValid)
                {
                    // If they specified an existing organization for the user, add the user to that.
                    if (org == null)
                    {
                        org = db.Organizations.FirstOrDefault();
                    }

                    var userManager = HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();

                    var user = await CreateUser(model.Email, model.Password, model.FirstName, model.LastName,
                        false,
                        org,
                        userManager,
                        ModelState,
                        db);
                    if (user != null)
                    {
                        return RedirectToAction("Index");
                    }
                }

                return View(model);
            }
        }

        [HttpPost]
        public ActionResult Permissions(UserDetailsModel model)
        {
            using (var db = ApplicationDbContext.Create())
            {
                // Only allow Admins to edit.
                var thisUser = db.Users.Where(x => x.UserName == User.Identity.Name)
                    .Include(x => x.Organizations)
                    .FirstOrDefault();

                var org = OrganizationHelper.GetOrganizationByHost(Request, db);
                if (org == null)
                {
                    return RedirectToAction("Index");
                }

                bool isOrgAdmin = OrganizationHelper.DoesUserHaveRight(db, User, org.Id, Right.CanAssignRights);

                if (!thisUser.IsAdministrator &&
                    !isOrgAdmin)
                {
                    throw new HttpException(403, "Only administrators can edit users permissions.");
                }

                var user = db.Users.Where(x => x.UserName == model.UserName)
                    .Include(x => x.Permissions)
                    .FirstOrDefault();

                if (user == null)
                {
                    throw new HttpException(404, "NotFound");
                }

                // Add or remove site administrator status.
                if (thisUser.IsAdministrator)
                {
                    user.IsAdministrator = model.IsSiteAdministrator;
                }

                // Add or remove permissions.
                SetPermission(user, org, Right.CanAssignRights, model.CanAssignRights);
                SetPermission(user, org, Right.CanViewAllCatalogRecords, model.CanViewAllCatalogRecords);
                SetPermission(user, org, Right.CanAssignCurator, model.CanAssignCurator);
                SetPermission(user, org, Right.CanEditOrganization, model.CanEditOrganization);
                SetPermission(user, org, Right.CanApprove, model.CanApprove);

                db.SaveChanges();

                return RedirectToAction("Index");
            }
        }

        void SetPermission(ApplicationUser user, Organization org, Right right, bool shouldHavePermission)
        {
            var orgPermissions = user.Permissions.Where(x => x.Organization.Id == org.Id);

            bool alreadyHasPermission = orgPermissions.Any(x => x.Right == right);
            if (shouldHavePermission && !alreadyHasPermission)
            {
                var permission = new Permission()
                {
                    User = user,
                    Organization = org,
                    Right = right
                };
                user.Permissions.Add(permission);
            }
            else if (!shouldHavePermission && alreadyHasPermission)
            {
                var permission = orgPermissions.Where(x => x.Right == right).FirstOrDefault();
                if (permission != null)
                {
                    user.Permissions.Remove(permission);
                }
            }
        }

        [HttpPost]
        public ActionResult CreatePlaceholder(CreatePlaceholderUserModel model)
        {
            using (var db = ApplicationDbContext.Create())
            {
                if (!ModelState.IsValid)
                {
                    throw new HttpException(400, "Bad request");
                }

                var org = db.Organizations.Single(x => x.Name == model.OrganizationName);

                // TODO Ensure the authenticated user belongs to the organization and 
                // has authorization to create new users.

                // Create the user.
                var user = new ApplicationUser();
                Mapper.Map(model, user);
                user.IsPlaceholder = true;
                user.Id = Guid.NewGuid().ToString();
                user.Organizations.Add(org);

                // Make sure this email is not already used.
                var existingUser = db.Users.Where(x => x.Email == user.Email)
                    .FirstOrDefault();
                if (existingUser != null)
                {
                    throw new HttpException(409, "Email address is already used");
                }

                //if (string.IsNullOrWhiteSpace(user.Email))
                //{
                //    user.Email = "placeholder-" + Guid.NewGuid().ToString() + "@example.com";
                //}
                if (string.IsNullOrWhiteSpace(user.UserName))
                {
                    user.UserName = "placeholder-" + Guid.NewGuid().ToString();
                }

                db.Users.Add(user);
                db.SaveChanges();

                var resultModel = new UserSearchResultModel();
                Mapper.Map(user, resultModel);
                return Json(resultModel);
            }
        }

        [Route("User/Search/{query}")]
        public ActionResult Search(string query)
        {
            using (var db = ApplicationDbContext.Create())
            {
                var users = db.Users
                    .Where(x =>
                        (x.UserName.Contains(query) ||
                        x.Email.Contains(query) ||
                        x.FirstName.Contains(query) ||
                        x.LastName.Contains(query) ||
                        x.Affiliation.Contains(query) ||
                        x.Orcid.Contains(query)))
                    .Select(x =>
                        new UserSearchResultModel
                        {
                            Id = x.Id,
                            FirstName = x.FirstName,
                            LastName = x.LastName,
                            FullName = x.FirstName + " " + x.LastName,
                            Affiliation = x.Affiliation,
                            Email = x.Email,
                            Orcid = x.Orcid,
                            IsVerified = !x.IsPlaceholder
                        })
                    .Take(10)
                    .ToList();

                return Json(users, JsonRequestBehavior.AllowGet);
            }
        }

        public static async Task<ApplicationUser> CreateUser(string email, string password, string firstName, string lastName,
            bool isAdministrator,
            Organization organization,
            ApplicationUserManager userManager,
            ModelStateDictionary modelState,
            ApplicationDbContext db)
        {

            var user = new ApplicationUser()
            {
                UserName = email,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                IsAdministrator = isAdministrator
            };

            userManager.PasswordValidator = new Microsoft.AspNet.Identity.PasswordValidator
            {
                RequiredLength = 6,
                RequireNonLetterOrDigit = false,
                RequireDigit = false,
                RequireLowercase = false,
                RequireUppercase = false
            };

            var result = await userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                db.Users.Attach(user);
                user.Organizations.Add(organization);


                // Log the creation of the user.
                var log = new Event()
                {
                    EventType = EventTypes.CreateUser,
                    Timestamp = DateTime.UtcNow,
                    User = user,
                    Title = user.UserName + " created",
                    Details = string.Empty
                };
                db.Events.Add(log);

                db.SaveChanges();

                return user;
            }
            else
            {
                return null;
            }

        }

    }
}
