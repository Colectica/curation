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
using Colectica.Curation.Web.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;
using System.IO;
using log4net;
using System.ComponentModel.Composition;
using Colectica.Curation.Contracts;

// Make this an IAddFileAction not an IOperation
// Run from Add Files

namespace Colectica.Curation.Operations
{
    // TODO Rename this operation something generic, like ManagedFileOperation.
    // CreatePhysicalInstance is one thing that we provide an addin to perform, but
    // there could be others.

    /// <summary>
    /// Operation to create a PhysicalInstance for any ManagedFiles that do not already have one.
    /// </summary>
    public class CreatePhysicalInstances : IOperation
    {
        public static readonly Guid OperationTypeId = new Guid("1D4A4C4B-5885-4E45-ABD3-FD819B26B3A9");
        public Guid OperationType { get { return OperationTypeId; } }

        public string Name { get; set; }

        public int Timeout { get; set; }

        public string UserId { get; set; }

        public Guid CatalogRecordId { get; set; }

        public string ProcessingDirectory { get; set; }

        [ImportMany]
        public List<IFileAction> FileActions { get; set; }

        ILog logger;

        public CreatePhysicalInstances()
        {
            logger = LogManager.GetLogger("Curation");
        }

        public bool Execute()
        {
            using (var db = ApplicationDbContext.Create())
            {
                var record = db.CatalogRecords.Where(x => x.Id == CatalogRecordId)
                    .Include(x => x.Files)
                    .Include(x => x.Organization)
                    .FirstOrDefault();
                if (record == null)
                {
                    logger.Warn("CatalogRecord does not exist. " + CatalogRecordId.ToString());
                    return false;
                }

                var user = db.Users.Find(UserId.ToString());

                string agencyId = record.Organization.AgencyID;

                // Run any addin actions on each file.
                foreach (var file in record.Files)
                {
                    foreach (var addin in FileActions)
                    {
                        try
                        {
                            if (!addin.CanRun(file))
                            {
                                continue;
                            }

                            string path = Path.Combine(ProcessingDirectory, record.Id.ToString());
                            addin.Run(record, file, user, db, path, agencyId);
                        }
                        catch (Exception ex)
                        {
                            logger.Error($"Error performing file action {addin.Name}.", ex);
                        }
                    }
                }

                return true;
            }
        }


    }
}
