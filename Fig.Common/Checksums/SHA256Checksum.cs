namespace Fig.Common.Checksums
{
    using System;
    using System.IO;
    using System.Security.Cryptography;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// A checksum implementation which uses SHA256 to compute checksums.
    /// </summary>
    public class SHA256Checksum : IChecksum
    {
        /// <inheritdoc/>
        public string Identifier => "sha256";

        /// <inheritdoc/>
        public byte[] Hash(ReadOnlySpan<byte> content)
        {
            using (var hash = SHA256.Create())
            {
                return hash.ComputeHash(content.ToArray());
            }
        }

        /// <inheritdoc/>
        public async Task<byte[]> HashAsync(Stream stream, CancellationToken cancellationToken)
        {
            using (var hash = SHA256.Create())
            {
                return await hash.ComputeHashAsync(stream, cancellationToken);
            }
        }
    }
}
