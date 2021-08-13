namespace Fig.Agent.Commands
{
    using Fig.Agent.Infrastructure;
    using Fig.Common;
    using Fig.Common.Checksums;
    using Fig.Config;
    using Microsoft.Extensions.Logging;
    using Spectre.Console;
    using Spectre.Console.Cli;
    using System;
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    [Description("Validates that the local configuration matches the stored manifest.")]
    internal class VerifyVersionCommand : AsyncCommand<VerifyVersionCommand.Settings>
    {
        private readonly ConfigurationImporter configValidator;

        protected readonly CancellationToken cancellationToken;

        protected readonly ILogger<VerifyVersionCommand> logger;

        public VerifyVersionCommand(ConfigurationImporter configValidator, ILogger<VerifyVersionCommand> logger, CancelSource cancelSource)
        {
            this.cancellationToken = cancelSource.Token;
            this.configValidator = configValidator;
            this.logger = logger;
        }

        public class Settings : CommonSettings
        {
            /// <summary>
            /// Gets or sets the human readable version associated with the manifest.
            /// </summary>
            [CommandArgument(0, "[VERSION]")]
            [Description("The human readable version associated with the manifest.")]
            public string? Version { get; init; }
        }

        public override async Task<int> ExecuteAsync([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            return await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync<int>("Loading latest version...", async (spinner) =>
                {
                    var dataDirectory = settings.GetDataDirectory();

                    var version = settings.Version;
                    if (version is null)
                    {
                        version = await dataDirectory.VersionLog.GetVersionsAsync().Select(v => v.Version).LastOrDefaultAsync(cancellationToken);
                    }

                    if (version is null)
                    {
                        throw new Common.Exceptions.FigNoVersionSelectedException();
                    }

                    spinner.Status = "Loading manifest file...";
                    var manifest = await dataDirectory.GetManifestAsync(version, cancellationToken);
                    logger.LogInformation("Manifest file loaded for version {Version}.", manifest.Version);

                    spinner.Status = "Validating manifest file...";
                    manifest.Validate();
                    logger.LogInformation("Manifest file passed validation.");

                    
                    spinner.Status = $"Computing file hashes...";
                    var fileHashes = await Task.WhenAll(manifest.Files.Select(async f =>
                    {
                        using var fs = await dataDirectory.GetFileReadStreamAsync(f.Checksum!, cancellationToken);
                        var trueChecksum = await Checksum.Get(f.Checksum).GetHashStringAsync(fs, cancellationToken);

                        return (File: f, TrueChecksum: trueChecksum);
                    }));

                    logger.LogInformation("File hashes computed for {FileCount} files.", fileHashes.Length);

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
