namespace Fig.Agent.Commands
{
    using Fig.Agent.Infrastructure;
    using Fig.Common;
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

    [Description("Validates that the local configuration matches the stored manifest.")]
    internal class VerifyManifestCommand : AsyncCommand<VerifyManifestCommand.Settings>
    {
        private readonly ConfigurationImporter importer;

        protected readonly CancellationToken cancellationToken;

        protected readonly ILogger<VerifyManifestCommand> logger;

        public VerifyManifestCommand(ConfigurationImporter importer, ILogger<VerifyManifestCommand> logger, CancelSource cancelSource)
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
            [CommandArgument(0, "[PATH]")]
            [Description("The path to the directory containing the manifest.")]
            public DirectoryInfo Path { get; init; } = new DirectoryInfo(Environment.CurrentDirectory);
        }

        public override async Task<int> ExecuteAsync([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            return await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync<int>("Loading manifest file...", async (spinner) =>
                {
                    var manifest = await Manifest.ReadAsync(settings.Path, settings.PollingInterval, this.cancellationToken);
                    logger.LogInformation("Manifest file loaded.");

                    spinner.Status = "Validating manifest file...";
                    manifest.Validate();
                    logger.LogInformation("Manifest file passed validation.");

                    spinner.Status = $"Computing file hashes...";
                    var fileHashes = await importer.GetTrueFileHashesAsync(manifest, settings.Path, settings.PollingInterval, cancellationToken);
                    logger.LogInformation("File hashes computed.");
                    
                    spinner.Status = $"Comparing file hashes...";

                    var invalidFiles = fileHashes.Where(f => !string.Equals(f.File.Checksum, f.TrueChecksum, StringComparison.Ordinal));

                    if (!invalidFiles.Any())
                    {
                        logger.LogInformation("All files matched their expected hashes.");
                        return 1;
                    }

                    var table = new Table();
                    table
                        .AddColumn("[bold blue]File[/]")
                        .AddColumn("[bold blue]Expected Hash[/]")
                        .AddColumn("[bold red]True Hash[/]");

                    foreach (var file in invalidFiles)
                    {
                        table.AddRow(
                            file.File.FileName!,
                            file.File.Checksum!,
                            file.TrueChecksum);
                    }

                    logger.LogError("The following files had checksums which did not match the manifest");
                    AnsiConsole.Render(table);

                    return 0;
                });
        }
    }
}
