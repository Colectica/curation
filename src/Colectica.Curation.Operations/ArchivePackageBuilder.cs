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
using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Colectica.Curation.Operations
{
    public class ArchivePackageBuilder
    {
        public static void CreateArchivePackage(CatalogRecord record, string sourceDirectory, bool includeHandleMap, string archiveDirectory, string nameSuffix)
        {
            string bagitFileContents = "BagIt-Version: 0.97\nTag-File-Character-Encoding: UTF-8";

            // Make the manifest file.
            string manifestFileName = "manifest-sha256.txt";
            var manifestBuilder = new StringBuilder();
            foreach (var managedFile in record.Files.Where(x => x.Status != FileStatus.Removed))
            {
                manifestBuilder.AppendLine(string.Format("{0} data/{1}", managedFile.Checksum, managedFile.Name));
            }

            // Make the handle mapping file.
            string handleMapFileName = "handle-map.txt";
            var handleMapBuilder = new StringBuilder();

            // Header line
            handleMapBuilder.AppendLine("Handle,File,UUID");

            foreach (var managedFile in record.Files.Where(x => x.Status != FileStatus.Removed))
            {
                handleMapBuilder.AppendLine(string.Format("{0},data/{1},{2}", managedFile.PersistentLink, managedFile.Name, managedFile.Id));
            }

            // Create the ZIP with the text files and all ManagedFiles into the data/ directory
            string basePath = string.Empty;
            if (!string.IsNullOrWhiteSpace(record.StudyId))
            {
                basePath = record.StudyId.ToString();
            }
            else
            {
                basePath = record.Id.ToString();
            }

            string zipPath = Path.Combine(archiveDirectory, basePath + "-" + nameSuffix + ".zip");
            if (File.Exists(zipPath))
            {
                zipPath = GetUniqueFileName(zipPath);
            }

            using (var zip = new ZipFile(zipPath))
            {
                // Add the bagit.txt file
                zip.AddEntry(basePath + "/bagit.txt", bagitFileContents);

                // Add the manifest file.
                zip.AddEntry(basePath + "/" + manifestFileName, manifestBuilder.ToString());

                // Add the handle map.
                if (includeHandleMap)
                {
                    zip.AddEntry(basePath + "/" + handleMapFileName, handleMapBuilder.ToString());
                }

                // Add each ManagedFile.
                foreach (var managedFile in record.Files.Where(x => x.Status != FileStatus.Removed))
                {
                    var managedFilePath = Path.Combine(sourceDirectory, record.Id.ToString(), managedFile.Name);

                    string pathInZip = basePath + "/data/" + managedFile.Name;

                    if (!zip.ContainsEntry(pathInZip))
                    {
                        using (var fileStream = new FileStream(managedFilePath, FileMode.Open))
                        {
                            zip.AddEntry(pathInZip, fileStream);
                            zip.Save();
                        }
                    }
                }

                // Save the ZIP to the archive space.
                zip.Save();
            }
        }

        private static string GetUniqueFileName(string zipPath)
        {
            for (int i = 1; true; i++)
            {
                string freeFileName = zipPath.Insert(zipPath.Length - 4, "[" + i.ToString() + "]");
                if (!File.Exists(freeFileName))
                {
                    return freeFileName;
                }
            }
        }
    }
}
