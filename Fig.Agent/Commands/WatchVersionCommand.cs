namespace Fig.Agent.Commands
{
    using Fig.Agent.Infrastructure;
    using Fig.Common;
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

    [Description("Prints the details of newly applied configuration versions to the command line.")]
    internal class WatchVersionCommand : AsyncCommand<WatchVersionCommand.Settings>
    {
        protected readonly CancellationToken cancellationToken;


        protected readonly ILogger<WatchVersionCommand> logger;

        public WatchVersionCommand(ILogger<WatchVersionCommand> logger, CancelSource cancelSource)
        {
            this.cancellationToken = cancelSource.Token;
            this.logger = logger;
        }

        public class Settings : CommonSettings
        {
        }

        public override async Task<int> ExecuteAsync([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            return await AnsiConsole.Status()
                .Spinner(Spinner.Known.Material)
                .StartAsync<int>("Watching the version log...", async (spinner) =>
                {
                    var client = settings.GetConfigClient();

                    await foreach (var versionEntry in client.GetVersionStream(cancellationToken))
                    {
                        logger.LogInformation("Most recent version is {Version} (applied at {TimeStamp} with checksum {Checksum})", versionEntry.Version, versionEntry.Timestamp, versionEntry.ManifestChecksum);
                    }

                    return 0;
                });
        }
    }
}
