namespace Fig.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// The healthcheck runner is responsible for running health checks defined in the healthcheck manifest
    /// and reporting their results using a <see cref="IHealthcheckReporter"/>.
    /// </summary>
    public class HealthcheckRunner
    {
        private readonly IEnumerable<HealthcheckerBase> healthcheckKinds;
        private readonly IHealthcheckReporter healthcheckReporter;

        /// <summary>
        /// Initializes a new instance of the <see cref="HealthcheckRunner"/> class.
        /// </summary>
        /// <param name="healthcheckKinds">The kinds of healthcheck which are supported for execution.</param>
        public HealthcheckRunner(IEnumerable<HealthcheckerBase> healthcheckKinds, IHealthcheckReporter healthcheckReporter)
        {
            this.healthcheckKinds = healthcheckKinds;
            this.healthcheckReporter = healthcheckReporter;
        }

        /// <summary>
        /// Runs the healthcheck runner until the <paramref name="cancellationToken"/> is cancelled.
        /// </summary>
        /// <param name="manifest">The healthcheck manifest which should be consulted to determine which healthchecks to run.</param>
        /// <param name="cancellationToken">The cancellation token which determines when to stop running.</param>
        /// <returns>A <see cref="Task"/> representing the async operation.</returns>
        public async Task RunAsync(HealthcheckManifest manifest, CancellationToken cancellationToken)
        {
            // We use this to cancel all the other healthchecks if one fails but the root cancellation token hasn't been cancelled yet.
            using (var cancellationTokenSource = new CancellationTokenSource())
            using (var cancellationRegistration = cancellationToken.Register(() => cancellationTokenSource.Cancel()))
            {
                await Task.WhenAll(manifest.Healthchecks.Select(async healthcheck => await this.RunAsync(healthcheck, cancellationTokenSource.Token)));
            }
        }

        /// <summary>
        /// Runs the healthcheck runner for a single <paramref name="healthcheck"/> until the <paramref name="cancellationToken"/> is cancelled.
        /// </summary>
        /// <param name="healthcheck">The healthcheck to run the probes for.</param>
        /// <param name="cancellationToken">The cancellation token which determines when to stop running.</param>
        /// <returns>A <see cref="Task"/> representing the async operation.</returns>
        public async Task RunAsync(Healthcheck healthcheck, CancellationToken cancellationToken)
        {
            if (healthcheck.Kind is null)
            {
                throw new Exceptions.FigMissingFieldException("kind");
            }

            var healthchecker = this.healthcheckKinds.FirstOrDefault(h => string.Equals(h.Kind, healthcheck.Kind, StringComparison.OrdinalIgnoreCase));
            if (healthchecker is null)
            {
                throw new Exceptions.FigUnrecognizedKindException("healthcheck", healthcheck.Kind, this.healthcheckKinds.Select(h => h.Kind));
            }

            await Task.Delay(healthcheck.InitialDelay, cancellationToken);

            while (!cancellationToken.IsCancellationRequested)
            {
                var nextRun = DateTime.UtcNow + healthcheck.Period;
                var result = await healthchecker.GetHealthAsync(healthcheck);
                await this.healthcheckReporter.ReportAsync(healthcheck, result);

                var now = DateTime.UtcNow;
                if (now < nextRun)
                {
                    await Task.Delay(nextRun - now, cancellationToken);
                }
            }
        }
    }
}
