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

using AutoMapper;
using Colectica.Curation.Common.Utility;
using Colectica.Curation.Common.ViewModels;
using Colectica.Curation.Data;
using Colectica.Curation.Models;
using Colectica.Curation.Operations;
using Colectica.Curation.Web.Areas.Ddi.Utility;
using Colectica.Curation.Web.Models;
using Colectica.Curation.Web.Utility;
using log4net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;

namespace Colectica.Curation.Web.Controllers
{
    [Authorize]
    public class CatalogRecordController : CurationControllerBase
    {
        public ActionResult Index()
        {
            using (var db = ApplicationDbContext.Create())
            {
                var thisUser = db.Users.Where(x => x.UserName == User.Identity.Name)
                    .Include(x => x.Organizations)
                    .FirstOrDefault();

                List<CatalogRecordIndexMemberModel> allRecords = new List<CatalogRecordIndexMemberModel>();

                var org = OrganizationHelper.GetOrganizationByHost(Request, db);
                if (org == null)
                {
                    return RedirectToAction("Index", "Dashboard");
                }

                bool canUserViewAll = OrganizationHelper.DoesUserHaveRight(db, User, org.Id, Right.CanViewAllCatalogRecords);

                List<CatalogRecordIndexMemberModel> orgRecords = null;

                if (canUserViewAll)
                {
                    orgRecords = db.CatalogRecords
                        .OrderBy(x => x.Title)
                        .Include(x => x.TaskStatuses)
                        .Include("TaskStatuses.CompletedBy")
                        .Where(x => x.Organization.Id == org.Id)
                        .Select(x => new CatalogRecordIndexMemberModel
                        {
                            CatalogRecord = x,
                            CuratorCount = x.Curators.Count()
                        })
                        .ToList();
                }
                else
                {
                    // Only get the records for which the user is a curator or depositor or author.
                    orgRecords = db.CatalogRecords
                        .OrderBy(x => x.Title)
                        .Include(x => x.TaskStatuses)
                        .Include("TaskStatuses.CompletedBy")
                        .Where(x => x.Organization.Id == org.Id)
                        .Where(x =>
                            x.CreatedBy.UserName == thisUser.UserName ||
                            x.Owner.UserName == thisUser.UserName ||
                            x.Curators.Any(u => u.UserName == thisUser.UserName) ||
                            x.Authors.Any(u => u.UserName == thisUser.UserName))
                        .Select(x => new CatalogRecordIndexMemberModel
                        {
                            CatalogRecord = x,
                            CuratorCount = x.Curators.Count()
                        })
                        .ToList();
                }

                allRecords.AddRange(orgRecords);

                var statuses = GetPipeline(allRecords);
                return View(statuses);
            }
        }

        public ActionResult Locked()
        {
            using (var db = ApplicationDbContext.Create())
            {
                var thisUser = db.Users.Where(x => x.UserName == User.Identity.Name)
                    .Include(x => x.Organizations)
                    .FirstOrDefault();

                var org = OrganizationHelper.GetOrganizationByHost(Request, db);
                if (org == null)
                {
                    return RedirectToAction("Index", "Dashboard");
                }

                bool canUserViewAll = OrganizationHelper.DoesUserHaveRight(db, User, org.Id, Right.CanViewAllCatalogRecords);
                if (!canUserViewAll)
                {
                    throw new HttpException(500, "Only administrators can view locked records");
                }

                List<CatalogRecordIndexMemberModel> allLockedRecords = new List<CatalogRecordIndexMemberModel>();

                if (canUserViewAll)
                {
                    allLockedRecords = db.CatalogRecords
                        .OrderBy(x => x.Title)
                        .Include(x => x.TaskStatuses)
                        .Include("TaskStatuses.CompletedBy")
                        .Where(x => x.Organization.Id == org.Id)
                        .Where(x => x.OperationLockId.HasValue)
                        .Select(x => new CatalogRecordIndexMemberModel
                        {
                            CatalogRecord = x,
                            CuratorCount = x.Curators.Count()
                        })
                        .ToList();
                }


                var statuses = GetPipeline(allLockedRecords);
                return View("Index", statuses);
            }

        }

        public ActionResult WaitForUnlock(Guid id, string returnUrl)
        {
            using (var db = ApplicationDbContext.Create())
            {
                var record = GetRecord(id, db);
                if (record == null)
                {
                    throw new HttpException(404, "NotFound");
                }

                var model = new WaitForLockedRecordModel()
                {
                    CatalogRecordId = id,
                    CatalogRecordName = record.Title,
                    Message = record.OperationStatus,
                    ReturnUrl = returnUrl
                };

                return View(model);
            }
        }

        public ActionResult IsUnlocked(Guid id)
        {
            using (var db = ApplicationDbContext.Create())
            {
                var record = GetRecord(id, db);
                if (record == null)
                {
                    throw new HttpException(404, "NotFound");
                }

                if (record.IsLocked)
                {
                    return Content("false");
                }
                else
                {
                    return Content("true");
                }
            }
        }

        public ActionResult Create()
        {
            using (var db = ApplicationDbContext.Create())
            {
                var model = new CatalogRecordGeneralViewModel();

                var org = OrganizationHelper.GetOrganizationByHost(Request, db);
                model.Organization = org?.Name;

                return View("General", model);
            }
        }

        public ActionResult General(Guid id)
        {
            var logger = LogManager.GetLogger("CatalogRecordController");

            using (var db = ApplicationDbContext.Create())
            {
                var record = GetRecord(id, db);

                if (record.IsLocked)
                {
                    return RedirectToAction("WaitForUnlock", new { id = record.Id, returnUrl = Request.Path });
                }

                EnsureUserIsAllowed(record, db);

                var model = new CatalogRecordGeneralViewModel(record);

                // Set authors in the order from AuthorsText
                var authorModels = new List<UserSearchResultModel>();
                
                if (!string.IsNullOrWhiteSpace(record.AuthorsText))
                {
                    // Parse AuthorsText to get the order of full names
                    var authorFullNames = record.AuthorsText.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => x.Trim())
                        .ToList();
                    
                    // Create a dictionary for quick lookup of authors by full name
                    var authorsByFullName = record.Authors
                        .ToDictionary(a => a.FullName.Trim(), a => a);
                    
                    // Add authors in the order they appear in AuthorsText
                    foreach (var fullName in authorFullNames)
                    {
                        if (authorsByFullName.TryGetValue(fullName, out var author))
                        {
                            var authorModel = new UserSearchResultModel();
                            Mapper.Map(author, authorModel);
                            authorModels.Add(authorModel);
                        }
                    }
                }

                model.Authors = JsonConvert.SerializeObject(authorModels);

                // HACK?
                model.Authors = model.Authors.Replace("\\", "\\\\");

                // Initialize available Keywords.
                var allKeywords = db.CatalogRecords
                    .Select(x => x.Keywords)
                    .Distinct()
                    .Take(20);
                foreach (var combined in allKeywords)
                {
                    if (combined == null)
                    {
                        continue;
                    }

                    string[] keywords = combined.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string keyword in keywords)
                    {
                        model.AvailableKeywords.Add(keyword);
                    }
                }


                model.CanAssignCurators = OrganizationHelper.DoesUserHaveRight(db, User, record.Organization.Id, Right.CanAssignCurator);
                model.TaskCount = record.TaskStatuses.Where(x => !x.IsComplete).Count();
                model.IsUserCurator = record.Curators.Any(x => x.UserName == User.Identity.Name);
                model.IsUserApprover = record.Approvers.Any(x => x.UserName == User.Identity.Name) ||
                    OrganizationHelper.DoesUserHaveRight(db, User, record.Organization.Id, Right.CanApprove);
                model.CuratorCount = record.Curators.Count;

                logger.Debug($"IsCurator: {model.IsUserCurator}");
                logger.Debug($"IsApprover: {model.IsUserApprover}");

                return View(model);
            }
        }

        public ActionResult Methods(Guid id)
        {
            using (var db = ApplicationDbContext.Create())
            {
                var record = GetRecord(id, db);

                if (record.IsLocked)
                {
                    return RedirectToAction("WaitForUnlock", new { id = record.Id, returnUrl = Request.Path });
                }


                EnsureUserIsAllowed(record, db);

                var model = new CatalogRecordMethodsViewModel(record);

                // Initialize available OutcomeMeasures.
                var allOutcomeMeasures = db.CatalogRecords
                    .Select(x => x.OutcomeMeasures)
                    .Distinct()
                    .Take(20);
                foreach (var combined in allOutcomeMeasures)
                {
                    if (combined == null)
                    {
                        continue;
                    }

                    string[] outcomes = combined.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string outcome in outcomes)
                    {
                        model.AvailableOutcomeMeasures.Add(outcome);
                    }
                }


                model.CanAssignCurators = OrganizationHelper.DoesUserHaveRight(db, User, record.Organization.Id, Right.CanAssignCurator);
                model.TaskCount = record.TaskStatuses.Where(x => !x.IsComplete).Count();
                model.IsUserCurator = record.Curators.Any(x => x.UserName == User.Identity.Name);
                model.IsUserApprover = record.Approvers.Any(x => x.UserName == User.Identity.Name) ||
                    OrganizationHelper.DoesUserHaveRight(db, User, record.Organization.Id, Right.CanApprove);

                return View(model);
            }
        }

        public ActionResult Files(Guid id)
        {
            if (id == Guid.Empty)
            {
                throw new HttpException(400, "Bad Request");
            }

            using (var db = ApplicationDbContext.Create())
            {
                var record = db.CatalogRecords
                    .Where(x => x.Id == id)
                    .Include(x => x.Files)
                    .Include(x => x.TaskStatuses)
                    .Include(x => x.Curators)
                    .Include(x => x.Approvers)
                    .Include(x => x.CreatedBy)
                    .FirstOrDefault();

                if (record == null)
                {
                    throw new HttpException(404, "Not Found");
                }

                if (record.IsLocked)
                {
                    return RedirectToAction("WaitForUnlock", new { id = record.Id, returnUrl = Request.Path });
                }


                EnsureUserIsAllowed(record, db);

                var model = new CatalogRecordFilesViewModel(record);

                model.CanAssignCurators = OrganizationHelper.DoesUserHaveRight(db, User, record.Organization.Id, Right.CanAssignCurator);
                model.TaskCount = record.TaskStatuses.Where(x => !x.IsComplete).Count();
                model.IsUserCurator = record.Curators.Any(x => x.UserName == User.Identity.Name);
                model.IsUserApprover = record.Approvers.Any(x => x.UserName == User.Identity.Name) ||
                    OrganizationHelper.DoesUserHaveRight(db, User, record.Organization.Id, Right.CanApprove);

                // Add a VM for each file.
                foreach (var file in record.Files.Where(x => x.Status != FileStatus.Rejected))
                {
                    var vm = new FileViewModel(file);
                    vm.Id = file.Id;
                    vm.Name = file.Name;
                    vm.CatalogRecordName = record.Title;
                    vm.Type = file.Type;
                    vm.Version = file.Version.ToString();
                    vm.Status = file.Status;

                    vm.TaskStatus = FileStatusHelper.GetFileStatusModel(file, db);

                    vm.IsUserCurator = model.IsUserCurator;
                    vm.IsUserApprover = model.IsUserApprover;
                    vm.TaskStatus.IsUserCurator = model.IsUserCurator;
                    vm.TaskStatus.IsUserApprover = model.IsUserApprover;

                    model.Files.Add(vm);
                }

                return View(model);
            }
        }

        public ActionResult Deposit(Guid id)
        {
            using (var db = ApplicationDbContext.Create())
            {
                var record = db.CatalogRecords
                    .Where(x => x.Id == id)
                    .Include(x => x.Organization)
                    .Include(x => x.Authors)
                    .Include(x => x.Files)
                    .Include(x => x.CreatedBy)
                    .Include(x => x.Curators)
                    .Include(x => x.Approvers)
                    .FirstOrDefault();

                if (record == null)
                {
                    throw new HttpException(404, "Not Found");
                }

                if (record.IsLocked)
                {
                    return RedirectToAction("WaitForUnlock", new { id = record.Id, returnUrl = Request.Path });
                }


                EnsureUserIsAllowed(record, db);

                var model = new DepositFilesViewModel(record);

                // Make a list of existing file names.
                foreach (var file in record.Files.Where(x => x.Status != FileStatus.Removed))
                {
                    model.AvailableExistingFileNames.Add(file.Name);
                }

                // Set the deposit agreement using the first organization.
                // TODO What if there is more than one organization for the user?
                // In this case, the deposit agreement should dynamically update based
                // on which organization is selected.
                if (record.Organization != null)
                {
                    model.DepositAgreement = record.Organization.DepositAgreement;
                    model.TermsOfService = record.Organization.TermsOfService;
                    model.Policy = record.Organization.OrganizationPolicy;
                }

                model.CanAssignCurators = OrganizationHelper.DoesUserHaveRight(db, User, record.Organization.Id, Right.CanAssignCurator);
                model.TaskCount = record.TaskStatuses.Where(x => !x.IsComplete).Count();
                model.IsUserCurator = record.Curators.Any(x => x.UserName == User.Identity.Name);
                model.IsUserApprover = record.Approvers.Any(x => x.UserName == User.Identity.Name) ||
                    OrganizationHelper.DoesUserHaveRight(db, User, record.Organization.Id, Right.CanApprove);

                return View(model);
            }
        }

        public ActionResult Submit(Guid id)
        {
            using (var db = ApplicationDbContext.Create())
            {
                var record = GetRecord(id, db);

                if (record.IsLocked)
                {
                    return RedirectToAction("WaitForUnlock", new { id = record.Id, returnUrl = Request.Path });
                }


                EnsureUserIsAllowed(record, db);

                var model = new CatalogRecordSubmitViewModel(record);

                // Set the deposit agreement using the first organization.
                // TODO What if there is more than one organization for the user?
                // In this case, the deposit agreement should dynamically update based
                // on which organization is selected.
                if (record.Organization != null)
                {
                    model.DepositAgreement = record.Organization.DepositAgreement;
                    model.TermsOfService = record.Organization.TermsOfService;
                    model.Policy = record.Organization.OrganizationPolicy;
                }

                if (model.DepositAgreement == null)
                {
                    model.DepositAgreement = "";
                }

                // Check for required properties.
                model.IsOkayToSubmit = true;

                var urlHelper = new UrlHelper(Request.RequestContext);
                string generalUrl = urlHelper.Action("General", "CatalogRecord", new { id = record.Id });
                string methodsUrl = urlHelper.Action("Methods", "CatalogRecord", new { id = record.Id });

                if (record.Status == CatalogRecordStatus.New ||
                    record.Status == CatalogRecordStatus.Processing)
                {

                    RequireContent(record.Title, new RequiredInformationModel(generalUrl, "Title"), model);

                    if (record.Authors.Count == 0 &&
                        string.IsNullOrWhiteSpace(record.AuthorsText))
                    {
                        model.IsOkayToSubmit = false;
                        string url = urlHelper.Action("General", "CatalogRecord", new { id = record.Id });
                        model.Messages.Add(new RequiredInformationModel(url, "Authors"));
                    }

                    RequireContent(record.Description, new RequiredInformationModel(generalUrl, "Description"), model);
                    RequireContent(record.Keywords, new RequiredInformationModel(generalUrl, "Keywords"), model);
                    RequireContent(record.Funding, new RequiredInformationModel(generalUrl, "Funding"), model);
                    RequireContent(record.AccessStatement, new RequiredInformationModel(generalUrl, "Access Statement"), model);
                    RequireContent(record.ConfidentialityStatement, new RequiredInformationModel(generalUrl, "Confidentiality Statement"), model);
                    RequireContent(record.EmbargoStatement, new RequiredInformationModel(generalUrl, "Embargo Statement"), model);

                    RequireContent(record.ResearchDesign, new RequiredInformationModel(methodsUrl, "Research Design"), model);
                    RequireContent(record.ModeOfDataCollection, new RequiredInformationModel(methodsUrl, "Mode of Data Collection"), model);

                    var methodsModel = new CatalogRecordMethodsViewModel(record);
                    RequireContent(methodsModel.FieldDates, new RequiredInformationModel(methodsUrl, "Field Dates"), model);
                    RequireContent(methodsModel.StudyTimePeriod, new RequiredInformationModel(methodsUrl, "Study Time Period"), model);

                    RequireContent(record.Location, new RequiredInformationModel(methodsUrl, "Location"), model);

                    if (record.Files.Where(x => x.Status != FileStatus.Removed).Count() == 0)
                    {
                        model.IsOkayToSubmit = false;
                        string url = urlHelper.Action("Files", "CatalogRecord", new { id = record.Id });
                        model.Messages.Add(new RequiredInformationModel(url, "Files"));
                    }
                }

                // Moving from Processing to PublicationRequested requires some additional information.
                if (record.Status == CatalogRecordStatus.Processing)
                {
                    if (record.TermsOfUse == "Custom Dataset Terms")
                    {
                        RequireContent(record.RelatedDatabase, new RequiredInformationModel(generalUrl, "Related Databases"), model);
                    }

                    RequireContent(record.RelatedPublications, new RequiredInformationModel(generalUrl, "Related Publications"), model);
                    RequireContent(record.RelatedProjects, new RequiredInformationModel(generalUrl, "Related Projects"), model);

                    RequireContent(record.LocationDetails, new RequiredInformationModel(methodsUrl, "Location Details"), model);
                    RequireContent(record.UnitOfObservation, new RequiredInformationModel(methodsUrl, "Unit of Observation"), model);
                    RequireContent(record.SampleSize, new RequiredInformationModel(methodsUrl, "Sample Size"), model);
                    RequireContent(record.InclusionExclusionCriteria, new RequiredInformationModel(methodsUrl, "Inclusion/Exclusion Criteria"), model);
                    RequireContent(record.RandomizationProcedure, new RequiredInformationModel(methodsUrl, "Randomization Procedure"), model);
                    RequireContent(record.UnitOfRandomization, new RequiredInformationModel(methodsUrl, "Unit of Randomization"), model);
                    RequireContent(record.Treatment, new RequiredInformationModel(methodsUrl, "Intervention"), model);
                    RequireContent(record.TreatmentAdministration, new RequiredInformationModel(methodsUrl, "Intervention Administration"), model);
                    RequireContent(record.OutcomeMeasures, new RequiredInformationModel(methodsUrl, "Outcome Measures"), model);

                    RequireContent(record.ReviewType, new RequiredInformationModel(generalUrl, "Review Type"), model);

                    // Make sure all the processing tasks are marked complete.
                    var incompleteTasks = record.TaskStatuses.Where(x => x.StageName == "Processing" && !x.IsComplete).ToList();
                    foreach (var task in incompleteTasks)
                    {
                        string msg = string.Empty;

                        if (task.File == null)
                        {
                            msg = string.Format("Incomplete task: {0}", task.Name);
                        }
                        else
                        {
                            msg = string.Format("Incomplete task for {0}: {1}", task.File.Name, task.Name);
                        }

                        string url = urlHelper.Action("Status", "File", new { id = task.File.Id });
                        model.Messages.Add(new RequiredInformationModel(url, msg));
                        model.IsOkayToSubmit = false;
                    }

                    // Make sure required fields on files are set.
                    foreach (var file in record.Files.Where(x => x.Status != FileStatus.Removed))
                    {
                        string url = urlHelper.Action("General", "File", new { id = file.Id });
                        RequireContent(file.PublicName, new RequiredInformationModel(url, file.Name + ": Public File Name"), model);
                        RequireContent(file.Software, new RequiredInformationModel(url, file.Name + ": Software"), model);

                        if (file.IsStatisticalDataFile())
                        {
                            RequireContent(file.KindOfData, new RequiredInformationModel(url, file.Name + ": Data Type"), model);
                        }
                    }

                    // Make sure no files have outstanding metadata updates
                    foreach (var file in record.Files.Where(x => x.Status != FileStatus.Removed))
                    {
                        if (file.HasPendingMetadataUpdates)
                        {
                            model.IsOkayToSubmit = false;
                            string url = urlHelper.Action("Status", "File", new { id = file.Id });
                            string msg = string.Format("{0} has outstanding metadata updates", file.Name);
                            model.Messages.Add(new RequiredInformationModel(url, msg));
                        }
                    }

                }


                var thisUser = db.Users.Where(x => x.UserName == User.Identity.Name)
                    .Include(x => x.Organizations)
                    .FirstOrDefault();
                string userFullName = string.Empty;
                if (thisUser != null)
                {
                    userFullName = thisUser.FullName;
                }

                // Replace tokens in the deposit agreement.
                model.DepositAgreement = ProcessDepositAgreement(model.DepositAgreement, userFullName, model.CatalogRecord.Title);

                model.CanAssignCurators = OrganizationHelper.DoesUserHaveRight(db, User, record.Organization.Id, Right.CanAssignCurator);
                model.TaskCount = record.TaskStatuses.Where(x => !x.IsComplete).Count();
                model.IsUserCurator = record.Curators.Any(x => x.UserName == User.Identity.Name);
                model.IsUserApprover = record.Approvers.Any(x => x.UserName == User.Identity.Name) ||
                    OrganizationHelper.DoesUserHaveRight(db, User, record.Organization.Id, Right.CanApprove);

                return View(model);
            }
        }

        public ActionResult ChangeLogPdf(Guid id)
        {
            using (var db = ApplicationDbContext.Create())
            {
                var record = GetRecord(id, db);

                if (record.IsLocked)
                {
                    return RedirectToAction("WaitForUnlock", new { id = record.Id, returnUrl = Request.Path });
                }

                EnsureUserIsAllowed(record, db);

                var events = db.Events
                    .Where(x => x.RelatedCatalogRecord != null &&
                            x.RelatedCatalogRecord.Id == id)
                    .OrderBy(x => x.Timestamp)
                    .Include(x => x.RelatedCatalogRecord)
                    .Include(x => x.RelatedManagedFiles)
                    .Include(x => x.User)
                    .ToList();


                var builder = new ChangeLogBuilder();
                byte[] pdfData = builder.BuildPdf(record, events);

                string fileDownloadName = MakeSafeFileName(record.Title) + ".pdf";

                // Return the PDF.
                return File(pdfData, "application/pdf", fileDownloadName);
            }
        }

        public static string MakeSafeFileName(string fileName)
        {
            string result = fileName;

            // Make sure the file name is safe.
            foreach (char c in System.IO.Path.GetInvalidFileNameChars())
            {
                result = result.Replace(c, '-');
            }

            return result;
        }

        string ProcessDepositAgreement(string depositAgreement, string userFullName, string catalogRecordTitle)
        {
            string text = string.Empty;

            if (!string.IsNullOrWhiteSpace(depositAgreement))
            {
                text = Markdig.Markdown.ToHtml(depositAgreement);
            }

            text = text.Replace("@UserName", User.Identity.Name);
            text = text.Replace("@TermsOfServiceLink", "<a href=\"#tosModal\" data-toggle=\"modal\">Terms of Service</a>");
            text = text.Replace("@PolicyLink", "<a href=\"#policyModal\" data-toggle=\"modal\">Data Archive Policy</a>");
            text = text.Replace("@FullName", userFullName);
            text = text.Replace("@Title", catalogRecordTitle);
            text = text.Replace("@Date", DateTime.Now.ToLongDateString());

            // If no @check tokens are specified, include a single checkbox at the end of the statement.
            if (!text.Contains("@check"))
            {
                text = text += "\n\n@check I agree with these terms.";
            }

            // Replace any @check tokens with checkboxes.
            string checkToken = "@check";
            int index = text.IndexOf(checkToken);
            int i = 1;
            while (index != -1)
            {
                text = text.Remove(index, checkToken.Length);
                text = text.Insert(index, string.Format("<input type='checkbox' name='agree-{0}' class='agreement-checkbox'>", i));

                index = text.IndexOf(checkToken);
                i++;
            }

            return text;
        }

        void RequireContent(string content, RequiredInformationModel message, CatalogRecordSubmitViewModel model)
        {
            if (string.IsNullOrWhiteSpace(content) ||
                content == "Not Selected")
            {
                model.IsOkayToSubmit = false;
                model.Messages.Add(message);
            }
        }

        void RequireContent(DateModel dateModel, RequiredInformationModel message, CatalogRecordSubmitViewModel model)
        {
            if (dateModel == null ||
                dateModel.dateType == "Not Selected" ||
                string.IsNullOrWhiteSpace(dateModel.date))
            {
                model.IsOkayToSubmit = false;
                model.Messages.Add(message);
            }
        }

        public ActionResult History(Guid id)
        {
            using (var db = ApplicationDbContext.Create())
            {
                var record = GetRecord(id, db);

                EnsureUserIsAllowed(record, db);

                var events = db.Events
                    .Where(x => x.RelatedCatalogRecord != null &&
                            x.RelatedCatalogRecord.Id == id)
                    .OrderByDescending(x => x.Timestamp)
                    .Include(x => x.RelatedCatalogRecord)
                    .Include(x => x.RelatedManagedFiles)
                    .Include(x => x.User);

                var model = new CatalogRecordHistoryModel();
                model.CatalogRecord = record;

                model.CanAssignCurators = OrganizationHelper.DoesUserHaveRight(db, User, record.Organization.Id, Right.CanAssignCurator);
                model.TaskCount = record.TaskStatuses.Where(x => !x.IsComplete).Count();
                model.IsUserCurator = record.Curators.Any(x => x.UserName == User.Identity.Name);
                model.IsUserApprover = record.Approvers.Any(x => x.UserName == User.Identity.Name) ||
                    OrganizationHelper.DoesUserHaveRight(db, User, record.Organization.Id, Right.CanApprove);

                foreach (var log in events)
                {
                    var eventModel = HistoryEventModel.FromEvent(log, log.User);
                    model.Events.Add(eventModel);
                }

                return View(model);
            }
        }

        public ActionResult Status(Guid id)
        {
            using (var db = ApplicationDbContext.Create())
            {
                var record = GetRecord(id, db);

                EnsureUserIsAllowed(record, db);

                List<CatalogRecordIndexMemberModel> records = new List<CatalogRecordIndexMemberModel>();

                var indexMember = new CatalogRecordIndexMemberModel
                {
                    CatalogRecord = record,
                    CuratorCount = record.Curators.Count()
                };

                records.Add(indexMember);
                var pipeline = GetPipeline(records).First();

                pipeline.IsUserCurator = record.Curators.Any(x => x.UserName == User.Identity.Name);
                pipeline.TaskCount = record.TaskStatuses.Where(x => !x.IsComplete).Count();
                pipeline.IsUserApprover = record.Approvers.Any(x => x.UserName == User.Identity.Name) ||
                    OrganizationHelper.DoesUserHaveRight(db, User, record.Organization.Id, Right.CanApprove);

                return View(pipeline);
            }
        }

        public ActionResult Permissions(Guid id)
        {
            using (var db = ApplicationDbContext.Create())
            {
                var record = GetRecord(id, db);

                EnsureUserCanAssignPermissions(db, record.Organization.Id);


                // Get the list of all users in the organization of the CatalogRecord.
                var model = new CatalogRecordPermissionsModel();

                var dbUsers = db.Users
                    .Where(x => x.Organizations.Any(o => o.Id == record.Organization.Id))
                    .Where(x => !x.IsPlaceholder)
                    .OrderBy(x => x.LastName);

                foreach (var dbUser in dbUsers)
                {
                    var user = new CatalogRecordPermissionsUserModel
                    {
                        UserName = dbUser.UserName,
                        FullName = dbUser.FirstName + " " + dbUser.LastName,
                        Email = dbUser.Email,
                        IsCurator = record.Curators.Any(c => c.UserName == dbUser.UserName),
                        IsApprover = record.Approvers.Any(a => a.UserName == dbUser.UserName)
                    };

                    model.Users.Add(user);
                }

                model.CatalogRecord = record;

                model.CanAssignCurators = OrganizationHelper.DoesUserHaveRight(db, User, record.Organization.Id, Right.CanAssignCurator);
                model.TaskCount = record.TaskStatuses.Where(x => !x.IsComplete).Count();
                model.IsUserCurator = record.Curators.Any(x => x.UserName == User.Identity.Name);
                model.IsUserApprover = record.Approvers.Any(x => x.UserName == User.Identity.Name) ||
                    OrganizationHelper.DoesUserHaveRight(db, User, record.Organization.Id, Right.CanApprove);

                return View(model);
            }
        }

        [HttpPost]
        public ActionResult Permissions(SetCatalogRecordPermissionsModel model)
        {
            using (var db = ApplicationDbContext.Create())
            {
                var record = GetRecord(model.CatalogRecordId, db);
                EnsureUserCanAssignPermissions(db, record.Organization.Id);

                foreach (var userModel in model.Users)
                {
                    var dbUser = db.Users.Where(x => x.UserName == userModel.UserName).FirstOrDefault();

                    if (userModel.IsCurator)
                    {
                        // Are they not a curator already? Then add them.
                        if (!record.Curators.Any(x => x.UserName == userModel.UserName))
                        {
                            record.Curators.Add(dbUser);

                            // Store an event about this permission change.
                            var log = new Event()
                            {
                                EventType = EventTypes.AddCatalogRecordPermission,
                                Timestamp = DateTime.UtcNow,
                                User = dbUser,
                                RelatedCatalogRecord = record,
                                Title = userModel.UserName + " is now a curator for " + record.Title,
                                Details = "Assigned by " + User.Identity.Name
                            };
                            db.Events.Add(log);

                            SendAssignmentNotification("curator", dbUser, record, record.Organization, db);
                        }

                    }
                    else
                    {
                        // Are they a curator now? Then remove them.
                        if (record.Curators.Any(x => x.UserName == userModel.UserName))
                        {
                            record.Curators.Remove(dbUser);

                            // Store an event about this permission change.
                            var log = new Event()
                            {
                                EventType = EventTypes.AddCatalogRecordPermission,
                                Timestamp = DateTime.UtcNow,
                                User = dbUser,
                                RelatedCatalogRecord = record,
                                Title = userModel.UserName + " is no longer a curator for " + record.Title,
                                Details = "Assigned by " + User.Identity.Name
                            };
                            db.Events.Add(log);
                        }
                    }

                    if (userModel.IsApprover)
                    {
                        // Are they not an approver already? Then add them.
                        if (!record.Approvers.Any(x => x.UserName == userModel.UserName))
                        {
                            record.Approvers.Add(dbUser);

                            // Store an event about this permission change.
                            var log = new Event()
                            {
                                EventType = EventTypes.AddCatalogRecordPermission,
                                Timestamp = DateTime.UtcNow,
                                User = dbUser,
                                RelatedCatalogRecord = record,
                                Title = userModel.UserName + " is now an approver for " + record.Title,
                                Details = "Assigned by " + User.Identity.Name
                            };
                            db.Events.Add(log);

                            SendAssignmentNotification("approver", dbUser, record, record.Organization, db);
                        }
                    }
                    else
                    {
                        // Are they an approver now? Then remove them.
                        if (record.Approvers.Any(x => x.UserName == userModel.UserName))
                        {
                            record.Approvers.Remove(dbUser);

                            // Store an event about this permission change.
                            var log = new Event()
                            {
                                EventType = EventTypes.AddCatalogRecordPermission,
                                Timestamp = DateTime.UtcNow,
                                User = dbUser,
                                RelatedCatalogRecord = record,
                                Title = userModel.UserName + " is no longer an approver for " + record.Title,
                                Details = "Assigned by " + User.Identity.Name
                            };
                            db.Events.Add(log);
                        }

                    }
                }

                db.SaveChanges();

            }

            return RedirectToAction("Permissions", new { id = model.CatalogRecordId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(CatalogRecordGeneralViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("General", model);
            }

            using (var db = ApplicationDbContext.Create())
            {
                using (var dbContextTransaction = db.Database.BeginTransaction())
                {
                    var user = db.Users.Where(x => x.UserName == User.Identity.Name)
                        .FirstOrDefault();
                    if (user == null)
                    {
                        return RedirectToAction("Index");
                    }

                    var org = OrganizationHelper.GetOrganizationByHost(Request, db);
                    if (org == null)
                    {
                        return RedirectToAction("Index");
                    }

                    // Create the catalog record.
                    var catalogRecord = new CatalogRecord()
                    {
                        Id = Guid.NewGuid(),
                        Organization = org,
                        CreatedBy = user,
                        CreatedDate = DateTime.UtcNow,
                        Version = 1,
                        LastUpdatedDate = DateTime.UtcNow
                    };
                    Mapper.Map(model, catalogRecord);

                    catalogRecord.DepositAgreement = org.DepositAgreement;

                    // Assign authors in the order specified
                    var authorsList = db.Users.Where(x => model.AuthorIds.Contains(x.Id)).ToList();
                    foreach (string authorId in model.AuthorIds)
                    {
                        var author = authorsList.FirstOrDefault(x => x.Id == authorId);
                        if (author != null)
                        {
                            catalogRecord.Authors.Add(author);
                        }
                    }
                    catalogRecord.AuthorsText = string.Join(", ", model.AuthorIds
                        .Select(id => authorsList.FirstOrDefault(x => x.Id == id)?.FullName)
                        .Where(name => !string.IsNullOrWhiteSpace(name)));

                    // Initialize the CatalogRecord. Perhaps the user (or something) should determine
                    // which initializer to use, instead of the first/only.
                    var initializer = MefConfig.AddinManager.AllCatalogRecordInitializers.FirstOrDefault();
                    if (initializer != null)
                    {
                        try
                        {
                            initializer.Initialize(catalogRecord, org.AgencyID);
                        }
                        catch (Exception ex)
                        {
                            Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
                        }
                    }

                    db.CatalogRecords.Add(catalogRecord);

                    // Create the basic, built-in curation steps for the record.
                    db.TaskStatuses.Add(CreateTaskStatus(catalogRecord, BuiltInCatalogRecordTasks.CreateCatalogRecordTaskId, 100, "Created", "Collection", true, user));
                    db.TaskStatuses.Add(CreateTaskStatus(catalogRecord, BuiltInCatalogRecordTasks.AcceptCatalogRecordTaskId, 200, "Accepted", "Collection", true, user));
                    db.TaskStatuses.Add(CreateTaskStatus(catalogRecord, BuiltInCatalogRecordTasks.RequestCatalogRecordPublicationTaskId, 10000, "Request Publication", "Publish", false, user));
                    db.TaskStatuses.Add(CreateTaskStatus(catalogRecord, BuiltInCatalogRecordTasks.ApproveCatalogRecordPublicationTaskId, 11000, "Publication Approved", "Publish", false, user));
                    db.TaskStatuses.Add(CreateTaskStatus(catalogRecord, BuiltInCatalogRecordTasks.PublishCatalogRecordTaskId, 12000, "Published", "Publish", false, user));
                    db.TaskStatuses.Add(CreateTaskStatus(catalogRecord, BuiltInCatalogRecordTasks.ArchiveCatalogRecordTaskId, 13000, "Archived", "Archive", false, user));

                    // Log the creation of the catalog record.
                    var log = new Event()
                    {
                        EventType = EventTypes.CreateCatalogRecord,
                        Timestamp = DateTime.UtcNow,
                        User = user,
                        RelatedCatalogRecord = catalogRecord,
                        Title = "Create " + catalogRecord.Title,
                        Details = string.Empty
                    };
                    db.Events.Add(log);

                    try
                    {
                        db.SaveChanges();
                        dbContextTransaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        dbContextTransaction.Rollback();
                        Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
                    }

                    try
                    {
                        // Send notifications to organizational admins.
                        var usersToNotify = OrganizationHelper.GetUsersWithRight(db, org.Id, Right.CanAssignRights)
                            .ToList();
                        foreach (var notifyUser in usersToNotify)
                        {
                            NotificationService.SendActionEmail(
                                notifyUser.Email, notifyUser.FullName,
                                org.ReplyToAddress, org.Name,
                                "[New Catalog Record] " + catalogRecord.Title,
                                string.Format("A Catalog Record named {0} has been created by {1}.", catalogRecord.Title, user.FullName),
                                "You can view the record, but the depositor has not submitted it for curation yet.",
                                "View " + catalogRecord.Title,
                                Request.Url.Scheme + "://" + Request.Url.Host + "/CatalogRecord/General/" + catalogRecord.Id.ToString(),
                                org.NotificationEmailClosing,
                                Request.Url.Scheme + "://" + Request.Url.Host + "/User/EmailPreferences/" + notifyUser.UserName,
                                db);
                        }
                    }
                    catch (Exception ex)
                    {
                        Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
                    }

                    CreateRepository createGitRepository = new CreateRepository()
                    {
                        GitRepositoryPath = SettingsHelper.GetProcessingDirectory(catalogRecord.Organization, db),
                        Name = "Creating catalog storage" + (string.IsNullOrWhiteSpace(catalogRecord.Title) ? "" : " for " + catalogRecord.Title),
                        CatalogRecordId = catalogRecord.Id
                    };
                    bool queued = db.Enqueue(catalogRecord.Id, user.Id, createGitRepository);

                    return RedirectToAction("Methods", new { id = catalogRecord.Id });
                }
            }
        }

        public ActionResult RemovePersistentId(CatalogRecordGeneralViewModel model)
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

                // Fetch the appropriate catalog record by ID.
                Guid id = model.CatalogRecordId;

                var record = GetRecord(id, db);

                EnsureUserIsAllowed(record, db);
                EnsureUserCanEdit(record, db, user);

                // Clear the persistent ID.
                record.PersistentId = null;

                // Log the editing of the catalog record.
                var log = new Event()
                {
                    EventType = EventTypes.EditCatalogRecord,
                    Timestamp = DateTime.UtcNow,
                    User = user,
                    RelatedCatalogRecord = record,
                    Title = "Edit a Catalog Record",
                    Details = "Remove persistent link"
                };
                db.Events.Add(log);
                db.SaveChanges();

                return RedirectToAction("General", new { id = id });
            }
        }

        [HttpPost]
        public ActionResult General(CatalogRecordGeneralViewModel model)
        {
            var logger = LogManager.GetLogger("CatalogRecordController");
            logger.Debug("Entering CatalogRecordController.General() POST handler");

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
                    return RedirectToAction("Index");
                }

                logger.Debug("Got user object");

                // Fetch the appropriate catalog record by ID.
                Guid id = model.CatalogRecordId;

                var record = GetRecord(id, db);

                EnsureUserIsAllowed(record, db);
                EnsureUserCanEdit(record, db, user);

                logger.Debug("User is allowed");

                if (record.IsLocked)
                {
                    logger.Debug("Record is locked. Throwing.");
                    throw new HttpException(400, "This operation cannot be performed while the record is locked");
                }

                // Create a change summary.
                List<string> authorNames = new List<string>();
                foreach (string authorId in model.AuthorIds)
                {
                    string name = db.Users.FirstOrDefault(x => x.Id == authorId)?.FullName;
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        authorNames.Add(name);
                    }
                }
                model.Authors = string.Join(", ", authorNames);
                string changeSummary = CatalogRecordChangeDetector.GetChangeSummary(record, model);

                // Copy information from the POST to the CatalogRecord.
                Mapper.Map(model, record);
                logger.Debug("Mapped");

                // Manually map authors based on the AuthorIDs property
                record.Authors.Clear();
                
                // Retrieve all authors at once
                var authorsList = db.Users.Where(x => model.AuthorIds.Contains(x.Id)).ToList();
                
                // Add them in the order specified in AuthorIds
                foreach (string authorId in model.AuthorIds)
                {
                    var author = authorsList.FirstOrDefault(x => x.Id == authorId);
                    if (author != null)
                    {
                        record.Authors.Add(author);
                    }
                }
                
                // Update AuthorsText with names in the correct order
                record.AuthorsText = string.Join(", ", model.AuthorIds
                    .Select(authorId => authorsList.FirstOrDefault(x => x.Id == authorId)?.FullName)
                    .Where(name => !string.IsNullOrWhiteSpace(name)));

                logger.Debug("Authors retrieved");

                // Up the version.
                record.Version++;
                record.LastUpdatedDate = DateTime.UtcNow;

                // Log the editing of the catalog record.
                var log = new Event()
                {
                    EventType = EventTypes.EditCatalogRecord,
                    Timestamp = DateTime.UtcNow,
                    User = user,
                    RelatedCatalogRecord = record,
                    Title = "Edit a Catalog Record",
                    Details = changeSummary
                };
                db.Events.Add(log);

                logger.Debug("Logged");

                // Save the updated record.
                db.SaveChanges();

                logger.Debug("Saved to database");

                return RedirectToAction("Methods", new { id = id });
            }
        }

        [HttpPost]
        public ActionResult Methods(CatalogRecordMethodsViewModel model)
        {
            using (var db = ApplicationDbContext.Create())
            {
                var logger = LogManager.GetLogger("CatalogRecordController");
                logger.Debug("Entering CatalogRecord.Methods() POST handler");

                var user = db.Users
                    .Where(x => x.UserName == User.Identity.Name)
                    .FirstOrDefault();
                if (user == null)
                {
                    logger.Debug("Could not get user. Returning");
                    return RedirectToAction("Index");
                }


                // Fetch the appropriate catalog record by ID.
                Guid id = model.CatalogRecordId;

                var record = GetRecord(id, db);

                logger.Debug("Got record");

                EnsureUserIsAllowed(record, db);
                EnsureUserCanEdit(record, db, user);

                logger.Debug("User is allowed");

                if (record.IsLocked)
                {
                    logger.Debug("Record is locked. Throwing.");
                    throw new HttpException(400, "This operation cannot be performed while the record is locked");
                }

                // Create a change summary.
                string changeSummary = CatalogRecordChangeDetector.GetChangeSummary(record, model);

                // Copy information from the POST to the CatalogRecord.
                Mapper.Map(model, record);

                logger.Debug("Mapped");

                if (Request.Form["FieldDates.isRange"] != null)
                {
                    model.FieldDates.isRange = true;
                }
                if (Request.Form["StudyTimePeriod.isRange"] != null)
                {
                    model.StudyTimePeriod.isRange = true;
                }

                record.FieldDates = JsonConvert.SerializeObject(model.FieldDates);
                record.StudyTimePeriod = JsonConvert.SerializeObject(model.StudyTimePeriod);

                record.ResearchDesign = Request.Form["ResearchDesign"];
                record.TreatmentAdministration = Request.Form["TreatmentAdministration"];
                record.UnitOfObservation = Request.Form["UnitOfObservation"];
                record.ModeOfDataCollection = Request.Form["ModeOfDataCollection"];
                record.UnitOfRandomization = Request.Form["UnitOfRandomization"];

                record.DataType = model.CatalogRecordDataType;
                record.DataSource = model.CatalogRecordDataSource;
                record.DataSourceInformation = model.CatalogRecordDataSourceInformation;

                record.Version++;
                record.LastUpdatedDate = DateTime.UtcNow;

                logger.Debug("Done manually mapping");

                // Log the editing of the catalog record.
                var log = new Event()
                {
                    EventType = EventTypes.EditCatalogRecord,
                    Timestamp = DateTime.UtcNow,
                    User = user,
                    RelatedCatalogRecord = record,
                    Title = "Edit a Catalog Record",
                    Details = changeSummary
                };
                db.Events.Add(log);

                logger.Debug("Logged");

                // Save the updated record.
                db.SaveChanges();

                logger.Debug("Saved to database");

                return RedirectToAction("Files", new { id = id });
            }
        }

        [HttpPost]
        public ActionResult Deposit(DepositFilesViewModel model)
        {
            if (Request.Files.Count == 0)
            {
                return RedirectToAction("Files", new { id = model.CatalogRecordId });
            }

            using (var db = ApplicationDbContext.Create())
            {
                using (var dbContextTransaction = db.Database.BeginTransaction())
                {
                    var record = GetRecord(model.CatalogRecordId, db);

                    var user = db.Users.Where(x => x.UserName == User.Identity.Name).FirstOrDefault();

                    EnsureUserIsAllowed(record, db);
                    EnsureUserCanEdit(record, db, user);

                    if (record.IsLocked)
                    {
                        throw new HttpException(400, "This operation cannot be performed while the record is locked");
                    }


                    string processingDirectory = SettingsHelper.GetProcessingDirectory(record.Organization, db);
                    string ingestDirectory = SettingsHelper.GetIngestDirectory(record.Organization, db);

                    AddFiles addFilesOp = new AddFiles()
                    {
                        CatalogRecordId = model.CatalogRecordId,
                        CommitMessage = model.Notes,
                        GitRepositoryPath = processingDirectory,
                        Name = "Adding files",
                        UserEmail = user.Email,
                        UserId = new Guid(user.Id),
                        Username = user.UserName
                    };

                    // Store an event about this file upload.
                    var log = new Event()
                    {
                        EventType = EventTypes.Upload,
                        Timestamp = DateTime.UtcNow,
                        User = user,
                        RelatedCatalogRecord = record,
                        Details = model.Notes
                    };

                    for (int i = 0; i < Request.Files.Count; i++)
                    {
                        var postedFile = Request.Files[i];
                        string title = model.Titles[i];
                        string source = model.Sources[i];
                        string type = model.Types[i];
                        string sourceInformation = model.SourceInformations[i];
                        string rights = model.Rights[i];
                        string publicAccess = model.PublicAccesses[i];

                        DateTime? creationDate = null;
                        DateTime dt;
                        if (DateTime.TryParse(model.CreationDates[i], out dt))
                        {
                            creationDate = dt;
                        }

                        bool isPublicAccess = publicAccess == "Yes";

                        Guid fileId = Guid.NewGuid();

                        // If this file is marked as replacing an existing file, update the existing file
                        bool isReplacement = false;
                        string fileName = postedFile.FileName;

                        // See if they selected to replace an existing file.
                        string originalFileName = string.Empty;
                        if (model.SelectedExistingFileNames != null &&
                            model.SelectedExistingFileNames.Count > i)
                        {
                            originalFileName = model.SelectedExistingFileNames[i];
                        }

                        // If this newly uploaded file has the same name as an existing file, 
                        // treat it as an update to that file, no matter what the user specified.
                        bool isExactMatchOfExistingFile = record.Files.Any(x => string.Compare(fileName, x.Name, true) == 0);
                        if (isExactMatchOfExistingFile)
                        {
                            originalFileName = fileName;
                        }

                        if (!string.IsNullOrWhiteSpace(originalFileName))
                        {
                            isReplacement = true;

                            // If this is a replacement but they claim "Initial Upload" as the action,
                            // set the action to "New Version".
                            if (model.ActionType == "Initial Upload")
                            {
                                model.ActionType = "New Version";
                            }

                            // Since this is a replacement, update the existing record.
                            var existingFile = db.Files.Where(x => string.Compare(x.Name, originalFileName, true) == 0 && x.CatalogRecord.Id == record.Id).FirstOrDefault();
                            if (existingFile != null)
                            {
                                fileId = existingFile.Id;

                                existingFile.CreationDate = creationDate;
                                existingFile.Name = fileName;//new name
                                existingFile.Size = postedFile.ContentLength;
                                existingFile.Source = source;
                                existingFile.SourceInformation = sourceInformation;
                                existingFile.Rights = rights;
                                existingFile.IsPublicAccess = isPublicAccess;
                                existingFile.PublicName = title;
                                existingFile.Type = type;
                                existingFile.UploadedDate = DateTime.UtcNow;
                                existingFile.Status = FileStatus.Pending;
                                existingFile.Owner = user;
                                existingFile.Version++;

                                // Updated files should get new Handles. By removing any current
                                // Persistent ID, a new one will be requested during publication.
                                existingFile.PersistentLink = string.Empty;
                                existingFile.PersistentLinkDate = null;

                                log.RelatedManagedFiles.Add(existingFile);

                                existingFile.Status = FileStatus.Pending;

                                if (originalFileName != fileName)
                                {
                                    addFilesOp.RenamedFileNames.Add(fileId, new Tuple<string, string>(originalFileName, fileName));

                                    log.Details += string.Format("\nFile name changed from {0} to {1}\n", originalFileName, fileName);
                                }

                                // Add the note to the the existing file.
                                var existingFileNote = new Note()
                                {
                                    CatalogRecord = record,
                                    File = existingFile,
                                    Timestamp = DateTime.UtcNow,
                                    User = user,
                                    Text = model.Notes
                                };
                                db.Notes.Add(existingFileNote);

                                // Reset the all task statuses for this file, so it is re-checked.
                                TaskHelpers.ResetTasksForFile(existingFile, record, db);
                            }
                        }

                        // If this is not a replacement, create the file.
                        if (!isReplacement)
                        {
                            var file = new ManagedFile()
                            {
                                Id = fileId,
                                CatalogRecord = record,
                                CreationDate = creationDate,
                                Name = fileName,
                                FormatName = Path.GetExtension(fileName).ToLower(),
                                Size = postedFile.ContentLength,
                                Source = source,
                                SourceInformation = sourceInformation,
                                Rights = rights,
                                IsPublicAccess = isPublicAccess,
                                PublicName = title,
                                Type = type,
                                UploadedDate = DateTime.UtcNow,
                                Status = FileStatus.Pending,
                                Owner = user,
                                Version = 1
                            };

                            file.Software = MetadataHelpers.GetSoftware(fileName);

                            file.Status = FileStatus.Pending;

                            // Add tasks for the file.
                            TaskHelpers.AddProcessingTasksForFile(file, record, db);

                            db.Files.Add(file);

                            // Add the note to the the existing file.
                            var fileNote = new Note()
                            {
                                CatalogRecord = record,
                                File = file,
                                Timestamp = DateTime.UtcNow,
                                User = user,
                                Text = model.Notes
                            };
                            db.Notes.Add(fileNote);

                            // Add the file to the log message.
                            log.RelatedManagedFiles.Add(file);
                        }

                        // Save the file to the ingest space.
                        string path = Path.Combine(ingestDirectory, model.CatalogRecordId.ToString());
                        Directory.CreateDirectory(path);

                        string savePath = Path.Combine(path, fileName);
                        postedFile.SaveAs(savePath);

                        // Add the file to the ingest -> processing space operation.
                        addFilesOp.IncomingFileNames.Add(fileId, savePath);
                    }

                    log.Title = "Upload: " + model.ActionType;

                    db.Events.Add(log);

                    // If this record is already published, set its state back to Processing.
                    if (record.Status == CatalogRecordStatus.Published)
                    {
                        record.Status = CatalogRecordStatus.New;
                    }

                    try
                    {
                        db.SaveChanges();
                        dbContextTransaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        dbContextTransaction.Rollback();
                        Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
                    }

                    bool locked = db.Enqueue(model.CatalogRecordId, user.Id, addFilesOp);

                    return RedirectToAction("Files", new { id = model.CatalogRecordId });
                }
            }
        }

        [HttpPost]
        public ActionResult SubmitForCuration(Guid id)
        {
            using (var db = ApplicationDbContext.Create())
            {
                var record = GetRecord(id, db);

                var user = db.Users.Where(x => x.UserName == User.Identity.Name).FirstOrDefault();

                bool isCurator = record.Curators.Any(x => x.UserName == User.Identity.Name);

                // Ensure the user has rights to submit this.
                // For now we give these rights to Curators.
                if (!user.IsAdministrator &&
                    record.CreatedBy.UserName != User.Identity.Name &&
                    !isCurator)
                {
                    throw new HttpException(403, "Forbidden");
                }

                if (record.Status != CatalogRecordStatus.New)
                {
                    throw new HttpException(500, "Invalid operation");
                }

                // If the organization has a deposit agreement, make sure it is checked.
                if (!string.IsNullOrWhiteSpace(record.Organization.DepositAgreement))
                {
                    // Determine whether all the agreement-checkboxes are checked.
                    int requiredAgreeCount = Regex.Matches(record.Organization.DepositAgreement, "@check").Count;

                    // If no checkboxes are explicitely in the agreement, we add one automatically.
                    if (requiredAgreeCount == 0)
                    {
                        requiredAgreeCount = 1;
                      }

                    int agreedCount = this.Request.Form.AllKeys.Where(x => x.StartsWith("agree-")).Count();
                    if (agreedCount == requiredAgreeCount)
                    {
                        record.DepositAgreement = record.Organization.DepositAgreement;
                    }
                    else
                    {
                        return RedirectToAction("Submit", new { id = id });
                    }
                }

                // Create a job to push all original files to archive storage, now that we are moving
                // from ingest to processing.
                var operation = new ArchiveIngestFiles()
                {
                    Name = "Archiving ingest files" + (string.IsNullOrWhiteSpace(record.Title) ? "" : " for " + record.Title),
                    UserId = user.Id,
                    CatalogRecordId = record.Id,
                    IngestDirectory = SettingsHelper.GetIngestDirectory(record.Organization, db),
                    ArchiveDirectory = SettingsHelper.GetArchiveDirectory(record.Organization, db),
                };

                bool queued = db.Enqueue(record.Id, user.Id, operation);


                record.Status = CatalogRecordStatus.Processing;

                // Log the request.
                var log = new Event()
                {
                    EventType = EventTypes.SubmitForCuration,
                    Timestamp = DateTime.UtcNow,
                    User = user,
                    RelatedCatalogRecord = record,
                    Title = "Submit for Curation",
                    Details = string.Empty
                };
                db.Events.Add(log);

                db.SaveChanges();

                var org = record.Organization;

                // Notify curation assigners.
                var usersToNotify = OrganizationHelper.GetUsersWithRight(db, org.Id, Right.CanAssignRights)
                    .ToList();
                foreach (var notifyUser in usersToNotify)
                {
                    try
                    {
                        NotificationService.SendActionEmail(
                            notifyUser.Email, notifyUser.FullName,
                            org.ReplyToAddress, org.Name,
                            "[Catalog Record Submitted for Curation] " + record.Title,
                            string.Format("{0} has been submitted for curation by {1}.", record.Title, user.FullName),
                            "Please assign a curator to the record.",
                            "Assign Curator to " + record.Title,
                            Request.Url.Scheme + "://" + Request.Url.Host + "/CatalogRecord/Permissions/" + record.Id.ToString(),
                            org.NotificationEmailClosing,
                            Request.Url.Scheme + "://" + Request.Url.Host + "/User/EmailPreferences/" + notifyUser.UserName,
                            db);
                    }
                    catch (Exception ex)
                    {
                        Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
                    }
                }

                // Notify curators, if there are any already.
                usersToNotify = record.Curators.ToList();
                foreach (var notifyUser in usersToNotify)
                {
                    SendAssignmentNotification("curator", user, record, org, db);
                }
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult RejectRecord(Guid id)
        {
            using (var db = ApplicationDbContext.Create())
            {
                var record = GetRecord(id, db);

                // Ensure the user has rights to reject the record.
                // For now we give these rights to Approvers.
                if (!record.Approvers.Any(x => x.UserName == User.Identity.Name) &&
                    !OrganizationHelper.DoesUserHaveRight(db, User, record.Organization.Id, Right.CanApprove))
                {
                    throw new HttpException(403, "Only approvers may perform this task.");
                }

                var user = db.Users.Where(x => x.UserName == User.Identity.Name).FirstOrDefault();

                record.Status = CatalogRecordStatus.Rejected;

                // Log the rejection.
                var log = new Event()
                {
                    EventType = EventTypes.RejectFile,
                    Timestamp = DateTime.UtcNow,
                    User = user,
                    RelatedCatalogRecord = record,
                    Title = "Reject Catalog Record",
                    Details = string.Empty
                };
                db.Events.Add(log);

                db.SaveChanges();
            }

            return RedirectToAction("Status", new { id = id });

        }

        [HttpPost]
        public ActionResult RequestPublish(Guid id)
        {
            using (var db = ApplicationDbContext.Create())
            {
                var record = GetRecord(id, db);

                // Ensure the user has rights to request publication.
                // For now we give these rights to Curators.
                if (!record.Curators.Any(x => x.UserName == User.Identity.Name))
                {
                    throw new HttpException(403, "Only curators may perform this task.");
                }

                var user = db.Users.Where(x => x.UserName == User.Identity.Name).FirstOrDefault();

                record.Status = CatalogRecordStatus.PublicationRequested;

                // Log the request.
                var log = new Event()
                {
                    EventType = EventTypes.RequestPublication,
                    Timestamp = DateTime.UtcNow,
                    User = user,
                    RelatedCatalogRecord = record,
                    Title = "Publication Request",
                    Details = string.Empty
                };
                db.Events.Add(log);

                // Mark this step as done.
                var task = db.TaskStatuses.Where(x => x.CatalogRecord.Id == record.Id &&
                    x.TaskId == BuiltInCatalogRecordTasks.RequestCatalogRecordPublicationTaskId)
                    .FirstOrDefault();
                if (task != null)
                {
                    task.IsComplete = true;
                    task.CompletedDate = DateTime.UtcNow;
                    task.CompletedBy = user;
                }

                // Save everything.
                db.SaveChanges();

                // Send email notifications.
                var org = record.Organization;
                var usersToNotify = record.Approvers.ToList();
                foreach (var notifyUser in usersToNotify)
                {
                    try
                    {
                        NotificationService.SendActionEmail(
                            notifyUser.Email, notifyUser.FullName,
                            org.ReplyToAddress, org.Name,
                            "[Catalog Record Publication Request] " + record.Title,
                            string.Format("{0} requests that {1} be approved for publication.", user.FullName, record.Title),
                            "Please review the record and decide whether to approve the request.",
                            "Review " + record.Title,
                            Request.Url.Scheme + "://" + Request.Url.Host + "/CatalogRecord/General/" + record.Id.ToString(),
                            org.NotificationEmailClosing,
                            Request.Url.Scheme + "://" + Request.Url.Host + "/User/EmailPreferences/" + notifyUser.UserName,
                            db);
                    }
                    catch (Exception ex)
                    {
                        Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
                    }
                }
            }

            return RedirectToAction("Status", new { id = id });
        }

        [HttpPost]
        public ActionResult ApprovePublish(Guid id)
        {
            using (var db = ApplicationDbContext.Create())
            {
                var record = GetRecord(id, db);

                // Ensure the user has rights to request publication.
                // For now we give these rights to Curators.
                if (!record.Approvers.Any(x => x.UserName == User.Identity.Name) &&
                    !OrganizationHelper.DoesUserHaveRight(db, User, record.Organization.Id, Right.CanApprove))
                {
                    throw new HttpException(403, "Only approvers may perform this task.");
                }

                var user = db.Users.Where(x => x.UserName == User.Identity.Name).FirstOrDefault();

                record.Status = CatalogRecordStatus.PublicationApproved;

                record.CertifiedDate = DateTime.UtcNow;

                // Log the approval.
                var log = new Event()
                {
                    EventType = EventTypes.ApprovePublication,
                    Timestamp = DateTime.UtcNow,
                    User = user,
                    RelatedCatalogRecord = record,
                    Title = "Publication Approved",
                    Details = string.Empty
                };
                db.Events.Add(log);

                // Mark this step as done.
                var task = db.TaskStatuses.Where(x => x.CatalogRecord.Id == record.Id &&
                    x.TaskId == BuiltInCatalogRecordTasks.ApproveCatalogRecordPublicationTaskId)
                    .FirstOrDefault();
                if (task != null)
                {
                    task.IsComplete = true;
                    task.CompletedDate = DateTime.UtcNow;
                    task.CompletedBy = user;
                }

                // Archive and publish.
                var operation = new FinalizeCatalogRecord()
                {
                    Name = "Finalizing catalog record" + (string.IsNullOrWhiteSpace(record.Title) ? "" : " for " + record.Title),
                    UserId = user.Id,
                    CatalogRecordId = record.Id,
                    ProcessingDirectory = SettingsHelper.GetProcessingDirectory(record.Organization, db),
                    ArchiveDirectory = SettingsHelper.GetArchiveDirectory(record.Organization, db),
                    UrlBase = Request.Url.Scheme + "://" + Request.Url.Host
                };

                bool queued = db.Enqueue(record.Id, user.Id, operation);

                db.SaveChanges();
            }

            return RedirectToAction("Status", new { id = id });
        }

        [HttpPost]
        public ActionResult RejectPublish(Guid id)
        {
            using (var db = ApplicationDbContext.Create())
            {
                var record = GetRecord(id, db);

                // Ensure the user has rights to request publication.
                // For now we give these rights to Curators.
                if (!record.Approvers.Any(x => x.UserName == User.Identity.Name) &&
                    !OrganizationHelper.DoesUserHaveRight(db, User, record.Organization.Id, Right.CanApprove))
                {
                    throw new HttpException(403, "Only approvers may perform this task.");
                }

                var user = db.Users.Where(x => x.UserName == User.Identity.Name).FirstOrDefault();

                record.Status = CatalogRecordStatus.Processing;

                // Log the request.
                var log = new Event()
                {
                    EventType = EventTypes.RejectPublication,
                    Timestamp = DateTime.UtcNow,
                    User = user,
                    RelatedCatalogRecord = record,
                    Title = "Publication Request Rejected",
                    Details = string.Empty
                };
                db.Events.Add(log);

                db.SaveChanges();

                // Send email notifications.
                var org = record.Organization;
                var toNotify = record.Curators.Union(record.CreatedBy.Yield());
                foreach (var notifyUser in toNotify)
                {
                    try
                    {
                        NotificationService.SendDecisionNotification(false, notifyUser, record, org, Request.Url.Scheme + "://" + Request.Url.Host, db);
                    }
                    catch (Exception ex)
                    {
                        Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
                    }
                }
            }

            return RedirectToAction("Status", new { id = id });
        }

        [HttpPost]
        public ActionResult RevertToProcessing(Guid id)
        {
            using (var db = ApplicationDbContext.Create())
            {
                var record = GetRecord(id, db);

                // Ensure the user is an Approver.
                if (!record.Approvers.Any(x => x.UserName == User.Identity.Name) &&
                    !OrganizationHelper.DoesUserHaveRight(db, User, record.Organization.Id, Right.CanApprove))
                {
                    throw new HttpException(403, "Only approvers may perform this task.");
                }

                var user = db.Users.Where(x => x.UserName == User.Identity.Name).FirstOrDefault();

                record.Status = CatalogRecordStatus.Processing;

                // Log the request.
                var log = new Event()
                {
                    EventType = EventTypes.SubmitForCuration,
                    Timestamp = DateTime.UtcNow,
                    User = user,
                    RelatedCatalogRecord = record,
                    Title = "Revert to Curation",
                    Details = string.Empty
                };
                db.Events.Add(log);

                // Mark the appropriate steps as not done.
                var task = db.TaskStatuses.Where(x => x.CatalogRecord.Id == record.Id &&
                    x.TaskId == BuiltInCatalogRecordTasks.RequestCatalogRecordPublicationTaskId)
                    .FirstOrDefault();
                if (task != null)
                {
                    task.IsComplete = false;
                }
                task = db.TaskStatuses.Where(x => x.CatalogRecord.Id == record.Id &&
                    x.TaskId == BuiltInCatalogRecordTasks.ApproveCatalogRecordPublicationTaskId)
                    .FirstOrDefault();
                if (task != null)
                {
                    task.IsComplete = false;
                }

                // Save everything.
                db.SaveChanges();

                // Notify curators, if there are any already.
                var org = record.Organization;
                var usersToNotify = record.Curators.ToList();
                foreach (var notifyUser in usersToNotify)
                {
                    SendAssignmentNotification("curator", user, record, org, db);
                }
            }

            return RedirectToAction("Status", new { id = id });
        }

        [Route("CatalogRecord/Editor/Notes/{id}")]
        public ActionResult Notes(Guid id)
        {
            using (var db = ApplicationDbContext.Create())
            {
                var record = GetRecord(id, db);

                if (!record.Curators.Any(x => x.UserName == User.Identity.Name) &&
                    !record.Approvers.Any(x => x.UserName == User.Identity.Name) &&
                    record.CreatedBy.UserName != User.Identity.Name &&
                    !OrganizationHelper.DoesUserHaveRight(db, User, record.Organization.Id, Right.CanApprove))
                {
                    throw new HttpException(403, "Only curators may perform this task.");
                }

                var model = new NotesModel();
                model.CatalogRecord = record;

                model.IsUserCurator = record.Curators.Any(x => x.UserName == User.Identity.Name);
                model.IsUserApprover = record.Approvers.Any(x => x.UserName == User.Identity.Name) ||
                    OrganizationHelper.DoesUserHaveRight(db, User, record.Organization.Id, Right.CanApprove);

                var notes = db.Notes.Where(x => x.CatalogRecord.Id == id)
                    .Include(x => x.User)
                    .Include(x => x.File);

                // If the user is the depositor, only show notes made by the depositor.
                if (record.CreatedBy.UserName == User.Identity.Name &&
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
                        File = note.File,
                        VariableAgency = note.VariableAgency,
                        VariableId = note.VariableId,
                        VariableName = note.VariableName
                    });
                }

                // Get all notes from files in this catalog record.
                foreach (var file in record.Files)
                {
                    var fileNotes = db.Notes.Where(x => x.File.Id == id).Include(x => x.User);
                    // If the user is the depositor, only show notes made by the depositor.
                    if (file.CatalogRecord.CreatedBy.UserName == User.Identity.Name &&
                        !model.IsUserCurator && 
                        !model.IsUserApprover)
                    {
                        fileNotes = notes.Where(x => x.User.UserName == User.Identity.Name);
                    }

                    foreach (var note in fileNotes)
                    {
                        model.Comments.Add(new UserCommentModel
                        {
                            Text = note.Text,
                            UserName = note.User.UserName,
                            Timestamp = note.Timestamp,
                        });
                    }

                }

                return View("Notes", model);
            }
        }

        [Route("CatalogRecord/AddNote/{id}")]
        [HttpPost]
        public ActionResult AddNote(Guid id, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return Content(string.Empty);
            }

            // Get the CatalogRecord.
            using (var db = ApplicationDbContext.Create())
            {
                var user = db.Users.Where(x => x.UserName == User.Identity.Name)
                    .FirstOrDefault();
                if (user == null)
                {
                    return RedirectToAction("Index");
                }

                var record = GetRecord(id, db);

                if (!record.Curators.Any(x => x.UserName == User.Identity.Name) &&
                    !record.Approvers.Any(x => x.UserName == User.Identity.Name) &&
                    record.CreatedBy.UserName != User.Identity.Name &&
                    !OrganizationHelper.DoesUserHaveRight(db, User, record.Organization.Id, Right.CanApprove))
                {
                    throw new HttpException(403, "Only curators may perform this task.");
                }

                try
                {
                    var note = new Note()
                    {
                        CatalogRecord = record,
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
                        RelatedCatalogRecord = record,
                        Title = "Add a Note",
                        Details = text
                    };
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

        internal CatalogRecord GetRecord(Guid id, ApplicationDbContext db)
        {
            if (id == Guid.Empty)
            {
                throw new HttpException(400, "Bad Request");
            }

            var record = db.CatalogRecords
                .Where(x => x.Id == id)
                .Include(x => x.Organization)
                .Include(x => x.Authors)
                .Include(x => x.Curators)
                .Include(x => x.Approvers)
                .Include(x => x.CreatedBy)
                .Include(x => x.TaskStatuses)
                .Include("TaskStatuses.File")
                .FirstOrDefault();

            if (record == null)
            {
                throw new HttpException(404, "Not Found");
            }

            return record;
        }

        void EnsureUserCanAssignPermissions(ApplicationDbContext db, Guid organizationId)
        {
            if (!OrganizationHelper.DoesUserHaveRight(db, User, organizationId, Right.CanAssignCurator))
            {
                throw new HttpException(403, "Forbidden");
            }
        }

        TaskStatus CreateTaskStatus(CatalogRecord catalogRecord, Guid taskId, int weight, string name, string stageName, bool isComplete, ApplicationUser user)
        {
            var status = new TaskStatus()
            {
                Id = Guid.NewGuid(),
                TaskId = taskId,
                CatalogRecord = catalogRecord,
                Name = name,
                StageName = stageName,
                Weight = weight,
                IsComplete = isComplete
            };

            if (isComplete)
            {
                status.CompletedDate = DateTime.UtcNow;
                status.CompletedBy = user;
            }

            return status;
        }

        List<CatalogRecordStatusViewModel> GetPipeline(IList<CatalogRecordIndexMemberModel> records)
        {
            var result = new List<CatalogRecordStatusViewModel>();

            foreach (var record in records)
            {
                var statuses = record.CatalogRecord.TaskStatuses
                    .OrderBy(x => x.Weight)
                    .ToList();

                var pipeline = new CatalogRecordStatusViewModel();
                pipeline.CatalogRecord = record.CatalogRecord;
                pipeline.CuratorCount = record.CuratorCount;
                result.Add(pipeline);

                var stages = statuses.Select(x => x.StageName).Distinct();
                foreach (var stageName in stages)
                {
                    var stage = new PipelineStageViewModel() { Name = stageName };
                    pipeline.Stages.Add(stage);
                }

                foreach (var statusGroup in statuses.GroupBy(x => x.Name))
                {
                    var stepName = statusGroup.Key;

                    var step = new PipelineStepViewModel()
                    {
                        Name = stepName,
                    };

                    int totalCount = statusGroup.Count();
                    int completeCount = 0;
                    foreach (var fileStep in statusGroup)
                    {
                        if (fileStep.IsComplete)
                        {
                            completeCount++;
                        }

                        if (fileStep.File != null)
                        {
                            step.Files.Add(fileStep.File);
                        }
                    }

                    if (completeCount == totalCount)
                    {
                        var lastStatus = statusGroup.Last();
                        step.IsComplete = true;

                        if (lastStatus != null)
                        {
                            if (lastStatus.CompletedBy != null)
                            {
                                step.CompletedByUser = lastStatus.CompletedBy.UserName;
                            }

                            if (lastStatus.CompletedDate.HasValue)
                            {
                                step.CompletedDate = lastStatus.CompletedDate.Value;
                            }
                        }
                        step.Message = string.Empty;
                    }
                    else
                    {
                        step.CompletedUnits = completeCount;
                        step.TotalUnits = totalCount;
                    }

                    var stage = pipeline.Stages.Where(x => x.Name == statusGroup.First().StageName).FirstOrDefault();
                    stage.Steps.Add(step);
                }

                foreach (var stage in pipeline.Stages)
                {
                    stage.IsComplete = !stage.Steps.Any(x => !x.IsComplete);
                }
            }

            return result;
        }

        void SendAssignmentNotification(string role, ApplicationUser notifyUser, CatalogRecord record, Organization org, ApplicationDbContext db)
        {
            try
            {
                NotificationService.SendActionEmail(
                    notifyUser.Email, notifyUser.FullName,
                    org.ReplyToAddress, org.Name,
                    "[Catalog Record Assignment] " + record.Title,
                    string.Format("You have been assigned as {0} for {1}.", role, record.Title),
                    "Click below to open the record.",
                    "Curate " + record.Title,
                    Request.Url.Scheme + "://" + Request.Url.Host + "/CatalogRecord/General/" + record.Id.ToString(),
                    org.NotificationEmailClosing,
                    Request.Url.Scheme + "://" + Request.Url.Host + "/User/EmailPreferences/" + notifyUser.UserName,
                    db);
            }
            catch (Exception ex)
            {
                Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
            }
        }

        void EnsureUserIsAllowed(CatalogRecord record, ApplicationDbContext db)
        {
            if (OrganizationHelper.DoesUserHaveRight(db, User, record.Organization.Id, Right.CanAssignCurator))
            {
                return;
            }

            var thisUser = db.Users.Where(x => x.UserName == User.Identity.Name)
                .Include(x => x.Organizations)
                .FirstOrDefault();
            if (thisUser.IsAdministrator)
            {
                return;
            }

            // Records created with older versions might not have CreatedBy set.
            string createdByUserName = record.CreatedBy != null ? record.CreatedBy.UserName : string.Empty;

            bool isCuratorForRecord = record.Curators.Any(x => x.UserName == User.Identity.Name);
            bool isApproverForRecord = record.Approvers.Any(x => x.UserName == User.Identity.Name);

            if (createdByUserName != User.Identity.Name &&
                !isCuratorForRecord &&
                !isApproverForRecord &&
                !OrganizationHelper.DoesUserHaveRight(db, User, record.Organization.Id, Right.CanApprove))
            {
                throw new HttpException(403, "Forbidden");
            }
        }

        void EnsureUserCanEdit(CatalogRecord record, ApplicationDbContext db, ApplicationUser user)
        {
            bool isCuratorForRecord = record.Curators.Any(x => x.UserName == User.Identity.Name);
            bool isApproverForRecord = record.Approvers.Any(x => x.UserName == User.Identity.Name);

            if (user.IsAdministrator)
            {
                return;
            }

            if (record.Status != CatalogRecordStatus.New &&
                !isCuratorForRecord &&
                !isApproverForRecord)
            {
                throw new HttpException(403, "Forbidden");
            }
        }

    }
}
