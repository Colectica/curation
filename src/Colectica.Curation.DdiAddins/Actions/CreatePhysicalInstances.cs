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
using Spss.Data;
using Colectica.Curation.DdiAddins.Utility;
using Algenta.Colectica.Model;
using Algenta.Colectica.Model.Repository;
using Algenta.Colectica.Commands.SummaryStatistics;
using Algenta.Colectica.Navigator.NodeTypes;
using Algenta.Colectica.ViewModel.Import;
using System.Data;
using Algenta.Colectica.Commands.Data;

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

            string path = Path.Combine(processingDirectory, file.Name);
            string errorMessage = string.Empty;

            // Create the PhysicalInstance
            // Calculate the summary statistics.
            if (file.Name.EndsWith(".dta"))
            {
                errorMessage = CreateOrUpdatePhysicalInstanceForFile<StataImporter>(file.Id, path, agencyId, existingPhysicalInstance);
            }
            if (file.Name.EndsWith(".sav"))
            {
                errorMessage = CreateOrUpdatePhysicalInstanceForFile<SpssImporter>(file.Id, path, agencyId, existingPhysicalInstance);
            }
            if (file.Name.EndsWith(".csv"))
            {
                errorMessage = CreateOrUpdatePhysicalInstanceForFile<CsvImporter>(file.Id, path, agencyId, existingPhysicalInstance);
            }
            if (file.Name.EndsWith(".rdata") ||
                file.Name.EndsWith(".rda"))
            {
                errorMessage = CreateOrUpdatePhysicalInstanceForFile<RDataImporter>(file.Id, path, agencyId, existingPhysicalInstance);
            }

            // Log any errors.
            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                var note = new Data.Note()
                {
                    CatalogRecord = file.CatalogRecord,
                    File = file,
                    Timestamp = DateTime.UtcNow,
                    User = user,
                    Text = errorMessage
                };
                db.Notes.Add(note);
                db.SaveChanges();
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

        public static string CreateOrUpdatePhysicalInstanceForFile<TImporter>(Guid fileId, string filePath, string agencyId, PhysicalInstance existingPhysicalInstance)
                where TImporter : IDataImporter, new()
        {
            var logger = LogManager.GetLogger("Curation");
            logger.Debug("File import: " + fileId);

            IDataImporter importer = new TImporter();
            var client = RepositoryHelper.GetClient();
            PhysicalInstance physicalInstance = existingPhysicalInstance;

            // For PhysicalInstances that do not exist yet, create from scratch.
            if (physicalInstance == null)
            {
                // Extract metadata.
                ResourcePackage rp = importer.Import(filePath, agencyId);
                logger.Debug("Imported metadata from data file.");

                if (rp.PhysicalInstances.Count == 0)
                {
                    logger.Debug("No dataset could be extracted from SPSS");
                    return string.Empty;
                }

                physicalInstance = rp.PhysicalInstances[0];
                physicalInstance.Identifier = fileId;
            }

            // Calculate summary statistics, for both new and updated files.
            try
            {
                logger.Debug("Calculating summary statistics");
                var calculator = new PhysicalInstanceSummaryStatisticComputer();
                SummaryStatisticsOptions options = new SummaryStatisticsOptions(physicalInstance) { CalculateQuartiles = true };
                var stats = calculator.ComputeStatistics(importer, filePath, physicalInstance,
                    physicalInstance.FileStructure.CaseQuantity, options, (percent, message) => { });
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
            catch (Exception ex)
            {
                logger.Error("Problem calculating summary statistics.", ex);
                return "Problem calculating summary statistics. " + ex.Message;
            }


            // Register all items that were created with the repository.
            DirtyItemGatherer visitor = new DirtyItemGatherer(false, true);
            physicalInstance.Accept(visitor);

            logger.Debug("Setting agency IDs");

            // The static default agency id is not thread safe, so set it explicitly here
            foreach (var item in visitor.DirtyItems)
            {
                item.AgencyId = agencyId;
            }

            logger.Debug("Done setting agency IDs");
            logger.Debug("Registering items with the repository");

            var repoOptions = new Algenta.Colectica.Model.Repository.CommitOptions();
            repoOptions.NamedOptions.Add("RegisterOrReplace");
            client.RegisterItems(visitor.DirtyItems, repoOptions);

            logger.Debug("Done registering items with the repository");
            logger.Debug("Done with CreatePhysicalInstanceForFile");

            return string.Empty;
        }

    }
}
