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

﻿using Algenta.Colectica.Model;
using Algenta.Colectica.Model.Ddi;
using Algenta.Colectica.Model.Utility;
using Algenta.Colectica.Repository;
using Colectica.Curation.Contracts;
using Colectica.Curation.Data;
using Colectica.Curation.Addins.Editors.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Web;
using Colectica.Curation.DdiAddins.Utility;

namespace Colectica.Curation.Addins.Editors.CatalogRecordInitializers
{
    [Export(typeof(ICreateCatalogRecordAction))]
    public class StudyUnitInitializer : ICreateCatalogRecordAction
    {
        string agency;

        public string Name
        {
            get { return "Initialize DDI StudyUnit"; }
        }

        public StudyUnitInitializer()
        {
            MultilingualString.CurrentCulture = "en-US"; // TODO Configuration
        }

        public void Initialize(CatalogRecord catalogRecord, string agencyID)
        {
            this.agency = agencyID;

            // Create the item.
            var studyUnit = new StudyUnit() { AgencyId = agency };
            studyUnit.DublinCoreMetadata.Title.Current = catalogRecord.Title;

            // Store the item in the repository.
            // Where is this configured?
            var client = RepositoryHelper.GetClient();
            var repoOptions = new Algenta.Colectica.Model.Repository.CommitOptions();
            repoOptions.NamedOptions.Add("RegisterOrReplace");
            client.RegisterItem(studyUnit, repoOptions);

            // Assign the identification to the catalog record.
            catalogRecord.Id = studyUnit.Identifier;
        }
    }
}
