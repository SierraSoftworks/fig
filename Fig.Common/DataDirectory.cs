namespace Fig.Common
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides methods for interacting with the contents of a Fig data directory.
    /// </summary>
    public sealed class DataDirectory
    {
        private readonly TimeSpan filesystemPollingInterval;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataDirectory"/> class.
        /// </summary>
        /// <param name="path">The directory containing the Fig data files and configuration.</param>
        public DataDirectory(DirectoryInfo path, TimeSpan filesystemPollingInterval)
        {
            Folder = path;
            VersionLog = new VersionLog(path);
            
            this.filesystemPollingInterval = filesystemPollingInterval;
        }

        /// <summary>
        /// Gets the directory that is being used to store Fig data and configuration files.
        /// </summary>
        public DirectoryInfo Folder { get; }

        /// <summary>
        /// Gets the version log associated with this data directory.
        /// </summary>
        public VersionLog VersionLog { get; }

        /// <summary>
        /// Initializes the data directory by creating any necessary elements and populating them with
        /// valid initial states.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token which can be used to abort this operation.</param>
        /// <returns>A task representing the async operation.</returns>
        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            if (!this.ManifestsDirectory.Exists)
            {
                this.ManifestsDirectory.Create();
            }

            if (!this.FileCacheDirectory.Exists)
            {
                this.FileCacheDirectory.Create();
            }

            if (!this.VersionLog.FilePath.Exists)
            {
                // Add an empty default manifest
                var initialManifest = new Manifest { Version = "initial" };
                await this.StoreManifestAsync(initialManifest, cancellationToken);
                await this.VersionLog.AddVersionAsync(new VersionLogEntry(initialManifest.Version, initialManifest.GetChecksum()), cancellationToken);
            }
        }

        /// <summary>
        /// Gets a manifest based on its version, if present locally.
        /// </summary>
        /// <param name="version">The version of the manifest which should be retrieved.</param>
        /// <param name="cancellationToken">A cancellation token which can be used to abort the operation.</param>
        /// <returns>The manifest which was present in this data directory under the given version.</returns>
        public async Task<Manifest> GetManifestAsync(string version, CancellationToken cancellationToken)
        {
            if (!this.ManifestsDirectory.Exists)
            {
                throw new Exceptions.FigNotInitializedException();
            }

            try
            {
                return await Manifest.ReadAsync(new FileInfo(Path.Combine(this.ManifestsDirectory.FullName, $"{ConfigVersion.ToSafeName(version)}.json")), this.filesystemPollingInterval, cancellationToken);
            }
            catch (FileNotFoundException)
            {
                throw new Exceptions.FigVersionNotFoundException(version);
            }
        }

        /// <summary>
        /// Gets the manifests which are currently present in this data directory.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token which can be used to abort the operation.</param>
        /// <returns>A list of the manifests in this data directory.</returns>
        public async IAsyncEnumerable<Manifest> GetManifestsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (!this.ManifestsDirectory.Exists)
            {
                throw new Exceptions.FigNotInitializedException();
            }

            foreach (var file in this.ManifestsDirectory.GetFiles("*.json"))
            {
                yield return await Manifest.ReadAsync(file, this.filesystemPollingInterval, cancellationToken);
            }
        }

        /// <summary>
        /// Stores a manifest in the data directory, overwriting an existing entry if it is present.
        /// </summary>
        /// <param name="manifest">The manifest which should be stored.</param>
        /// <param name="cancellationToken">A cancellation token which can be used to abort the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task StoreManifestAsync(Manifest manifest, CancellationToken cancellationToken)
        {
            using var fs = await FilesystemHelpers.GetFileWriteStreamAsync(new FileInfo(Path.Combine(this.ManifestsDirectory.FullName, $"{ConfigVersion.ToSafeName(manifest.Version!)}.json")), this.filesystemPollingInterval, cancellationToken);

            // Truncate the file if it has existing content to avoid corrupt JSON when overwriting.
            fs.SetLength(0);

            await JsonSerializer.SerializeAsync(fs, manifest, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Removes a manifest from this data directory.
        /// </summary>
        /// <param name="version">The version of the manifest to remove from the data directory.</param>
        /// <param name="cancellationToken">A cancellation token which can be used to abort the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task RemoveManifestAsync(string version, CancellationToken cancellationToken)
        {
            var file = new FileInfo(Path.Combine(this.ManifestsDirectory.FullName, $"{ConfigVersion.ToSafeName(version)}.json"));

            if (!file.Exists)
            {
                return;
            }

            // Make this behave like an async method so that we can change the API safely in future.
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            file.Delete();
        }

        /// <summary>
        /// Gets a read stream for a file based on its file hash.
        /// </summary>
        /// <param name="fileHash">The hash of the file to retrieve.</param>
        /// <param name="cancellationToken">A cancellation token which can be used to terminate the operation.</param>
        /// <returns>A stream which allows reading from the file.</returns>
        public async Task<Stream> GetFileReadStreamAsync(string fileHash, CancellationToken cancellationToken)
        {
            return await FilesystemHelpers.GetFileReadStreamAsync(new FileInfo(Path.Combine(this.FileCacheDirectory.FullName, fileHash)), this.filesystemPollingInterval, cancellationToken);
        }

        /// <summary>
        /// Gets a write stream for a file based on its file hash.
        /// </summary>
        /// <param name="fileHash">The hash of the file to retrieve.</param>
        /// <param name="cancellationToken">A cancellation token which can be used to terminate the operation.</param>
        /// <returns>A stream which allows writing to the file.</returns>
        public async Task<Stream> GetFileWriteStreamAsync(string fileHash, CancellationToken cancellationToken)
        {
            return await FilesystemHelpers.GetFileWriteStreamAsync(new FileInfo(Path.Combine(this.FileCacheDirectory.FullName, fileHash)), this.filesystemPollingInterval, cancellationToken);
        }

        /// <summary>
        /// Prunes the cache by removing any files which are not referenced by known manifests.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token which can be used to terminate the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task PruneCacheAsync(CancellationToken cancellationToken)
        {
            var referencedFiles = new HashSet<string>();
            await foreach (var manifest in this.GetManifestsAsync(cancellationToken))
            {
                foreach (var file in manifest.Files)
                {
                    referencedFiles.Add(file.Checksum!);
                }
            }

            foreach (var file in this.FileCacheDirectory.GetFiles().Where(f => !referencedFiles.Contains(f.Name)))
            {
                file.Delete();
            }
        }

        /// <summary>
        /// Gets the directory which contains manifest files.
        /// </summary>
        private DirectoryInfo ManifestsDirectory => new DirectoryInfo(Path.Combine(Folder.FullName, "manifests"));

        /// <summary>
        /// Gets the directory which contains cached files (identified uniquely by their hashes).
        /// </summary>
        private DirectoryInfo FileCacheDirectory => new DirectoryInfo(Path.Combine(Folder.FullName, "cache"));
    }
}
