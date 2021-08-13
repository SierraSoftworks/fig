namespace Fig.Agent.Infrastructure
{
    using Microsoft.Extensions.Logging;
    using Spectre.Console;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    class AnsiConsoleLogger : ILogger
    {
        private readonly static Dictionary<LogLevel, string> LogLevelLineFormats = new Dictionary<LogLevel, string>
        {
            [LogLevel.Debug] = "[bold grey42]DEBUG[/]|[grey]{0}[/]: {1}",
            [LogLevel.Trace] = "[bold aqua]TRACE[/]|[grey]{0}[/]: {1}",
            [LogLevel.Information] = "[bold green] INFO[/]|[grey]{0}[/]: {1}",
            [LogLevel.Warning] = "[bold orange] WARN[/]|[grey]{0}[/]: {1}",
            [LogLevel.Error] = "[bold red]ERROR[/]|[grey]{0}[/]: {1}",
            [LogLevel.Critical] = "[bold maroon] CRIT[/]|[grey]{0}[/]: {1}"
        };
        private readonly string categoryName;

        public AnsiConsoleLogger(string categoryName)
        {
            this.categoryName = categoryName;
        }

        public IDisposable BeginScope<TState>(TState state) => default!;

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var lineFormat = LogLevelLineFormats.GetValueOrDefault(logLevel) ?? $"[bold magenta]{Markup.Escape(logLevel.ToString())}[/]||[grey]{{0}}[/] {{1}}";

            AnsiConsole.MarkupLine(
                lineFormat,
                Markup.Escape(this.categoryName),
                Markup.Escape(formatter(state, exception)));
        }
    }
}
