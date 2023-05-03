using System;
using System.IO;
using System.Linq;

namespace Mastersign.ConfigModel
{
    internal static class PathHelper
    {
        public static string GetCanonicalPath(string path, string root = null) 
            => new Uri(
                Path.IsPathRooted(path)
                    ? path
                    : string.IsNullOrEmpty(root)
                        ? Path.GetFullPath(path)
                        : Path.Combine(root, path)
                ).LocalPath;

        /// <summary>
        /// Returns a tuple with a root path without wildcards as item 1
        /// and a relative sub-path with potential wildcards as item 2.
        /// 
        /// The root path is as long as possible without including a segment
        /// with a wildcard.
        /// The sub path is as short as possible, but contains at least
        /// the last segment (file name) of the pattern.
        /// </summary>
        /// <param name="pattern">A path with wildcards</param>
        /// <param name="defaultRoot">An absolute path as reference, in case the <paramref name="pattern"/> is not rooted.</param>
        /// <returns>A tuple with a steady root and a sub-path with wildcards.</returns>
        /// <exception cref="ArgumentException">Is thrown if <paramref name="pattern"/> is not rooted 
        /// and no default root is given; or if the given default root is not rooted.</exception>
        public static Tuple<string, string> PrepareGlobbingPattern(string pattern, string defaultRoot = null)
        {
            var parts = pattern.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var rootParts = parts.TakeWhile(p => !p.Contains('*')).ToList();
            if (rootParts.Count == parts.Length) rootParts.RemoveAt(rootParts.Count - 1);
            var root = string.Join(new string(Path.DirectorySeparatorChar, 1), rootParts);
            var patternParts = parts.Skip(rootParts.Count).ToList();
            if (defaultRoot is null && !Path.IsPathRooted(root))
            {
                throw new ArgumentException(
                    "The given pattern is not rooted and no default root is given",
                    nameof(pattern));
            }
            if (defaultRoot != null && !Path.IsPathRooted(defaultRoot))
            {
                throw new ArgumentException(
                    "The given default root path is not rooted itself",
                    nameof(defaultRoot));
            }
            return Tuple.Create(
                Path.IsPathRooted(root) ? root : Path.Combine(defaultRoot, root),
                string.Join(new string(Path.DirectorySeparatorChar, 1), patternParts));
        }
    }
}
