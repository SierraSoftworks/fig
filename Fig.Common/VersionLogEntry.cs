namespace Fig.Common
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// An entry which appears in the version log file.
    /// </summary>
    public sealed record VersionLogEntry
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VersionLogEntry"/> class.
        /// </summary>
        /// <param name="version">The version which has been applied.</param>
        /// <param name="manifestChecksum">The checksum of the manifest which has been applied.</param>
        public VersionLogEntry(string version, string manifestChecksum)
        {
            this.Version = version;
            this.ManifestChecksum = manifestChecksum;
        }

        /// <summary>
        /// Gets the configuration version which was applied.
        /// </summary>
        [JsonPropertyName("version")]
        public string Version { get; init; }

        /// <summary>
        /// Gets the checksum of the manifest which was applied.
        /// </summary>
        [JsonPropertyName("manifestChecksum")]
        public string ManifestChecksum { get; init; }

        /// <summary>
        /// Gets the timestamp representing when this configuration was applied.
        /// </summary>
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    }
}
