namespace Fig.Agent
{
    using Fig.Config;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using System.Threading.Tasks;
    using Spectre.Console.Cli;
    using Fig.Agent.Infrastructure;
    using System.Threading;
    using System;
    using Spectre.Console;
    using Fig.Common;
    using Fig.Common.Exceptions;

    class Program
    {
        static async Task<int> Main(string[] args)
        {
            var services = new ServiceCollection();
            RegisterServices(services);

            var app = new CommandApp(new TypeRegistrar(services));
            app.Configure(config =>
            {
                config.PropagateExceptions();

                config.AddCommand<Commands.RunCommand>("run")
                    .WithDescription("Runs the Fig agent to continuously synchronize new configuration onto the local machine.")
                    .WithExample(new[] { "run" });

                config.AddCommand<Commands.InitCommand>("init");

                config.AddCommand<Commands.PruneCommand>("prune");

                config.AddCommand<Commands.WatchVersionCommand>("watch")
                    .WithExample(new[] { "watch" });

                config.AddBranch("version", version =>
                {
                    version.SetDescription("Manage the current configuration version on this machine.");

                    version.AddCommand<Commands.VerifyVersionCommand>("verify")
                        .WithExample(new[] { "version", "verify", "1.6.2-beta.1" });

                    version.AddCommand<Commands.SetVersionCommand>("set")
                        .WithDescription("Sets the configuration version that should be used by the local machine.")
                        .WithExample(new[] { "version", "set", "1.6.2-beta.1" });

                    version.AddCommand<Commands.ListVersionsCommand>("list")
                        .WithAlias("ls")
                        .WithDescription("Lists the configuration versions available on the local machine")
                        .WithExample(new[] { "version", "list" });

                    version.AddCommand<Commands.ImportVersionCommand>("import")
                        .WithDescription("Import a specific configuration version for use on the local machine.")
                        .WithExample(new[] { "version", "import", "./v1.6.2-beta.3/" });

                    version.AddCommand<Commands.ExportVersionCommand>("export")
                        .WithExample(new[] { "version", "export", "1.6.2-beta.3" });

                    version.AddCommand<Commands.RemoveVersionCommand>("remove")
                        .WithAlias("rm")
                        .WithDescription("Remove a specific configuration version from the local machine.")
                        .WithExample(new[] { "version", "remove", "v1.6.2-alpha.1" });
                });

                config.AddBranch("manifest", manifest =>
                {
                    manifest.SetDescription("Manage your Fig manifest files");

                    manifest.AddCommand<Commands.VerifyManifestCommand>("verify")
                        .WithExample(new[] { "manifest", "verify" });

                    manifest.AddCommand<Commands.BuildManifestCommand>("build")
                        .WithExample(new[] { "manifest", "build" })
                        .WithExample(new[] { "manifest", "build", "v1.6.2-beta.2", "--filter", "*.json" });
                });

                config.AddBranch("file", file =>
                {
                    file.SetDescription("Tools for working with files.");

                    file.AddCommand<Commands.HashFileCommand>("hash")
                        .WithExample(new[] { "file", "hash", "example.json" })
                        .WithExample(new[] { "file", "hash", "config.ini", "--hash", "sha256" });

                    file.AddCommand<Commands.CatFileCommand>("cat")
                        .WithAlias("read")
                        .WithExample(new[] { "file", "cat", "config.ini" })
                        .WithExample(new[] { "file", "cat", "config.ini", "--version", "1.6.2-beta.1" });
                });
            });

            try
            {
                return await app.RunAsync(args);
            }
            catch (TaskCanceledException)
            {
                return 1;
            }
            catch (FigException ex)
            {
                AnsiConsole.MarkupLine("[bold red]ERROR[/]: {0}", Markup.Escape(ex.Message));
                return 1;
            }
            catch (Exception ex)
            {
                AnsiConsole.WriteException(ex);
                return 1;
            }
        }

        static void RegisterServices(ServiceCollection services)
        {
            services.AddLogging(config => config.ClearProviders().AddProvider(new AnsiConsoleLoggerProvider()));
            services
                .AddSingleton<ConfigurationImporter>()
                .AddSingleton<CancelSource>();
        }
    }
}
