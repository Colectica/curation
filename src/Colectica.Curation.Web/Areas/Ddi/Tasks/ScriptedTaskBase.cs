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
using System.Linq;
using System.Text;

namespace Colectica.Curation.Addins.Tasks
{
    public abstract class ScriptedTaskBase : IScriptedTask
    {
        public Guid Id { get; set; }
        
        public string Name { get; set; }

        public int Weight { get; set; }

        public bool CanUpdate { get; set; }

        public string Instructions { get; set; }

        public string Controller { get; set; }

        public virtual bool AppliesToFile(Data.ManagedFile file)
        {
            return false;
        }
    }
}
