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

﻿using Algenta.Colectica.Model.Ddi;
using Algenta.Colectica.Repository;
using Colectica.Curation.Addins.Editors.Models;
using Colectica.Curation.Addins.Tasks;
using Colectica.Curation.Data;
using Colectica.Curation.DdiAddins.Utility;
using Colectica.Curation.Models;
using Colectica.Curation.Web.Areas.Ddi.Utility;
using Colectica.Curation.Web.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Colectica.Curation.Web.Areas.Ddi.Controllers
{
    [Authorize]
    public class CheckObservationCountController : CurationControllerBase
    {
        CheckObservationCountTask task = new CheckObservationCountTask();

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

                // Add information for the observation count.
                try
                {
                    // Get the DDI PhysicalInstance that corresponds to this ManagedFile.
                    var client = RepositoryHelper.GetClient();
                    var physicalInstance = client.GetLatestItem(
                        model.File.Id, 
                        model.File.CatalogRecord.Organization.AgencyID) as PhysicalInstance;

                    if (physicalInstance.FileStructure != null &&
                        physicalInstance.FileStructure.CaseQuantity.HasValue)
                    {
                        model.ObservationCount = physicalInstance.FileStructure.CaseQuantity.Value;
                    }

                    return View("~/Areas/Ddi/Views/CheckObservationCount/Details.cshtml", model);
                }
                catch (Exception)
                {
                    return View("~/Areas/Ddi/Views/CheckObservationCount/Details.cshtml", model);
                }
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
