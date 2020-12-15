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
using Colectica.Curation.Web.Areas.Ddi.Models;
using System.IO;
using Colectica.Curation.Common.ViewModels;
using Colectica.Curation.Web.Controllers;
using System.Windows.Documents;
using System.Text;
using System.Windows;

namespace Colectica.Curation.Web.Areas.Ddi.Controllers
{
    [Authorize]
    public class PreviewController : CurationControllerBase
    {
        [Route("Preview/Editor/{id}")]
        public ActionResult Editor(Guid id)
        {
            using (var db = ApplicationDbContext.Create())
            {
                var file = GetFile(id, db);

                EnsureUserHasPermission(file, db);

                var model = new PreviewFileModel();
                model.FileName = file.Name;
                model.CatalogRecordId = file.CatalogRecord.Id;
                model.CatalogRecordTitle = file.CatalogRecord.Title;
                model.File = file;

                model.IsUserCurator = file.CatalogRecord.Curators.Any(x => x.UserName == User.Identity.Name);
                model.IsUserApprover = file.CatalogRecord.Approvers.Any(x => x.UserName == User.Identity.Name) ||
                    OrganizationHelper.DoesUserHaveRight(db, User, file.CatalogRecord.Organization.Id, Right.CanApprove);

                if (file.IsTextFile())
                {
                    model.PreviewType = PreviewType.Text;
                    model.Syntax = GetSyntax(Path.GetExtension(file.Name));

                    string path = string.Empty;

                    string processingDirectory = SettingsHelper.GetProcessingDirectory(file.CatalogRecord.Organization, db);
                    if (!string.IsNullOrWhiteSpace(processingDirectory))
                    {
                        path = Path.Combine(processingDirectory, file.CatalogRecord.Id.ToString(), file.Name);

                        if (System.IO.File.Exists(path))
                        {
                            model.Content = System.IO.File.ReadAllText(path);
                        }
                    }
                }
                else if (file.Name.ToLower().EndsWith(".pdf"))
                {
                    model.PreviewType = PreviewType.Pdf;
                }
                else if (file.Name.ToLower().EndsWith(".rtf"))
                {
                    model.PreviewType = PreviewType.Text;

                    string path = string.Empty;

                    string processingDirectory = SettingsHelper.GetProcessingDirectory(file.CatalogRecord.Organization, db);
                    if (!string.IsNullOrWhiteSpace(processingDirectory))
                    {
                        path = Path.Combine(processingDirectory, file.CatalogRecord.Id.ToString(), file.Name);

                        if (System.IO.File.Exists(path))
                        {
                            string richContent = System.IO.File.ReadAllText(path);
                            model.Content = richContent.ConvertRtfToPlainText();
                        }
                    }
                }
                else if (file.IsImageFile())
                {
                    model.PreviewType = PreviewType.Image;
                }

                return View("~/Areas/Ddi/Views/Preview/Editor.cshtml", model);
            }
        }

        private void EnsureUserHasPermission(ManagedFile file, ApplicationDbContext db)
        {
            var user = db.Users
                .Where(x => x.UserName == User.Identity.Name)
                .FirstOrDefault();
            var permissions = db.Permissions
                .Where(x => x.User.Id == user.Id && x.Organization.Id == file.CatalogRecord.Organization.Id);
            bool userCanViewAll = permissions.Any(x => x.Right == Right.CanViewAllCatalogRecords);

            string createdByUserName = file.CatalogRecord.CreatedBy != null ? file.CatalogRecord.CreatedBy.UserName : string.Empty;
            if (createdByUserName != User.Identity.Name &&
                !file.CatalogRecord.Curators.Any(x => x.UserName == User.Identity.Name) &&
                !file.CatalogRecord.Approvers.Any(x => x.UserName == User.Identity.Name) &&
                !OrganizationHelper.DoesUserHaveRight(db, User, file.CatalogRecord.Organization.Id, Right.CanApprove) &&
                !userCanViewAll)
            {
                throw new HttpException(403, "Only curators may perform this task.");
            }
        }

        string GetSyntax(string extension)
        {
            switch (extension)
            {
                case ".do": return "language-stata";
                case ".sps": return "language-spss";
                case ".r": return "language-r";
                default: return "language-txt";
            }
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

    public static class PreviewStringExtensions
    {
        public static string ConvertRtfToPlainText(this string content)
        {
            var flowDocument = new FlowDocument();
            var textRange = new TextRange(flowDocument.ContentStart, flowDocument.ContentEnd);

            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(content ?? string.Empty)))
            {
                textRange.Load(stream, DataFormats.Rtf);
            }

            return textRange.Text;
        }
    }
}
