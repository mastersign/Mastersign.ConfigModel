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
            Assert.AreEqual("Base B", result.B);
        }

        [MergableConfigModel]
        class Model : ConfigModelBase
        {
            public string? A { get; set; }

            public string? B { get; set; }

            public List<int>? Primitives { get; set; }

            [MergeDictionary(DictionaryMergeMode.MergeValue)]
            public Dictionary<string, SlimChild>? SlimJims { get; set; }

            [MergeDictionary(DictionaryMergeMode.MergeValue)]
            public Dictionary<string, FatChild>? FatBoys { get; set; }
        }

        class SlimChild
        {
            public int X { get; set; }
            public int Y { get; set; }
        }

        class FatChild : ConfigModelBase
        {
            public string? X { get; set; }

            [MergeList(ListMergeMode.AppendDistinct)]
            public List<string>? Array { get; set; }
        }
    }


}
