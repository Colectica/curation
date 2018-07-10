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
using System.Text;
using System.Threading.Tasks;
using Colectica.Curation.Data;
using Colectica.Curation.Web.Models;
using System.Data.Entity;
using Colectica.Curation.Operations;

namespace Colectica.Curation.Web.Utility
{
    public class FileStatusHelper
    {
        public static FileStatusModel GetFileStatusModel(ManagedFile file, ApplicationDbContext db)
        {
            var model = new FileStatusModel();
            model.File = file;
            model.FileName = file.Name;

            var tasks = db.TaskStatuses
                .Where(x => x.File.Id == file.Id)
                .Include(x => x.CompletedBy)
                .OrderBy(x => x.Weight);

            foreach (var task in tasks)
            {
                var taskModel = new FileTaskModel()
                {
                    TaskType = task.Name,
                    CompletedDate = task.CompletedDate.HasValue ? task.CompletedDate.Value.ToShortDateString() : string.Empty,
                    IsComplete = task.IsComplete
                };

                var taskDefinition = MefConfig.AddinManager.AllTasks.Where(x => x.Id == task.TaskId).FirstOrDefault();
                if (taskDefinition != null)
                {
                    taskModel.Controller = taskDefinition.Controller;
                }

                if (task.CompletedBy != null)
                {
                    taskModel.CuratorName = task.CompletedBy.UserName;
                    taskModel.CuratorId = task.CompletedBy.Id;
                }

                model.Tasks.Add(taskModel);
            }

            return model;
        }
    }
}
