using static Mastersign.ConfigModel.Test.TestData;

namespace Mastersign.ConfigModel.Test
{
    [TestClass]
    public class IncludeTest
    {
        private static readonly string SCENARIO = "Includes";

        [TestMethod]
        public void SimpleIncludeTest()
        {
            var mgr = new ConfigModelManager<Model>();
            mgr.AddLayer(GetTestDataFilePath(SCENARIO, "SimpleMain.yaml"));
            var result = mgr.LoadModel();
            Assert.IsNotNull(result);

            Assert.AreEqual("Base A", result.A);
            Assert.AreEqual("Main B", result.B);
        }

        [TestMethod]
        public void ComplexIncludeTest()
        {
            var mgr = new ConfigModelManager<Model>();
            mgr.AddLayer(GetTestDataFilePath(SCENARIO, "ComplexMain.yaml"));
            var result = mgr.LoadModel();
            Assert.IsNotNull(result);

            Assert.AreEqual("Main A", result.A);
            Assert.AreEqual("Base B", result.B);

            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, result.Primitives);

            Assert.IsNotNull(result.SlimJims);
            Assert.AreEqual(3, result.SlimJims.Count);
            Assert.IsTrue(result.SlimJims.ContainsKey("a"));
            Assert.AreEqual(211, result.SlimJims["a"].X);
            Assert.AreEqual(212, result.SlimJims["a"].Y);
            Assert.IsTrue(result.SlimJims.ContainsKey("b"));
            Assert.IsNull(result.SlimJims["b"].X);
            Assert.IsTrue(result.SlimJims.ContainsKey("c"));
            Assert.AreEqual(231, result.SlimJims["c"].X);
            Assert.IsNull(result.SlimJims["c"].Y);

            Assert.IsNotNull(result.FatChild);
            Assert.AreEqual("Include X", result.FatChild.X);
            Assert.AreEqual("Main Y", result.FatChild.Y);
            CollectionAssert.AreEqual(new[] { "a", "b", "c", "d" }, result.FatChild.Array);
            Assert.IsNull(result.FatChild.Includes);

            Assert.IsNotNull(result.FatBoys);
            Assert.IsTrue(result.FatBoys.ContainsKey("a"));
            Assert.AreEqual("Main a.X", result.FatBoys["a"].X);
            Assert.AreEqual("Y from File", result.FatBoys["a"].Y);
            CollectionAssert.AreEqual(new[] { "A", "B", "C", "D" }, result.FatBoys["a"].Array);
            Assert.IsNull(result.FatBoys["a"].Includes);
        }

        [TestMethod]
        public void GlobbingIncludeTest()
        {
            var mgr = new ConfigModelManager<Model>();
            mgr.AddLayer(GetTestDataFilePath(SCENARIO, "GlobbingMain.yaml"));
            var result = mgr.LoadModel();
            Assert.IsNotNull(result);

            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5, 6 }, result.Primitives);
        }

        [TestMethod]
        public void GetIncludePatternsAndLoadedPathsTest()
        {
            var mgr = new ConfigModelManager<Model>();
            mgr.AddLayer(GetTestDataFilePath(SCENARIO, "ComplexMain.yaml"));

            Assert.AreEqual(0, mgr.GetIncludePatterns().Length);
            Assert.AreEqual(0, mgr.GetLoadedIncludePaths().Length);

            mgr.LoadModel();

            CollectionAssert.AreEquivalent(
                new[]
                {
                    GetTestDataFilePath(SCENARIO, "ComplexBase.yaml"),
                    GetTestDataFilePath(SCENARIO, "ComplexFatBoyA.yaml"),
                    GetTestDataFilePath(SCENARIO, "ComplexMainFatChild.yaml"),
                },
                mgr.GetIncludePatterns());

            CollectionAssert.AreEquivalent(
                new[]
                {
                    GetTestDataFilePath(SCENARIO, "ComplexBase.yaml"),
                    GetTestDataFilePath(SCENARIO, "ComplexFatBoyA.yaml"),
                    GetTestDataFilePath(SCENARIO, "ComplexMainFatChild.yaml"),
                },
                mgr.GetLoadedIncludePaths());
        }

        [TestMethod]
        public void ThrowsIncludeNotFoundExceptionTest()
        {
            var mgr = new ConfigModelManager<Model>();
            mgr.AddLayer(GetTestDataFilePath(SCENARIO, "NonExistantInclude.yaml"));
            try
            {
                mgr.LoadModel();
                Assert.Fail("Should throw IncludeNotFoundException");
            }
            catch (ConfigModelIncludeNotFoundException ex)
            {
                Assert.AreEqual(GetTestDataFilePath(SCENARIO, "NonExistantInclude.yaml"), ex.ModelFile);
                Assert.AreEqual(GetTestDataFilePath(SCENARIO, "Missing", "NotExistant.yaml"), ex.FileName);
            }
        }

        [TestMethod]
        public void CycleDetectionTest()
        {
            var mgr = new ConfigModelManager<Model>();
            var basePath = GetTestDataFilePath(SCENARIO);
            mgr.AddLayer(Path.Combine(basePath, "CycleDetectionMain.yaml"));
            try
            {
                mgr.LoadModel();
                Assert.Fail();
            }
            catch (ConfigModelIncludeCycleException ex)
            {
                CollectionAssert.AreEqual(
                    new[] {
                        Path.Combine(basePath, "CycleDetectionInclude1.yaml"),
                        Path.Combine(basePath, "CycleDetectionInclude2.yaml"),
                        Path.Combine(basePath, "CycleDetectionInclude3.yaml"),
                        Path.Combine(basePath, "CycleDetectionInclude1.yaml"),
                    },
                    ex.PathCycle);
            }
        }

        [MergableConfigModel]
        class Model : ConfigModelBase
        {
            public string? A { get; set; }

            public string? B { get; set; }

            public FatChild? FatChild { get; set; }

            [MergeList(ListMergeMode.Append)]
            public List<int>? Primitives { get; set; }

            [MergeDictionary(DictionaryMergeMode.MergeValue)]
            public Dictionary<string, SlimChild>? SlimJims { get; set; }

            [MergeDictionary(DictionaryMergeMode.MergeValue)]
            public Dictionary<string, FatChild>? FatBoys { get; set; }
        }

        class SlimChild
        {
            public int? X { get; set; }
            public int? Y { get; set; }

            public override bool Equals(object? obj) => obj is SlimChild child && X == child.X && Y == child.Y;
            public override int GetHashCode() => HashCode.Combine(X, Y);
        }

        class FatChild : ConfigModelBase
        {
            public string? X { get; set; }
            public string? Y { get; set; }

            [MergeList(ListMergeMode.AppendDistinct)]
            public List<string>? Array { get; set; }
        }
    }


}
