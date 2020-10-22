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
using System.Data.Entity;
using Colectica.Curation.Contracts;
using System.Web.Mvc;
using System.Security.Principal;
using Colectica.Curation.Web.Utility;
using Colectica.Curation.Common.Utility;
using Colectica.Curation.Operations;
using Colectica.Curation.Models;

namespace Colectica.Curation.Web.Areas.Ddi.Utility
{
    public static class TaskHelpers
    {
        public static void InitializeTaskModel(ScriptedTaskModel model, Guid id, IScriptedTask task, ApplicationDbContext db)
        {
            model.Task = task;

            var file = db.Files
                .Where(x => x.Id == id)
                .Include(x => x.CatalogRecord)
                .Include(x => x.CatalogRecord.Organization)
                .FirstOrDefault();

            model.CatalogRecordId = file.CatalogRecord.Id;
            model.CatalogRecordTitle = file.CatalogRecord.Title;
            model.File = file;
            model.FileId = file.Id;

            var record = file.CatalogRecord;

            // The file with the given ID is the primary file for this task.
            model.PrimaryFiles.Add(new FileViewModel(file));

            // Add all the other files, too, for reference.
            foreach (var x in record.Files.Where(x => x.Status != FileStatus.Removed))
            {
                model.AllFiles.Add(new FileViewModel(x));
            }
        }

        public static ManagedFile UpdateStatus(Guid id, IScriptedTask task, IPrincipal principal, System.Web.Mvc.FormCollection form, ApplicationDbContext db)
        {
            var file = db.Files.Where(x => x.Id == id).Include(x => x.CatalogRecord).FirstOrDefault();

            if (!file.CatalogRecord.Curators.Any(x => x.UserName == principal.Identity.Name))
            {
                throw new HttpException(403, "Forbidden");
            }

            var status = db.TaskStatuses.Where(x =>
                x.CatalogRecord.Id == file.CatalogRecord.Id &&
                x.File.Id == file.Id &&
                x.TaskId == task.Id)
                .FirstOrDefault();

            var user = db.Users.Where(x => x.UserName == principal.Identity.Name).FirstOrDefault();

            string result = form["result"];
            string notes = form["notes"];

            // Add a note.
            if (!string.IsNullOrWhiteSpace(notes))
            {
                var note = new Note()
                {
                    CatalogRecord = file.CatalogRecord,
                    File = file,
                    Timestamp = DateTime.UtcNow,
                    User = user,
                    Text = notes
                };
                db.Notes.Add(note);
            }

            if (result == "Accept" ||
                result == "Not Applicable")
            {
                status.IsComplete = true;
                status.CompletedBy = user;
                status.CompletedDate = DateTime.UtcNow;

                string eventTitle = null;
                if (result == "Accept")
                {
                    eventTitle = "Accept task: " + task.Name;
                }
                else if (result == "Not Applicable")
                {
                    eventTitle = "Not applicable task: " + task.Name;
                }

                // Log the acceptance.
                var log = new Event()
                {
                    EventType = EventTypes.AcceptTask,
                    Timestamp = DateTime.UtcNow,
                    User = user,
                    RelatedCatalogRecord = file.CatalogRecord,
                    Title = eventTitle,
                    Details = notes,
                };

                log.RelatedManagedFiles.Add(file);

                db.Events.Add(log);
            }
            else if (result == "Undo")
            {
                status.IsComplete = false;
                status.CompletedBy = null;
                status.CompletedDate = null;

                // Log the rejection.
                var log = new Event()
                {
                    EventType = EventTypes.RejectTask,
                    Timestamp = DateTime.UtcNow,
                    User = user,
                    RelatedCatalogRecord = file.CatalogRecord,
                    Title = "Task unaccepted: " + task.Name,
                    Details = notes
                };

                log.RelatedManagedFiles.Add(file);

                db.Events.Add(log);
            }
            else
            {
                // Log the rejection.
                var log = new Event()
                {
                    EventType = EventTypes.RejectTask,
                    Timestamp = DateTime.UtcNow,
                    User = user,
                    RelatedCatalogRecord = file.CatalogRecord,
                    Title = "Reject task: " + task.Name,
                    Details = notes
                };

                log.RelatedManagedFiles.Add(file);

                db.Events.Add(log);
            }

            db.SaveChanges();

            return file;

        }

        public static void AddProcessingTasksForFile(ManagedFile file, CatalogRecord record, ApplicationDbContext db)
        {
            foreach (var task in MefConfig.AddinManager.AllTasks)
            {
                if (!task.AppliesToFile(file))
                {
                    continue;
                }

                var status = new TaskStatus()
                {
                    Id = Guid.NewGuid(),
                    TaskId = task.Id,
                    CatalogRecord = record,
                    File = file,
                    Name = task.Name,
                    Weight = task.Weight,
                    StageName = "Processing"
                };

                db.TaskStatuses.Add(status);
            }

        }

        public static void ResetTasksForFile(ManagedFile file, CatalogRecord record, ApplicationDbContext db)
        {
            var tasks = db.TaskStatuses.Where(x => x.File.Id == file.Id).ToList();
            foreach (var task in tasks)
            {
                task.IsComplete = false;
                task.CompletedBy = null;
                task.CompletedDate = null;
            }

        }
    }
}
