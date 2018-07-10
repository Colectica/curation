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

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Colectica.Curation.Contracts
{
    /// <summary>
    /// Represents an action that is triggered at the end of the publication process, to publish the catalog record and its files to a publication target.
    /// For example, an addin could publish the catalog record and files to a Dataverse instance.
    /// </summary>
    public interface IPublishAction
    {
        string Name { get; }

        void PublishRecord(Data.CatalogRecord record, Data.ApplicationUser user, Data.ApplicationDbContext db, string ProcessingDirectory);
    }
}
