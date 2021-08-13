namespace Fig.Common
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// A healthcheck
    /// </summary>
    public sealed record Healthcheck
    {
        /// <summary>
        /// Gets the ID associated with this healthcheck entry.
        /// </summary>
        [JsonPropertyName("id")]
        public string? ID { get; init; }

        /// <summary>
        /// Gets or sets the kind of healthcheck which should be executed.
        /// </summary>
        [JsonPropertyName("kind")]
        public string? Kind { get; init; }

        /// <summary>
        /// Gets or sets the amount of time which must elapse after a config change before this
        /// healthcheck will be evaluated.
        /// </summary>
        [JsonPropertyName("initialDelay")]
        public TimeSpan InitialDelay { get; init; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Gets or sets the amount of time between successive evaluations of this healthcheck.
        /// </summary>
        [JsonPropertyName("period")]
        public TimeSpan Period { get; init; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Gets or sets the additional configuration options which are provided
        /// for this healthcheck.
        /// </summary>
        [JsonExtensionData]
        public IDictionary<string, JsonElement> AdditionalOptions { get; init; } = new Dictionary<string, JsonElement>();
    }
}
