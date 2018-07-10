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

namespace Colectica.Curation.Web.Models
{
    public class CatalogRecordIndexModel
    {
        public List<CatalogRecord> records { get; set; }
        public string pi { get; set; }
        public string field_data_subject_tid { get; set; }
        public string keywords { get; set; }
        public string policyArea { get; set; }
        public string field_data_location_tid { get; set; }
        public string archived_value_year { get; set; }
        public string research_design { get; set; }
        public List<SelectListItem> pi_option { get; set; }
        public List<SelectListItem> field_data_subject_tid_option { get; set; }
        public List<SelectListItem> keywords_option { get; set; }
        public List<SelectListItem> policyarea_option { get; set; }
        public List<SelectListItem> field_data_location_option { get; set; }
        public List<SelectListItem> archived_value_year_option { get; set; }
        public List<SelectListItem> research_design_option { get; set; }
    }
}
