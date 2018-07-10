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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Data.Entity;
using System.Text;
using System.Threading.Tasks;
using Colectica.Curation.Common.Utility;
using LibGit2Sharp;
using log4net;
using Colectica.Curation.Contracts;
using System.ComponentModel.Composition;

namespace Colectica.Curation.Operations
{
    public class ApplyMetadataUpdates : IOperation
    {
        public static readonly Guid OperationTypeId = new Guid("295A391E-E211-49C6-A466-68F53234B845");
        public Guid OperationType { get { return OperationTypeId; } }

        public string Name { get; set; }

        public int Timeout { get; set; }

        public Guid FileId { get; set; }

        public string ProcessingDirectory { get; set; }

        public string UserId { get; set; }

        [ImportMany]
        public List<IApplyMetadataUpdatesAction> ApplyMetadataUpdateActions { get; set; }

        ILog logger;

        public ApplyMetadataUpdates()
        {
            logger = LogManager.GetLogger("Curation");
        }

        public bool Execute()
        {
            logger.Debug("Applying metadata updates");

            using (var db = ApplicationDbContext.Create())
            {
                // Get the file to update.
                var file = db.Files.Where(x => x.Id == FileId)
                    .Include(x => x.CatalogRecord)
                    .Include(x => x.CatalogRecord.Organization)
                    .FirstOrDefault();
                if (file == null)
                {
                    return false;
                }

                var record = file.CatalogRecord;
                var user = db.Users.Find(UserId.ToString());

                // Give any ApplyMetadataUpdate addins the chance to act on this.
                foreach (var addin in ApplyMetadataUpdateActions)
                {
                    try
                    {
                        if (addin.CanApplyMetadataUpdates(file))
                        {
                            addin.ApplyMetadataUpdates(record, file, user, UserId, db, ProcessingDirectory);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error($"Error applying metadata update via addin: {addin.Name}.", ex);
                    }
                }

                // If no addins are registered to handle the updates for this file type, 
                // mark the file as not pending. Otherwise it will be stuck in that state
                // forever.
                if (ApplyMetadataUpdateActions.Count == 0)
                {
                    file.HasPendingMetadataUpdates = false;
                }

                db.SaveChanges();
            }

            logger.Debug("Finished applying metadata updates to Stata file.");

            return true;
        }

    }
}
