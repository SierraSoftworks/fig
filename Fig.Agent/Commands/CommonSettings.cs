namespace Fig.Agent.Commands
{
    using Fig.Common;
    using Fig.Config;
    using Microsoft.Extensions.Options;
    using Spectre.Console.Cli;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Common settings used to derive most Fig agent commands.
    /// </summary>
    internal class CommonSettings : CommandSettings
    {
        /// <summary>
        /// Gets the configuration directory used by Fig.
        /// </summary>
        [CommandOption("--config-path <PATH>")]
        [Description("The path to the configuration directory used by Fig.")]
        public DirectoryInfo ConfigurationDirectory { get; init; } = new DirectoryInfo(Environment.GetEnvironmentVariable("FIG_CONFIG_DIR") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Fig", "Configs"));

        /// <summary>
        /// Gets the polling interval used for filesystem operations.
        /// </summary>
        [CommandOption("--polling-interval <INTERVAL>")]
        [Description("The amount of time between successive attempts to read from the filesystem when deconflicting with other apps.")]
        public TimeSpan PollingInterval { get; init; } = TimeSpan.FromMilliseconds(500);

        /// <summary>
        /// Gets the data directory associated with these command line options.
        /// </summary>
        /// <returns>The data directory that Fig uses to store its configuration files.</returns>
        public DataDirectory GetDataDirectory()
        {
            return new DataDirectory(this.ConfigurationDirectory, this.PollingInterval);
        }

        /// <summary>
        /// Constructs a new <see cref="ConfigClient"/> based on these command line options.
        /// </summary>
        /// <returns>The config client used to retrieve Fig configuration.</returns>
        public ConfigClient GetConfigClient()
        {
            return new ConfigClient(Options.Create(new ConfigOptions
            {
                DataDirectory = this.ConfigurationDirectory
            }));
        }
    }
}
