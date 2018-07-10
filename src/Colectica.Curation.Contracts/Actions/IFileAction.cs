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
using System.Text;
using System.Threading.Tasks;

namespace Colectica.Curation.Contracts
{
    /// <summary>
    /// Represents an action that is triggered when a file is ingested into the curation system.
    /// For example, DDI variable-level metadata can be extracted when a statistical data file is ingested.
    /// </summary>
    /// <remarks>
    /// Addins of this type are automatically called when the file is ingested into the curation system.
    /// Addins of this type can also be manually triggered by the user, if allowed by the addin.
    /// </remarks>
    public interface IFileAction
    {

        string Name { get; }

        /// <summary>
        /// Determines whether the user is ever allowed to manually run this action for a file.
        /// </summary>
        bool IsManualActivationAllowed { get; }

        /// <summary>
        /// Determines whether this action can currently run for the specified file.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        bool CanRun(ManagedFile file);

        void Run(CatalogRecord record, ManagedFile file, ApplicationUser user, ApplicationDbContext db, string processingDirectory, string agencyId);
    }
}
