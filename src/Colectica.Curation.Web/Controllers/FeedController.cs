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

﻿using Colectica.Curation.Common.ViewModels;
using Colectica.Curation.Data;
using Colectica.Curation.Web.Utility;
using log4net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Xml.Linq;

namespace Colectica.Curation.Web.Controllers
{
    public class FeedController : Controller
    {
        public ActionResult Records()
        {
            var logger = LogManager.GetLogger("Curation");
            logger.Debug("Entering Records()");

            logger.Debug("Creating XML root");
            var root = new XElement("publishedCatalogRecords");

            logger.Debug("Opening DB context");
            using (var db = ApplicationDbContext.Create())
            {
                var org = OrganizationHelper.GetOrganizationByHost(Request, db);
                if (org == null)
                {
                    return RedirectToAction("Index", "Dashboard");
                }

                // Get all published records.
                logger.Debug("Getting published records");
                var publishedRecords = db.CatalogRecords
                    .Where(x => x.Organization.Id == org.Id)
                    .Where(x => x.Status == CatalogRecordStatus.Published)
                    .Include(x => x.Authors)
                    .Include(x => x.Files);

                int i = 1;
                foreach (var record in publishedRecords)
                {
                    logger.Debug($"Writing record {i++}");

                    // Create an element for this record.
                    var recordElement = new XElement("Record");
                    root.Add(recordElement);

                    // Add all the properties to the XML.
                    recordElement.Add(new XElement("Guid", record.Id));
                    recordElement.Add(new XElement("Title", record.Title));
                    recordElement.Add(new XElement("Author", record.AuthorsText));

                    if (!string.IsNullOrWhiteSpace(record.Owner?.FullName))
                    {
                        recordElement.Add(new XElement("Owner", record.Owner.FullName));
                    }
                    else
                    {
                        recordElement.Add(new XElement("Owner", record.OwnerText));
                    }

                    recordElement.Add(new XElement("Description", record.Description));
                    recordElement.Add(new XElement("StudyID", record.Number));
                    recordElement.Add(new XElement("StudyIDLower", record.Number?.ToLower()));
                    recordElement.Add(new XElement("RelatedPublication", record.RelatedPublications));
                    recordElement.Add(new XElement("RelatedProject", record.RelatedProjects));
                    recordElement.Add(new XElement("RelatedDatabase", record.RelatedDatabase));
                    recordElement.Add(new XElement("keywords", record.Keywords));
                    recordElement.Add(new XElement("CreateDate", record.CreatedDate));
                    recordElement.Add(new XElement("ResearchDesign", record.ResearchDesign));

                    CatalogRecordMethodsViewModel.GetConcatenatedDataProperties(record, out string dataType, out string dataSource, out string dataSourceInformation);
                    recordElement.Add(new XElement("DataType", dataType));
                    recordElement.Add(new XElement("DataSource", dataSource));
                    recordElement.Add(new XElement("DataSourceInformation", dataSourceInformation));

                    // Show information from the catalog record independently.
                    recordElement.Add(new XElement("CatalogRecordDataType", record.DataType));
                    recordElement.Add(new XElement("CatalogRecordDataSource", record.DataSource));
                    recordElement.Add(new XElement("CatalogRecordDataSourceInformation", record.DataSourceInformation));

                    if (!string.IsNullOrWhiteSpace(record.PersistentId))
                    {
                        string pid = record.PersistentId;
                        if (!pid.StartsWith("http"))
                        {
                            pid = "http://hdl.handle.net/" + pid;
                        }

                        recordElement.Add(new XElement("PersistentId", pid));
                    }
                    else
                    {
                        recordElement.Add(new XElement("PersistentId", string.Empty));
                    }

                    bool hasFieldDates = false;
                    if (!string.IsNullOrWhiteSpace(record.FieldDates))
                    {
                        var fieldDatesModel = JsonConvert.DeserializeObject<DateModel>(record.FieldDates);
                        if (fieldDatesModel != null)
                        {
                            hasFieldDates = true;

                            if (fieldDatesModel.isRange)
                            {
                                recordElement.Add(new XElement("FieldDates", $"{fieldDatesModel.date} - {fieldDatesModel.endDate}"));
                            }
                            else
                            {
                                recordElement.Add(new XElement("FieldDates", fieldDatesModel.date));
                            }
                        }
                    }
                    if (!hasFieldDates)
                    {
                        recordElement.Add(new XElement("FieldDates", string.Empty));
                    }

                    recordElement.Add(new XElement("Location", record.Location));
                    recordElement.Add(new XElement("LocationDetails", record.LocationDetails));
                    recordElement.Add(new XElement("UnitOfObservation", record.UnitOfObservation));
                    recordElement.Add(new XElement("SampleSize", record.SampleSize));
                    recordElement.Add(new XElement("InclusionExclusionCriteria", record.InclusionExclusionCriteria));
                    recordElement.Add(new XElement("RandomizedProcedure", record.RandomizationProcedure));
                    recordElement.Add(new XElement("Treatment", record.Treatment));
                    recordElement.Add(new XElement("TreatmentAdministration", record.TreatmentAdministration));
                    recordElement.Add(new XElement("OutcomeMeasures", record.OutcomeMeasures));
                    recordElement.Add(new XElement("ArchiveDate", record.ArchiveDate));
                    var fileElement = new XElement("FileElement");

                    int f = 1;
                    foreach(var file in record.Files)
                    {
                        logger.Debug($"Writing file {f++}");

                        if (string.IsNullOrWhiteSpace(file.Number))
                        {
                            continue;
                        }
                        
                        var fileinfo = new XElement("File");
                        fileinfo.Add(new XElement("id", file.Id));
                        fileinfo.Add(new XElement("FileSize", file.Size));

                        string fileUrl = file.PersistentLink;
                        if (!string.IsNullOrWhiteSpace(fileUrl))
                        {
                            if (!fileUrl.StartsWith("http"))
                            {
                                fileUrl = "http://hdl.handle.net/" + fileUrl;
                            }
                            fileinfo.Add(new XElement("FileUrl", fileUrl));
                        }
                        else
                        {
                            fileinfo.Add(new XElement("FileUrl", string.Empty));
                        }

                        fileinfo.Add(new XElement("FileNumber", file.Number));
                        fileinfo.Add(new XElement("FileDescription", file.Title));
                        fileinfo.Add(new XElement("FileFormat", file.FormatName));
                        fileinfo.Add(new XElement("PublicFile", file.IsPublicAccess ? "1" : "0"));
                        fileinfo.Add(new XElement("CatalogRecordId", record.Number));
                        fileElement.Add(fileinfo);
                    }

                    recordElement.Add(fileElement);
                }

            }

            // Write the XML to the web response.
            logger.Debug("Writing XML");

            XDocument document = new XDocument(root);
            string xmlStr = document.Root.ToString();
            return Content(xmlStr, "text/xml");

            logger.Debug("Leaving Records()");
        }

        public ActionResult Keywords()
        {
            using (var db = ApplicationDbContext.Create())
            {
                var root = new XElement("keywords");

                var combinedKeywords = db.CatalogRecords
                    .Where(x => x.Status == CatalogRecordStatus.Published)
                    .Select(x => x.Keywords)
                    .ToList();
                var allKeywords = new List<string>();
                foreach (string combined in combinedKeywords)
                {
                    allKeywords.AddRange(combined.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries));
                }

                var distinct = allKeywords.Distinct();

                foreach (string keyword in distinct)
                {
                    root.Add(new XElement("keyword", keyword));
                }

                XDocument document = new XDocument(root);
                string xmlStr = document.Root.ToString();
                return Content(xmlStr, "text/xml");
            }
        }

        public ActionResult ResearchDesigns()
        {
            using (var db = ApplicationDbContext.Create())
            {
                var root = new XElement("researchDesigns");

                var combinedResults = db.CatalogRecords
                    .Where(x => x.Status == CatalogRecordStatus.Published)
                    .Select(x => x.ResearchDesign)
                    .ToList();
                var allResults = new List<string>();
                foreach (string combined in combinedResults)
                {
                    allResults.AddRange(combined.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries));
                }

                var distinct = allResults
                    .Select(x => x.Trim())
                    .Distinct();

                foreach (string x in distinct)
                {
                    root.Add(new XElement("researchDesign", x));
                }

                XDocument document = new XDocument(root);
                string xmlStr = document.Root.ToString();
                return Content(xmlStr, "text/xml");
            }
        }

        public ActionResult Locations()
        {
            using (var db = ApplicationDbContext.Create())
            {
                var root = new XElement("locations");

                var location = db.CatalogRecords
                    .Where(x => x.Status == CatalogRecordStatus.Published)
                    .Select(x => x.Location)
                    .Where(x => x != "")
                    .Distinct()
                    .ToList();

                foreach (string x in location)
                {
                    root.Add(new XElement("location", x));
                }

                XDocument document = new XDocument(root);
                string xmlStr = document.Root.ToString();
                return Content(xmlStr, "text/xml");
            }

        }
    }
}
