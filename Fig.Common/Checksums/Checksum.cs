namespace Fig.Common.Checksums
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;

    public static class Checksum
    {
        private static readonly IDictionary<string, IChecksum> KnownChecksumTypes = new Dictionary<string, IChecksum>();

        /// <summary>
        /// The default algorithm used to compute checksums.
        /// </summary>
        public const string DefaultAlgorithm = "sha256";

        static Checksum()
        {
            var checksumInterfaceType = typeof(IChecksum);
            var checksumTypes = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.IsClass && !t.IsAbstract && checksumInterfaceType.IsAssignableFrom(t));

            foreach (var checksumType in checksumTypes)
            {
                var checksum = Activator.CreateInstance(checksumType) as IChecksum;
                if (checksum is IChecksum)
                {
                    KnownChecksumTypes.Add(checksum.Identifier, checksum);
                }
            }
        }

        /// <summary>
        /// Gets the checksum identified by the provided <paramref name="preference"/>, or the default checksum implementation if that is not found.
        /// </summary>
        /// <param name="preference">The checksum algorithm you wish to use.</param>
        /// <returns>An <see cref="IChecksum"/> implementation.</returns>
        public static IChecksum Get(string? preference = DefaultAlgorithm)
        {
            var checksumType = preference?.Split(new char[] { '@' })?.First() ?? DefaultAlgorithm;

            if (KnownChecksumTypes.TryGetValue(checksumType, out var checksum))
            {
                return checksum;
            }

            return KnownChecksumTypes[DefaultAlgorithm];
        }
    }
}
