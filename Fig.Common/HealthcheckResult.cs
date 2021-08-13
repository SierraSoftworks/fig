namespace Fig.Common
{
    /// <summary>
    /// A result from a healthcheck's execution, providing information on whether the
    /// monitored service is healthy or not.
    /// </summary>
    public record HealthcheckResult
    {
        /// <summary>
        /// Gets a value indicating whether the healthcheck represents a healthy result or not.
        /// </summary>
        public bool IsHealthy { get; init; }

        /// <summary>
        /// Gets an optional message providing additional information on the health state, if relevant.
        /// </summary>
        public string? Message { get; init; }
    }
}
