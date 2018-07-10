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

﻿using Colectica.Curation.Common.Utility;
using Colectica.Curation.Contracts;
using Colectica.Curation.Data;
using Colectica.Curation.Web.Models;
using LibGit2Sharp;
using log4net;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Colectica.Curation.DdiAddins.Actions
{
    [Export(typeof(ICreatePreservationFormatsAction))]
    public class CreatePreservationFormatsWithStatTransfer : ICreatePreservationFormatsAction
    {
        ILog logger;

        public string Name { get { return "Create Preservation formats (Stat/Transfer)"; } }

        public CreatePreservationFormatsWithStatTransfer()
        {
            logger = LogManager.GetLogger("Curation");
        }

        public void CreatePreservationFormats(CatalogRecord record, ApplicationUser user, ApplicationDbContext db, 
            string userId, string processingDirectory, string statTransferPath)
        {
            string stPath = statTransferPath;
            if (string.IsNullOrWhiteSpace(stPath))
            {
                logger.Info("Skipping preservation formats because StatTransfer is not configured.");
                return;
            }

            if (!File.Exists(stPath))
            {
                logger.Info("Skipping preservation formts because the configured StatTransfer path is not valid.");
                return;
            }

            logger.Debug("Creating preservation formats");

            bool hasNewFiles = false;

            string recordPath = Path.Combine(processingDirectory, record.Id.ToString());
            var author = new Signature(user.UserName, user.Email, DateTime.UtcNow);
            var committer = new Signature(userId.ToString(), userId.ToString() + "@curator", DateTime.UtcNow);

            using (Repository repo = new Repository(recordPath))
            {
                // Get a list of the data files of the types we want to convert.
                var dataFiles = record.Files
                    .Where(x =>
                        {
                            if (x.Status == Data.FileStatus.Removed)
                            {
                                return false;
                            }

                            string ext = Path.GetExtension(x.Name).ToLower();
                            return ext == ".dta" || 
                                ext == ".sav" ||
                                ext.StartsWith(".rda");
                        })
                    .ToList();

                foreach (var managedFile in dataFiles)
                {
                    string dataFilePath = Path.Combine(recordPath, managedFile.Name);
                    string csvFileName = Path.GetFileNameWithoutExtension(managedFile.Name) + ".csv";
                    string csvFilePath = Path.Combine(recordPath, csvFileName);

                    if (File.Exists(csvFilePath))
                    {
                        // TODO Overwrite, or skip?
                        logger.Info("The CSV file " + csvFilePath + " already exists. Skipping.");
                        continue;
                    }

                    string stArguments = string.Format("\"{0}\" \"{1}\"",
                        dataFilePath,
                        csvFilePath);

                    logger.Info("Using StatTransfer arguments: " + stArguments);

                    try
                    {
                        // Run Stat/Transfer to generate the CSV file.
                        var process = new Process();
                        process.StartInfo.FileName = stPath;
                        process.StartInfo.Arguments = stArguments;
                        process.StartInfo.UseShellExecute = false;
                        process.StartInfo.RedirectStandardOutput = true;
                        process.StartInfo.RedirectStandardError = true;
                        
                        process.OutputDataReceived += (outputS, outputE) =>
                        {
                            logger.Debug(outputE.Data);
                        };
                        process.ErrorDataReceived += (outputS, outputE) =>
                        {
                            logger.Debug(outputE.Data);
                        };

                        process.Start();
                        process.WaitForExit(60 * 1000);

                        logger.Info("StatTransfer exited with code " + process.ExitCode.ToString());

                        string stError = process.StandardError.ReadToEnd();
                        if (!string.IsNullOrWhiteSpace(stError))
                        {
                            logger.Warn("StatTransfer Error: " + stError);
                        }

                        string stOutput = process.StandardOutput.ReadToEnd();
                        if (!string.IsNullOrWhiteSpace(stOutput))
                        {
                            logger.Warn("StatTransfer Output: " + stOutput);
                        }


                        // Add the new file to the git repository.
                        hasNewFiles = true;
                        Commands.Stage(repo, csvFileName);

                        // Create a ManagedFile to represent the CSV and add it to the CatalogRecord.
                        var csvManagedFile = new ManagedFile()
                        {
                            Id = Guid.NewGuid(),
                            CatalogRecord = record,
                            CreationDate = DateTime.UtcNow,
                            Name = csvFileName,
                            FormatName = Path.GetExtension(csvFileName).ToLower(),
                            Size = new FileInfo(csvFilePath).Length,
                            Source = "Curation System",
                            PublicName = csvFileName,
                            Type = "Data",
                            Software = "Text Editor",
                            UploadedDate = DateTime.UtcNow,
                            Status = Colectica.Curation.Data.FileStatus.Accepted,
                            Owner = user
                        };

                        record.Files.Add(csvManagedFile);

                        EventService.LogEvent(record, user, db, EventTypes.CreatePreservationFormat, "Preservation file created: " + csvFileName);
                    }
                    catch (Exception ex)
                    {
                        logger.Warn("Error while running StatTransfer", ex);
                        EventService.LogEvent(record, user, db, EventTypes.FinalizeCatalogRecordFailed, "Failed to create preservation file", ex.Message);
                    }
                }

                // Commit the preservation formats to git.
                try
                {
                    if (hasNewFiles)
                    {
                        var commit = repo.Commit("System-created preservation format", author, committer);
                    }
                }
                catch (Exception ex)
                {
                    logger.Warn("Error commiting preservation formats", ex);
                }
            }
        

        }

    }
}
