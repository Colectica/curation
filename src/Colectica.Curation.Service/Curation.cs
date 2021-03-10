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
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Data.Entity;
using Colectica.Curation.Operations;
using Newtonsoft.Json;
using System.IO;
using LibGit2Sharp;
using System.ComponentModel.Composition;
using System.Reflection;

namespace Colectica.Curation.Service
{
    public class Curation
    {
        Timer timer;
        ILog logger;

        public Curation()
        {
            logger = LogManager.GetLogger("Curation");
            logger.Info("Starting Colectica Curation Service");
            logger.Info($"Version {RevisionInfo.FullVersionString}");

            //logger.Info("Loading Colectica Curation Addins");

            // Load Addins
            string binPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string addinsPath = Path.Combine(binPath, "CurationAddins");

#if ISPRO
            DdiAddins.Utility.RepositoryHelper.InitializeLogging("CurationService-.log");

            MefConfig.RegisterMef(addinsPath, typeof(Colectica.Curation.DdiAddins.DdiAddinManifest).Assembly);

            string repositoryHostName = Properties.Settings.Default.RepositoryHostName;
            if (!string.IsNullOrWhiteSpace(repositoryHostName))
            {
                DdiAddins.Utility.RepositoryHelper.RepositoryHostName = repositoryHostName;
            }

            DdiAddins.Utility.RepositoryHelper.UserName = Properties.Settings.Default.RepositoryUserName;
            DdiAddins.Utility.RepositoryHelper.Password = Properties.Settings.Default.RepositoryPassword;

#else
            MefConfig.RegisterMef(addinsPath, typeof(Colectica.Curation.BaseAddins.BaseAddinManifest).Assembly);
#endif


            timer = new Timer(1000);
            timer.AutoReset = false; // non reentrant
            timer.Elapsed += timer_Elapsed;
        }

        public void Start(string[] args)
        {
            timer.Enabled = true; // this is a 1 second wait
        }

        public void Stop()
        {
            logger.Info("Stopping the Colectica Curation Service");
            timer.Enabled = false;
        }

        void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            while (true)
            {
                bool success = false;

                try
                {
                    using (var db = ApplicationDbContext.Create())
                    {
                        Operation next = null;

                        next = db.Operations.Where(x => x.Status == OperationStatus.Queued).OrderBy(x => x.Id).FirstOrDefault();
                        if (next == null)
                        {
                            break;
                        }

                        logger.Info("Beginning operation: " + next.OperationName);

                        next.Status = OperationStatus.Working;
                        next.StartedOn = DateTime.UtcNow;
                        db.SaveChanges();

                        IOperation operation = GetOperation(next);
                        MefConfig.Container.SatisfyImportsOnce(operation);


                        // Perform the operation, with error handling.
                        try
                        {
                            success = operation.Execute();
                        }
                        catch (EmptyCommitException emptyCommitException)
                        {
                            logger.Debug("No changes to commit, files were identical. " + emptyCommitException.Message);
                            success = true;
                        }
                        catch (Exception operationException)
                        {
                            logger.Error("Error in operation execution.", operationException);
                            success = false;
                        }


                        // Mark the operation as completed or in error.
                        if (success)
                        {
                            next.Status = OperationStatus.Completed;
                        }
                        else
                        {
                            next.Status = OperationStatus.Error;
                        }
                        next.CompletedOn = DateTime.UtcNow;


                        // Update the catalog record status.
                        CatalogRecord record = db.CatalogRecords.Where(x => x.Id == next.CatalogRecordContext).FirstOrDefault();
                        if (record != null)
                        {
                            record.OperationLockId = null;
                            record.OperationStatus = string.Empty;
                        }

                        int changes = db.SaveChanges();

                    } //end using
                } //end try
                catch (Exception ex)
                {
                    logger.Error("Error while processing operations.", ex);
                    success = false;
                }
            } //end while there are operations to process

            timer.Interval = 1000 * 10;
            timer.Enabled = true;
        }


        //TODO this should be moved to a MEF addin, and operations discovered dynamically
        private IOperation GetOperation(Operation storedOperation)
        {
            if (storedOperation.OperationType == AddFiles.OperationTypeId)
            {
                return JsonConvert.DeserializeObject<AddFiles>(storedOperation.Data);
            }
            else if (storedOperation.OperationType == CreateRepository.OperationTypeId)
            {
                return JsonConvert.DeserializeObject<CreateRepository>(storedOperation.Data);
            }
            else if (storedOperation.OperationType == FinalizeCatalogRecord.OperationTypeId)
            {
                return JsonConvert.DeserializeObject<FinalizeCatalogRecord>(storedOperation.Data);
            }
            else if (storedOperation.OperationType == CreatePhysicalInstances.OperationTypeId)
            {
                return JsonConvert.DeserializeObject<CreatePhysicalInstances>(storedOperation.Data);
            }
            else if (storedOperation.OperationType == ArchiveIngestFiles.OperationTypeId)
            {
                return JsonConvert.DeserializeObject<ArchiveIngestFiles>(storedOperation.Data);
            }
            else if (storedOperation.OperationType == ApplyMetadataUpdates.OperationTypeId)
            {
                return JsonConvert.DeserializeObject<ApplyMetadataUpdates>(storedOperation.Data);
            }

            throw new InvalidOperationException("Unknown operation type id " + storedOperation.OperationType.ToString());
        }

    }
}
