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
    /// Represents an action that is triggered during catalog record publication.
    /// The action can create one or more persistent identifiers and assign the identifier(s)
    /// to the catalog record and its files.
    /// </summary>
    public interface ICreatePersistentIdentifiersAction
    {
        string Name { get; }

        CreatePersisitentIdentifiersResult CreatePersistentIdentifiers(CatalogRecord record, ApplicationUser user, ApplicationDbContext db);
    }

    public class CreatePersisitentIdentifiersResult
    {
        public Guid DdiFileId { get; set; }
        public string DdiFileHandle { get; set; }

        public bool Skipped { get; set; }
        public bool Successful { get; set; }

        public List<string> Messages { get; set; }

        public CreatePersisitentIdentifiersResult()
        {
            Messages = new List<string>();
        }


    }
}
