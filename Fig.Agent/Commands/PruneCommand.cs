namespace Fig.Agent.Commands
{
    using Fig.Agent.Infrastructure;
    using Microsoft.Extensions.Logging;
    using Spectre.Console;
    using Spectre.Console.Cli;
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    [Description("Removes any cached files which are no longer referenced by stored manifests.")]
    internal class PruneCommand : AsyncCommand<PruneCommand.Settings>
    {
        protected readonly CancellationToken cancellationToken;
        private readonly ILogger<PruneCommand> logger;

        public PruneCommand(ILogger<PruneCommand> logger, CancelSource cancelSource)
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
                .StartAsync("Cleaning data directory...", async spinner =>
                {
                    var dataDirectory = settings.GetDataDirectory();

                    await dataDirectory.PruneCacheAsync(cancellationToken);
                    logger.LogInformation("Unused files have been removed from the data directory.");

                    return 0;
                });
        }
    }
}
