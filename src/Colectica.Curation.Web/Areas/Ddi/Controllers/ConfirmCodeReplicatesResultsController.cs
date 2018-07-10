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

﻿using Colectica.Curation.Addins.Editors.Models;
using Colectica.Curation.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using Colectica.Curation.Web.Models;
using Colectica.Curation.Addins.Tasks;
using Colectica.Curation.Web.Areas.Ddi.Utility;
using Colectica.Curation.Web.Controllers;
using Colectica.Curation.Models;

namespace Colectica.Curation.Addins.Editors.Controllers
{
    [Authorize]
    public class ConfirmCodeReplicatesResultsController : CurationControllerBase
    {
        ConfirmCodeReplicatesResultsTask task = new ConfirmCodeReplicatesResultsTask();

        public ActionResult Details(Guid id)
        {
            using (var db = ApplicationDbContext.Create())
            {
                var model = new ScriptedTaskModel();
                TaskHelpers.InitializeTaskModel(model, id, this.task, db);

                if (!model.File.CatalogRecord.Curators.Any(x => x.UserName == User.Identity.Name))
                {
                    throw new HttpException(403, "Only curators may perform this task.");
                }

                model.IsUserCurator = true;

                return View("~/Areas/Ddi/Views/ConfirmCodeReplicatesResults/Details.cshtml", model);
            }
        }

        [HttpPost]
        public ActionResult Details(Guid id, FormCollection form)
        {
            using (var db = ApplicationDbContext.Create())
            {
                var file = TaskHelpers.UpdateStatus(id, this.task, User, form, db);
                return RedirectToAction("Status", "File", new { id = file.Id });
            }
        }
    }
}
