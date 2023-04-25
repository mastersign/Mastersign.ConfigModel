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

        [TestMethod]
        public void PrepareGlobbingPatternNoRootTest()
        {
            var result = PathHelper.PrepareGlobbingPattern("a/b/*.txt", @"C:\Users\Me\Documents");
            Assert.AreEqual(@"C:\Users\Me\Documents\a\b", result.Item1);
            Assert.AreEqual(@"*.txt", result.Item2);
        }

        [TestMethod]
        public void PrepareGlobbingPatternRootedTest()
        {
            var result = PathHelper.PrepareGlobbingPattern("D:/a/**/b/*.txt", @"C:\Users\Me\Documents");
            Assert.AreEqual(@"D:\a", result.Item1);
            Assert.AreEqual(@"**\b\*.txt", result.Item2);
        }

    }
}
