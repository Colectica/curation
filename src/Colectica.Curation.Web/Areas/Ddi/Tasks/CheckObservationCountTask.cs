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
    public class CheckObservationCountTask : ScriptedTaskBase
    {
        public static readonly Guid TypeId = new Guid("CBB44988-F0CC-455F-8A18-F0B001B3AA3F");

        public CheckObservationCountTask()
        {
            Id = TypeId;
            Name = "Review Observation Count";
            Weight = 1000;

            CanUpdate = false;
            Instructions = "Please verify that the number of observations reported in the relevant documents and publications matches the number of observations in this data file.";
            Controller = "CheckObservationCount";
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
