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

using Colectica.Curation.Contracts;
using Colectica.Curation.Data;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Colectica.Curation.DdiAddins.Actions
{
    [Export(typeof(IPublishAction))]
    public class CopyPublishedFiles : IPublishAction
    {
        public string Name => "Copy published files";

        public void PublishRecord(CatalogRecord record, ApplicationUser user, ApplicationDbContext db, string ProcessingDirectory)
        {
            string destination = "TODO";

            if (!Directory.Exists(destination))
            {
                Log.Logger.Warning("Destination folder does not exist. Exiting.");
                return;
            }

            Log.Debug("Copying published files for record {recordId} {recordTitle}", record.Id, record.Title);

            foreach (var file in record.Files)
            {
                if (!file.IsPublicAccess)
                {
                    continue;
                }

                Log.Debug("Processing file {file}", file.Title);

                // Copy the file.
                string sourcePath = Path.Combine(
                    record.Organization.ProcessingDirectory,
                    file.CatalogRecord.Id.ToString(),
                    file.Name);
                string targetPath = Path.Combine(
                    destination,
                    record.Id.ToString(),
                    file.Name);

                Log.Debug("Copying from {sourcePath} to {targetPath}", sourcePath, targetPath);
                string directory = Path.GetDirectoryName(targetPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                try
                {
                    File.Copy(sourcePath, targetPath);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error copying the file.");
                }

            }


            Log.Debug("Done copying files");
        }
    }
}
