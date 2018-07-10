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
using Algenta.Colectica.Model.Repository;
using Algenta.Colectica.Repository;
using Colectica.Curation.Addins.Editors.Mappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Colectica.Curation.Web.Areas.Ddi.Utility
{
    public class VersionUpdater
    {
        /// <summary>
        /// Update versions and references to a Variable along this path:
        ///   PhysicalInstance
        ///     DataRelationship
        ///       Variable
        ///     VariableStatistic
        ///       Variable
        /// </summary>
        /// <param name="variable"></param>
        public static void UpdateVersionsAndSave(Variable variable, PhysicalInstance physicalInstance, DataRelationship dataRelationship, VariableStatistic variableStatistic)
        {
            var client = RepositoryHelper.GetClient();
            var now = DateTime.UtcNow;

            var itemsToRegister = new List<IVersionable>();
            itemsToRegister.Add(variable);
            itemsToRegister.Add(physicalInstance);
            itemsToRegister.Add(dataRelationship);
            itemsToRegister.Add(variableStatistic);

            // Increase version of the variable.
            variable.Version++;
            variable.VersionDate = now;

            // Increase the version and reference of the PhysicalInstance that 
            // references the DataRelationship and VariableStatistic.
            physicalInstance.Version++;
            physicalInstance.VersionDate = now;

            // Increase the version and reference of the DataRelationship that references the variable.
            dataRelationship.Version++;
            dataRelationship.VersionDate = now;

            // Update the reference to the variable.
            var variableInDataRelationship = dataRelationship.LogicalRecords
                .SelectMany(x => x.VariablesInRecord)
                .Where(x => x.Identifier == variable.Identifier)
                .FirstOrDefault();

            variableInDataRelationship.Version = variable.Version;
            variableInDataRelationship.VersionDate = now;

            // Increase the version and reference of hte VariableStatistic that references the Variable.
            variableStatistic.Version++;
            variableStatistic.VersionDate = now;
            variableStatistic.VariableReference.Version = variable.Version;

            // Register all the changes.
            var options = new CommitOptions();
            client.RegisterItems(itemsToRegister, options);
        }
    }
}
