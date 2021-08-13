namespace Fig.Agent.Commands
{
    using Fig.Agent.Infrastructure;
    using Fig.Common;
    using Fig.Config;
    using Microsoft.Extensions.Logging;
    using Spectre.Console;
    using Spectre.Console.Cli;
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    internal class SetVersionCommand : AsyncCommand<SetVersionCommand.Settings>
    {
        protected readonly CancellationToken cancellationToken;
        protected readonly ILogger<SetVersionCommand> logger;

        public SetVersionCommand(ILogger<SetVersionCommand> logger, CancelSource cancelSource)
        {
            this.cancellationToken = cancelSource.Token;
            this.logger = logger;
        }

        public class Settings : CommonSettings
        {
            /// <summary>
            /// Gets or sets the human readable version associated with the manifest.
            /// </summary>
            [CommandArgument(0, "<VERSION>")]
            [Description("The configuration version to switch to.")]
            public string? Version { get; init; }
        }

        public override async Task<int> ExecuteAsync([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            if (settings.Version is null)
            {
                logger.LogError("You have not specified a version to switch to.");
                return 1;
            }

            return await AnsiConsole.Status()
                .StartAsync<int>($"Validating version {settings.Version}...", async spinner =>
                {
                    var dataDirectory = settings.GetDataDirectory();
                    var client = settings.GetConfigClient();

                    var targetVersion = await client.GetVersionAsync(settings.Version!, cancellationToken);
                    if (targetVersion is null)
                    {
                        logger.LogError("The requested version could not be found, check that it has been imported.");
                        return 1;
                    }

                    await targetVersion.VerifyAsync(cancellationToken);

                    spinner.Status = "Updating version log...";
                    await dataDirectory.VersionLog.AddVersionAsync(new VersionLogEntry(settings.Version, targetVersion.Manifest.GetChecksum()), cancellationToken);

                    logger.LogInformation("Configuration version updated to {Version}", settings.Version);

                    return 0;
                });
        }
    }
}
