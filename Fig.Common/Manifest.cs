namespace Fig.Common
{
    using Fig.Common.Checksums;
    using Fig.Common.Exceptions;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// A manifest describes the various configuration files which are expected to be present, as well
    /// as their expected checksums. Configuration updates are accompanied by a new manifest version.
    /// </summary>
    public sealed record Manifest
    {
        /// <summary>
        /// Gets the filename used to hold a Fig manifest.
        /// </summary>
        public const string Filename = "fig.manifest.json";

        /// <summary>
        /// Gets or sets the version number associated with this manifest version.
        /// </summary>
        [JsonPropertyName("version")]
        public string? Version { get; init; }

        /// <summary>
        /// Gets or sets the list of files which should be present in this manifest.
        /// </summary>
        [JsonPropertyName("files")]
        public IEnumerable<Manifest.File> Files { get; init; } = Enumerable.Empty<Manifest.File>();

        /// <summary>
        /// Reads a manifest from the provided file.
        /// </summary>
        /// <param name="manifestFile">The file to read the manifest from.</param>
        /// <param name="filesystemPollingInterval">The time period between successive attempts to read from the filesystem.</param>
        /// <param name="cancellationToken">A cancellation token which can be used to abort the operation.</param>
        /// <returns></returns>
        public static async Task<Manifest> ReadAsync(FileInfo manifestFile, TimeSpan filesystemPollingInterval, CancellationToken cancellationToken)
        {
            using (var fs = await FilesystemHelpers.GetFileReadStreamAsync(manifestFile, filesystemPollingInterval, cancellationToken))
            {
                var manifest = await JsonSerializer.DeserializeAsync<Manifest>(fs, cancellationToken: cancellationToken);

                if (manifest is null)
                {
                    throw new FigException("The manifest file could not be parsed correctly.");
                }

                return manifest;
            }
        }

        /// <summary>
        /// Reads a manifest from the provided file.
        /// </summary>
        /// <param name="directory">The directory containing the manifest file to read.</param>
        /// <param name="filesystemPollingInterval">The time period between successive attempts to read from the filesystem.</param>
        /// <param name="cancellationToken">A cancellation token which can be used to abort the operation.</param>
        /// <returns></returns>
        public static async Task<Manifest> ReadAsync(DirectoryInfo directory, TimeSpan filesystemPollingInterval, CancellationToken cancellationToken)
        {
            return await ReadAsync(new FileInfo(Path.Combine(directory.FullName, Filename)), filesystemPollingInterval, cancellationToken);
        }

        /// <summary>
        /// Calculates the checksum for this manifest based on the list of files which are expected
        /// to be present and their expected checksums.
        /// </summary>
        /// <param name="checksumKind">The preferred Checksum kind to use, will fallback to the default if not provided or not found.</param>
        /// <returns>The computed checksum for this manifest.</returns>
        public string GetChecksum(string? checksumKind = null)
        {
            var fileList = string.Join("\n", Files.OrderBy(f => f.FileName).Select(f => $"{f.FileName} [{f.Checksum}]"));
            return Checksum.Get(checksumKind).GetHashString(fileList);
        }

        /// <summary>
        /// Validates that the manifest contains a suitable list of files with no conflicts and throws
        /// an exception if a problem is discovered.
        /// </summary>
        /// <exception cref="Exceptions.FigException">Thrown if an issue is discovered with the manifest.</exception>
        public void Validate()
        {
            var conflictingFiles = this.Files.GroupBy(f => f.FileName).Where(g => g.Count() > 1).Select(g => g.Key);
            if (conflictingFiles.Any())
            {
                throw new Exceptions.FigException($"The manifest contains multiple files: {string.Join(",", conflictingFiles)}");
            }

            foreach (var file in this.Files)
            {
                file.Validate();
            }
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return string.Join("\n", Files.Select(f => $"{f.FileName} [{f.Checksum}]"));
        }

        /// <summary>
        /// Describes the files which are included in this manifest version.
        /// </summary>
        public sealed record File
        {
            /// <summary>
            /// Gets or sets the checksum associated with this file.
            /// </summary>
            [JsonPropertyName("checksum")]
            public string? Checksum { get; init; }

            /// <summary>
            /// Gets or sets the name of the file which will be written on disk.
            /// </summary>
            [JsonPropertyName("fileName")]
            public string? FileName { get; init; }

            /// <summary>
            /// Determines whether the file entry is valid or not.
            /// </summary>
            public void Validate()
            {
                if(this.FileName is null)
                {
                    // The file cannot be valid if it doesn't have a filename
                    throw new FigMissingFieldException("fileName");
                }

                if (this.Checksum is null)
                {
                    throw new FigMissingFieldException("checksum");
                }

                if (this.FileName.Contains(".."))
                {
                    throw new FigException("The manifest contains one or more file entries which use '..' to access relative file paths in an unsafe manner.");
                }
            }
        }
    }
}
