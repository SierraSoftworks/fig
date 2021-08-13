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

    [Description("Initializes your Fig data directory to prepare it for use.")]
    internal class InitCommand : AsyncCommand<InitCommand.Settings>
    {
        protected readonly CancellationToken cancellationToken;
        private readonly ILogger<InitCommand> logger;

        public InitCommand(ILogger<InitCommand> logger, CancelSource cancelSource)
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
                .StartAsync("Initializing directory...", async spinner =>
                {
                    var dataDirectory = settings.GetDataDirectory();

                    await dataDirectory.InitializeAsync(cancellationToken);
                    logger.LogInformation("Initialized data directory.");

                    return 0;
                });
        }
    }
}
