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
using Colectica.Curation.Web.Controllers;
using Colectica.Curation.Web.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web;

namespace Colectica.Curation.Common.ViewModels
{
    public class SettingsHelper
    {
        public static SiteSettings GetSiteSettings(ApplicationDbContext db)
        {
            SiteSettings settings = null;
            if (HttpContext.Current?.Items.Contains("AdminController.SiteSettingsName") == true)
            {
                settings = HttpContext.Current.Items["AdminController.SiteSettingsName"] as SiteSettings;                
            }
            else
            {
                var settingsRow = db.Settings.Where(x => x.Name == AdminController.SiteSettingsName).FirstOrDefault();
                if (settingsRow != null)
                {
                    settings = JsonConvert.DeserializeObject<SiteSettings>(settingsRow.Value);                    
                }
                else
                {
                    settings = new SiteSettings();
                }

                if (HttpContext.Current != null)
                {
                    HttpContext.Current.Items["AdminController.SiteSettingsName"] = settings;
                }
            }            
            return settings;
        }

        public static string GetIngestDirectory(Organization organization, ApplicationDbContext db)
        {
            if (organization.IngestDirectory != null)
            {
                return organization.IngestDirectory;
            }
            else
            {
                SiteSettings settings = SettingsHelper.GetSiteSettings(db);
                return settings.IngestDirectory;
            }
        }

        public static string GetProcessingDirectory(Organization organization, ApplicationDbContext db)
        {
            if (organization.ProcessingDirectory != null)
            {
                return organization.ProcessingDirectory;
            }
            else
            {
                SiteSettings settings = SettingsHelper.GetSiteSettings(db);
                return settings.ProcessingDirectory;
            }
        }
        public static string GetArchiveDirectory(Organization organization, ApplicationDbContext db)
        {
            if (organization.ProcessingDirectory != null)
            {
                return organization.ArchiveDirectory;
            }
            else
            {
                SiteSettings settings = SettingsHelper.GetSiteSettings(db);
                return settings.ArchiveDirectory;
            }
        }

    }
}
