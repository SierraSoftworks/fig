namespace Fig.Agent.Infrastructure
{
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class AnsiConsoleLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentDictionary<string, ILogger> loggers = new();

        public ILogger CreateLogger(string categoryName) => loggers.GetOrAdd(categoryName, name => new AnsiConsoleLogger(name));

        public void Dispose()
        {
            loggers.Clear();
        }
    }
}
