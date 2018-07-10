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
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Colectica.Curation.Addins.Tasks
{
    [Export(typeof(IScriptedTask))]
    public class CheckForMissingLabelsTask : ScriptedTaskBase
    {

        public static readonly Guid TypeId = new Guid("301BF93B-AE0E-4AE0-A373-1285317CE654");
        public CheckForMissingLabelsTask()
        {
            Id = TypeId; 
            Name = "Check Missing Labels";
            Weight = 2000;

            CanUpdate = true;
            Instructions = "Please review the following items that have missing labels. Both variable labels and value labels should be provided before approving this task. Variables and values with missing labels are automatically detected and displayed below.";
            Controller = "CheckForMissingLabels";
        }

        public override bool AppliesToFile(Data.ManagedFile file)
        {
            if (string.IsNullOrWhiteSpace(file.Name))
            {
                return false;
            }

            string lower = file.Name.ToLower();
            if (lower.Contains(".dta") ||
                lower.Contains(".sav"))
            {
                return true;
            }

            return false;
        }
    }


}
