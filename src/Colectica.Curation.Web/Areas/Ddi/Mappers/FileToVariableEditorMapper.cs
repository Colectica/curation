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

﻿using Algenta.Colectica.Model.Ddi;
using Algenta.Colectica.Repository;
using Colectica.Curation.Addins.Editors.Models;
using Colectica.Curation.Data;
using Colectica.Curation.DdiAddins.Utility;
using Colectica.Curation.Web.Areas.Ddi.Utility;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Colectica.Curation.Addins.Editors.Mappers
{
    public class FileToVariableEditorMapper
    {
        public static VariableEditorViewModel GetModelFromFile(ManagedFile file)
        {
            var logger = LogManager.GetLogger("FileToVariableEditorMapper");
            logger.Debug("Entering GetModelFromFile()");

            var model = new VariableEditorViewModel(file);

            // Get the DDI PhysicalInstance that corresponds to this ManagedFile.
            logger.Debug("Getting PhysicalInstance");
            var physicalInstance = GetPhysicalInstance(file, file.CatalogRecord.Organization.AgencyID);
            logger.Debug("Got PhysicalInstance");

            // Make the list of variables.
            if (physicalInstance == null)
            {
                logger.Debug("Got null PhysicalInstance");
                return model;
            }

            // Populate what is needed to get the list of variables.
            logger.Debug("Populating PhysicalInstance");
            var client = RepositoryHelper.GetClient();
            foreach (var dr in physicalInstance.DataRelationships)
            {
                client.PopulateItem(dr, false, Algenta.Colectica.Model.Repository.ChildReferenceProcessing.PopulateLatest);
            }
            logger.Debug("Populated PhysicalInstance");

            var allVariables = physicalInstance.DataRelationships
                .SelectMany(x => x.LogicalRecords)
                .SelectMany(x => x.VariablesInRecord);

            foreach (var variable in allVariables)
            {
                var variableModel = new VariableModel()
                {
                    Id = variable.Identifier.ToString(),
                    Agency = variable.AgencyId,
                    Name = variable.ItemName.Current,
                    Label = variable.Label.Current,
                    Version = variable.Version,
                    LastUpdated = variable.VersionDate.ToShortDateString()
                };
                model.Variables.Add(variableModel);
            }

            model.VariablesJson = Newtonsoft.Json.JsonConvert.SerializeObject(model.Variables, Newtonsoft.Json.Formatting.None);

            model.IsUserCurator = false;
            model.IsUserApprover = false;

            logger.Debug("Leaving GetModelFromFile()");
            return model;
        }

        public static PhysicalInstance GetPhysicalInstance(ManagedFile file, string agencyID)
        {
            var client = RepositoryHelper.GetClient();

            var pi = client.GetLatestItem(file.Id, agencyID)
                as PhysicalInstance;

            return pi;
        }
    }
}
