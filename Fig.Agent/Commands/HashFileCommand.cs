namespace Fig.Agent.Commands
{
    using Fig.Agent.Infrastructure;
    using Fig.Common.Checksums;
    using Microsoft.Extensions.Logging;
    using Spectre.Console;
    using Spectre.Console.Cli;
    using System;
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    [Description("Hashes a specific file and prints the checksum to stdout.")]
    internal class HashFileCommand : AsyncCommand<HashFileCommand.Settings>
    {

        protected CancellationToken CancellationToken { get; }

        protected ILogger<HashFileCommand> Logger { get; }

        public HashFileCommand(ILogger<HashFileCommand> logger, CancelSource cancelSource)
        {
            this.CancellationToken = cancelSource.Token;
            Logger = logger;
        }

        public class Settings : CommandSettings
        {
            /// <summary>
            /// Gets or sets the human readable version associated with the manifest.
            /// </summary>
            [CommandArgument(0, "<FILE>")]
            [Description("The path to the file which should be hashed.")]
            [TypeConverter(typeof(FileInfoTypeConverter))]
            public FileInfo? File { get; init; }

            /// <summary>
            /// Gets or sets the hash algorithm used to generate checksums for the files.
            /// </summary>
            [CommandOption("--hash <ALGORITHM>")]
            [Description("The identifier for the hash function which should be used to generate the hash.")]
            [DefaultValue(Checksum.DefaultAlgorithm)]
            public string HashAlgorithm { get; init; } = Checksum.DefaultAlgorithm;
        }

        public override async Task<int> ExecuteAsync([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            if (!(settings.File?.Exists ?? false))
            {
                this.Logger.LogError("The file {File} does not exist.", settings.File?.FullName);
                return 1;
            }

            await AnsiConsole.Status()
                .StartAsync("Calculating checksum...", async (spinner) =>
                {
                    using (var file = settings.File.OpenRead())
                    {
                        var checksum = await Checksum.Get(settings.HashAlgorithm).GetHashStringAsync(file, this.CancellationToken);

                        AnsiConsole.MarkupLine($"Checksum: [bold yellow]{checksum}[/]");
                    }
                });

            

            return 0;
        }
    }
}
