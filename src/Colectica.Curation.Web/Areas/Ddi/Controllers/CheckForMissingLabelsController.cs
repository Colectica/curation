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
using Colectica.Curation.Addins.Tasks;
using Colectica.Curation.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using Colectica.Curation.Web.Models;
using Colectica.Curation.Addins.Editors.Mappers;
using Colectica.Curation.Web.Areas.Ddi.Utility;
using Newtonsoft.Json;
using Algenta.Colectica.Model.Repository;
using Algenta.Colectica.Repository;
using Colectica.Curation.Web.Controllers;

namespace Colectica.Curation.Addins.Editors.Controllers
{
    [Authorize]
    public class CheckForMissingLabelsController : CurationControllerBase
    {
        CheckForMissingLabelsTask task = new CheckForMissingLabelsTask();

        public ActionResult Details(Guid id)
        {
            using (var db = ApplicationDbContext.Create())
            {
                var model = new CheckForMissingLabelsViewModel();
                TaskHelpers.InitializeTaskModel(model, id, this.task, db);

                if (!model.File.CatalogRecord.Curators.Any(x => x.UserName == User.Identity.Name))
                {
                    throw new HttpException(403, "Only curators may perform this task.");
                }

                // Find and add any missing labels.
                try
                {
                    var physicalInstance = FileToVariableEditorMapper.GetPhysicalInstance(model.File, model.File.CatalogRecord.Organization.AgencyID);
                    var variablesWithMissingLabels = new List<VariableModel>();

                    var client = RepositoryHelper.GetClient();
                    var populator = new GraphPopulator(client);
                    populator.ChildProcessing = ChildReferenceProcessing.PopulateLatest;

                    physicalInstance.Accept(populator);

                    foreach (var variable in physicalInstance.DataRelationships
                        .SelectMany(x => x.LogicalRecords)
                        .SelectMany(x => x.VariablesInRecord))
                    {
                        bool isUnlabeled = false;

                        // Check if the variable is unlabeled.
                        if (variable.Label.IsEmpty)
                        {
                            isUnlabeled = true;
                        }

                        // Check if any categories are unlabeled.
                        if (variable.CodeRepresentation != null &&
                            variable.CodeRepresentation.Codes != null &&
                            variable.CodeRepresentation.Codes.Codes.Any(x => x.Category == null || x.Category.Label.IsEmpty))
                        {
                            isUnlabeled = true;
                        }

                        if (isUnlabeled)
                        {
                            var variableModel = new VariableModel
                            {
                                Id = variable.Identifier.ToString(),
                                Agency = variable.AgencyId,
                                Name = variable.ItemName.Current,
                                Label = variable.Label.Current,
                                Version = variable.Version,
                                LastUpdated = variable.VersionDate.ToShortDateString()
                            };

                            variablesWithMissingLabels.Add(variableModel);
                        }
                    }

                    model.VariablesJson = JsonConvert.SerializeObject(variablesWithMissingLabels);

                    return View("~/Areas/Ddi/Views/CheckForMissingLabels/Details.cshtml", model);
                }
                catch (Exception ex)
                {
                    throw new HttpException(500, "Could not retrieve the PhysicalInstance for a file", ex);
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
