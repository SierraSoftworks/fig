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
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    [Description("Builds a manifest file for the provided configuration directory.")]
    internal class BuildManifestCommand : AsyncCommand<BuildManifestCommand.Settings>
    {

        public ILogger<BuildManifestCommand> Logger { get; }

        protected CancellationToken CancellationToken { get; }

        public BuildManifestCommand(ILogger<BuildManifestCommand> logger, CancelSource cancelSource)
        {
            this.Logger = logger;
            this.CancellationToken = cancelSource.Token;
        }

        public class Settings : CommandSettings
        {
            /// <summary>
            /// Gets or sets the path to the directory containing configuration files to build a manifest for.
            /// </summary>
            [CommandArgument(0, "[PATH]")]
            [Description("The path to the directory containing the manifest.")]
            [TypeConverter(typeof(DirectoryInfoTypeConverter))]
            public DirectoryInfo Path { get; init; } = new DirectoryInfo(Environment.CurrentDirectory);

            /// <summary>
            /// Gets or sets the human readable version associated with the manifest.
            /// </summary>
            [CommandArgument(0, "[VERSION]")]
            [Description("The human readable version associated with the manifest.")]
            public string Version { get; init; } = DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ");

            /// <summary>
            /// Gets or sets the hash algorithm used to generate checksums for the files.
            /// </summary>
            [CommandOption("--hash <ALGORITHM>")]
            [Description("The hash algorithm to use when computing manifest file hashes.")]
            [DefaultValue("sha256")]
            public string HashAlgorithm { get; init; } = Checksum.DefaultAlgorithm;

            /// <summary>
            /// Gets or sets the filter used to select files which are included in the manfiest.
            /// </summary>
            [Description("The filter used to select the files included in the manifest.")]
            [CommandOption("--filter <PATTERN>")]
            [DefaultValue("*")]
            public string Filter { get; init; } = "*";
        }

        public override async Task<int> ExecuteAsync([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync("Collecting matching files...", async (spinner) =>
                {
                    var alwaysIgnoredFiles = new HashSet<string>
                    {
                        Manifest.Filename
                    };

                    var matchingFiles = settings.Path
                        .GetFiles(settings.Filter, SearchOption.AllDirectories)
                        .Where(f => !alwaysIgnoredFiles.Contains(f.Name));

                    Logger.LogInformation("Found {Files} files which matched your pattern.", matchingFiles.Count());
                    spinner.Status = $"Calculating checksums for files (0/{matchingFiles.Count()})";

                    var fileIndex = 0;
                    var fileHashTasks = matchingFiles.Select(async file =>
                    {
                        using (var fs = await FilesystemHelpers.GetFileReadStreamAsync(file, TimeSpan.FromMilliseconds(500), this.CancellationToken))
                        {
                            var checksum = await ChecksumExtensions.GetHashStringAsync(Checksum.Get(settings.HashAlgorithm), fs, this.CancellationToken);

                            spinner.Status = $"Calculating checksums for files ({Interlocked.Increment(ref fileIndex)}/{matchingFiles.Count()})";

                            return new Manifest.File
                            {
                                FileName = Path.GetRelativePath(settings.Path.FullName, file.FullName),
                                Checksum = checksum
                            };
                        }
                    });

                    var files = await Task.WhenAll(fileHashTasks);
                    Logger.LogInformation("Checksum calculation completed.");
                    spinner.Status = "Saving manifest file...";

                    var manifest = new Manifest
                    {
                        Version = settings.Version,
                        Files = files.Distinct().OrderBy(f => f.FileName),
                    };

                    Logger.LogInformation("New manifest generated with checksum {Checksum}", manifest.GetChecksum());

                    using (var fs = await FilesystemHelpers.GetFileWriteStreamAsync(new FileInfo(Path.Combine(settings.Path.FullName, Manifest.Filename)), TimeSpan.FromMilliseconds(500), this.CancellationToken))
                    {
                        fs.SetLength(0);

                        Logger.LogTrace("Writing manifest to {Path}", Path.Combine(settings.Path.FullName, Manifest.Filename));
                        await JsonSerializer.SerializeAsync(fs, manifest, new JsonSerializerOptions(JsonSerializerDefaults.Web)
                        {
                            WriteIndented = true
                        });
                    }

                    Logger.LogInformation("Manifest file generated, use 'version import \"{Path}\"' to import this version.", settings.Path);
                });

            return 0;
        }
    }
}
