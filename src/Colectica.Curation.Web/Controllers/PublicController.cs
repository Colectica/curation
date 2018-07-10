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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using Colectica.Curation.Web.Models;

namespace Colectica.Curation.Web.Controllers
{
    public class PublicController : Controller
    {
        [Route("research/data")]
        public ActionResult Index(Query query)
        {
            using (var db = ApplicationDbContext.Create())
            {
                var records = db.CatalogRecords
                    .Where(x => x.Status == CatalogRecordStatus.Published);
           
                if(!String.IsNullOrWhiteSpace(query.pi) && !query.pi.Equals("All"))
                {
                    records = records.Where(x => x.AuthorsText.Equals(query.pi));
                }
                if (!String.IsNullOrWhiteSpace(query.field_data_subject_tid) && !query.field_data_subject_tid.Equals("All"))
                {
                    records = records.Where(x => x.DataSource.Equals(query.field_data_subject_tid));
                }
                if (!String.IsNullOrWhiteSpace(query.field_data_location_tid) && !query.field_data_location_tid.Equals("All"))
                {
                    records = records.Where(x => x.Location.Equals(query.field_data_location_tid));
                }
                if (!String.IsNullOrWhiteSpace(query.keywords) && !query.keywords.Equals("All"))
                {
                    records = records.Where(x => x.Keywords.Contains(query.keywords));
                }
                if (!String.IsNullOrWhiteSpace(query.policyArea) && !query.policyArea.Equals("All"))
                {
                    records = records.Where(x => x.ResearchDesign.Equals(query.policyArea));
                }
                if (!String.IsNullOrWhiteSpace(query.archived_value_year) && !query.archived_value_year.Equals("All"))
                {
                    records = records.Where(x => x.ArchiveDate.Value.Year.ToString().Equals(query.archived_value_year));
                }
                if (!String.IsNullOrWhiteSpace(query.research_design) && !query.research_design.Equals("All"))
                {
                    records = records.Where(x => x.ResearchDesign.Equals(query.research_design));
                }
                var model = new CatalogRecordIndexModel();
                model.records = records.ToList();
                //get the unique author name
                var AuthorRecord = db.CatalogRecords
                    .Include(x => x.Authors)
                    .Where(x => x.Status == CatalogRecordStatus.Published)
                    .Select(x => x.AuthorsText).Distinct();
                
                var option = new List<SelectListItem>();
                foreach (var record in AuthorRecord)
                {
                    var define = new SelectListItem();
                    define.Text = record;
                    define.Value = record;
                    option.Add(define);
                }
                //get Discipline
                //TODO ~~

                //get keywords
                var Keywords = db.CatalogRecords
                    .Include(x => x.Authors)
                    .Where(x => x.Status == CatalogRecordStatus.Published)
                    .Select(x => x.Keywords).Distinct();

                var keywordsoption = new List<SelectListItem>();
                foreach (var record in Keywords)
                {
                    var define = new SelectListItem();
                    define.Text = record;
                    define.Value = record;
                    keywordsoption.Add(define);
                }

                //archive year
                var year = db.CatalogRecords
                    .Include(x => x.Authors)
                    .Where(x => x.Status == CatalogRecordStatus.Published)
                    .Select(x => x.ArchiveDate.Value.Year.ToString()).Distinct();
                var ListOfYear = new List<SelectListItem>();
                foreach(var y in year)
                {
                    var define = new SelectListItem();
                    define.Text = y;
                    define.Value = y;
                    ListOfYear.Add(define);
                }

                //Location option
                var location_opt = db.CatalogRecords
                    .Include(x => x.Authors)
                    .Where(x => x.Status == CatalogRecordStatus.Published)
                    .Select(x => x.Location).Distinct();
                var location_option = new List<SelectListItem>();
                foreach(var opt in location_opt)
                {
                    if (String.IsNullOrEmpty(opt)) continue;
                    var define = new SelectListItem();
                    define.Text = opt;
                    define.Value = opt;
                    location_option.Add(define);
                }

                //policy area
                //TODO ~~

                //research design
                var research_design = db.CatalogRecords
                    .Include(x => x.Authors)
                    .Where(x => x.Status == CatalogRecordStatus.Published)
                    .Select(x => x.ResearchDesign).Distinct();
                var research_design_option = new List<SelectListItem>();
                foreach(var rdo in research_design)
                {
                    if (String.IsNullOrEmpty(rdo)) continue;
                    var define = new SelectListItem();
                    define.Text = rdo;
                    define.Value = rdo;
                    research_design_option.Add(define);
                }

                model.pi_option = option;
                model.keywords_option = keywordsoption;
                //model.field_data_subject_tid = field_data;
                //model.policyArea = policyArea;
                model.field_data_location_option = location_option;
                model.archived_value_year_option = ListOfYear;
                model.research_design_option = research_design_option;
                return View(model);
            }
        }
     
        public ActionResult Option()
        {
            using (var db = ApplicationDbContext.Create())
            {
                var records = db.CatalogRecords
                    .Include(x => x.Authors)
                    .Where(x => x.Status == CatalogRecordStatus.Published)
                    .Select(x => x.AuthorsText).Distinct();

                
                return View();
            }
        }

        [Route("research/data/{studyId}")]
        public ActionResult Details(string studyId)
        {
            using (var db = ApplicationDbContext.Create())
            {
                var catalogRecord = db.CatalogRecords
                    .Include(x => x.Authors)
                    .Include(x => x.Files)
                    .FirstOrDefault(x => string.Compare(x.Number, studyId, true) == 0);
                if (catalogRecord == null ||
                    catalogRecord.Status != CatalogRecordStatus.Published)
                {
                    return HttpNotFound("Catalog record could not be found");
                }

                return View(catalogRecord);
            }

        }
    }
}
