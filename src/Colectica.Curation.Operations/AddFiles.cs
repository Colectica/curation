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
using Colectica.Curation.Web.Models;
using LibGit2Sharp;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;
using nClam;
using Colectica.Curation.Common.Utility;
using System.Security.Cryptography;
using log4net;
using System.ComponentModel.Composition;
using Colectica.Curation.Contracts;

namespace Colectica.Curation.Operations
{
    public class AddFiles : IOperation
    {
        public static readonly Guid OperationTypeId = new Guid("2E265EFD-FB75-4F1F-B3C3-1618EEDBF485");
        public Guid OperationType { get { return OperationTypeId; } }

        public string Name
        {
            get;
            set;
        }

        public int Timeout
        {
            get;
            set;
        }

        public Dictionary<Guid, string> IncomingFileNames = new Dictionary<Guid, string>();

        public Dictionary<Guid, Tuple<string, string>> RenamedFileNames = new Dictionary<Guid, Tuple<string, string>>();

        public string GitRepositoryPath { get; set; }
        public string CommitMessage { get; set; }

        public Guid UserId { get; set; }
        public Guid CatalogRecordId { get; set; }
        public string Username { get; set; }
        public string UserEmail { get; set; }

        [ImportMany]
        public List<IFileAction> FileActions { get; set; }

        ILog logger;

        public AddFiles()
        {
            logger = LogManager.GetLogger("Curation");
        }

        public bool Execute()
        {
            string path = Path.Combine(GitRepositoryPath, CatalogRecordId.ToString());
            //path = Path.Combine(path, ".git");

            logger.Debug("Starting AddFiles operation for " + path);

            var managedFiles = new List<ManagedFile>();

            // virus check
            bool virusError = false;
            using (var db = ApplicationDbContext.Create())
            {
                ApplicationUser user = new ApplicationUser { Id = UserId.ToString() };
                db.Users.Attach(user);
                CatalogRecord record = new CatalogRecord { Id = CatalogRecordId };
                db.CatalogRecords.Attach(record);
                foreach (var incomingFile in IncomingFileNames)
                {
                    var log = new Data.Event()
                    {
                        EventType = EventTypes.EditManagedFile,
                        Timestamp = DateTime.UtcNow,
                        User = user,
                        RelatedCatalogRecord = record,
                        Title = "Virus Scan",
                        Details = string.Empty
                    };

                    ManagedFile mf = db.Files.Where(x => x.Id == incomingFile.Key).First();
                    managedFiles.Add(mf);
                    log.RelatedManagedFiles.Add(mf);

                    string fileNameOnly = Path.GetFileName(incomingFile.Value);

                    // Virus check
                    try
                    {
                        mf.VirusCheckMethod = "ClamAV";
                        mf.VirusCheckDate = DateTime.UtcNow;

                        logger.Debug("Running virus check on " + incomingFile.Value);

                        ClamClient scanner = new ClamClient("localhost", 3310);
                        var result = scanner.ScanFileOnServer(incomingFile.Value);

                        switch (result.Result)
                        {
                            case ClamScanResults.Clean:
                                mf.Status = Data.FileStatus.Accepted;
                                mf.AcceptedDate = DateTime.UtcNow;
                                mf.VirusCheckOutcome = "OK";

                                log.Details = "Clean virus scan on " + fileNameOnly;
                                logger.Debug(log.Details);

                                break;
                            case ClamScanResults.VirusDetected:
                                //TODO delete/quarentine? remove from incoming files.
                                mf.Status = Data.FileStatus.Rejected;
                                mf.VirusCheckOutcome = log.Details;

                                //result.InfectedFiles.First().VirusName
                                virusError = true;
                                log.Details = "Virus " + result.InfectedFiles.First().VirusName + "found on " + fileNameOnly;
                                logger.Warn(log.Details);

                                break;
                            case ClamScanResults.Error:
                                mf.Status = Data.FileStatus.Accepted;
                                mf.AcceptedDate = DateTime.UtcNow;
                                mf.VirusCheckOutcome = log.Details;

                                virusError = true;
                                log.Details = "Virus scan error on " + fileNameOnly + " " + result.RawResult;
                                logger.Error(log.Details);

                                break;
                        }
                    }
                    catch (System.Net.Sockets.SocketException se)
                    {
                        virusError = true;
                        log.Details = "Could not connect to virus scanner for " + fileNameOnly + " " + se.Message;
                        mf.VirusCheckOutcome = log.Details;

                        logger.Error("Socket exception during virus check", se);
                    }
                    catch (Exception e)
                    {
                        virusError = true;
                        log.Details = "Error during virus scan " + fileNameOnly + " " + e.Message;
                        mf.VirusCheckOutcome = log.Details;

                        logger.Error("Problem during virus check", e);
                    }

                    db.Events.Add(log);
                }

                try
                {
                    db.SaveChanges();
                }
                catch (Exception )
                {

                }
            }

            if (virusError)
            {
                return false;
            }

            // Add the files to the git repository.
            using (Repository repo = new Repository(path))
            {
                #region rename
                foreach (var rename in RenamedFileNames)
                {
                    Guid fileId = rename.Key;
                    string originalFileName = rename.Value.Item1;
                    string newFileName = rename.Value.Item2;

                    string source = Path.Combine(repo.Info.WorkingDirectory, originalFileName);
                    string destination = Path.Combine(repo.Info.WorkingDirectory, newFileName);
                    //File.Move(source, destination);
                    LibGit2Sharp.Commands.Move(repo, source, destination);
                }

                string authorEmail = UserEmail;
                if (string.IsNullOrWhiteSpace(authorEmail))
                {
                    authorEmail = "missing@example.org";
                }

                Signature author = new Signature(Username, authorEmail, DateTime.UtcNow);
                Signature committer = new Signature(UserId.ToString(), UserId.ToString() + "@curator", DateTime.UtcNow);

                try
                {
                    Commit commit = repo.Commit("Renaming Files", author, committer);
                }
                catch
                {
                    // Nothing to rename
                }
                #endregion

                foreach (var incomingFile in IncomingFileNames)
                {
                    string filename = Path.GetFileName(incomingFile.Value);
                    string destination = Path.Combine(repo.Info.WorkingDirectory, filename);
                    File.Copy(incomingFile.Value, destination, true);

                    // For files that are larger than a gigabyte, do not stage them. 
                    // gitlib throws an AccessViolationException.
                    var fileInfo = new FileInfo(incomingFile.Value);
                    if (fileInfo.Length > 1024 * 1024 * 1024)
                    {
                        continue;
                    }

                    try
                    {
                        LibGit2Sharp.Commands.Stage(repo, filename);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error while staging. " + ex.Message);
                    }

                }


                if (string.IsNullOrWhiteSpace(CommitMessage))
                {
                    CommitMessage = "Adding Files";
                }

                try
                {
                    if (repo.Index.Count > 0)
                    {
                        Commit commit = repo.Commit(CommitMessage, author, committer);
                    }
                }
                catch
                {
                    // TODO
                }
            }

            // Find the agency ID to use, based on the CatalogRecord.
            string agencyId = null;
            using (var db = ApplicationDbContext.Create())
            {
                var record = db.CatalogRecords.Where(x => x.Id == this.CatalogRecordId).Include(x => x.Organization).FirstOrDefault();
                if (record != null && record.Organization != null && !string.IsNullOrWhiteSpace(record.Organization.AgencyID))
                {
                    agencyId = record.Organization.AgencyID;
                }
                else
                {
                    //TODO error
                }

                var user = db.Users.Find(UserId.ToString());

                // Run any addin actions on each file.
                foreach (var file in managedFiles)
                {
                    foreach (var addin in FileActions)
                    {
                        try
                        {
                            if (!addin.CanRun(file))
                            {
                                continue;
                            }

                            addin.Run(file.CatalogRecord, file, user, db, path, agencyId);
                        }
                        catch (Exception ex)
                        {
                            logger.Error("Error running file action: " + addin.Name, ex);
                        }
                    }
                }
            }

           return true;
        }
    }
}
