namespace Fig.Agent.Commands
{
    using Fig.Agent.Infrastructure;
    using Microsoft.Extensions.Logging;
    using Spectre.Console.Cli;
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    internal class RunCommand : AsyncCommand<RunCommand.Settings>
    {
        protected CancellationToken CancellationToken { get; }

        public RunCommand(CancelSource cancelSource)
        {
            this.CancellationToken = cancelSource.Token;
        }

        public class Settings : CommonSettings
        {
            
        }

        public override Task<int> ExecuteAsync([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            return Task.FromResult(1);
        }
    }
}
