namespace Fig.Agent.Infrastructure
{
    using Fig.Common;
    using Spectre.Console;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class ConsoleHealthcheckReporter : IHealthcheckReporter
    {
        private readonly LiveDisplayContext context;

        private readonly ConcurrentDictionary<string, HealthcheckResult> healthcheckResults = new ConcurrentDictionary<string, HealthcheckResult>();

        public ConsoleHealthcheckReporter(LiveDisplayContext context)
        {
            this.context = context;
        }

        public Task ReportAsync(Healthcheck healthcheck, HealthcheckResult result)
        {
            healthcheckResults[healthcheck.ID!] = result;

            var table = new Table()
                .AddColumns("Healthcheck", "Status");

            foreach (var item in healthcheckResults) 
            {
                var message = item.Value.Message ?? (item.Value.IsHealthy ? "Healthy" : "Unhealthy");
                var color = item.Value.IsHealthy ? Color.Green : Color.Red;

                table.AddRow(new Text(item.Key), new Text(message, new Style(color)));
            }

            context?.UpdateTarget(table);

            return Task.CompletedTask;
        }
    }
}
