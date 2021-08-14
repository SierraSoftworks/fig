namespace Fig.Agent.Infrastructure
{
    using Fig.Common;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// A reporter implementation which allows the active reporter to be switched at runtime.
    /// </summary>
    public class ReplaceableHealthcheckReporter : IHealthcheckReporter
    {
        private IHealthcheckReporter? reporter = null;

        /// <summary>
        /// Sets the reporter which should be used.
        /// </summary>
        /// <param name="reporter"></param>
        public void SetReporter(IHealthcheckReporter reporter)
        {
            this.reporter = reporter;
        }

        /// <inheritdoc/>
        public Task ReportAsync(Healthcheck healthcheck, HealthcheckResult result)
        {
            return this.reporter?.ReportAsync(healthcheck, result) ?? Task.CompletedTask;
        }
    }
}
