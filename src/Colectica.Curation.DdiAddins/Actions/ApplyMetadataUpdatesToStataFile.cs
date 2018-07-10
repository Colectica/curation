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
using Algenta.Colectica.Model.Repository;
using Algenta.Colectica.Repository;
using Colectica.Curation.Common.Utility;
using Colectica.Curation.Contracts;
using Colectica.Curation.Data;
using LibGit2Sharp;
using log4net;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Colectica.Curation.Web.Utility;
using Colectica.Curation.DdiAddins.Utility;

namespace Colectica.Curation.DdiAddins.Actions
{
    [Export(typeof(IApplyMetadataUpdatesAction))]
    public class ApplyMetadataUpdatesToStataFile : IApplyMetadataUpdatesAction
    {
        ILog logger;

        public string Name { get { return "Apply Metadata Updates to Stata File"; } }

        public ApplyMetadataUpdatesToStataFile()
        {
            logger = LogManager.GetLogger("Curation");
        }

        public bool CanApplyMetadataUpdates(ManagedFile file)
        {
            return file.IsStataDataFile();
        }

        public bool ApplyMetadataUpdates(CatalogRecord record, ManagedFile file, ApplicationUser user, string userId, ApplicationDbContext db, string processingDirectory)
        {
            if (record == null)
            {
                throw new ArgumentNullException("record");
            }

            if (file == null)
            {
                throw new ArgumentNullException("file");
            }

            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            // Read the metadata from the repository.
            var physicalInstance = GetPhysicalInstance(file, file.CatalogRecord.Organization.AgencyID);
            if (physicalInstance == null)
            {
                logger.Warn("No PhysicalInstance for Stata file " + file.Name);
                return false;
            }

            // Get the path of the Stata file.
            string gitRepositoryPath = Path.Combine(processingDirectory, record.Id.ToString());
            string originalStataFilePath = Path.Combine(gitRepositoryPath, file.Name);

            // Get a temporary file name to which the new Stata file will be written.
            // This actually creates the file, which we don't want, so delete it.
            string newStataFilePath = Path.GetTempFileName();
            File.Delete(newStataFilePath);

            logger.Debug("Reading stata file from " + originalStataFilePath);
            logger.Debug("Writing Stata update to temporary file " + newStataFilePath);

            // Read the Stata file.
            using (var reader = new StataDataReader(originalStataFilePath))
            using (var writer = new StataDataWriter(newStataFilePath))
            {
                if (reader.StataFile == null)
                {
                    logger.Warn("Reader StataFile is null");
                }

                writer.StataFile = reader.StataFile;

                // For all the information in the repository, update the Stata file.

                // For each variable:
                var repositoryVariables = physicalInstance.DataRelationships
                    .SelectMany(x => x.LogicalRecords)
                    .SelectMany(x => x.VariablesInRecord)
                    .ToList();

                if (repositoryVariables == null)
                {
                    logger.Warn("No variable information in the repository");
                    return false;
                }

                logger.Debug("Loaded " + repositoryVariables.Count + " repository variables");

                if (writer.StataFile.Variables == null)
                {
                    logger.Warn("Stata file Variables collection is null");
                }

                if (repositoryVariables.Count != writer.StataFile.Variables.Count)
                {
                    logger.Warn("Stata variable count does not match metadata variable count.");
                    return false;
                }

                for (int variableIdx = 0; variableIdx < repositoryVariables.Count; variableIdx++)
                {
                    // Match the Stata variable to the metadata variable by index.
                    var metadataVariable = repositoryVariables[variableIdx];
                    var stataVariable = writer.StataFile.Variables[variableIdx];

                    if (metadataVariable == null)
                    {
                        logger.Warn("No metadata for variable " + variableIdx.ToString());
                        return false;
                    }

                    if (stataVariable == null)
                    {
                        logger.Warn("No information available for Stata variable " + variableIdx.ToString());
                    }

                    // Variable name.
                    stataVariable.Name = GetSafeStataString(metadataVariable.ItemName.Best, StataDataWriter.VariableNameLengthLimit);

                    // Variable label.
                    stataVariable.Label = GetSafeStataString(metadataVariable.Label.Best, StataDataWriter.VariableLabelLengthLimit);

                    // If the DDI has a CodeList but the Stata file does not, assign it.
                    bool stataHasCodeList = false;
                    if (string.IsNullOrWhiteSpace(stataVariable.CodeListName))
                    {
                        stataHasCodeList = false;
                    }
                    else
                    {
                        // Don't trust the presence of the name. Verify that the code list actually exists.
                        stataHasCodeList = GetStataCodeList(stataVariable.CodeListName, writer.StataFile) != null;
                    }


                    // Create the Stata Code List, if it needs one but one does not yet exist.
                    if (metadataVariable.CodeRepresentation != null &&
                        !stataHasCodeList)
                    {
                        var stataCodeList = CreateStataCodeList(metadataVariable.CodeRepresentation.Codes);
                        writer.StataFile.CodeLists.Add(stataCodeList);

                        if (metadataVariable.CodeRepresentation != null &&
                            metadataVariable.CodeRepresentation.Codes != null)
                        {
                            stataVariable.CodeListName = GetSafeCodeListName(metadataVariable.CodeRepresentation.Codes);
                        }
                    }

                    // For each category in a code list:
                    if (!string.IsNullOrWhiteSpace(stataVariable.CodeListName))
                    {
                        var stataCodeList = GetStataCodeList(stataVariable.CodeListName, writer.StataFile);

                        var allValuesInStata = new List<int>();
                        if (stataCodeList != null &&
                            stataCodeList.Codes != null &&
                            stataCodeList.Codes.Keys != null)
                        {
                            allValuesInStata = stataCodeList.Codes.Keys.ToList();
                        }

                        foreach (int stataCodeValue in allValuesInStata)
                        {
                            // Value label.
                            if (metadataVariable.CodeRepresentation != null &&
                                metadataVariable.CodeRepresentation.Codes != null)
                            {
                                var metadataCode = metadataVariable.CodeRepresentation
                                    .Codes
                                    .GetFlattenedCodes()
                                    .Where(x => x.Value == stataCodeValue.ToString())
                                    .FirstOrDefault();

                                if (metadataCode != null &&
                                    metadataCode.Category != null)
                                {
                                    stataCodeList.Codes[stataCodeValue] = GetSafeStataString(metadataCode.Category.Label.Best, StataDataWriter.ValueLabelLengthLimit);
                                }

                                if (metadataCode.Category == null)
                                {
                                    logger.Warn("metadataCode.Category is null");
                                }
                            }
                        }
                    }
                }

                writer.CommitSchema();

                // Write the data as-is.
                object[] values = new object[reader.FieldCount];
                while (reader.Read())
                {
                    reader.GetValues(values, false);
                    writer.WriteValues(values);
                }

                writer.CommitData();
            }

            // Overwrite the original Stata file with the updated one.
            try
            {
                File.Copy(newStataFilePath, originalStataFilePath, true);
                File.Delete(newStataFilePath);
            }
            catch (Exception ex)
            {
                logger.Warn("Failed to copy new Stata file.", ex);
                return false;
            }

            // Commit the changes to git.
            try
            {
                using (Repository repo = new Repository(gitRepositoryPath))
                {
                    var author = new Signature(user.UserName, user.Email, DateTime.UtcNow);
                    var committer = new Signature(userId.ToString(), userId.ToString() + "@curator", DateTime.UtcNow);

                    Commands.Stage(repo, originalStataFilePath);
                    repo.Commit("Update Stata file from metadata", author, committer);
                }
            }
            catch
            {

            }

            // Log this operation.
            var log = new Event()
            {
                EventType = EventTypes.ApplyMetadataUpdates,
                Timestamp = DateTime.UtcNow,
                User = user,
                RelatedCatalogRecord = file.CatalogRecord,
                Title = "Finished applying metadata updates",
            };

            log.RelatedManagedFiles.Add(file);

            db.Events.Add(log);

            // Mark no more pending metadata changes.
            file.HasPendingMetadataUpdates = false;

            return true;
        }

        string GetSafeCodeListName(CodeList codeList)
        {
            if (!codeList.ItemName.IsEmpty)
            {
                return codeList.ItemName.Best;
            }

            return codeList.Label.Best.Replace(" ", string.Empty);
        }

        StataCodeList GetStataCodeList(string name, StataFile stataFile)
        {
            return stataFile.CodeLists.Where(x => x.Name == name).FirstOrDefault();
        }

        StataCodeList CreateStataCodeList(CodeList codeList)
        {
            var stataCodeList = new StataCodeList();

            stataCodeList.Name = codeList.ItemName.Best;

            foreach (var code in codeList.GetFlattenedCodes())
            {
                int value = 0;
                if (int.TryParse(code.Value, out value))
                {
                    string label = code.Category.Label.Best;
                    stataCodeList.Codes.Add(value, label);
                }
            }

            return stataCodeList;
        }

        string GetSafeStataString(string str, int limit)
        {
            if (str.Length > limit)
            {
                return str.Substring(0, limit);
            }

            return str;
        }

        public static PhysicalInstance GetPhysicalInstance(ManagedFile file, string agencyID)
        {
            var client = RepositoryHelper.GetClient();

            var pi = client.GetLatestItem(file.Id, agencyID)
                as PhysicalInstance;

            var populator = new GraphPopulator(client);
            populator.ChildProcessing = ChildReferenceProcessing.PopulateLatest;
            pi.Accept(populator);

            return pi;
        }
    }
}
