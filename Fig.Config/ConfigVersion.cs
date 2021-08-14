namespace Fig.Config
{
    using Fig.Common;
    using Fig.Common.Checksums;
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides access to the files which make up a specific configuration version.
    /// </summary>
    public sealed class ConfigVersion
    {
        private readonly DataDirectory dataDirectory;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigVersion"/> class.
        /// </summary>
        /// <param name="dataDirectory">The directory which contains the configuration data files used by Fig.</param>
        /// <param name="manifest">The manifest which corresponds to the version which should be used.</param>
        public ConfigVersion(DataDirectory dataDirectory, Manifest manifest)
        {
            this.dataDirectory = dataDirectory;
            this.Manifest = manifest;
        }

        /// <summary>
        /// Gets the version number associated with the manifest for this config version.
        /// </summary>
        public string Version => this.Manifest.Version!;

        /// <summary>
        /// Gets the manifest which represents this config version.
        /// </summary>
        public Manifest Manifest { get; }

        /// <summary>
        /// Gets the stream which can be used to read a configuration file based on its name.
        /// </summary>
        /// <param name="fileName">The name of the configuration file which should be retrieved.</param>
        /// <param name="cancellationToken">A cancellation token which can be used to abort this operation.</param>
        /// <returns>A stream which can be used to read the file.</returns>
        public async Task<Stream> GetFileReadStreamAsync(string fileName, CancellationToken cancellationToken)
        {
            var fileEntry = this.Manifest.Files.FirstOrDefault(f => string.Equals(f.FileName, fileName, StringComparison.Ordinal));

            if (fileEntry?.Checksum is null)
            {
                throw new FileNotFoundException("The file was not present in the configuration manifest and could not be loaded.", fileName);
            }

            return await this.dataDirectory.GetFileReadStreamAsync(fileEntry.Checksum, cancellationToken);
        }

        /// <summary>
        /// Verifies that all expected files are present and that their on-disk checksums match
        /// those provided in the manifest.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token which can be used to abort this operation.</param>
        /// <returns>A task which represents the asynchronous operation.</returns>
        public async Task VerifyAsync(CancellationToken cancellationToken)
        {
            await Task.WhenAll(this.Manifest.Files.Select(async f =>
            {
                f.Validate();

                using var fs = await this.GetFileReadStreamAsync(f.FileName!, cancellationToken);

                var trueChecksum = await Checksum.Get(f.Checksum).GetHashStringAsync(fs, cancellationToken);
                if (!string.Equals(f.Checksum, trueChecksum, StringComparison.Ordinal))
                {
                    throw new Common.Exceptions.FigWrongChecksumException(f.FileName!, f.Checksum!, trueChecksum);
                }
            }));
        }
    }
}
