using System.Reflection;

namespace Mastersign.ConfigModel.Test
{
    internal static class TestData
    {
        public static string GetTestDataFilePath(string scenario, params string[] path)
        {
            var a = Assembly.GetExecutingAssembly();
            var afn = new Uri(a.GetName().CodeBase ?? throw new NotSupportedException("The test assembly has no code base?")).LocalPath;
            return Path.Combine((new string[] {
                Path.GetDirectoryName(afn) ?? throw new NotSupportedException("The code base of the test assembly is no local path?"),
                "TestData", scenario,
            })
            .Concat(path).ToArray());
        }
    }
}
