namespace Fig.Common.Checksums
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// A checksum provides a means to detect tampering with data. This interface abstracts
    /// the process for different checksum algorithms.
    /// </summary>
    public interface IChecksum
    {
        /// <summary>
        /// Gets the identifier used to uniquely reference this hash function.
        /// </summary>
        string Identifier { get; }

        /// <summary>
        /// Computes the hash of a span of data, returning the hash's digest.
        /// </summary>
        /// <param name="content">The content to hash.</param>
        /// <returns>The digest of the hash function.</returns>
        byte[] Hash(ReadOnlySpan<byte> content);

        /// <summary>
        /// Asynchronously computes the hash of a stream of data, returning the hash's digest.
        /// </summary>
        /// <param name="stream">The stream of data to hash.</param>
        /// <param name="cancellationToken">The cancellation token used to abort the operation if required.</param>
        /// <returns>The digest of the hash function.</returns>
        Task<byte[]> HashAsync(Stream stream, CancellationToken cancellationToken);
    }
}
