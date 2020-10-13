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

﻿using Algenta.Colectica.Data.Stata;
using Algenta.Colectica.Model.Ddi;
using Algenta.Colectica.Model.Utility;
using Algenta.Colectica.Repository;
using Colectica.Curation.Contracts;
using Colectica.Curation.Data;
using Colectica.Curation.Operations;
using log4net;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Colectica.Curation.Web.Utility;
using System.IO;
using Algenta.Colectica.Commands.Import;
using Algenta.Colectica.Commands.TypeSpecific;
using Spss.Data;
using Colectica.Curation.DdiAddins.Utility;
using Algenta.Colectica.Model;
using Algenta.Colectica.Model.Repository;
using Algenta.Colectica.Commands.SummaryStatistics;

namespace Colectica.Curation.DdiAddins.Actions
{
    [Export(typeof(IFileAction))]
    public class CreatePhysicalInstances : IFileAction
    {
        public string Name { get { return "Extract Variable-level Metadata"; } }
        public bool IsManualActivationAllowed
        {
            get { return true; }
        }

        public bool CanRun(ManagedFile file)
        {
            return file.IsStatisticalDataFile();
        }

        public void Run(CatalogRecord record, ManagedFile file, ApplicationUser user, ApplicationDbContext db, string processingDirectory, string agencyId)
        {
            if (string.IsNullOrWhiteSpace(agencyId))
            {
                agencyId = "int.example";
            }

            VersionableBase.DefaultAgencyId = agencyId;

            // Set SPSS path.
            string spssPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory);
            SpssRaw.Instance.Initialize(spssPath);

            var existingPhysicalInstance = GetExistingPhysicalInstance(file, agencyId);

            // If there is no PhysicalInstance, or if there is one but it has no variables,
            // then create one.
            if (existingPhysicalInstance == null ||
                existingPhysicalInstance.DataRelationships
                    .SelectMany(x => x.LogicalRecords)
                    .SelectMany(x => x.VariablesInRecord)
                    .Count() == 0)
            {
                string path = Path.Combine(processingDirectory, file.Name);

                if (file.Name.EndsWith(".dta"))
                {
                    CreatePhysicalInstanceForStataFile(file.Id, path, agencyId, existingPhysicalInstance?.Version);
                }
                if (file.Name.EndsWith(".sav"))
                {
                    CreatePhysicalInstanceForSpssFile(file.Id, path, agencyId, existingPhysicalInstance?.Version);
                }
                if (file.Name.EndsWith(".csv"))
                {
                    CreatePhysicalInstanceForCsvFile(file.Id, path, agencyId, existingPhysicalInstance?.Version);
                }
                if (file.Name.EndsWith(".rdata") ||
                    file.Name.EndsWith(".rda"))
                {
                    CreatePhysicalInstanceForRdataFile(file.Id, path, agencyId, existingPhysicalInstance?.Version);
                }
            }
        }

        private PhysicalInstance GetExistingPhysicalInstance(ManagedFile file, string agencyId)
        {
            try
            {
                var client = RepositoryHelper.GetClient();
                var item = client.GetLatestItem(file.Id, agencyId, ChildReferenceProcessing.Instantiate) as PhysicalInstance;
                if (item != null)
                {
                    foreach (var dr in item.DataRelationships)
                    {
                        if (!dr.IsPopulated)
                        {
                            client.PopulateItem(dr, false, ChildReferenceProcessing.Populate);
                        }
                    }

                }

                return item;
            }
            catch
            {
                return null;
            }
        }

        public static void CreatePhysicalInstanceForStataFile(Guid fileId, string stataFilePath, string agencyId, long? existingVersion)
        {
            var logger = LogManager.GetLogger("Curation");
            logger.Debug("Stata import file: " + fileId);

            ResourcePackage rp = new ResourcePackage();
            using (StataDataReader reader = new StataDataReader(stataFilePath))
            {
                StataImporter.ImportMetadata(reader, rp, "Temp Package", stataFilePath);
            }

            if (rp.PhysicalInstances.Count == 0)
            {
                return;
            }

            PhysicalInstance physicalInstance = rp.PhysicalInstances[0];
            physicalInstance.Identifier = fileId;

            if (existingVersion.HasValue)
            {
                physicalInstance.Version = existingVersion.Value + 1;
            }

            StataImporter importer = new StataImporter();
            try
            {
                var calculator = new PhysicalInstanceSummaryStatisticComputer();
                var stats = calculator.ComputeStatistics(importer, stataFilePath, physicalInstance, 
                    physicalInstance.FileStructure.CaseQuantity, null);
                if (stats != null)
                {
                    physicalInstance.Statistics.Clear();
                    foreach (VariableStatistic stat in stats)
                    {
                        physicalInstance.Statistics.Add(stat);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error("Problem calculating summary statistics.", ex);
            }


            var client = RepositoryHelper.GetClient();
            ItemGathererVisitor visitor = new ItemGathererVisitor();
            physicalInstance.Accept(visitor);

            // The static default agency id is not thread safe, so set it explicitly here
            foreach (var item in visitor.FoundItems)
            {
                item.AgencyId = agencyId;
            }

            client.RegisterItems(visitor.FoundItems, new Algenta.Colectica.Model.Repository.CommitOptions());
        }

        public static void CreatePhysicalInstanceForSpssFile(Guid fileId, string spssFilePath, string agencyId, long? existingVersion)
        {
            var logger = LogManager.GetLogger("Curation");
            logger.Debug("SPSS import file: " + fileId);

            ResourcePackage rp = new ResourcePackage();
            using (var reader = new SpssDataReader(spssFilePath))
            {
                SpssImporter.ImportMetadata(reader, rp, "Temp Package", spssFilePath);
                logger.Debug("Imported metadata from SPSS");
            }

            if (rp.PhysicalInstances.Count == 0)
            {
                logger.Debug("No dataset could be extracted from SPSS");
                return;
            }

            PhysicalInstance physicalInstance = rp.PhysicalInstances[0];
            physicalInstance.Identifier = fileId;
            if (existingVersion.HasValue)
            {
                physicalInstance.Version = existingVersion.Value + 1;
            }


            var importer = new SpssImporter();
            try
            {
                logger.Debug("Calculating summary statistics");
                var calculator = new PhysicalInstanceSummaryStatisticComputer();
                var stats = calculator.ComputeStatistics(importer, spssFilePath, physicalInstance,
                    physicalInstance.FileStructure.CaseQuantity, null);
                logger.Debug("Done calculating summary statistics");

                if (stats != null)
                {
                    physicalInstance.Statistics.Clear();
                    foreach (VariableStatistic stat in stats)
                    {
                        physicalInstance.Statistics.Add(stat);
                    }
                }
            }
            catch (Exception)
            {

            }


            var client = RepositoryHelper.GetClient();
            ItemGathererVisitor visitor = new ItemGathererVisitor();
            physicalInstance.Accept(visitor);

            logger.Debug("Setting agency IDs");

            // The static default agency id is not thread safe, so set it explicitly here
            foreach (var item in visitor.FoundItems)
            {
                item.AgencyId = agencyId;
            }

            logger.Debug("Done setting agency IDs");
            logger.Debug("Registering items with the repository");

            client.RegisterItems(visitor.FoundItems, new Algenta.Colectica.Model.Repository.CommitOptions());

            logger.Debug("Done registering items with the repository");
            logger.Debug("Done with CreatePhysicalInstanceForSpssFile");
        }

        internal static void CreatePhysicalInstanceForCsvFile(Guid fileId, string filePath, string agencyId, long? existingVersion)
        {
            var logger = LogManager.GetLogger("Curation");
            logger.Debug("CSV import file: " + fileId);

            var importer = new CsvImporter();
            ResourcePackage rp = importer.Import(filePath, agencyId);

            logger.Debug("CSV parsed");

            if (rp.PhysicalInstances.Count == 0)
            {
                return;
            }

            PhysicalInstance physicalInstance = rp.PhysicalInstances[0];
            physicalInstance.Identifier = fileId;
            if (existingVersion.HasValue)
            {
                physicalInstance.Version = existingVersion.Value + 1;
            }

            try
            {
                var calculator = new PhysicalInstanceSummaryStatisticComputer();
                var stats = calculator.ComputeStatistics(importer, filePath, physicalInstance,
                    physicalInstance.FileStructure.CaseQuantity, null);
                if (stats != null)
                {
                    physicalInstance.Statistics.Clear();
                    foreach (VariableStatistic stat in stats)
                    {
                        physicalInstance.Statistics.Add(stat);
                    }
                }

                logger.Debug("CSV statistics generated");
            }
            catch (Exception)
            {

            }

            var client = RepositoryHelper.GetClient();
            ItemGathererVisitor visitor = new ItemGathererVisitor();
            physicalInstance.Accept(visitor);

            // The static default agency id is not thread safe, so set it explicitly here
            foreach (var item in visitor.FoundItems)
            {
                item.AgencyId = agencyId;
            }

            client.RegisterItems(visitor.FoundItems, new Algenta.Colectica.Model.Repository.CommitOptions());
            logger.Debug("CSV items registered");
        }

        internal static void CreatePhysicalInstanceForRdataFile(Guid fileId, string filePath, string agencyId, long? existingVersion)
        {
            var logger = LogManager.GetLogger("Curation");
            logger.Debug("RData import file: " + fileId);

            var importer = new RDataImporter();
            ResourcePackage rp = importer.Import(filePath, agencyId);

            if (rp.PhysicalInstances.Count == 0)
            {
                return;
            }

            PhysicalInstance physicalInstance = rp.PhysicalInstances[0];
            physicalInstance.Identifier = fileId;
            if (existingVersion.HasValue)
            {
                physicalInstance.Version = existingVersion.Value + 1;
            }

            try
            {
                var calculator = new PhysicalInstanceSummaryStatisticComputer();
                var stats = calculator.ComputeStatistics(importer, filePath, physicalInstance,
                    physicalInstance.FileStructure.CaseQuantity, null);
                if (stats != null)
                {
                    physicalInstance.Statistics.Clear();
                    foreach (VariableStatistic stat in stats)
                    {
                        physicalInstance.Statistics.Add(stat);
                    }
                }
            }
            catch (Exception)
            {

            }

            var client = RepositoryHelper.GetClient();
            ItemGathererVisitor visitor = new ItemGathererVisitor();
            physicalInstance.Accept(visitor);

            // The static default agency id is not thread safe, so set it explicitly here
            foreach (var item in visitor.FoundItems)
            {
                item.AgencyId = agencyId;
            }

            client.RegisterItems(visitor.FoundItems, new Algenta.Colectica.Model.Repository.CommitOptions());
        }
    }
}
