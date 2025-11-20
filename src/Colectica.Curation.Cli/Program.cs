using Serilog;
using System.CommandLine.Invocation;
using System.CommandLine;
using Colectica.Curation.Cli.Commands;
using Microsoft.Extensions.Configuration;
using System.Runtime.CompilerServices;
using System.Data.Entity;
using Colectica.Curation.Data;

namespace Colectica.Curation.Cli
{
    internal class Program
    {
        static IConfiguration? config;

        static void Main(string[] args)
        {
            try
            {
                // CRITICAL: Configure Entity Framework to use Npgsql BEFORE anything else
                // This is required for .NET Core/.NET 5+ as App.config is not automatically loaded
                DbConfiguration.SetConfiguration(new NpgsqlConfiguration());
                Console.WriteLine("DbConfiguration set to NpgsqlConfiguration");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting DbConfiguration: {ex}");
                return;
            }

            // Set up logging.
            var log = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File("logs/curation-cli-.txt", rollingInterval: RollingInterval.Day)
                .WriteTo.File(new Serilog.Formatting.Json.JsonFormatter(), "logs/curation-cli-.json", Serilog.Events.LogEventLevel.Warning, rollingInterval: RollingInterval.Day)
                .MinimumLevel.Verbose()
                .CreateLogger();
            Log.Logger = log;
            
            // Note: Database.SetInitializer is NOT called here because it would try to use
            // the parameterless ApplicationDbContext constructor which looks for "DefaultConnection" 
            // in app.config. Instead, migrations will run automatically when the context is first used,
            // since AutomaticMigrationsEnabled = true in the Configuration class.

            // Load the configuration.
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            IConfigurationBuilder builder = new ConfigurationBuilder()
               .SetBasePath(basePath)
               .AddJsonFile("appsettings.json")
               .AddJsonFile("appsettings.Development.json", optional:true)
               .AddEnvironmentVariables();

            builder.AddEnvironmentVariables();
            config = builder.Build();
            if (config == null)
            {
                Log.Error("Failed to load configuration.");
                return;
            }

            // Build the commands.
            var root = new RootCommand("Colectica Curation Command Line Tool");

            // copy-published-files
            var copyPublishedFilesCommand = new Command("copy-published-files", "Copies any published files to a new location");
            copyPublishedFilesCommand.Add(new Option<string>("--destination", "The folder to which the published files should be copied"));
            copyPublishedFilesCommand.Handler = CommandHandler.Create<string>(CopyPublishedFiles);
            root.Add(copyPublishedFilesCommand);

            // publish-all-to-dataverse
            var publishAllToDataverseCommand = new Command("publish-to-dataverse", "Publish records to Dataverse");
            publishAllToDataverseCommand.Add(new Option<string>("--dataverse-url", "The URL of the Dataverse instance"));
            publishAllToDataverseCommand.Add(new Option<string>("--catalog-record-number", "The number of the catalog record to publish; if not specified, all records are published"));
            publishAllToDataverseCommand.Handler = CommandHandler.Create<string, string>(PublishAllToDataverse);
            root.Add(publishAllToDataverseCommand);

            // inpsect-records
            var inspectRecordsCommand = new Command("inspect-records", "Inspect records for potential issues before publishing");
            inspectRecordsCommand.Handler = CommandHandler.Create<string>(InspectRecords);
            root.Add(inspectRecordsCommand);

            // Run the command
            try
            {
                root.InvokeAsync(args).Wait();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unhandled error.");
            }
        }

        private static void InspectRecords(string obj)
        {
            if (config == null)
            {
                return;
            }

            var inspector = new InspectRecords();
            inspector.Inspect(config);
        }

        private static void CopyPublishedFiles(string destination)
        {
            if (config == null)
            {
                return;
            }

            var copyPublishedFiles = new CopyPublishedFiles();
            copyPublishedFiles.Copy(destination, config);
        }

        private static void PublishAllToDataverse(string dataverseUrl, string catalogRecordNumber)
        {
            if (config == null)
            {
                return;
            }

            var publisher = new PublishToDataverse(dataverseUrl, config, catalogRecordNumber);
            publisher.Publish().Wait();
        }

    }
}