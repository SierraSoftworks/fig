namespace Fig.Agent.Commands
{
    using Fig.Agent.Infrastructure;
    using Fig.Config;
    using Microsoft.Extensions.Logging;
    using Spectre.Console;
    using Spectre.Console.Cli;
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    internal class RemoveVersionCommand : AsyncCommand<RemoveVersionCommand.Settings>
    {
        private readonly ILogger<RemoveVersionCommand> logger;

        protected readonly CancellationToken cancellationToken;

        public RemoveVersionCommand(ILogger<RemoveVersionCommand> logger, CancelSource cancelSource)
        {
            this.cancellationToken = cancelSource.Token;
            this.logger = logger;
        }

        public class Settings : CommonSettings
        {
            /// <summary>
            /// Gets or sets the configuration version which should be removed.
            /// </summary>
            [CommandArgument(0, "<VERSION>")]
            [Description("The version which should be removed.")]
            public string? Version { get; init; }

            /// <summary>
            /// Gets or sets a value indicating whether the operator wishes to allow dangerous operations to proceed.
            /// </summary>
            [CommandOption("-f|--force")]
            [Description("Forces the removal of this configuration version, even if that will remove the active configuration or the version doesn't match exactly.")]
            public bool Force { get; init; } = false;
        }

        public override async Task<int> ExecuteAsync([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            if (settings.ConfigurationDirectory is null)
            {
                logger.LogError("You have not specified a valid version to remove.");
                return 1;
            }

            return await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync<int>("Preparing to remove configuration...", async (spinner) =>
                {
                    var dataDirectory = settings.GetDataDirectory();

                    if (!settings.Force)
                    {
                        spinner.Status = "Loading current configuration version...";
                        var currentVersion = await dataDirectory.VersionLog.GetVersionsAsync().LastOrDefaultAsync(cancellationToken);

                        spinner.Status = "Loading manifest targetted for removal...";
                        var manifest = await dataDirectory.GetManifestAsync(settings.Version!, cancellationToken);
                        if (!settings.Force && string.Equals(currentVersion?.Version, manifest.Version, System.StringComparison.Ordinal))
                        {
                            logger.LogError("You have tried to remove the active configuration. Run this command with the '--force' flag if you really wish to proceed.");
                            return 1;
                        }

                        if (!settings.Force && !string.Equals(manifest.Version, settings.Version, System.StringComparison.Ordinal))
                        {
                            logger.LogWarning("The configuration version you specified did not match its manifest version exactly, use 'version remove \"{Version}\"' to remove it.", manifest.Version);
                            return 1;
                        }
                    }

                    spinner.Status = "Removing configuration...";
                    await dataDirectory.RemoveManifestAsync(settings.Version!, cancellationToken);
                    logger.LogInformation("Configuration version {Version} has been removed, run 'prune' to cleanup unused configuration files.", settings.Version);

                    return 0;
                });
        }
    }
}
