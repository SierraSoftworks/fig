namespace Fig.Config
{
    using System;
    using System.IO;

    /// <summary>
    /// Options which configure how the <see cref="ConfigClient"/> behaves.
    /// </summary>
    public record ConfigOptions
    {
        /// <summary>
        /// Gets or sets the directory which will contain the configuration files used by the config client.
        /// </summary>
        public DirectoryInfo DataDirectory { get; init; } = new DirectoryInfo(Environment.GetEnvironmentVariable("FIG_CONFIG_DIR") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Fig", "Config"));

        /// <summary>
        /// Gets or sets the amount of time between consecutive polling operations on the filesystem
        /// when attempting to open locked files.
        /// </summary>
        public TimeSpan FilesystemPollingInterval { get; init; } = TimeSpan.FromMilliseconds(500);
    }
}
