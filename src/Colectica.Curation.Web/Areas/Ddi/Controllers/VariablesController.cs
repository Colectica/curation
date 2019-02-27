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
using Colectica.Curation.Web.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using Colectica.Curation.Addins.Editors.Mappers;
using Algenta.Colectica.Repository;
using Algenta.Colectica.Model.Ddi;
using Colectica.Curation.Addins.Editors.Models;
using Algenta.Colectica.Model.Repository;
using Colectica.Curation.Web.Models;
using Colectica.Curation.Web.Areas.Ddi.Models;
using Colectica.Curation.Common.ViewModels;
using Colectica.Curation.Operations;
using Colectica.Curation.Web.Controllers;
using Colectica.Curation.Web.Areas.Ddi.Utility;
using Algenta.Colectica.Model.Utility;
using Colectica.Curation.Common.Utility;

namespace Colectica.Curation.Addins.Editors.Controllers
{
    [Authorize]
    public class VariablesController : CurationControllerBase
    {
        [Route("Variables/Editor/{id}")]
        public ActionResult Editor(Guid id)
        {
            using (var db = ApplicationDbContext.Create())
            {
                var file = GetFile(id, db);

                try
                {
                    var model = FileToVariableEditorMapper.GetModelFromFile(file);

                    var user = db.Users
                        .Where(x => x.UserName == User.Identity.Name)
                        .FirstOrDefault();
                    var permissions = db.Permissions
                        .Where(x => x.User.Id == user.Id && x.Organization.Id == file.CatalogRecord.Organization.Id);
                    bool userCanViewAll = permissions.Any(x => x.Right == Right.CanViewAllCatalogRecords);
                    string createdByUserName = file.CatalogRecord.CreatedBy != null ? file.CatalogRecord.CreatedBy.UserName : string.Empty;
                    bool isUserCreator = createdByUserName == User.Identity.Name;

                    model.IsUserCurator = file.CatalogRecord.Curators.Any(x => x.UserName == User.Identity.Name);
                    bool isApprover = file.CatalogRecord.Approvers.Any(x => x.UserName == User.Identity.Name ||
                        OrganizationHelper.DoesUserHaveRight(db, User, file.CatalogRecord.Organization.Id, Right.CanApprove));

                    if (!isUserCreator &&
                        !model.IsUserCurator &&
                        !isApprover &&
                        !userCanViewAll)
                    {
                        throw new HttpException(403, "You must be a curator or an administrator to perform this action.");
                    }

                    return View("~/Areas/Ddi/Views/Variables/Editor.cshtml", model);
                }
                catch (InvalidOperationException ex)
                {
                    var model = new MissingPhysicalInstanceModel()
                    {
                        File = file,
                        IsLocked = file.CatalogRecord.IsLocked,
                        IsUserCurator = file.CatalogRecord.Curators.Any(x => x.UserName == User.Identity.Name),
                        IsUserApprover = file.CatalogRecord.Approvers.Any(x => x.UserName == User.Identity.Name) ||
                            OrganizationHelper.DoesUserHaveRight(db, User, file.CatalogRecord.Organization.Id, Right.CanApprove),
                        OperationStatus = file.CatalogRecord.OperationStatus
                    };

                    return View("~/Areas/Ddi/Views/Variables/MissingPhysicalInstance.cshtml", model);
                }
            }
        }

        [HttpPost]
        [Route("Variables/Editor/AddNote/{id}")]
        public ActionResult AddNote(Guid id, Guid fileId, string note)
        {
            if (string.IsNullOrWhiteSpace(note))
            {
                return Content(string.Empty);
            }

            using (var db = ApplicationDbContext.Create())
            {
                var file = GetFile(fileId, db);

                var user = db.Users.Where(x => x.UserName == User.Identity.Name)
                    .FirstOrDefault();

                if (!file.CatalogRecord.Curators.Any(x => x.UserName == User.Identity.Name) &&
                    !file.CatalogRecord.Approvers.Any(x => x.UserName == User.Identity.Name) &&
                    !OrganizationHelper.DoesUserHaveRight(db, User, file.CatalogRecord.Organization.Id, Right.CanApprove))
                {
                    throw new HttpException(403, "Only curators may perform this task.");
                }

                string agency = file.CatalogRecord.Organization.AgencyID;

                try
                {
                    var client = RepositoryHelper.GetClient();
                    var variable = client.GetLatestItem(id, agency) as Variable;

                    var noteObj = new Data.Note()
                    {
                        CatalogRecord = file.CatalogRecord,
                        File = file,
                        Timestamp = DateTime.UtcNow,
                        User = user,
                        Text = note,
                        VariableAgency = variable.AgencyId,
                        VariableId = variable.Identifier,
                        VariableName = variable.ItemName.Best
                    };
                    db.Notes.Add(noteObj);

                    // Log the adding of the note.
                    var log = new Event()
                    {
                        EventType = EventTypes.AddNote,
                        Timestamp = DateTime.UtcNow,
                        User = user,
                        RelatedCatalogRecord = file.CatalogRecord,
                        Title = "Add a Note",
                        Details = note
                    };
                    log.RelatedManagedFiles.Add(file);

                    db.Events.Add(log);

                    db.SaveChanges();

                    var comment = new CommentModel()
                    {
                        UserName = User.Identity.Name,
                        Date = noteObj.Timestamp.ToShortDateString(),
                        Comment = noteObj.Text
                    };
                    return Json(comment);
                }
                catch (Exception ex)
                {
                    throw new HttpException(500, ex.Message);
                }
            }
        }

        public ActionResult Details(Guid id, string agency, Guid fileId)
        {
            var client = RepositoryHelper.GetClient();

            // Basic variable details.
            var variable = client.GetLatestItem(id, agency) as Variable;
            if (variable.CodeRepresentation != null &&
                variable.CodeRepresentation.Codes != null)
            {
                client.PopulateItem(variable.CodeRepresentation.Codes, false, ChildReferenceProcessing.PopulateLatest);
            }

            var model = new VariableModel()
            {
                Id = variable.Identifier.ToString(),
                Agency = variable.AgencyId,
                Name = variable.ItemName.Current,
                Label = variable.Label.Current,
                Description = variable.Description.Current,
                ResponseUnit = variable.ResponseUnit,
                AnalysisUnit = variable.AnalysisUnit,
                ClassificationLevel = variable.ActiveRepresentation != null ? variable.ActiveRepresentation.ClassificationLevel.ToString() : string.Empty,
                Version = variable.Version,
                LastUpdated = variable.VersionDate.ToShortDateString()
            };

            if (variable.RepresentationType == RepresentationType.Text)
            {
                model.RepresentationType = "Text";
            }
            else if (variable.RepresentationType == RepresentationType.Numeric)
            {
                model.RepresentationType = "Numeric";
            }
            else if (variable.RepresentationType == RepresentationType.Code)
            {
                model.RepresentationType = "Code";
            }


            // Summary statistics.
            var facet = new GraphSearchFacet();
            facet.TargetItem = variable.CompositeId;
            facet.UseDistinctTargetItem = true;
            facet.ItemTypes.Add(DdiItemType.VariableStatistic);
            var statsId = client.GetRelationshipByObject(facet).FirstOrDefault();
            if (statsId != null)
            {
                var stats = client.GetItem(statsId.CompositeId, ChildReferenceProcessing.Populate) as VariableStatistic;

                model.Valid = stats.Valid;
                model.Invalid = stats.Invalid;
                model.Minimum = stats.Minimum;
                model.Maximum = stats.Maximum;

                model.Mean = Round(variable, stats.Mean);
                model.StandardDeviation = Round(variable, stats.StandardDeviation);

                foreach (var catStat in stats.UnfilteredCategoryStatistics)
                {
                    var freqModel = new FrequencyModel();
                    freqModel.CategoryValue = catStat.CategoryValue;
                    freqModel.Frequency = catStat.Frequency;
                    SetIdAndLabelForCategory(catStat.CategoryValue, variable, freqModel);
                    model.Frequencies.Add(freqModel);
                }

                // Calculate bar widths.
                double maxWidth = 200;
                if (model.Frequencies.Count > 0)
                {
                    double maxFrequency = model.Frequencies.Select(x => x.Frequency).Max();
                    foreach (var freq in model.Frequencies)
                    {
                        double w = freq.Frequency * maxWidth / maxFrequency;
                        freq.NormalizedWidth = ((int)w).ToString() + "px";
                    }
                }
            }


            // Comments
            using (var db = ApplicationDbContext.Create())
            {
                var comments = db.Notes.Where(x => x.File.Id == fileId &&
                        x.VariableAgency == agency &&
                        x.VariableId == id)
                    .Include(x => x.User);

                foreach (var comment in comments)
                {
                    model.Comments.Add(new CommentModel
                    {
                        UserName = comment.User.UserName,
                        Date = comment.Timestamp.ToShortDateString(),
                        Comment = comment.Text
                    });
                }
            }

            string json = System.Web.Helpers.Json.Encode(model);
            return Content(json);
        }

        void SetIdAndLabelForCategory(string categoryValue, Variable variable, FrequencyModel freqModel)
        {
            if (variable.CodeRepresentation == null ||
                variable.CodeRepresentation.Codes == null)
            {
                return;
            }

            var category = variable.CodeRepresentation.Codes.Codes
                .Where(x => x.Value == categoryValue)
                .Select(x => x.Category)
                .FirstOrDefault();

            if (category != null)
            {
                freqModel.CategoryId = category.Identifier;
                freqModel.CategoryLabel = category.Label.Best;
            }
        }

        [HttpPost]
        public ActionResult Update(Guid pk, string agency, string name, string value)
        {
            using (var db = ApplicationDbContext.Create())
            {
                try
                {
                    // Fetch the appropriate variable by ID.
                    var client = RepositoryHelper.GetClient();
                    var variable = client.GetLatestItem(pk, agency) as Variable;

                    // Get all related items that will have their versions 
                    // increased, because they reference the variable.
                    // That is: PhysicalInstance, DataRelationship, and VariableStatistic.
                    PhysicalInstance physicalInstance;
                    DataRelationship dataRelationship;
                    VariableStatistic variableStatistic;
                    GetReferencingItems(client, variable.CompositeId, out physicalInstance, out dataRelationship, out variableStatistic);

                    // Copy information from the POST to the Variable.
                    VariableMapper.UpdateVariableProperty(variable, physicalInstance, dataRelationship, variableStatistic, name, value);

                    // Save the new version of the variable.
                    VersionUpdater.UpdateVersionsAndSave(variable, physicalInstance, dataRelationship, variableStatistic);

                    var file = db.Files.Where(x => x != null && physicalInstance != null && x.Id == physicalInstance.Identifier)
                        .FirstOrDefault();
                    if (file != null &&
                        file.IsStataDataFile())
                    {
                        file.HasPendingMetadataUpdates = true;
                        db.SaveChanges();
                    }


                    // For x-editable, just a simple 200 return is sufficient if things went well.
                    return Content(string.Empty);
                }
                catch (Exception ex)
                {
                    throw new HttpException(500, ex.Message, ex);
                }
            }
        }

        void GetReferencingItems(RepositoryClientBase client, IdentifierTriple variableId, out PhysicalInstance physicalInstance, out DataRelationship dataRelationship, out VariableStatistic variableStatistic)
        {
            dataRelationship = null;
            variableStatistic = null;

            physicalInstance = VariableMapper.GetPhysicalInstanceWithVariable(variableId, client);

            foreach (var dr in physicalInstance.DataRelationships)
            {
                client.PopulateItem(dr, false, ChildReferenceProcessing.Populate);

                if (dr.LogicalRecords
                    .SelectMany(x => x.VariablesInRecord)
                    .Any(x => x.CompositeId == variableId))
                {
                    dataRelationship = dr;
                }
            }


            foreach (var stats in physicalInstance.Statistics)
            {
                client.PopulateItem(stats);

                if (stats.AgencyId == variableId.AgencyId &
                    stats.VariableReference.Identifier == variableId.Identifier)
                {
                    variableStatistic = stats;
                    break;
                }
            }
        }

        [HttpPost]
        public ActionResult UpdateCategory(Guid pk, Guid physicalInstanceId, string agency, string name, string value)
        {
            try
            {
                var client = RepositoryHelper.GetClient();
                var category = client.GetLatestItem(pk, agency) as Category;

                CategoryMapper.UpdateCategoryProperty(category, name, value);

                category.Version++;
                client.RegisterItem(category, new CommitOptions());


                using (var db = ApplicationDbContext.Create())
                {
                    var file = db.Files.Where(x => x.Id == physicalInstanceId)
                        .FirstOrDefault();
                    if (file != null &&
                        file.IsStataDataFile())
                    {
                        file.HasPendingMetadataUpdates = true;
                        db.SaveChanges();
                    }
                }

                // For x-editable, just a simple 200 return is sufficient if things went well.
                return Content(string.Empty);
            }
            catch (Exception ex)
            {
                throw new HttpException(500, ex.Message);
            }
        }

        [HttpPost]
        public ActionResult CreatePhysicalInstance(Guid id)
        {
            using (var db = ApplicationDbContext.Create())
            {
                var file = GetFile(id, db);
                var record = file.CatalogRecord;

                var user = db.Users.Where(x => x.UserName == User.Identity.Name).FirstOrDefault();
                var permissions = db.Permissions
                    .Where(x => x.User.Id == user.Id && x.Organization.Id == record.Organization.Id);
                bool userCanViewAll = permissions.Any(x => x.Right == Right.CanViewAllCatalogRecords);

                if (!record.Curators.Any(x => x.UserName == User.Identity.Name) &&
                    !record.Approvers.Any(x => x.UserName == User.Identity.Name) &&
                    !OrganizationHelper.DoesUserHaveRight(db, User, file.CatalogRecord.Organization.Id, Right.CanApprove) &&
                    !userCanViewAll)
                {
                    throw new HttpException(403, "Only curators and administrators can perform this action.");
                }


                // Add an operation to make the PhysicalInstance.
                var operation = new CreatePhysicalInstances()
                {
                    Name = "Creating variable-level metadata" + (string.IsNullOrWhiteSpace(record.Title) ? "" : " for " + record.Title),
                    UserId = user.Id,
                    CatalogRecordId = record.Id,
                    ProcessingDirectory = SettingsHelper.GetProcessingDirectory(record.Organization, db)
                };

                bool queued = db.Enqueue(record.Id, user.Id, operation);

                db.SaveChanges();

                return RedirectToAction("Files", "CatalogRecord", new { id = file.CatalogRecord.Id });
            }
        }

        double? Round(Variable variable, double? value)
        {
            if (!value.HasValue)
            {
                return null;
            }

            int decimals = 2;

            if (variable.NumericRepresentation != null &&
                variable.NumericRepresentation.DecimalPositions.HasValue)
            {
                decimals = variable.NumericRepresentation.DecimalPositions.Value;
            }

            return Math.Round(value.Value, decimals);
        }

        internal ManagedFile GetFile(Guid id, ApplicationDbContext db)
        {
            if (id == Guid.Empty)
            {
                throw new HttpException(400, "Bad Request");
            }

            var file = db.Files
                .Where(x => x.Id == id)
                .Include(x => x.CatalogRecord)
                .Include(x => x.CatalogRecord.Curators)
                .Include(x => x.CatalogRecord.Approvers)
                .Include(x => x.CatalogRecord.Organization)
                .FirstOrDefault();

            if (file == null)
            {
                throw new HttpException(404, "Not Found");
            }

            return file;
        }

    }
}
