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
using Colectica.Curation.Common.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Colectica.Curation.Addins.Tasks
{
    [Export(typeof(IScriptedTask))]
    public class CompareMaterialsTask: ScriptedTaskBase
    {
        public static readonly Guid TypeId = new Guid("8EBEB3A6-B3A2-4B20-B4B1-EA50DFA32C8E");

        public CompareMaterialsTask()
        {
            Id = TypeId;
            Name = "Compare Questionnaire, Codebook, and Data in Data File";
            Weight = 3000;

            CanUpdate = false;
            Instructions = "Please review the relevant documents and publications and verify that references to variables in this data file are consistent with the metadata below.";
            Controller = "CompareMaterials";

            IsForDataFiles = true;
            IsForCodeFiles = false;
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
