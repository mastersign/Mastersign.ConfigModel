namespace Mastersign.ConfigModel.Test
{
    [TestClass]
    public class PathHelperTest
    {
        [TestMethod]
        public void GetCanonicalPathTest()
        {
            var DS = Path.DirectorySeparatorChar;

            Assert.AreEqual(
                Path.Combine(Environment.CurrentDirectory, $@"a{DS}B{DS}C"),
                PathHelper.GetCanonicalPath($@"a{DS}B{DS}C"));

            Assert.AreEqual(
                $@"c:{DS}folder A{DS}Folder b{DS}File Name.txt",
                PathHelper.GetCanonicalPath($@"c:{DS}folder A{DS}.{DS}Folder b{DS}X{DS}..{DS}File Name.txt"));

            Assert.AreEqual(
                $@"c:{DS}folder A{DS}Folder b{DS}File Name.txt",
                PathHelper.GetCanonicalPath($@"X{DS}..{DS}Folder b{DS}.{DS}File Name.txt", $@"c:{DS}folder A{DS}Y{DS}..{DS}"));
        }
    }
}
