namespace Fig.Agent.Commands
{
    using Fig.Agent.Infrastructure;
    using Fig.Common;
    using Microsoft.Extensions.Logging;
    using Spectre.Console;
    using Spectre.Console.Cli;
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    [Description("Runs a set of healthchecks and prints their results to the command line.")]
    internal class WatchHealthchecksCommand : AsyncCommand<WatchHealthchecksCommand.Settings>
    {
        protected readonly CancellationToken cancellationToken;
        protected readonly HealthcheckRunner healthcheckRunner;
        private readonly IHealthcheckReporter healthcheckReporter;
        protected readonly ILogger<WatchHealthchecksCommand> logger;

        public WatchHealthchecksCommand(HealthcheckRunner healthcheckRunner, IHealthcheckReporter healthcheckReporter, ILogger<WatchHealthchecksCommand> logger, CancelSource cancelSource)
        {
            this.cancellationToken = cancelSource.Token;
            this.healthcheckRunner = healthcheckRunner;
            this.healthcheckReporter = healthcheckReporter;
            this.logger = logger;
        }

        public class Settings : CommonSettings
        {
            /// <summary>
             /// Gets or sets the human readable version associated with the manifest.
             /// </summary>
            [CommandOption("-v|--version <VERSION>")]
            [Description("The version whose healthchecks should be executed.")]
            public string? Version { get; init; }
        }

        public override async Task<int> ExecuteAsync([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            var client = settings.GetConfigClient();

            var version = settings.Version is null ? await client.GetCurrentVersionAsync(cancellationToken) : await client.GetVersionAsync(settings.Version, cancellationToken);
            logger.LogInformation("Loaded configuration version {Version}.", version.Version);

            HealthcheckManifest healthcheckManifest;
            try
            {
                using var fs = await version.GetFileReadStreamAsync(HealthcheckManifest.Filename, cancellationToken);
                healthcheckManifest = await JsonSerializer.DeserializeAsync<HealthcheckManifest>(fs, cancellationToken: cancellationToken);
            }
            catch (FileNotFoundException)
            {
                logger.LogError("No {HealthcheckFile} file was found in config version {Version}.", HealthcheckManifest.Filename, version.Version);
                return 1;
            }

            if (!(healthcheckManifest?.Healthchecks?.Any() ?? false))
            {
                logger.LogError("No healthchecks were defined in the {HealthcheckFile} file.", HealthcheckManifest.Filename);
                return 1;
            }
            
            return await AnsiConsole.Live(new Table())
                .StartAsync(async ctx =>
                {
                    if (healthcheckReporter is ReplaceableHealthcheckReporter replaceableHealthcheckReporter)
                    {
                        replaceableHealthcheckReporter.SetReporter(new ConsoleHealthcheckReporter(ctx));
                    }

                    await healthcheckRunner.RunAsync(healthcheckManifest, cancellationToken);

                    return 0;
                });
        }
    }
}
