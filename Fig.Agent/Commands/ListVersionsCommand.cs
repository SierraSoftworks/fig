namespace Fig.Agent.Commands
{
    using Fig.Agent.Infrastructure;
    using Fig.Common;
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
    using System.Threading;
    using System.Threading.Tasks;

    internal class ListVersionsCommand : AsyncCommand<ListVersionsCommand.Settings>
    {
        protected CancellationToken cancellationToken { get; }

        public ListVersionsCommand(CancelSource cancelSource)
        {
            this.cancellationToken = cancelSource.Token;
        }

        public class Settings : CommonSettings
        {
            
        }

        public override async Task<int> ExecuteAsync([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            var dataDirectory = settings.GetDataDirectory();

            var manifests = await dataDirectory.GetManifestsAsync().ToDictionaryAsync(m => m.Version!, cancellationToken);
            var deployedVersions = new HashSet<string>();

            var table = new Table();
            table.AddColumn("Version");
            table.AddColumn("Applied On");
            table.AddColumn("In Cache");

            await foreach (var version in dataDirectory.VersionLog.GetVersionsAsync(this.cancellationToken))
            {
                deployedVersions.Add(version.Version);

                table.AddRow(
                    new Markup(Markup.Escape(version.Version)),
                    new Markup(Markup.Escape(version.Timestamp.ToString("s"))),
                    new Markup(manifests.ContainsKey(version.Version) ? "[green]Yes[/]" : "[red]No[/]"));
            }

            foreach (var manifest in manifests.Where(m => !deployedVersions.Contains(m.Key)).OrderBy(m => m.Key).Select(m => m.Value))
            {
                table.AddRow(new Markup(Markup.Escape(manifest.Version!)),
                    new Markup("[red bold]never[/]"),
                    new Markup(manifests.ContainsKey(manifest.Version!) ? "[green]Yes[/]" : "[red]No[/]"));
            }

            AnsiConsole.Render(table);

            return 0;
        }
    }
}
