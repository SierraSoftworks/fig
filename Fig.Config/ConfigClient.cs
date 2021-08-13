namespace Fig.Config
{
    using Fig.Common;
    using Fig.Common.Checksums;
    using Fig.Common.Exceptions;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// The Fig configuration client which is responsible for loading and monitoring
    /// configuration files for changes.
    /// </summary>
    public class ConfigClient : IDisposable
    {
        private readonly DataDirectory dataDirectory;
        private readonly TimeSpan versionLogPollingInterval;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigClient"/> class.
        /// </summary>
        /// <param name="configOptions">The options controlling the behaviour of this config client.</param>
        public ConfigClient(IOptions<ConfigOptions> configOptions)
        {
            this.dataDirectory = new DataDirectory(configOptions.Value.DataDirectory, configOptions.Value.FilesystemPollingInterval);
            this.versionLogPollingInterval = configOptions.Value.FilesystemPollingInterval;
        }

        /// <summary>
        /// Gets the most recently selected configuration version on the local machine.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token which may be used to abort the operation.</param>
        /// <returns>The current configuration version.</returns>
        public async Task<ConfigVersion> GetCurrentVersionAsync(CancellationToken cancellationToken)
        {
            var currentVersionEntry = await this.dataDirectory.VersionLog.GetVersionsAsync().LastOrDefaultAsync(cancellationToken);
            if (currentVersionEntry is null)
            {
                throw new FigNoVersionSelectedException();
            }

            var currentVersion = await this.GetVersionAsync(currentVersionEntry.Version, cancellationToken);
            
            var trueChecksum = currentVersion.Manifest.GetChecksum(currentVersionEntry.ManifestChecksum);
            if (!string.Equals(trueChecksum, currentVersionEntry.ManifestChecksum, StringComparison.OrdinalIgnoreCase))
            {
                throw new FigWrongChecksumException(Manifest.Filename, currentVersionEntry.ManifestChecksum, trueChecksum);
            }

            return currentVersion;
        }

        /// <summary>
        /// Gets a specific configuration version that is present on the local machine.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token which may be used to abort the operation.</param>
        /// <returns>The specified configuration version.</returns>
        public async Task<ConfigVersion> GetVersionAsync(string version, CancellationToken cancellationToken)
        {
            var manifest = await this.dataDirectory.GetManifestAsync(version, cancellationToken);
            return new ConfigVersion(this.dataDirectory, manifest);
        }

        /// <summary>
        /// Gets a stream of versions which will be updated each time a new configuration is rolled out.
        /// is changed.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token used to close the stream.</param>
        /// <returns>An <see cref="IAsyncEnumerable{VersionLogEntry}"/> which will emit each new version log entry when it is changed.</returns>
        public async IAsyncEnumerable<VersionLogEntry> GetVersionStream([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var lastEmissionUpdate = DateTime.MinValue;

            while (!cancellationToken.IsCancellationRequested)
            {
                var lastUpdated = new FileInfo(this.dataDirectory.VersionLog.FilePath.FullName).LastWriteTimeUtc;
                if (lastUpdated > lastEmissionUpdate)
                {
                    lastEmissionUpdate = lastUpdated;

                    var latestVersion = await this.dataDirectory.VersionLog.GetVersionsAsync(cancellationToken).LastOrDefaultAsync(cancellationToken);
                    if (latestVersion is null)
                    {
                        continue;
                    }

                    yield return latestVersion;
                }

                await Task.Delay(this.versionLogPollingInterval, cancellationToken);
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
