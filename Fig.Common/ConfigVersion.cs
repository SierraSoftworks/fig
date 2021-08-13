namespace Fig.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Methods used for interacting with configuration versions.
    /// </summary>
    public static class ConfigVersion
    {
        /// <summary>
        /// Converts a config version string into a safe version which may appear in paths.
        /// </summary>
        /// <param name="configVersion">The configuration version to convert into its safe representation.</param>
        /// <returns>The safe representation of this config version.</returns>
        public static string ToSafeName(string configVersion)
        {
            var safeChars = new HashSet<char>
            {
                '.','_','-'
            };

            var sb = new StringBuilder(configVersion.Length);
            foreach (var c in configVersion)
            {
                if (!char.IsLetterOrDigit(c) && !safeChars.Contains(c))
                {
                    sb.Append('_');
                }
                else
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }
    }
}
