using System;
using System.IO;

namespace Starcounter.Internal
{
    /// <summary>
    /// Utility methods for directories.
    /// </summary>
    public static class DirectoryExtensions
    {
        /// <summary>
        /// Compare two directory paths to see if the reference the same folder.
        /// </summary>
        /// <param name="dir1">First path</param>
        /// <param name="dir2">Second path</param>
        /// <returns>True if both paths reference the same folder</returns>
        public static bool EqualDirectories(string dir1, string dir2)
        {
            Guard.NotNull(dir1, nameof(dir1));
            Guard.NotNull(dir2, nameof(dir2));

            return string.Compare(
                Path.GetFullPath(dir1).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                Path.GetFullPath(dir2).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                StringComparison.CurrentCultureIgnoreCase) == 0;
        }
    }
}