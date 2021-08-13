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

    internal class ImportVersionCommand : AsyncCommand<ImportVersionCommand.Settings>
    {
        protected readonly CancellationToken cancellationToken;
        private readonly ConfigurationImporter importer;
        protected readonly ILogger<SetVersionCommand> logger;

        public ImportVersionCommand(ConfigurationImporter importer, ILogger<SetVersionCommand> logger, CancelSource cancelSource)
        {
            this.cancellationToken = cancelSource.Token;
            this.importer = importer;
            this.logger = logger;
        }

        public class Settings : CommonSettings
        {
            /// <summary>
            /// Gets or sets the path to the directory containing the manifest to import.
            /// </summary>
            [CommandArgument(0, "[PATH]")]
            [Description("The path to the directory containing the manifest to import.")]
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
                .StartAsync<int>($"Validating manifest...", async spinner =>
                {
                    var manifest = await Manifest.ReadAsync(settings.Path, settings.PollingInterval, cancellationToken);
                    manifest.Validate();
                    logger.LogInformation("Manifest is vaild");

                    spinner.Status = "Validating files in manifest...";
                    await importer.ValidateAsync(manifest, settings.Path, settings.PollingInterval, cancellationToken);
                    logger.LogInformation("Configuration files are valid.");

                    spinner.Status = "Preparing data directory...";
                    var dataDirectory = settings.GetDataDirectory();
                    await dataDirectory.InitializeAsync(cancellationToken);

                    if (!settings.Force)
                    {
                        try
                        {
                            await dataDirectory.GetManifestAsync(manifest.Version!, cancellationToken);
                            logger.LogError("This configuration version already exists. Specify the '--force' flag to overwrite it.");
                            return 1;
                        }
                        catch (FigVersionNotFoundException)
                        {
                        }
                    }

                    spinner.Status = "Importing configuration...";
                    await importer.ImportAsync(manifest, settings.Path, dataDirectory, cancellationToken);
                    logger.LogInformation("Finished importing configuration.");

                    spinner.Status = "Validating imported configuration...";
                    var client = settings.GetConfigClient();
                    var importedVersion = await client.GetVersionAsync(manifest.Version!, cancellationToken);
                    await importedVersion.VerifyAsync(cancellationToken);
                    logger.LogInformation($"Import completed successfully, use 'version set {manifest.Version}' to switch to this configuration.");

                    return 0;
                });
        }
    }
}
