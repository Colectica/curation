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
    public class ConfirmCodeReplicatesResultsTask : ScriptedTaskBase
    {
        public static readonly Guid TypeId = new Guid("1816544E-EECB-4C6F-97D5-BFE1C6ED14F0");
        
        public ConfirmCodeReplicatesResultsTask()
        {
            Id = TypeId;
            Name = "Confirm Code Replicates Reported Results";
            Weight = 7000;

            CanUpdate = false;
            Instructions = "Please confirm that the code replicates the reported results.";
            Controller = "ConfirmCodeReplicatesResults";
        }

        public override bool AppliesToFile(Data.ManagedFile file)
        {
            if (string.IsNullOrWhiteSpace(file.Name))
            {
                return false;
            }

            if (file.IsStatisticalCommandFile())
            {
                return true;
            }   

            return false;
        }
        
    }
}
