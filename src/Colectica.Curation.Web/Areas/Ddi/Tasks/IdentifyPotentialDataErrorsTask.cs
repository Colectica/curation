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
using Colectica.Curation.Web.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Colectica.Curation.Addins.Tasks
{
    [Export(typeof(IScriptedTask))]
    public class IdentifyPotentialDataErrorsTask : ScriptedTaskBase
    {
        public static readonly Guid TypeId = new Guid("9FB1500B-EA6D-4A4B-BAFF-10AE148DC185");

        public IdentifyPotentialDataErrorsTask()
        {
            Id = TypeId;
            Name = "Identify Potential Errors in Data File";
            Weight = 5000;

            CanUpdate = false;
            Instructions = "Please review the summary statistics to ensure there are no errors in the data.";
            Controller = "IdentifyPotentialDataErrors";
        }

        public override bool AppliesToFile(Data.ManagedFile file)
        {
            if (string.IsNullOrWhiteSpace(file.Name))
            {
                return false;
            }

            if (file.IsStatisticalDataFile())
            {
                return true;
            }   

            return false;
        }
        
    }
}
