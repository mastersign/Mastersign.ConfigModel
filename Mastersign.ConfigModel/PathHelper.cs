using System;
using System.IO;

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
    }
}
