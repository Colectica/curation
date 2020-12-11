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
using Algenta.Colectica.Model.Ddi.Utility;
using Algenta.Colectica.Model.Utility;
using Algenta.Colectica.ViewModel.Import;
using log4net;
using RDotNet;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Colectica.Curation.Operations
{
    public class RDataImporter : IDataImporter
    {
        string interopError;
        REngine engine;
        HarmonizingCache harmonizingCache;

        public string Name
        {
            get { return "Import RData"; }
        }

        public int Weight
        {
            get { return 1500; }
        }

        ILog logger;

        public RDataImporter()
        {
            logger = LogManager.GetLogger("Curation");

            try
            {
                REngine.SetEnvironmentVariables();
                this.engine = REngine.GetInstance();

                logger.Info("REngine version " + engine.DllVersion);

            }
            catch (Exception ex)
            {
                interopError = ex.Message;
            }
        }

        public bool AreSystemRequirementsSatisfied(out string message)
        {
            message = this.interopError;
            return string.IsNullOrEmpty(message);
        }

        public bool CanImportDataSource(string path)
        {
            string lower = path.ToLower();
            if (lower.EndsWith(".rdata") ||
                lower.EndsWith(".rda"))
            {
                return true;
            }

            return false;
        }

        public string DefaultExtension
        {
            get { return ".rdata"; }
        }

        public string FileFilter
        {
            get { return "RData File|*.rdata"; }
        }

        public ResourcePackage Import(string path, string agencyId)
        {
            this.harmonizingCache = new HarmonizingCache(MultilingualString.CurrentCulture);

            var resourcePackage = new ResourcePackage();
            resourcePackage.AgencyId = agencyId;

            logger.Debug("Importing RData");

            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("fileName");
            }
            if (!File.Exists(path))
            {
                throw new ArgumentException("The specified file must exist");
            }

            string fileNameWithExtension = Path.GetFileName(path);
            string fileNameOnly = Path.GetFileNameWithoutExtension(path);

            logger.Debug("RData import file: " + fileNameOnly);


            resourcePackage.DublinCoreMetadata.Title.Current = fileNameOnly;

            // Create the PhysicalInstance.
            var physicalInstance = new PhysicalInstance() { AgencyId = agencyId };
            resourcePackage.PhysicalInstances.Add(physicalInstance);
            physicalInstance.DublinCoreMetadata.Title.Current = fileNameOnly;

            // File location
            if (path != null)
            {
                DataFileIdentification fileID = new DataFileIdentification();
                Uri uri;
                if (Uri.TryCreate(path, UriKind.RelativeOrAbsolute, out uri))
                {
                    fileID.Uri = uri;
                }
                fileID.Path = path;

                physicalInstance.FileIdentifications.Add(fileID);
            }

            // Create the DataRelationship.
            var dataRelationship = new DataRelationship();
            physicalInstance.DataRelationships.Add(dataRelationship);
            dataRelationship.AgencyId = agencyId;
            dataRelationship.Label.Current = fileNameOnly;

            // Load the file into R.
            string pathForR = path.Replace("\\", "/");
            engine.Evaluate(string.Format("load('{0}')", pathForR));


            // Find all the data frames.
            var dataFrames = GetDataFrames();

            // For each data frame in the RData file, create a LogicalRecord.
            foreach (var pair in dataFrames)
            {
                string name = pair.Key;
                var dataFrame = pair.Value;

                // TODO This should be tracked per record, not PhysicalInstance.
                physicalInstance.FileStructure.CaseQuantity = dataFrame.RowCount;

                var logicalRecord = new LogicalRecord() { AgencyId = agencyId };
                dataRelationship.LogicalRecords.Add(logicalRecord);
                logicalRecord.Label.Current = name;

                List<string> variableLabels = null;
                var variableLabelsExpr = dataFrame.GetAttribute("var.labels");
                if (variableLabelsExpr != null)
                {
                    var labelVector = variableLabelsExpr.AsVector();
                    variableLabels = new List<string>(labelVector.Select(x => (string)x));
                }

                for (int i = 0; i < dataFrame.ColumnCount; i++)
                {
                    string columnName = dataFrame.ColumnNames[i];
                    var column = dataFrame[i];

                    var variable = new Variable() { AgencyId = agencyId };
                    logicalRecord.VariablesInRecord.Add(variable);

                    // Name
                    variable.ItemName.Current = columnName;

                    // Label
                    if (variableLabels != null)
                    {
                        variable.Label.Current = variableLabels[i];
                    }

                    // Type
                    if (column.Type == RDotNet.Internals.SymbolicExpressionType.NumericVector)
                    {
                        variable.RepresentationType = RepresentationType.Numeric;
                        variable.Additivity = AdditivityType.Stock;
                    }
                    else if (column.Type == RDotNet.Internals.SymbolicExpressionType.IntegerVector)
                    {
                        if (column.IsFactor())
                        {
                            variable.RepresentationType = RepresentationType.Code;

                            string[] factors = column.AsFactor().GetLevels();
                            variable.CodeRepresentation.Codes = GetCodeList(factors, agencyId, resourcePackage);
                        }
                        else
                        {
                            variable.RepresentationType = RepresentationType.Numeric;
                            variable.NumericRepresentation.NumericType = NumericType.Integer;
                            variable.Additivity = AdditivityType.Stock;
                        }
                    }
                    else if (column.Type == RDotNet.Internals.SymbolicExpressionType.CharacterVector)
                    {
                        variable.RepresentationType = RepresentationType.Text;
                    }
                }
            }

            return resourcePackage;
        }

        private Dictionary<string, DataFrame> GetDataFrames()
        {
            // Look at all the symbols.
            var symbolNames = engine.GlobalEnvironment.GetSymbolNames();

            var dataFrames = new Dictionary<string, DataFrame>();
            foreach (var symbolName in symbolNames)
            {
                var expr = engine.GetSymbol(symbolName);
                if (expr.IsDataFrame())
                {
                    dataFrames.Add(symbolName, expr.AsDataFrame());
                }
            }

            return dataFrames;
        }

        CodeList GetCodeList(string[] factors, string agencyId, ResourcePackage resourcePackage)
        {
            var codeList = new CodeList { AgencyId = agencyId };

            for (int i = 0; i < factors.Length; i++)
            {
                int value = i + 1;
                string label = factors[i];

                var category = new Category { AgencyId = agencyId };
                category.Label.Current = label;

                var code = new Code() { AgencyId = agencyId };
                code.Category = category;
                code.Value = value.ToString();
                codeList.Codes.Add(code);

                // Check for existing Code+Cat schemes with which to harmonize.
                CodeList existingCodeScheme = null;
                bool codeSchemeAlreadyExists = harmonizingCache.TryGetExistingCodeScheme(
                    codeList, out existingCodeScheme);
                if (codeSchemeAlreadyExists)
                {
                    codeList = existingCodeScheme;
                }
                else
                {
                    // The CodeList isn't in this.ResourcePackage or the cache yet, so add it.
                    resourcePackage.CodeSchemes.Add(codeList);
                    harmonizingCache.AddCodeScheme(codeList);
                }
            }

            return codeList;
        }

        public void MoveDataReaderToBeginning(System.Data.IDataReader reader)
        {
            throw new NotImplementedException();
        }

        public System.Data.IDataReader GetDataReader(string path)
        {
            throw new NotImplementedException();
        }

        public System.Data.DataSet GetDataSet(string path)
        {
            var dataSet = new DataSet();

            var resourcePackage = Import(path, "dummy");
            dataSet.DataSetName = resourcePackage.DublinCoreMetadata.Title.Best;

            if (resourcePackage.PhysicalInstances.Count == 0 ||
                resourcePackage.PhysicalInstances[0].DataRelationships.Count == 0)
            {
                return dataSet;
            }

            var dataRelationship = resourcePackage.PhysicalInstances[0].DataRelationships[0];

            var dataFrames = GetDataFrames();

            // Create a table for each LogicalRecord.
            foreach (var logicalRecord in dataRelationship.LogicalRecords)
            {
                var dataTable = new DataTable(logicalRecord.Label.Best);
                dataSet.Tables.Add(dataTable);

                // Create a column for each Variable.
                foreach (var variable in logicalRecord.VariablesInRecord)
                {
                    Type type = null;
                    if (variable.RepresentationType == RepresentationType.Code)
                    {
                        type = typeof(double);
                    }
                    else if (variable.RepresentationType == RepresentationType.Text)
                    {
                        type = typeof(string);
                    }
                    else if (variable.RepresentationType == RepresentationType.Numeric)
                    {
                        if (variable.NumericRepresentation.NumericType.Value == NumericType.Integer.ToString())
                        {
                            type = typeof(double);
                        }
                        else
                        {
                            type = typeof(double);
                        }
                    }
                    else
                    {
                        type = typeof(double);
                    }

                    var dataColumn = new DataColumn(variable.ItemName.Best, type);
                    dataTable.Columns.Add(dataColumn);
                }

                if (!dataFrames.ContainsKey(logicalRecord.Label.Best))
                {
                    continue;
                }

                var frame = dataFrames[logicalRecord.Label.Best];
                for (int rowIdx = 0; rowIdx < frame.RowCount; rowIdx++)
                {
                    var dataRow = dataTable.NewRow();
                    for (int columnIdx = 0; columnIdx < frame.ColumnCount; columnIdx++)
                    {
                        try
                        {
                            if (frame[columnIdx].IsFactor())
                            {
                                dataRow[columnIdx] = frame[columnIdx].AsInteger()[rowIdx];
                            }
                            else
                            {
                                dataRow[columnIdx] = frame[rowIdx, columnIdx];
                            }
                        }
                        catch
                        {

                        }
                    }

                    dataTable.Rows.Add(dataRow);
                }
            }

            return dataSet;
        }

        public bool SupportsSingleColumnStreaming
        {
            get { return false; }
        }

        public bool SupportsColumnDataTypes => false;
    }
}
