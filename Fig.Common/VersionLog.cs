namespace Fig.Common
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides functionality for interacting with the <c>version.jsonl</c> file that
    /// keeps track of which configuration versions were applied and at what time.
    /// </summary>
    /// <remarks>
    /// This file is an append-only log of the configuration versions which have been
    /// applied on the local machine. It serves two key purposes, the first being a
    /// source of truth for transactional consistency when applying configuration
    /// updates which consist of multiple files, and the second being a log which can
    /// be used by operators to understand the changes that have taken place on a
    /// machine.
    /// </remarks>
    public sealed record VersionLog
    {
        private readonly TimeSpan filesystemPollingInterval;

        /// <summary>
        /// Gets the filename used to hold the version log file.
        /// </summary>
        public const string Filename = "versions.jsonl";

        /// <summary>
        /// Initializes a new instance of the <see cref="VersionLog"/> class.
        /// </summary>
        /// <param name="configPath">The path to the directory holding Fig's configuration files.</param>
        public VersionLog(DirectoryInfo configPath) : this(new FileInfo(Path.Combine(configPath.FullName, Filename)), TimeSpan.FromMilliseconds(500))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VersionLog"/> class.
        /// </summary>
        /// <param name="configPath">The path to the directory holding Fig's configuration files.</param>
        public VersionLog(DirectoryInfo configPath, TimeSpan filesystemPollingInterval) : this(new FileInfo(Path.Combine(configPath.FullName, Filename)), filesystemPollingInterval)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VersionLog"/> class.
        /// </summary>
        /// <param name="filePath">The path to the file holding version log information.</param>
        public VersionLog(FileInfo filePath) : this(filePath, TimeSpan.FromMilliseconds(500))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VersionLog"/> class.
        /// </summary>
        /// <param name="filePath">The path to the file holding version log information.</param>
        /// <param name="filesystemPollingInterval">The period between successive attempts to read the version log file if it is open by another process for writing.</param>
        public VersionLog(FileInfo filePath, TimeSpan filesystemPollingInterval)
        {
            this.FilePath = filePath;
            this.filesystemPollingInterval = filesystemPollingInterval;
        }

        /// <summary>
        /// Gets the path to the file that contains the version log.
        /// </summary>
        public FileInfo FilePath { get; init; }

        /// <summary>
        /// Gets the time when the version log file was las modified.
        /// </summary>
        public DateTime LastModified => this.FilePath.LastWriteTimeUtc;

        /// <summary>
        /// Gets the most recently selected configuration version on the local machine.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token which may be used to abort the operation.</param>
        /// <returns>The current configuration version, if set, otherwise <c>null</c>.</returns>
        public async IAsyncEnumerable<VersionLogEntry> GetVersionsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            using (var fs = await FilesystemHelpers.GetFileReadStreamAsync(this.FilePath, this.filesystemPollingInterval, cancellationToken))
            using (var sr = new StreamReader(fs))
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var line = await sr.ReadLineAsync();
                    if (line is null)
                    {
                        break;
                    }

                    var logEntry = JsonSerializer.Deserialize<VersionLogEntry>(line);
                    if (logEntry is null)
                    {
                        continue;
                    }

                    yield return logEntry;
                }
            }
        }

        /// <summary>
        /// Gets the most recently selected configuration version on the local machine.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token which may be used to abort the operation.</param>
        /// <returns>The current configuration version, if set, otherwise <c>null</c>.</returns>
        public async Task AddVersionAsync(VersionLogEntry logEntry, CancellationToken cancellationToken = default)
        {
            if (!this.FilePath.Directory?.Exists ?? false)
            {
                this.FilePath.Directory!.Create();
            }

            using (var fs = await FilesystemHelpers.GetFileAppendStreamAsync(this.FilePath, this.filesystemPollingInterval, cancellationToken))
            using (var sw = new StreamWriter(fs))
            {
                await sw.WriteLineAsync(JsonSerializer.Serialize(logEntry));
            }
        }
    }
}
