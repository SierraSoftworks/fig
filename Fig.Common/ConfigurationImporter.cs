namespace Fig.Common
{
    using Fig.Common.Checksums;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// A configuration builder is responsible for checking that a configuration directory matches its
    /// manifest file.
    /// </summary>
    public class ConfigurationImporter
    {
        /// <summary>
        /// Gets the logger used to report information about the operations taking place.
        /// </summary>
        protected ILogger<ConfigurationImporter> Logger { get; }

        /// <summary>
        /// Creates a new instance of the <see cref="ConfigurationImporter"/> class.
        /// </summary>
        public ConfigurationImporter(ILogger<ConfigurationImporter> logger)
        {
            Logger = logger;
        }

        /// <summary>
        /// Validates that a configuration directory matches a specific manifest.
        /// </summary>
        /// <param name="manifest">The manfiest to validate files against.</param>
        /// <param name="configDirectory">The directory containing the configuration to validate.</param>
        /// <param name="filesystemPollingInterval">The interval between successive attempts to read from the filesystem.</param>
        /// <param name="cancellationToken">A cancellation token which can be used to abort the operation.</param>
        /// <returns>A task representing the async operation.</returns>
        public async Task ValidateAsync(Manifest manifest, DirectoryInfo configDirectory, TimeSpan filesystemPollingInterval, CancellationToken cancellationToken)
        {
            var hashes = await this.GetTrueFileHashesAsync(manifest, configDirectory, filesystemPollingInterval, cancellationToken);
            if (hashes.Any(f => !string.Equals(f.File.Checksum, f.TrueChecksum, StringComparison.Ordinal)))
            {
                throw new Exceptions.FigException("One or more files had checksums which did not match those present in the manifest.");
            }
        }

        /// <summary>
        /// Gathers the true hashes for each file listed in the manifest.
        /// /summary>
        /// <param name="manifest">The manfiest to validate files against.</param>
        /// <param name="configDirectory">The directory containing the configuration to validate.</param>
        /// <param name="filesystemPollingInterval">The interval between successive attempts to read from the filesystem.</param>
        /// <param name="cancellationToken">A cancellation token which can be used to abort the operation.</param>
        /// <returns>A task representing the async operation.</returns>
        public async Task<IEnumerable<(Manifest.File File, string TrueChecksum)>> GetTrueFileHashesAsync(Manifest manifest, DirectoryInfo configDirectory, TimeSpan filesystemPollingInterval, CancellationToken cancellationToken)
        {
            var validationTasks = manifest.Files.Where(f => f?.FileName is string).Select(async file =>
            {
                Logger.LogDebug("Opening file {File} for checksum validation", file.FileName);
                using (var fs = await FilesystemHelpers.GetFileReadStreamAsync(new FileInfo(Path.Combine(configDirectory.FullName, file.FileName!)), filesystemPollingInterval, cancellationToken))
                {
                    Logger.LogDebug("Computing checksum for file {File}", file.FileName);
                    var trueChecksum = await Checksum.Get(file.Checksum).GetHashStringAsync(fs, cancellationToken);
                    Logger.LogTrace("Checksum for file {File} is {Checksum}", file.FileName, trueChecksum);

                    return (File: file, TrueChecksum: trueChecksum);
                }
            });

            return await Task.WhenAll(validationTasks);
        }

        /// <summary>
        /// Performs the import of a specific manifest and its associated files from the source directory into the data directory.
        /// </summary>
        /// <param name="manifest">The manifest which lists the files to be imported.</param>
        /// <param name="sourceDirectory">The directory which contains the files to be imported.</param>
        /// <param name="dataDirectory">The data directory into which the files should be imported.</param>
        /// <param name="cancellationToken">The cancellation token wihch can be used to abort this operation.</param>
        /// <returns>A task which represents the asynchronous operation.</returns>
        public async Task ImportAsync(Manifest manifest, DirectoryInfo sourceDirectory, DataDirectory dataDirectory, CancellationToken cancellationToken)
        {
            await Task.WhenAll(manifest.Files.Select(async file =>
            {
                file.Validate();

                var sourceFile = new FileInfo(Path.Combine(sourceDirectory.FullName, file.FileName!));
                using var source = sourceFile.OpenRead();
                using var target = await dataDirectory.GetFileWriteStreamAsync(file.Checksum!, cancellationToken).ConfigureAwait(false);

                await source.CopyToAsync(target, cancellationToken).ConfigureAwait(false);
            }));

            await dataDirectory.StoreManifestAsync(manifest, cancellationToken);
        }
    }
}
