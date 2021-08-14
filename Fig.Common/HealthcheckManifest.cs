namespace Fig.Common
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json.Serialization;
    using System.Threading.Tasks;

    /// <summary>
    /// A healthcheck manifest describes how Fig can evaluate the health of the service(s) it is providing
    /// configuration to and is an integral component in how Fig manages configuration rollouts and rollbacks.
    /// </summary>
    public sealed record HealthcheckManifest
    {
        /// <summary>
        /// Gets the filename used to hold a Fig healthchecks manifest.
        /// </summary>
        public const string Filename = "fig.healthchecks.json";

        /// <summary>
        /// Gets the list of healthchecks which should be run to determine whether the configured system is working.
        /// </summary>
        [JsonPropertyName("healthchecks")]
        public IEnumerable<Healthcheck> Healthchecks { get; init; } = Enumerable.Empty<Healthcheck>();
    }
}
