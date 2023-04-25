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

        public static Tuple<string, string> PrepareGlobbingPattern(string pattern, string defaultRoot)
        {
            var parts = pattern.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var rootParts = parts.TakeWhile(p => !p.Contains('*')).ToList();
            var root = string.Join(new string(Path.DirectorySeparatorChar, 1), rootParts);
            var patternParts = parts.Skip(rootParts.Count).ToList();
            return Tuple.Create(
                Path.IsPathRooted(root) ? root : Path.Combine(defaultRoot, root),
                string.Join(new string(Path.DirectorySeparatorChar, 1), patternParts));
        }
    }
}
