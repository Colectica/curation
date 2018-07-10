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
using Newtonsoft.Json;
using Microsoft.AspNet.Identity.Owin;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;
using System.Threading.Tasks;
using Colectica.Curation.Web.Utility;

namespace Colectica.Curation.Web.Controllers
{
    public class SetupController : CurationControllerBase
    {
        public static string IsConfiguredMarkerPath = "~/App_Data/isConfigured.txt";

        public ActionResult FirstRun()
        {
            var model = new FirstRunViewModel();

            var checker = new SystemStatusChecker();
            model.SystemStatus = checker.GetSystemStatus();

            // See if there are no connection strings.
            if (WebConfigurationManager.ConnectionStrings.Count == 0)
            {
                model.HasNoConnectionStrings = true;
            }

            // See if the DefaultConnection is empty.
            var defaultConnectionString = WebConfigurationManager.ConnectionStrings["DefaultConnection"];
            if (defaultConnectionString == null)
            {
                model.HasNoConnectionStrings = true;
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> FirstRun(FirstRunViewModel model)
        {
            // Don't allow this to be run if it has already been run successfully.
            string isConfiguredPath = HttpContext.Server.MapPath(SetupController.IsConfiguredMarkerPath);
            if (System.IO.File.Exists(isConfiguredPath))
            {
                throw new HttpException(404, "Not found");
            }

            var checker = new SystemStatusChecker();

            using (var db = ApplicationDbContext.Create())
            {
                try
                {
                    // Create the database if needed.
                    if (!db.Database.Exists())
                    {
                        db.Database.Create();
                    }
                }
                catch (Exception ex)
                {
                    model.Message = ex.Message;
                    model.SystemStatus = checker.GetSystemStatus();
                    return View(model);
                }

                try
                {
                    // Set the site-wide settings.
                    // Boo lookup instead of add-or-update.
                    var settingsRow = db.Settings.Where(x => x.Name == AdminController.SiteSettingsName).FirstOrDefault();
                    if (settingsRow == null)
                    {
                        settingsRow = new Setting();
                        settingsRow.Name = AdminController.SiteSettingsName;
                        db.Settings.Add(settingsRow);
                    }

                    var settings = new SiteSettings();
                    settings.SiteName = model.SiteName;

                    settingsRow.Value = JsonConvert.SerializeObject(settings);

                    // Create the organization, if it does not already exist.
                    var org = db.Organizations.Where(x => x.Name == model.OrganizationName).FirstOrDefault();
                    if (org == null)
                    {
                        string host = Request.Headers["Host"];
                        org = new Organization()
                        {
                            Id = Guid.NewGuid(),
                            Name = model.OrganizationName,
                            AgencyID = model.AgencyId,
                            Hostname = host
                        };

                        db.Organizations.Add(org);
                    }

                    // Create the user.
                    var userManager = HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
                    var user = await UserController.CreateUser(model.UserName, model.Password, model.FirstName, model.LastName,
                        true,
                        org,
                        userManager,
                        ModelState,
                        db);

                    if (user == null)
                    {
                        model.Message = "The user could not be created. Please try again.";
                        model.SystemStatus = checker.GetSystemStatus();
                        return View(model);
                    }

                    db.SaveChanges();

                    // Indicate that setup has been run.
                    System.IO.File.WriteAllText(isConfiguredPath, DateTime.UtcNow.ToString());

                    return RedirectToAction("Index", "Admin");
                }
                catch (Exception ex)
                {
                    model.Message = ex.Message;
                    model.SystemStatus = checker.GetSystemStatus();
                    return View(model);
                }
            }
        }
    }
}
