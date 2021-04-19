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

﻿using Colectica.Curation.Addins.Editors.Mappers;
using Colectica.Curation.Common.Utility;
using Colectica.Curation.Contracts;
using Colectica.Curation.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Colectica.Curation.DdiAddins.Actions
{
    [Export(typeof(IFinalizeMetadataAction))]
    public class CreateDdiMetadata : IFinalizeMetadataAction
    {
        public string Name { get { return "Create DDI Metadata"; } }

        public void FinalizeMetadata(CatalogRecord record, ApplicationUser user, ApplicationDbContext db, string processingDirectory, 
            Guid reservedUniqueId, string reservedPersistentId)
        {
            // Update the DDI StudyUnit.
            try
            {
                CurationToDdiMapper.UpdateRepositoryItemFromModel(record);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error updating repository from catalog record");
                EventService.LogEvent(record, user, db, EventTypes.FinalizeCatalogRecordFailed, "Failed to update repository from catalog record.", ex.Message);
            }

            // Export the DDI XML.
            string fileName = string.Empty;
            if (!string.IsNullOrWhiteSpace(record.StudyId))
            {
                fileName = record.StudyId + ".ddi32.xml";
            }
            else
            {
                fileName = record.Id.ToString() + ".ddi32.xml";
            }

            string ddiFilePath = Path.Combine(processingDirectory, record.Id.ToString(), fileName);
            try
            {
                CurationToDdiMapper.SaveCatalogRecordXml(record, ddiFilePath);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error creating DDI XML file.");
                EventService.LogEvent(record, user, db, EventTypes.FinalizeCatalogRecordFailed, "Failed to create DDI XML file.", ex.Message);
            }

            // Make a checksum.
            string checksum = string.Empty;
            using (var hasher = SHA256Managed.Create())
            using (var fileStream = new FileStream(ddiFilePath, FileMode.Open))
            {
                byte[] hashValue = hasher.ComputeHash(fileStream);
                checksum = BitConverter.ToString(hashValue).Replace("-", String.Empty);
            }

            var existingFile = record.Files.Where(x => x.Name == fileName).FirstOrDefault();
            if (existingFile == null)
            {
                var id = (reservedUniqueId != Guid.Empty) ? reservedUniqueId : Guid.NewGuid();

                // Make a ManagedFile for the DDI XML file.
                var managedFile = new ManagedFile()
                {
                    Id = id,
                    PersistentLink = reservedPersistentId,
                    PersistentLinkDate = DateTime.UtcNow,
                    CatalogRecord = record,
                    CreationDate = DateTime.UtcNow,
                    Name = fileName,
                    FormatName = Path.GetExtension(fileName).ToLower(),
                    Size = new FileInfo(ddiFilePath).Length,
                    Source = "Curation System",
                    PublicName = fileName,
                    Type = "Codebook",
                    Software = "DDI",
                    UploadedDate = DateTime.UtcNow,
                    Status = Colectica.Curation.Data.FileStatus.Accepted,
                    Owner = user,
                    Checksum = checksum,
                    ChecksumMethod = "SHA256",
                    ChecksumDate = DateTime.UtcNow
                };

                record.Files.Add(managedFile);
            }
            else
            {
                // Update the file existing information.
                existingFile.CreationDate = DateTime.UtcNow;
                existingFile.UploadedDate = DateTime.UtcNow;
                existingFile.Size = new FileInfo(ddiFilePath).Length;
                existingFile.Checksum = checksum;
                existingFile.ChecksumMethod = "SHA256";
                existingFile.ChecksumDate = DateTime.UtcNow;

                if (string.IsNullOrWhiteSpace(existingFile.PersistentLink) &&
                    !string.IsNullOrWhiteSpace(reservedPersistentId))
                {
                    existingFile.PersistentLink = reservedPersistentId;
                    existingFile.PersistentLinkDate = DateTime.UtcNow;
                }
            }

            EventService.LogEvent(record, user, db, EventTypes.GenerateMetadata, "Generated DDI 3 Metadata");
        }
    }
}
