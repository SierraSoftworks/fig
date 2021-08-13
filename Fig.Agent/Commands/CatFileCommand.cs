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

    [Description("Prints the contents of a specific file to the stdout stream.")]
    internal class CatFileCommand : AsyncCommand<CatFileCommand.Settings>
    {

        protected readonly CancellationToken cancellationToken;

        protected readonly ILogger<CatFileCommand> logger;

        public CatFileCommand(ILogger<CatFileCommand> logger, CancelSource cancelSource)
        {
            this.cancellationToken = cancelSource.Token;
            this.logger = logger;
        }

        public class Settings : CommonSettings
        {
            /// <summary>
            /// Gets or sets the human readable version associated with the manifest.
            /// </summary>
            [CommandArgument(0, "<FILE>")]
            [Description("The path to the file which should be read.")]
            public string? File { get; init; }

            /// <summary>
            /// Gets or sets the human readable version associated with the manifest.
            /// </summary>
            [CommandOption("-v|--version <VERSION>")]
            [Description("The configuration version from which to read the file.")]
            public string? Version { get; init; }
        }

        public override async Task<int> ExecuteAsync([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.File))
            {
                logger.LogError("You have not specified a valid file name.");
                return 1;
            }

            var client = settings.GetConfigClient();

            var configVersion = settings.Version is null
                ? await client.GetCurrentVersionAsync(cancellationToken)
                : await client.GetVersionAsync(settings.Version, cancellationToken);

            using var fs = await configVersion.GetFileReadStreamAsync(settings.File!, cancellationToken);
            using var stdout = Console.OpenStandardOutput();

            await fs.CopyToAsync(stdout);

            return 0;
        }
    }
}
