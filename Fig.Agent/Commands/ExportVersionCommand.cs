namespace Fig.Agent.Commands
{
    using Fig.Agent.Infrastructure;
    using Fig.Common;
    using Fig.Common.Exceptions;
    using Fig.Config;
    using Microsoft.Extensions.Logging;
    using Spectre.Console;
    using Spectre.Console.Cli;
    using System;
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    [Description("Exports a specific configuration version to a directory for viewing/editing.")]
    internal class ExportVersionCommand : AsyncCommand<ExportVersionCommand.Settings>
    {
        protected readonly CancellationToken cancellationToken;
        private readonly ConfigurationImporter importer;
        protected readonly ILogger<ExportVersionCommand> logger;

        public ExportVersionCommand(ConfigurationImporter importer, ILogger<ExportVersionCommand> logger, CancelSource cancelSource)
        {
            this.cancellationToken = cancelSource.Token;
            this.importer = importer;
            this.logger = logger;
        }

        public class Settings : CommonSettings
        {
            /// <summary>
            /// Gets or sets the human readable version associated with the manifest.
            /// </summary>
            [CommandArgument(0, "[VERSION]")]
            [Description("The version which should be exported.")]
            public string? Version { get; init; }

            /// <summary>
            /// Gets or sets the path to the directory containing the manifest to import.
            /// </summary>
            [CommandArgument(1, "[PATH]")]
            [Description("The path to the directory which should receive the exported configuration.")]
            [TypeConverter(typeof(DirectoryInfoTypeConverter))]
            public DirectoryInfo Path { get; init; } = new DirectoryInfo(Environment.CurrentDirectory);

            /// <summary>
            /// Gets or sets a value indicating whether an existing configuration version will be overwritten.
            /// </summary>
            [CommandOption("-f|--force")]
            [Description("Forces the importing of this configuration, even if that involves overwriting existing files.")]
            public bool Force { get; init; } = false;
        }

        public override async Task<int> ExecuteAsync([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            if (settings.Path is null)
            {
                logger.LogError("You have not specified a valid path to a folder containing a manifest.");
                return 1;
            }

            var configClient = settings.GetConfigClient();

            return await AnsiConsole.Status()
                .StartAsync<int>($"Checking current version...", async spinner =>
                {
                    var client = settings.GetConfigClient();
                    var version = settings.Version is null ? await client.GetCurrentVersionAsync(cancellationToken) : await client.GetVersionAsync(settings.Version, cancellationToken);
                    logger.LogInformation("Loaded configuration version {Version}.", version.Manifest.Version);

                    var dataDirectory = settings.GetDataDirectory();

                    spinner.Status = "Exporting configuration...";
                    await importer.ExportAsync(version.Manifest, settings.Path, dataDirectory, cancellationToken);
                    logger.LogInformation("Exported {Files} configuration files.", version.Manifest.Files.Count());

                    return 0;
                });
        }
    }
}
