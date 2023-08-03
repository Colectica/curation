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

﻿using Colectica.Curation.Common.Utility;
using Colectica.Curation.Data;
using Colectica.Curation.Web.Models;
using Colectica.Curation.Web.Utility;
using Ionic.Zip;
using LibGit2Sharp;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Data.Entity;
using log4net;
using Colectica.Curation.Contracts;
using System.ComponentModel.Composition;

namespace Colectica.Curation.Operations
{
    public class FinalizeCatalogRecord : IOperation
    {
        public static readonly Guid OperationTypeId = new Guid("B08F4FA8-4559-42D2-ADFD-D697A62EE3E8");
        public Guid OperationType { get { return OperationTypeId; } }

        public string UserId { get; set; }

        public Guid CatalogRecordId { get; set; }

        public string ProcessingDirectory { get; set; }

        public string ArchiveDirectory { get; set; }

        public string UrlBase { get; set; }

        public string Name { get; set; }

        public int Timeout { get; set; }

        [ImportMany]
        public List<ICreatePreservationFormatsAction> CreatePreservationFormatsActions { get; set; }

        [ImportMany]
        public List<ICreatePersistentIdentifiersAction> CreatePersistentIdentifiersActions { get; set; }

        [ImportMany]
        public List<IFinalizeMetadataAction> IFinalizeMetadataActions { get; set; }

        [ImportMany]
        public List<IPublishAction> PublishActions { get; set; }

        ApplicationDbContext db;
        ApplicationUser user;
        CatalogRecord record;
        SiteSettings siteSettings;

        bool hasFailure;
        List<string> messages = new List<string>();
        private Guid ddiFileId;

        ILog logger;

        public FinalizeCatalogRecord()
        {
            logger = LogManager.GetLogger("Curation");
        }

        public bool Execute()
        {
            using (this.db = ApplicationDbContext.Create())
            {
                siteSettings = GetSiteSettings();

                this.user = db.Users.Find(UserId.ToString());

                this.record = db.CatalogRecords.Where(x => x.Id == CatalogRecordId)
                    .Include(x => x.Organization)
                    .Include(x => x.Files)
                    .Include(x => x.CreatedBy)
                    .Include(x => x.Approvers)
                    .FirstOrDefault();

                logger.Debug("Finalizing Catalog Record " + record.Title);

                CreatePreservationFormats(); // ICreatePreservationFormatsAction

                RequestAndAssignHandles(); // ICreatePersistentIdentifiersAction

                CreateAndAssignChecksums();

                ExportDdiMetadata(); // IFinalizeMetadataAction

                CreateArchivePackage();

                CopyToPublishTarget(); // IPublishAction


                if (hasFailure)
                {
                    // Add an event log for the failure.
                    var log = new Event()
                    {
                        EventType = EventTypes.FinalizeCatalogRecordFailed,
                        Timestamp = DateTime.UtcNow,
                        User = user,
                        RelatedCatalogRecord = record,
                        Title = "Catalog Record Finalization Failure",
                        Details = string.Join("\n", this.messages)
                    };
                    db.Events.Add(log);
                }
                else
                {
                    // Log the publication.
                    var log = new Event()
                    {
                        EventType = EventTypes.Publish,
                        Timestamp = DateTime.UtcNow,
                        User = user,
                        RelatedCatalogRecord = record,
                        Title = "Published " + record.Title,
                        Details = string.Empty
                    };
                    db.Events.Add(log);

                    record.Status = CatalogRecordStatus.Published;
                }

                db.SaveChanges();

                if (hasFailure)
                {
                    SendEmailNotificationsForFailure();
                }
                else
                {
                    SendEmailNotificationsForSuccess();
                }

                return !hasFailure;
            }
        }

        void CreatePreservationFormats()
        {
            foreach (var addin in CreatePreservationFormatsActions)
            {
                try
                {
                    addin.CreatePreservationFormats(record, user, db, UserId, ProcessingDirectory, siteSettings.StatTransferPath);
                }
                catch (Exception ex)
                {
                    logger.Error($"Error creating preservation formats via {addin.Name}", ex);

                }
            }
        }

        void RequestAndAssignHandles()
        {
            foreach (var addin in CreatePersistentIdentifiersActions)
            {
                try
                {
                    var result = addin.CreatePersistentIdentifiers(record, user, db);
                    this.ddiFileHandle = result.DdiFileHandle;
                    this.ddiFileId = result.DdiFileId;
                    foreach (string message in result.Messages)
                    {
                        messages.Add(message);
                    }

                    if (!result.Skipped && 
                        !result.Successful)
                    {
                        hasFailure = true;
                    }
                }
                catch (Exception ex)
                {
                    logger.Error($"Error requesting persistent identifiers via addin {addin.Name}", ex);

                    messages.Add("Could not assign Handle identifier. " + ex.Message);
                    hasFailure = true;
                }
            }

        }

        void CreateAndAssignChecksums()
        {
            logger.Debug("Generating checksums");

            using (var hasher = SHA256Managed.Create())
            {
                bool hasErrors = false;
                foreach (var managedFile in record.Files.Where(x => x.Status != Data.FileStatus.Removed))
                {
                    try
                    {
                        string path = Path.Combine(ProcessingDirectory, record.Id.ToString(), managedFile.Name);

                        using (var fileStream = new FileStream(path, FileMode.Open))
                        {
                            byte[] hashValue = hasher.ComputeHash(fileStream);
                            managedFile.Checksum = BitConverter.ToString(hashValue).Replace("-", String.Empty);
                            managedFile.ChecksumMethod = "SHA256";
                            managedFile.ChecksumDate = DateTime.UtcNow;

                            logger.Debug("Generated checksum: " + managedFile.Checksum);
                        }
                    }
                    catch (Exception ex)
                    {
                        hasErrors = true;
                        logger.Error("Problem while generating checksum", ex);
                        this.messages.Add(ex.Message);
                    }
                }

               if (!hasErrors)
               {
                   LogEvent(EventTypes.GenerateChecksums, "Generated Checksums ");
               }
            }
        }

        void ExportDdiMetadata()
        {
            foreach (var addin in IFinalizeMetadataActions)
            {
                try
                {
                    addin.FinalizeMetadata(record, user, db, ProcessingDirectory, this.ddiFileId, this.ddiFileHandle);
                }
                catch (Exception ex)
                {
                    logger.Error($"Error exporting metadata via addin {addin.Name}", ex);
                    this.messages.Add("Error exporting metadata via addin {addin.Name}. " + ex.Message);
                    hasFailure = true;
                }
            }

        }

        void CreateArchivePackage()
        {
            if (hasFailure)
            {
                return;
            }

            ArchivePackageBuilder.CreateArchivePackage(record, ProcessingDirectory, true, ArchiveDirectory, "published");

            record.ArchiveDate = DateTime.UtcNow;

            // Mark this step as done.
            var task = db.TaskStatuses.Where(x => x.CatalogRecord.Id == record.Id &&
                x.TaskId == BuiltInCatalogRecordTasks.ArchiveCatalogRecordTaskId)
                .FirstOrDefault();
            if (task != null)
            {
                task.IsComplete = true;
                task.CompletedDate = DateTime.UtcNow;
                task.CompletedBy = user;
            }

            LogEvent(EventTypes.CreateArchivePackage, "Created Archive Package");
        }

        void CopyToPublishTarget()
        {
            if (hasFailure)
            {
                return;
            }

            // Call any addins that want to publish.
            foreach (var addin in PublishActions)
            {
                try
                {
                    addin.PublishRecord(record, user, db, ProcessingDirectory);
                }
                catch (Exception ex)
                {
                    logger.Error($"Error in publication addin {addin.Name}", ex);
                }

                LogEvent(EventTypes.Publish, addin.Name);
            }

            // For now just record the date.
            record.PublishDate = DateTime.UtcNow;

            // Mark this step as done.
            var task = db.TaskStatuses.Where(x => x.CatalogRecord.Id == record.Id &&
                x.TaskId == BuiltInCatalogRecordTasks.PublishCatalogRecordTaskId)
                .FirstOrDefault();
            if (task != null)
            {
                task.IsComplete = true;
                task.CompletedDate = DateTime.UtcNow;
                task.CompletedBy = user;
            }
        }

        void SendEmailNotificationsForSuccess()
        {
            // Send email notifications.
            var org = record.Organization;

            var toNotify = new List<ApplicationUser>();
            foreach (var user in record.Curators)
            {
                toNotify.Add(user);
            }
            toNotify.Add(record.CreatedBy);

            foreach (var notifyUser in toNotify)
            {
                try
                {
                    NotificationService.SendDecisionNotification(true, notifyUser, record, org, UrlBase, db);
                }
                catch (Exception ex)
                {
                    logger.Warn("Could not send decision notification email", ex);
                }
            }
        }

        void SendEmailNotificationsForFailure()
        {
            var org = record.Organization;
            var toNotify = record.Approvers.ToList();
            foreach (var notifyUser in toNotify)
            {
                try
                {
                    NotificationService.SendAlertEmail(
                        notifyUser.Email, notifyUser.FullName,
                        org.ReplyToAddress, org.Name,
                        "[Catalog Record Finalization Failed] " + record.Title,
                        string.Format("Finalization of {0} Failed", record.Title),
                        "You are an approver for this record. Please review the problems below and reapprove the record if appropriate.",
                        string.Join("<br />", this.messages.Select(x => "- " + x)),
                        "Review " + record.Title,
                        UrlBase + "/CatalogRecord/History/" + record.Id.ToString(),
                        org.NotificationEmailClosing,
                        UrlBase + "/User/EmailPreferences/" + notifyUser.UserName,
                        db);
                }
                catch (Exception ex)
                {
                    logger.Warn("Could not send failure notification email", ex);
                }
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

        void LogEvent(Guid eventType, string title, string details = null)
        {
            var log = new Event()
            {
                EventType = eventType,
                Timestamp = DateTime.UtcNow,
                User = user,
                RelatedCatalogRecord = record,
                Title = title,
                Details = details
            };
            db.Events.Add(log);
        }


        public string ddiFileHandle { get; set; }
    }
}
