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

﻿using Colectica.Curation.Contracts;
using Colectica.Curation.Data;
using Colectica.Curation.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Colectica.Curation.Models
{
    public class ScriptedTaskModel
    {
        public IScriptedTask Task { get; set; }

        public bool IsUserCurator { get; set; }

        public Guid CatalogRecordId { get; set; }

        public string CatalogRecordTitle { get; set; }

        public ManagedFile File { get; set; }

        public Guid FileId { get; set; }

        public List<FileViewModel> PrimaryFiles { get; set; }

        public List<FileViewModel> AllFiles { get; set; }

        public string VariablesJson { get; set; }

        public ScriptedTaskModel()
        {
            PrimaryFiles = new List<FileViewModel>();
            AllFiles = new List<FileViewModel>();
        }

        public int ObservationCount { get; set; }
    }
}
