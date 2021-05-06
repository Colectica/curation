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

using Algenta.Colectica.Data.Stata;
using Algenta.Colectica.Model.Ddi;
using Algenta.Colectica.Model.Repository;
using Algenta.Colectica.Model.Utility;
using Algenta.Colectica.Repository;
using Colectica.Curation.DdiAddins.Utility;
using Colectica.Curation.Web.Areas.Ddi.Utility;
using LumenWorks.Framework.IO.Csv;
using Spss.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Web;

namespace Colectica.Curation.Addins.Editors.Mappers
{
    public class VariableMapper
    {

        public static void UpdateVariableProperty(Variable variable, PhysicalInstance physicalInstance, DataRelationship dataRelationship, VariableStatistic variableStatistic, string propertyName, string value)
        {
            if (propertyName == "ItemName")
            {
                variable.ItemName.Current = value;
            }
            else if (propertyName == "Label")
            {
                variable.Label.Current = value;
            }
            else if (propertyName == "Description")
            {
                variable.Description.Current = value;
            }
            else if (propertyName == "AnalysisUnit")
            {
                variable.AnalysisUnit = value;
            }
            else if (propertyName == "ResponseUnit")
            {
                variable.ResponseUnit = value;
            }
            else if (propertyName == "RepresentationType")
            {
                if (value == "Text" && variable.RepresentationType != RepresentationType.Text)
                {
                    // Clear any existing category statistics.
                    variableStatistic.UnfilteredCategoryStatistics.Clear();

                    variable.RepresentationType = RepresentationType.Text;
                }
                else if (value == "Numeric" && variable.RepresentationType != RepresentationType.Numeric)
                {
                    // Clear any existing category statistics.
                    variableStatistic.UnfilteredCategoryStatistics.Clear();

                    variable.RepresentationType = RepresentationType.Numeric;
                }
                else if (value == "Code" && variable.RepresentationType != RepresentationType.Code)
                {
                    variable.RepresentationType = RepresentationType.Code;
                    CreateCodeList(variable, physicalInstance, dataRelationship, variableStatistic);
                }
            }
        }

        static void CreateCodeList(Variable variable, PhysicalInstance physicalInstance, DataRelationship dataRelationship, VariableStatistic variableStatistic)
        {
            // Create and save a code list with all the unique values.
            var codeList = CreateCodeList(physicalInstance, dataRelationship, variable, variableStatistic);
            variable.CodeRepresentation.Codes = codeList;

            if (codeList != null)
            {
                var gatherer = new ItemGathererVisitor();
                codeList.Accept(gatherer);

                var client = RepositoryHelper.GetClient();
                var repoOptions = new CommitOptions();
                repoOptions.NamedOptions.Add("RegisterOrReplace");
                client.RegisterItems(gatherer.FoundItems, repoOptions);
            }
        }

        static CodeList CreateCodeList(PhysicalInstance physicalInstance, DataRelationship dataRelationship, Variable variable, VariableStatistic stats)
        {
            using (IDataReader reader = GetReaderForPhysicalInstance(physicalInstance))
            {
                if (reader == null)
                {
                    return null;
                }

                int columnIdx = GetColumnIndex(physicalInstance, dataRelationship, variable);
                if (columnIdx == -1)
                {
                    return null;
                }

                var gatherer = new DataValueFrequencyGatherer(reader);
                var values = gatherer.CountValueFrequencies(columnIdx);

                var codeList = new CodeList() { AgencyId = variable.AgencyId };
                codeList.ItemName.Copy(variable.ItemName);
                codeList.Label.Copy(variable.Label);

                // Clear any existing category statistics.
                stats.UnfilteredCategoryStatistics.Clear();

                foreach (var pair in values.OrderBy(x => x.Key))
                {
                    string value = pair.Key;
                    int count = pair.Value;

                    // Create the code and category.
                    var category = new Category() { AgencyId = variable.AgencyId };
                    var code = new Code() { AgencyId = variable.AgencyId };
                    code.Value = value;
                    code.Category = category;

                    codeList.Codes.Add(code);

                    // Update the statistics with category frequency.
                    var catStats = new CategoryStatistics();
                    catStats.CategoryValue = value.ToString();
                    catStats.Frequency = count;
                    stats.UnfilteredCategoryStatistics.Add(catStats);

                }

                return codeList;
            }
        }

        static int GetColumnIndex(PhysicalInstance physicalInstance, DataRelationship dataRelationship, Variable variable)
        {
            var logicalRecord = dataRelationship.LogicalRecords.FirstOrDefault();

            if (logicalRecord == null)
            {
                return -1;
            }

            var variableInRecord = logicalRecord.VariablesInRecord
                .Where(x => x.CompositeId == variable.CompositeId)
                .FirstOrDefault();
            if (variableInRecord == null)
            {
                return -1;
            }

            return logicalRecord.VariablesInRecord.IndexOf(variableInRecord);
        }

        static IDataReader GetReaderForPhysicalInstance(PhysicalInstance physicalInstance)
        {
            // HACK get this by looking at the IDataImporter addins.
            string fileName = GetLocalFileName(physicalInstance);
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return null;
            }

            string ext = System.IO.Path.GetExtension(fileName).ToLower();
            if (ext == ".dta")
            {
                return new StataDataReader(fileName);
            }
            else if (ext == ".sav")
            {
                return new SpssDataReader(fileName);
            }
            else if (ext == ".csv")
            {
                // TODO Use a stream and close it.
                string contents = File.ReadAllText(fileName);
                return new CsvReader(new StringReader(contents), true);
            }
            else if (ext == ".rdata" ||
                ext == ".rda")
            {
                // TODO
                return null;
            }

            return null;
        }

        public static PhysicalInstance GetPhysicalInstanceWithVariable(IdentifierTriple variableId, RepositoryClientBase client)
        {
            var piID = GetContainingPhysicalInstance(variableId, client);
            if (piID != null)
            {
                return client.GetItem(piID) as PhysicalInstance;
            }

            return null;
        }

        public static IdentifierTriple GetContainingPhysicalInstance(IdentifierTriple id, RepositoryClientBase client)
        {
            var facet = new SetSearchFacet();
            facet.ItemTypes.Add(DdiItemType.PhysicalInstance);
            facet.ReverseTraversal = true;
            facet.LeafItemTypes.Add(DdiItemType.VariableScheme);
            facet.LeafItemTypes.Add(DdiItemType.VariableGroup);

            var response = client.SearchTypedSet(id, facet);

            var piID = response.OrderByDescending(x => x.Version).FirstOrDefault();
            if (piID == null)
            {
                return null;
            }

            return piID.CompositeId;
        }

        static string GetLocalFileName(PhysicalInstance physicalInstance)
        {
            if (physicalInstance == null ||
                physicalInstance.FileIdentifications == null ||
                physicalInstance.FileIdentifications.Count == 0)
            {
                return string.Empty;
            }

            var localFileID = physicalInstance.FileIdentifications[0];

            string fileName = string.Empty;
            if (localFileID.Uri != null)
            {
                fileName = localFileID.Uri.ToString();
            }

            if (fileName == null)
            {
                return null;
            }

            string fullFileName = null;

            if (fileName.StartsWith("file:"))
            {
                fileName = fileName.Replace("file:///", string.Empty).Replace("file:", string.Empty);
            }

            if (!string.IsNullOrEmpty(fileName))
            {
                if (System.IO.File.Exists(fileName))
                {
                    fullFileName = fileName;
                    return fullFileName;
                }
                else
                {
                    Console.WriteLine("Test");
                }
            }
            else
            {
                Console.WriteLine("test");
            }

            return null;
        }


    }
}
