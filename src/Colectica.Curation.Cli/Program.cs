using Serilog;
using System.CommandLine.Invocation;
using System.CommandLine;
using Colectica.Curation.Cli.Commands;
using Microsoft.Extensions.Configuration;
using System.Runtime.CompilerServices;

namespace Colectica.Curation.Cli
{
    internal class Program
    {
        static IConfiguration? config;

        static void Main(string[] args)
        {
            // Set up logging.
            var log = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File("logs/curation-cli-.txt", rollingInterval: RollingInterval.Day)
                .MinimumLevel.Verbose()
                .CreateLogger();
            Log.Logger = log;

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
            var publishAllToDataverseCommand = new Command("publish-all-to-dataverse", "Publish all records to Dataverse");
            publishAllToDataverseCommand.Add(new Option<string>("--dataverse-url", "The URL of the Dataverse instance"));
            publishAllToDataverseCommand.Handler = CommandHandler.Create<string>(PublishAllToDataverse);
            root.Add(publishAllToDataverseCommand);

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


        private static void CopyPublishedFiles(string destination)
        {
            if (config == null)
            {
                return;
            }

            var copyPublishedFiles = new CopyPublishedFiles();
            copyPublishedFiles.Copy(destination, config);
        }

        private static void PublishAllToDataverse(string dataverseUrl)
        {
            if (config == null)
            {
                return;
            }

            var publisher = new PublishToDataverse(dataverseUrl, config);
            publisher.Publish().Wait();
        }

    }
}