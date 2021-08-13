namespace Fig.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;

    /// <summary>
    /// A healthcheck is a periodic operation which is used to evaluate the health of the
    /// service(s) that Fig updates configuration for.
    /// </summary>
    public abstract class HealthcheckerBase
    {
        /// <summary>
        /// Gets the healthcheck kind used bu this implementation.
        /// </summary>
        public abstract string Kind { get; }

        /// <summary>
        /// Runs a healthcheck for the provided <paramref name="healthcheck"/> specification.
        /// </summary>
        /// <param name="healthcheck">The healthcheck specification controlling how this healthcheck is executed.</param>
        /// <returns>A <see cref="HealthcheckResult"/> describing the health of the service based on the provided <paramref name="healthcheck"/> spec.</returns>
        public async Task<HealthcheckResult> GetHealthAsync(Healthcheck healthcheck)
        {
            try
            {
                return await this.GetHealthInternalAsync(healthcheck);
            }
            catch (Exception ex)
            {
                return new HealthcheckResult { IsHealthy = false, Message = $"The healthcheck encountered an unhandled exception: {ex.Message}" };
            }
        }

        /// <summary>
        /// Runs a healthcheck for the provided <paramref name="healthcheck"/> specification.
        /// </summary>
        /// <param name="healthcheck">The healthcheck specification controlling how this healthcheck is executed.</param>
        /// <returns>A <see cref="HealthcheckResult"/> describing the health of the service based on the provided <paramref name="healthcheck"/> spec.</returns>
        protected abstract Task<HealthcheckResult> GetHealthInternalAsync(Healthcheck healthcheck);

        /// <summary>
        /// Extracts a typed set of the additional options provided in the <paramref name="healthcheck"/> spec.
        /// </summary>
        /// <typeparam name="T">The type of the options to extract.</typeparam>
        /// <param name="healthcheck">The healthcheck from which to extract the options.</param>
        /// <returns>The extracted options.</returns>
        protected T? GetOptions<T>(Healthcheck healthcheck)
        {
            var serialized = JsonSerializer.Serialize(healthcheck.AdditionalOptions);
            return JsonSerializer.Deserialize<T>(serialized);
        }
    }
}
