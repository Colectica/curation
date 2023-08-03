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

using Colectica.Curation.Contracts;
using Colectica.Curation.Data;
using Colectica.Curation.Web.Models;
using log4net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Colectica.Curation.DdiAddins.Actions
{
    [Export(typeof(IPublishAction))]
    public class CopyPublishedFiles : IPublishAction
    {
        ILog logger;

        public string Name => "Copy published files";

        public CopyPublishedFiles()
        {
            logger = LogManager.GetLogger("Curation");
        }

        public void PublishRecord(CatalogRecord record, ApplicationUser user, ApplicationDbContext db, string ProcessingDirectory)
        {
            var settings = GetSiteSettings();
            string destination = settings.PublishedFilesDirectory;
            if (!Directory.Exists(destination))
            {
                logger.Warn("Destination folder does not exist. Exiting.");
                return;
            }

            logger.Debug($"Copying published files for record {record.Id} {record.Title}");

            foreach (var file in record.Files)
            {
                if (!file.IsPublicAccess)
                {
                    continue;
                }

                logger.Debug($"Processing file {file.Title}");

                // Copy the file.
                string sourcePath = Path.Combine(
                    record.Organization.ProcessingDirectory,
                    file.CatalogRecord.Id.ToString(),
                    file.Name);
                string targetPath = Path.Combine(
                    destination,
                    record.Id.ToString(),
                    file.Name);

                logger.Debug($"Copying from {sourcePath} to {targetPath}");
                string directory = Path.GetDirectoryName(targetPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                try
                {
                    File.Copy(sourcePath, targetPath);
                }
                catch (Exception ex)
                {
                    logger.Error("Error copying the file.", ex);
                }
            }

            logger.Debug("Done copying files");

            SiteSettings GetSiteSettings()
            {
                var settingsRow = db.Settings.Where(x => x.Name == "SiteSettings").FirstOrDefault();
                if (settingsRow != null)
                {
                    return JsonConvert.DeserializeObject<SiteSettings>(settingsRow.Value);
                }
                else
                {
                    return new SiteSettings();
                }
            }
        }
    }
}
