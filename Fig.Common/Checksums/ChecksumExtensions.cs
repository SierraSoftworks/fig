using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fig.Common.Checksums
{
    public static class ChecksumExtensions
    {
        public static string GetHashString(this IChecksum checksum, string data)
        {
            return $"{checksum.Identifier}@{checksum.Hash(Encoding.UTF8.GetBytes(data)).ToHex()}";
        }

        public static async Task<string> GetHashStringAsync(this IChecksum checksum, Stream stream, CancellationToken cancellationToken)
        {
            var hash = await checksum.HashAsync(stream, cancellationToken);
            return $"{checksum.Identifier}@{hash.ToHex()}";
        }

        public static bool Validate(string checksum, string data)
        {
            return string.Equals(Checksum.Get(checksum).GetHashString(data), checksum, StringComparison.Ordinal);
        }

        public static async Task<bool> ValidateAsync(string checksum, Stream stream, CancellationToken cancellationToken)
        {
            var trueHash = await Checksum.Get(checksum).GetHashStringAsync(stream, cancellationToken);
            return string.Equals(trueHash, checksum, StringComparison.Ordinal);
        }

        public static string ToHex(this byte[] digest)
        {
            return string.Concat(digest.Select(x => x.ToString("x2")));
        }
    }
}
