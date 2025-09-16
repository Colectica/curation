using System;
using System.Data.Entity;
using Colectica.Curation.Data;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Colectica.Curation.Cli.Commands;

public class InspectRecords
{
    internal void Inspect(IConfiguration config)
    {
        string connectionString = config["Data:DefaultConnection:ConnectionString"] ?? "";
        using var db = new ApplicationDbContext(connectionString);


        var publishedRecords = db.CatalogRecords
            .Include(x => x.Owner)
            .Where(x => x.Status == CatalogRecordStatus.Published)
            .ToList();
        if (!publishedRecords.Any())
        {
            Log.Logger.Information("No published records found. Exiting.");
            return;
        }

        foreach (var record in publishedRecords)
        {
            Log.Information("Inspecting record {RecordNumber} - {Title}", record.Number, record.Title);

            if (record.OutcomeMeasures.Contains(','))
            {
                Log.Information("    OutcomeMeasures with comma: {OutcomeMeasures}", record.OutcomeMeasures);
            }

            Log.Information("    Funding: " + record.Funding);
        }

    }
}
