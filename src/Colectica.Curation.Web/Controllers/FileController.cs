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

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Colectica.Curation.Web.Models;
using Colectica.Curation.Data;
using System.Data.Entity;
using Colectica.Curation.Common.ViewModels;
using AutoMapper;
using Colectica.Curation.Web.Utility;
using System.IO;
using Newtonsoft.Json;
using Colectica.Curation.Operations;
using Colectica.Curation.Web.Areas.Ddi.Utility;
using ICSharpCode.SharpZipLib.Zip;
using LibGit2Sharp;
using Colectica.Curation.Common.Utility;
using System.Net.Mime;
using Colectica.Curation.Models;
using log4net;

namespace Colectica.Curation.Web.Controllers
{
    [Authorize]
    public class FileController : CurationControllerBase
    {
        public ActionResult DownloadArchive(Guid catalogRecordId)
        {
            using (var db = ApplicationDbContext.Create())
            {
                CatalogRecord record = db.CatalogRecords
                    .Where(x => x.Id == catalogRecordId
                    ).Include(x => x.Organization).FirstOrDefault();

                if (record == null)
                {
                    //TODO error page?
                    return RedirectToAction("Index");
                }

                EnsureUserIsAllowed(record, db);
                EnsureUserCanDownload(record.Organization);

                string processingDirectory = SettingsHelper.GetProcessingDirectory(record.Organization, db);
                string path = Path.Combine(processingDirectory, record.Id.ToString());

                if (!Directory.Exists(path))
                {
                    //TODO error page?
                    return RedirectToAction("Index");
                }

                string fileName = record.Title.MakeSafeFileName() + ".zip";

                Response.AddHeader("Content-Disposition",
                    string.Format("attachment; filename=\"{0}\"", fileName));
                Response.ContentType = "application/zip";

                using (var zipStream = new ZipOutputStream(Response.OutputStream))
                {
                    foreach (string filePath in Directory.EnumerateFiles(path))
                    {
                        using (FileStream fs = System.IO.File.OpenRead(filePath))
                        {
                            var fileEntry = new ZipEntry(Path.GetFileName(filePath))
                            {
                                Size = fs.Length
                            };
                            zipStream.PutNextEntry(fileEntry);

                            fs.CopyTo(zipStream);
                        }
                    }

                    zipStream.Flush();
                    zipStream.Close();
                }
                HttpContext.ApplicationInstance.CompleteRequest();
                return Content(string.Empty);//doesn't execute
            }
        }

        public ActionResult DownloadVersion(Guid id, string sha)
        {
            using (var db = ApplicationDbContext.Create())
            {
                ManagedFile file = db.Files.Where(x => x.Id == id)
                    .Include(x => x.CatalogRecord)
                    .Include(x => x.CatalogRecord.Organization).FirstOrDefault();
                if (file == null)
                {
                    //TODO error page?
                    return RedirectToAction("Index");
                }

                EnsureUserIsAllowed(file.CatalogRecord, db);
                EnsureUserCanDownload(file.CatalogRecord.Organization);

                string processingDirectory = SettingsHelper.GetProcessingDirectory(file.CatalogRecord.Organization, db);
                string path = Path.Combine(processingDirectory, file.CatalogRecord.Id.ToString());


                using (var repo = new LibGit2Sharp.Repository(path))
                {
                    var blob = repo.Lookup<Blob>(sha);

                    var cd = new System.Net.Mime.ContentDisposition
                    {
                        FileName = file.Name,//always uses the current file name right now...
                        Inline = false,
                        Size = blob.Size
                    };

                    var contentStream = blob.GetContentStream();

                    Response.AppendHeader("Content-Disposition", cd.ToString());
                    return new FileStreamResult(contentStream, "application/octet-stream");
                }
            }
        }

        ActionResult Download(Guid id, bool inline)
        {
            using (var db = ApplicationDbContext.Create())
            {
                ManagedFile file = db.Files.Where(x => x.Id == id)
                    .Include(x => x.CatalogRecord)
                    .Include(x => x.CatalogRecord.Organization).FirstOrDefault();
                if (file == null)
                {
                    //TODO error page?
                    return RedirectToAction("Index");
                }

                // For non-public files, or unpublished files, verify that the user should have access.
                // For public files, anybody can download.
                if (!file.IsPublicAccess ||
                    file.CatalogRecord.Status != CatalogRecordStatus.Published)
                {
                    EnsureUserIsAllowed(file.CatalogRecord, db);
                    EnsureUserCanDownload(file.CatalogRecord.Organization);
                }

                string processingDirectory = SettingsHelper.GetProcessingDirectory(file.CatalogRecord.Organization, db);
                string path = Path.Combine(processingDirectory, file.CatalogRecord.Id.ToString());
                path = Path.Combine(path, file.Name);

                // Use the PublicName as the downloaded file name.
                string fileDownloadName = file.Name;

                var cd = new System.Net.Mime.ContentDisposition
                {
                    FileName = fileDownloadName,
                    Inline = inline,
                };
                Response.AppendHeader("Content-Disposition", cd.ToString());

                // Set the MIME type.
                string ext = Path.GetExtension(cd.FileName).ToLower();
                string mimeType = MediaTypeNames.Application.Octet;
                if (ext == ".pdf")
                {
                    mimeType = MediaTypeNames.Application.Pdf;
                }

                return File(path, mimeType);
            }
        }

        [AllowAnonymous]
        public ActionResult Download(Guid id)
        {
            return Download(id, false);
        }

        [AllowAnonymous]
        public ActionResult DownloadInline(Guid id)
        {
            return Download(id, true);
        }

        public ActionResult General(Guid id)
        {
            using (var db = ApplicationDbContext.Create())
            {
                var logger = LogManager.GetLogger("FileController");

                var file = GetFile(id, db);
                logger.Debug("Got file");

                EnsureUserIsAllowed(file.CatalogRecord, db);

                logger.Debug("User is allowed");

                var model = new FileViewModel(file) as FileViewModel;

                model.TermsOfUse = file.CatalogRecord.Organization.TermsOfService;
                model.IsUserCurator = file.CatalogRecord.Curators.Any(x => x.UserName == User.Identity.Name);
                model.IsUserApprover = file.CatalogRecord.Approvers.Any(x => x.UserName == User.Identity.Name) ||
                    OrganizationHelper.DoesUserHaveRight(db, User, file.CatalogRecord.Organization.Id, Right.CanApprove);

                string lowerExtension = Path.GetExtension(file.Name).ToLower();
                string[] autoDetectedFileTypes = { ".dta", ".sav", ".rdata", ".csv", ".do", ".r", ".sps" };
                model.IsFileTypeAutoDetected = autoDetectedFileTypes.Contains(lowerExtension);

                logger.Debug("Mapped");

                logger.Debug($"Record Status: {model.File.CatalogRecord.Status}");
                logger.Debug($"IsCurator: {model.IsUserCurator}");
                logger.Debug($"IsApprover: {model.IsUserApprover}");
                logger.Debug($"IsReadOnly: {model.IsReadOnly}");
                logger.Debug($"Persistent link: {model.File.PersistentLink}");

                return View(model);
            }
        }

        public ActionResult Status(Guid id)
        {
            using (var db = ApplicationDbContext.Create())
            {
                var file = GetFile(id, db);

                EnsureUserIsAllowed(file.CatalogRecord, db);

                var model = FileStatusHelper.GetFileStatusModel(file, db);

                model.IsUserCurator = file.CatalogRecord.Curators.Any(x => x.UserName == User.Identity.Name);
                model.IsUserApprover = file.CatalogRecord.Approvers.Any(x => x.UserName == User.Identity.Name) ||
                    OrganizationHelper.DoesUserHaveRight(db, User, file.CatalogRecord.Organization.Id, Right.CanApprove);


                return View(model);
            }
        }

        public ActionResult History(Guid id)
        {
            using (var db = ApplicationDbContext.Create())
            {
                //var record = GetRecord(id, db);
                var file = GetFile(id, db);

                EnsureUserIsAllowed(file.CatalogRecord, db);

                var events = db.Events
                    .Where(x => x.RelatedManagedFiles.Any(f => f.Id == id))
                    .OrderByDescending(x => x.Timestamp)
                    .Include(x => x.RelatedCatalogRecord)
                    .Include(x => x.RelatedManagedFiles)
                    .Include(x => x.User);

                var model = new ManagedFileHistoryModel();
                model.File = file;
                model.IsUserCurator = true;
                model.IsUserApprover = true;

                var logs = new List<HistoryEventModel>();

                foreach (var log in events)
                {
                    var eventModel = HistoryEventModel.FromEvent(log, log.User);
                    logs.Add(eventModel);
                }

                // Sort all the events.
                var sorted = logs.OrderByDescending(x => x.Timestamp);
                foreach (var log in sorted)
                {
                    model.Events.Add(log);
                }

                return View(model);
            }
        }

        public ActionResult Revisions(Guid id)
        {
            using (var db = ApplicationDbContext.Create())
            {
                var file = GetFile(id, db);

                EnsureUserIsAllowed(file.CatalogRecord, db);

                var model = new ManagedFileHistoryModel();
                model.File = file;

                model.IsUserCurator = file.CatalogRecord.Curators.Any(x => x.UserName == User.Identity.Name);
                model.IsUserApprover = file.CatalogRecord.Approvers.Any(x => x.UserName == User.Identity.Name) ||
                    OrganizationHelper.DoesUserHaveRight(db, User, file.CatalogRecord.Organization.Id, Right.CanApprove);

                string processingDirectory = SettingsHelper.GetProcessingDirectory(file.CatalogRecord.Organization, db);
                string path = Path.Combine(processingDirectory, file.CatalogRecord.Id.ToString());
                using (var repo = new LibGit2Sharp.Repository(path))
                {
                    List<Tuple<Commit, TreeEntry>> modificationCommits = new List<Tuple<Commit, TreeEntry>>();

                    string currentSha = null;// startingItemSha;
                    string currentPath = model.File.Name;//startingItemPath;
                    Tuple<Commit, TreeEntry> current = null;

                    foreach (Commit c in repo.Commits)
                    {
                        if (c.Tree.Any<TreeEntry>(entry => entry.Name == currentPath))
                        {
                            // If file with given name was found, check its SHA
                            TreeEntry te = c.Tree.First<TreeEntry>(entry => entry.Name == currentPath);

                            if (te.Target.Sha == currentSha)
                            {
                                // In case if file's SHA matches 
                                // file was not changed in this commit
                                // and temporary commit need to be updated to current one
                                current = new Tuple<Commit, TreeEntry>(c, te);
                            }
                            else
                            {
                                // file's SHA doesn't match 
                                // file was changed during commit (or is first one)
                                // current commit needs to be added to the commits collection
                                // The file's SHA updated to current one
                                modificationCommits.Add(new Tuple<Commit, TreeEntry>(c, te));
                                currentSha = te.Target.Sha;
                                current = null;
                            }
                        }
                        else
                        {
                            // File with given name not found. this means it was renamed.
                            // SHA should still be the same
                            if (c.Tree.Any<TreeEntry>(entry => entry.Target.Sha == currentSha))
                            {
                                TreeEntry te = c.Tree.First<TreeEntry>(entry => entry.Target.Sha == currentSha);
                                currentPath = te.Name;
                                modificationCommits.Add(new Tuple<Commit, TreeEntry>(c, te));
                                current = null;
                            }
                        }
                    }

                    if (current != null)
                    {
                        modificationCommits.Add(current);
                    }

                    foreach (var m in modificationCommits)
                    {
                        RevisionModel h = RevisionModel.FromCommit(m.Item1, m.Item2, file);

                        // replace uuid with real user name
                        ApplicationUser user = db.Users
                            .Where(x => x.Id == h.CommitterName)
                            .FirstOrDefault();
                        if (user != null)
                        {
                            h.CommitterName = user.FullName;
                            h.CommitterEmail = user.Email;
                            h.CommitterId = user.Id;
                        }

                        model.Revisions.Add(h);
                    }
                }

                return View(model);
            }
        }

        public ActionResult ApplyMetadataUpdates(Guid id)
        {
            using (var db = ApplicationDbContext.Create())
            {
                var user = db.Users.Where(x => x.UserName == User.Identity.Name).FirstOrDefault();
                var file = db.Files.Where(x => x.Id == id)
                    .Include(x => x.CatalogRecord)
                    .Include(x => x.CatalogRecord.Organization)
                    .FirstOrDefault();

                EnsureUserIsAllowed(file.CatalogRecord, db);
                EnsureUserCanEdit(file.CatalogRecord, db);

                if (file.CatalogRecord.IsLocked)
                {
                    throw new HttpException(400, "This operation cannot be performed while the record is locked");
                }

                // Store an event about this file upload.
                var log = new Event()
                {
                    EventType = EventTypes.ApplyMetadataUpdates,
                    Title = "Applying metadata updates",
                    Timestamp = DateTime.UtcNow,
                    User = user,
                    RelatedCatalogRecord = file.CatalogRecord
                };
                log.RelatedManagedFiles.Add(file);

                db.Events.Add(log);

                db.SaveChanges();

                var applyMetadataUpdatesOp = new ApplyMetadataUpdates()
                {
                    Name = "Apply Metadata Updates",
                    FileId = file.Id,
                    UserId = user.Id,
                    ProcessingDirectory = SettingsHelper.GetProcessingDirectory(file.CatalogRecord.Organization, db)
                };
                bool locked = db.Enqueue(file.CatalogRecord.Id, user.Id, applyMetadataUpdatesOp);

                return RedirectToAction("Files", "CatalogRecord", new { id = file.CatalogRecord.Id });
            }
        }


        [Route("File/Editor/{editorName}/{id}")]
        public ActionResult Notes(string editorName, Guid id)
        {
            using (var db = ApplicationDbContext.Create())
            {
                var file = GetFile(id, db);

                if (!file.CatalogRecord.Curators.Any(x => x.UserName == User.Identity.Name) &&
                    !file.CatalogRecord.Approvers.Any(x => x.UserName == User.Identity.Name) &&
                    file.CatalogRecord.CreatedBy.UserName != User.Identity.Name &&
                    !OrganizationHelper.DoesUserHaveRight(db, User, file.CatalogRecord.Organization.Id, Right.CanApprove))
                {
                    throw new HttpException(403, "Only curators may perform this task.");
                }

                var model = new NotesModel();
                model.CatalogRecord = file.CatalogRecord;
                model.File = file;

                model.IsUserCurator = file.CatalogRecord.Curators.Any(x => x.UserName == User.Identity.Name);
                model.IsUserApprover = file.CatalogRecord.Approvers.Any(x => x.UserName == User.Identity.Name) ||
                    OrganizationHelper.DoesUserHaveRight(db, User, file.CatalogRecord.Organization.Id, Right.CanApprove);


                var notes = db.Notes.Where(x => x.File.Id == id)
                    .Include(x => x.User);

                // If the user is the depositor, only show notes made by the depositor.
                if (file.CatalogRecord.CreatedBy.UserName == User.Identity.Name &&
                    !model.IsUserCurator && 
                    !model.IsUserApprover)
                {
                    notes = notes.Where(x => x.User.UserName == User.Identity.Name);
                }

                foreach (var note in notes)
                {
                    model.Comments.Add(new UserCommentModel
                    {
                        Text = note.Text,
                        UserName = note.User.UserName,
                        Timestamp = note.Timestamp,
                    });
                }

                return View("Notes", model);
            }
        }

        [Route("File/AddNote/{id}")]
        [HttpPost]
        public ActionResult AddNote(Guid id, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return Content(string.Empty);
            }

            // Get the CatalogRecord, so we can figure out what agency ID to use.
            using (var db = ApplicationDbContext.Create())
            {
                var user = db.Users.Where(x => x.UserName == User.Identity.Name)
                    .FirstOrDefault();
                if (user == null)
                {
                    return RedirectToAction("Index");
                }


                var file = GetFile(id, db);

                if (!file.CatalogRecord.Curators.Any(x => x.UserName == User.Identity.Name) &&
                    !file.CatalogRecord.Approvers.Any(x => x.UserName == User.Identity.Name) &&
                    file.CatalogRecord.CreatedBy.UserName != User.Identity.Name &&
                    !OrganizationHelper.DoesUserHaveRight(db, User, file.CatalogRecord.Organization.Id, Right.CanApprove))
                {
                    throw new HttpException(403, "Only curators may perform this task.");
                }

                try
                {
                    var note = new Data.Note()
                    {
                        CatalogRecord = file.CatalogRecord,
                        File = file,
                        Timestamp = DateTime.UtcNow,
                        User = user,
                        Text = text
                    };
                    db.Notes.Add(note);

                    // Log the adding of the note.
                    var log = new Event()
                    {
                        EventType = EventTypes.AddNote,
                        Timestamp = DateTime.UtcNow,
                        User = user,
                        RelatedCatalogRecord = file.CatalogRecord,
                        Title = "Add a Note",
                        Details = text
                    };
                    log.RelatedManagedFiles.Add(file);

                    db.Events.Add(log);

                    db.SaveChanges();

                    return Content(string.Empty);
                }
                catch (Exception ex)
                {
                    throw new HttpException(500, ex.Message);
                }
            }
        }

        [HttpPost]
        public ActionResult Delete(Guid id)
        {
            using (var db = ApplicationDbContext.Create())
            {
                var user = db.Users.Where(x => x.UserName == User.Identity.Name)
                    .FirstOrDefault();
                if (user == null)
                {
                    return RedirectToAction("Index");
                }

                var file = GetFile(id, db);

                EnsureUserIsAllowed(file.CatalogRecord, db);
                EnsureUserCanEdit(file.CatalogRecord, db);

                // "Remove" from the database.
                file.Status = Data.FileStatus.Removed;

                // Remove any tasks that reference this file.
                var fileTasks = db.TaskStatuses.Where(x => x.File.Id == file.Id);
                foreach (var task in fileTasks)
                {
                    db.TaskStatuses.Remove(task);
                }

                // Log an event.
                var log = new Event()
                {
                    EventType = EventTypes.RemoveFile,
                    Timestamp = DateTime.UtcNow,
                    User = user,
                    RelatedCatalogRecord = file.CatalogRecord,
                    Title = "Remove file",
                };
                log.RelatedManagedFiles.Add(file);
                db.Events.Add(log);

                db.SaveChanges();

                return RedirectToAction("Files", "CatalogRecord", new { id = file.CatalogRecord.Id });
            }
        }

        public ActionResult RemovePersistentId(FileViewModel model)
        {
            using (var db = ApplicationDbContext.Create())
            {
                var user = db.Users
                    .Where(x => x.UserName == User.Identity.Name)
                    .FirstOrDefault();
                if (user == null)
                {
                    return RedirectToAction("Index");
                }

                // Fetch the appropriate ManagedFile by ID.
                Guid id = model.Id;
                var file = GetFile(id, db);

                EnsureUserIsAllowed(file.CatalogRecord, db);
                EnsureUserCanEdit(file.CatalogRecord, db);

                // Clear the persistent ID.
                file.PersistentLink = null;
                file.PersistentLinkDate = null;

                // Log the editing of the file.
                var log = new Event()
                {
                    EventType = EventTypes.EditManagedFile,
                    Timestamp = DateTime.UtcNow,
                    User = user,
                    RelatedCatalogRecord = file.CatalogRecord,
                    Title = "Edit a File",
                    Details = "Remove persistent link"
                };
                db.Events.Add(log);

                db.SaveChanges();

                return RedirectToAction("General", new { id = file.Id });
            }
        }

        [HttpPost]
        public ActionResult General(FileViewModel model)
        {
            var logger = LogManager.GetLogger("FileController");
            logger.Debug("Entering FileController.General() POST handler");

            if (Request.Form.AllKeys.Contains("RemovePersistentId"))
            {
                logger.Debug("Removing persistent ID");
                return RemovePersistentId(model);
            }

            using (var db = ApplicationDbContext.Create())
            {
                logger.Debug("Created database object");

                var user = db.Users
                    .Where(x => x.UserName == User.Identity.Name)
                    .FirstOrDefault();
                if (user == null)
                {
                    logger.Debug("No user. Returning.");
                    return RedirectToAction("Index");
                }

                // Fetch the appropriate ManagedFile by ID.
                Guid id = model.Id;
                var file = GetFile(id, db);

                EnsureUserIsAllowed(file.CatalogRecord, db);
                EnsureUserCanEdit(file.CatalogRecord, db);

                logger.Debug("User is allowed");

                if (file.CatalogRecord.IsLocked)
                {
                    logger.Debug("Catalog record is locked. Throwing.");
                    throw new HttpException(400, "This operation cannot be performed while the record is locked");
                }

                string changeSummary = ManagedFileChangeDetector.GetChangeSummary(file, model);
                logger.Debug("Got change summary");

                // Copy the information from the POST to the ManagedFile, ignoring read-only properties.                   
                Mapper.Map(model, file);
                logger.Debug("Mapped");

                // Log the editing of the file.
                var log = new Event()
                {
                    EventType = EventTypes.EditManagedFile,
                    Timestamp = DateTime.UtcNow,
                    User = user,
                    RelatedCatalogRecord = file.CatalogRecord,
                    Title = "Edit a File",
                    Details = changeSummary
                };
                log.RelatedManagedFiles.Add(file);
                db.Events.Add(log);

                logger.Debug("Logged");

                // Update any tasks, in case the file type has changed.
                logger.Debug("Updating file tasks");
                TaskHelpers.AddProcessingTasksForFile(file, file.CatalogRecord, db);
                logger.Debug("Done updating file tasks");

                // Save the updated record.
                db.SaveChanges();
                logger.Debug("Wrote to database");

                logger.Debug("Returning from FileController.General() POST handler");
                return RedirectToAction("General", new { id = file.Id });
            }
        }

        ManagedFile GetFile(Guid id, ApplicationDbContext db)
        {
            if (id == Guid.Empty)
            {
                throw new HttpException(400, "Bad Request");
            }

            var file = db.Files
                .Where(x => x.Id == id)
                .Include(x => x.CatalogRecord)
                .Include(x => x.CatalogRecord.Curators)
                .Include(x => x.CatalogRecord.CreatedBy)
                .Include(x => x.Owner)
                .FirstOrDefault();

            if (file == null)
            {
                throw new HttpException(404, "Not Found");
            }

            return file;
        }

        void EnsureUserIsAllowed(CatalogRecord record, ApplicationDbContext db)
        {
            var user = db.Users
                .Where(x => x.UserName == User.Identity.Name)
                .FirstOrDefault();
            var permissions = db.Permissions
                .Where(x => x.User.Id == user.Id && x.Organization.Id == record.Organization.Id);
            bool userCanViewAll = permissions.Any(x => x.Right == Right.CanViewAllCatalogRecords);

            string createdByUserName = record.CreatedBy != null ? record.CreatedBy.UserName : string.Empty;

            if (createdByUserName != User.Identity.Name &&
                !record.Curators.Any(x => x.UserName == User.Identity.Name) &&
                !record.Approvers.Any(x => x.UserName == User.Identity.Name) &&
                !OrganizationHelper.DoesUserHaveRight(db, User, record.Organization.Id, Right.CanApprove) &&
                !userCanViewAll)
            {
                throw new HttpException(403, "This view is only available for the record's creator, curators, and administrators.");
            }
        }

        void EnsureUserCanEdit(CatalogRecord record, ApplicationDbContext db)
        {
            bool isCuratorForRecord = record.Curators.Any(x => x.UserName == User.Identity.Name);
            bool isApproverForRecord = record.Approvers.Any(x => x.UserName == User.Identity.Name);

            if (record.Status != CatalogRecordStatus.New &&
                !isCuratorForRecord &&
                !isApproverForRecord)
            {
                throw new HttpException(403, "Forbidden");
            }
        }

        void EnsureUserCanDownload(Organization org)
        {
            if (string.IsNullOrWhiteSpace(org.IPAddressesAllowedToDownloadFiles))
            {
                return;
            }

            string[] ipList = org.IPAddressesAllowedToDownloadFiles.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            var isOk = false;
            foreach (string ip in ipList)
            {
                if (ip == Request.UserHostAddress)
                {
                    isOk = true;
                    break;
                }
            }

            if (!isOk)
            {
                throw new HttpException(403, "Downloads are not allowed from your location.");
            }
        }

    }
}
