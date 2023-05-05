using static Mastersign.ConfigModel.Test.TestData;

namespace Mastersign.ConfigModel.Test
{
    [TestClass]
    public class AliasTest
    {
        private static readonly string SCENARIO = "Alias";

        [TestMethod]
        public void SimpleAliasTest()
        {
            var mgr = new ConfigModelManager<RootModel>();
            mgr.AddLayer(GetTestDataFilePath(SCENARIO, "Simple.yaml"));

            var result = mgr.LoadModel();
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Child);
            Assert.AreEqual("Template A", result.Child.X);
            Assert.IsNotNull(result.Children);
            Assert.AreEqual(1, result.Children.Count);
            Assert.AreSame(result.Children["A"], result.Child);
        }

        [TestMethod]
        public void MergeKeyWithoutMergingParserTest()
        {
            var mgr = new ConfigModelManager<RootModel>();
            mgr.AddLayer(GetTestDataFilePath(SCENARIO, "MergeKey.yaml"));

            var result = mgr.LoadModel();
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Child);
            Assert.AreEqual("Child X", result.Child.X);
            Assert.AreEqual("Child Y", result.Child.Y);
            Assert.IsNotNull(result.Children);

            Assert.AreEqual(2, result.Children.Count);
            Assert.AreNotSame(result.Children["A"], result.Child);
            Assert.AreNotSame(result.Children["B"], result.Child);

            Assert.AreEqual("Template XA", result.Children["A"].X);
            Assert.IsNull(result.Children["A"].Y);

            Assert.IsNull(result.Children["B"].X);
            Assert.AreEqual("Template YB", result.Children["B"].Y);
        }

        [TestMethod]
        public void MergeKeyTest()
        {
            var mgr = new ConfigModelManager<RootModel>(withMergeKeys: true);
            mgr.AddLayer(GetTestDataFilePath(SCENARIO, "MergeKey.yaml"));

            var result = mgr.LoadModel();
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Child);
            Assert.AreEqual("Template XA", result.Child.X);
            Assert.AreEqual("Child Y", result.Child.Y);
            Assert.IsNotNull(result.Children);
            
            Assert.AreEqual(2, result.Children.Count);
            Assert.AreNotSame(result.Children["A"], result.Child);
            Assert.AreNotSame(result.Children["B"], result.Child);

            Assert.AreEqual("Template XA", result.Children["A"].X);
            Assert.IsNull(result.Children["A"].Y);

            Assert.AreEqual("Template XA", result.Children["B"].X);
            Assert.AreEqual("Template YB", result.Children["B"].Y);
        }

        [TestMethod]
        public void MultiMergeKeyTest()
        {
            var mgr = new ConfigModelManager<RootModel>(withMergeKeys: true);
            mgr.AddLayer(GetTestDataFilePath(SCENARIO, "MultiMergeKey.yaml"));

            var result = mgr.LoadModel();
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Child);
            Assert.AreEqual("Template XA", result.Child.X);
            Assert.AreEqual("Template YB", result.Child.Y);
            Assert.IsNotNull(result.Children);

            Assert.AreEqual(2, result.Children.Count);
            Assert.AreNotSame(result.Children["A"], result.Child);
            Assert.AreNotSame(result.Children["B"], result.Child);

            Assert.AreEqual("Template XA", result.Children["A"].X);
            Assert.IsNull(result.Children["A"].Y);

            Assert.IsNull(result.Children["B"].X);
            Assert.AreEqual("Template YB", result.Children["B"].Y);
        }

        class RootModel
        {
            public ChildModel? Child { get; set; }

            public Dictionary<string, ChildModel>? Children { get; set; }
        }

        class ChildModel
        {
            public string? X { get; set; }

            public string? Y { get; set; }
        }
    }
}
