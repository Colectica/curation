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
using Colectica.Curation.Web.Utility;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using System.IO;
using System.Xml.Linq;

namespace Colectica.Curation.Web.Controllers
{
    [Authorize]
    public class AdminController : CurationControllerBase
    {
        public static string SiteSettingsName = "SiteSettings";

        public ActionResult Index()
        {
            using (var db = ApplicationDbContext.Create())
            {
                var thisUser = db.Users.Where(x => x.UserName == User.Identity.Name)
                    .Include(x => x.Organizations)
                    .FirstOrDefault();

                if (!thisUser.IsAdministrator)
                {
                    throw new HttpException(403, "Forbidden");
                }
            }
            return View();
        }

        public ActionResult Settings()
        {
            using (var db = ApplicationDbContext.Create())
            {
                var thisUser = db.Users.Where(x => x.UserName == User.Identity.Name)
                    .Include(x => x.Organizations)
                    .FirstOrDefault();

                if (!thisUser.IsAdministrator)
                {
                    throw new HttpException(403, "Forbidden");
                }

                var settingsRow = db.Settings.Where(x => x.Name == SiteSettingsName).FirstOrDefault();

                SiteSettings settings = null;
                if (settingsRow == null)
                {
                    settings = new SiteSettings();
                }
                else
                {
                    settings = JsonConvert.DeserializeObject<SiteSettings>(settingsRow.Value);
                }

                return View(settings);
            }
        }

        public ActionResult SystemStatus()
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

                if (!thisUser.IsAdministrator)
                {
                    throw new HttpException(403, "Forbidden");
                }
            }

            var checker = new SystemStatusChecker();
            var model = checker.GetSystemStatus();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Settings(SiteSettings settings)
        {
            if (!ModelState.IsValid)
            {
                return View(settings);
            }

            using (var db = ApplicationDbContext.Create())
            {
                var thisUser = db.Users.Where(x => x.UserName == User.Identity.Name)
                    .Include(x => x.Organizations)
                    .FirstOrDefault();

                if (!thisUser.IsAdministrator)
                {
                    throw new HttpException(403, "Forbidden");
                }

                // Boo lookup instead of add-or-update.
                var settingsRow = db.Settings.Where(x => x.Name == SiteSettingsName).FirstOrDefault();
                if (settingsRow == null)
                {
                    settingsRow = new Setting();
                    settingsRow.Name = SiteSettingsName;
                    db.Settings.Add(settingsRow);
                }

                settingsRow.Value = JsonConvert.SerializeObject(settings);

                db.SaveChanges();

                return RedirectToAction("Index");
            }
        }

    }
}
