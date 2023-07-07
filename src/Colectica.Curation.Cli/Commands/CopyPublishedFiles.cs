using Colectica.Curation.Data;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Colectica.Curation.Cli.Commands
{
    public class CopyPublishedFiles
    {
        public void Copy(string destination, IConfiguration config)
        {
            Log.Logger.Information("Copying published files to {destination}", destination);

            if (!Directory.Exists(destination))
            {
                Log.Logger.Warning("Destination folder does not exist. Exiting.");
                return;
            }

            string connectionString = config["Data:DefaultConnection:ConnectionString"];
            using var db = new ApplicationDbContext(connectionString);

            var publishedRecords = db.CatalogRecords.Where(x => x.Status == CatalogRecordStatus.Published).ToList();

            if (!publishedRecords.Any())
            {
                Log.Logger.Information("No published records found. Exiting.");
                return;
            }

            var builder = new StringBuilder();
            builder.AppendLine(@"""Handle"",""URL""");

            foreach (var record in publishedRecords)
            {
                Log.Debug("Processing record {recordId} {recordTitle}", record.Id, record.Title);

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
                        "published",
                        record.Id.ToString(),
                        file.Name);

                    Log.Debug("Copying from {sourcePath} to {targetPath}", sourcePath, targetPath);
                    string directory = Path.GetDirectoryName(targetPath);
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    File.Copy(sourcePath, targetPath);

                    // Add to CSV that maps the Handles to the new URLs
                    string url = $"/published/{record.Id.ToString()}/{file.Name}";
                    builder.AppendLine($"\"{file.PersistentLink}\",\"{url}\"");
                }
            }

            string handleMapFileName = Path.Combine(destination, "handle-map.csv");
            File.WriteAllText(handleMapFileName, builder.ToString());

        }

    }
}
