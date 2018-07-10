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
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;
using Colectica.Curation.Web.Models;
using Newtonsoft.Json;
using Colectica.Curation.Common.Utility;
using log4net;

namespace Colectica.Curation.Operations
{
    public class ArchiveIngestFiles : IOperation
    {
        public static readonly Guid OperationTypeId = new Guid("90ABD6C0-D854-4BBB-B16C-09C142404E2E");
        public Guid OperationType { get { return OperationTypeId; } }

        public string Name { get; set; }
        public int Timeout { get; set; }

        public string UserId { get; set; }

        public Guid CatalogRecordId { get; set; }

        public string IngestDirectory { get; set; }

        public string ArchiveDirectory { get; set; }

        ApplicationDbContext db;

        ILog logger;

        public ArchiveIngestFiles()
        {
            logger = LogManager.GetLogger("Curation");
        }

        public bool Execute()
        {
            using (db = ApplicationDbContext.Create())
            {
                var siteSettings = GetSiteSettings();

                var user = db.Users.Find(UserId.ToString());

                var record = db.CatalogRecords.Where(x => x.Id == CatalogRecordId)
                    .Include(x => x.Organization)
                    .Include(x => x.Files)
                    .Include(x => x.CreatedBy)
                    .Include(x => x.Approvers)
                    .FirstOrDefault();

                logger.Debug("Archiving ingest files for " + record.Title);

                // Create the archive package.
                try
                {
                    ArchivePackageBuilder.CreateArchivePackage(record, IngestDirectory, false, ArchiveDirectory, "ingest");
                    
                    // Log an event.
                    var log = new Event()
                    {
                        EventType = EventTypes.CreateArchivePackage,
                        Timestamp = DateTime.UtcNow,
                        User = user,
                        RelatedCatalogRecord = record,
                        Title = "Archived ingest files for " + record.Title,
                        Details = string.Empty
                    };
                    db.Events.Add(log);

                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    logger.Warn("Error while archiving ingest files", ex);
                    
                    // Log an event.
                    var log = new Event()
                    {
                        EventType = EventTypes.CreateArchivePackage,
                        Timestamp = DateTime.UtcNow,
                        User = user,
                        RelatedCatalogRecord = record,
                        Title = "Error while archiving ingest files for " + record.Title,
                        Details = ex.Message
                    };
                    db.Events.Add(log);

                    db.SaveChanges();
                }


                return true;
            }
        }

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
