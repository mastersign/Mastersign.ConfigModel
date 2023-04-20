using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Mastersign.ConfigModel.Test
{
    internal static class TestData
    {
        public static string GetTestDataFilePath(string scenario, string? filename = null)
        {
            var a = Assembly.GetExecutingAssembly();
            var afn = new Uri(a.GetName().CodeBase ?? throw new NotSupportedException("The test assembly has no code base?")).LocalPath;
            return Path.Combine(Path.GetDirectoryName(afn) ?? throw new NotSupportedException("The code base of the test assembly is no local path?"),
                "TestData", scenario, filename ?? string.Empty);
        }
    }
}
