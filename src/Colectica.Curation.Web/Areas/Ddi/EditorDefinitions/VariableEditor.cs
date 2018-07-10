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
using Colectica.Curation.Web.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Colectica.Curation.Addins.Editors
{
    [Export(typeof(IManagedFileEditor))]
    public class VariableEditor : IManagedFileEditor
    {
        public string Name { get; set; }

        public int Weight { get; set; }

        public string CatalogRecordId { get; set; }

        public string Location { get; set; }

        public string Controller { get; set; }

        public string Url
        {
            get
            {
                return "/Variables/Editor/";
            }
        }

        public VariableEditor()
        {
            Name = "Variables";
            Weight = 100;
            Controller = "Variables";
        }

        public bool IsValidForFile(ManagedFile file)
        {
            return file.IsStatisticalDataFile();
        }
    }
}
