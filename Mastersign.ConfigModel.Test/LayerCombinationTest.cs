using static Mastersign.ConfigModel.Test.TestData;

namespace Mastersign.ConfigModel.Test
{
    [TestClass]
    public class LayerCombinationTest
    {
        private static readonly string SCENARIO = "LayerCombination";

        [TestMethod]
        public void LayerMergeByAttributeTest()
        {
            var mgr = new ConfigModelManager<Model>();
            mgr.AddLayers("MergeByAttribute*.yaml", GetTestDataFilePath(SCENARIO));
            var result = mgr.LoadModel();
            Assert.IsNotNull(result);
            Assert.AreEqual("A1", result.A);
            Assert.AreEqual("B2", result.B);
            Assert.IsNotNull(result.MergableByAttribute);
            Assert.AreEqual("X1", result.MergableByAttribute.X);
            Assert.AreEqual("Y2", result.MergableByAttribute.Y);
        }

        [TestMethod]
        public void LayerMergeByInterfaceTest()
        {
            var mgr = new ConfigModelManager<Model>();
            mgr.AddLayers("MergeByInterface*.yaml", GetTestDataFilePath(SCENARIO));
            var result = mgr.LoadModel();
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.MergableByInterface);
            Assert.IsNull(result.MergableByInterface.X);
            Assert.AreEqual("Y1", result.MergableByInterface.Y);
        }

        [TestMethod]
        public void LayerMergeByReplacementTest()
        {
            var mgr = new ConfigModelManager<Model>();
            mgr.AddLayers("NonMergable*.yaml", GetTestDataFilePath(SCENARIO));
            var result = mgr.LoadModel();
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Child);
            Assert.AreEqual("X2", result.Child.X);
            Assert.IsNull(result.Child.Y);
            Assert.IsNull(result.Child.NestedModel);
        }

        [TestMethod]
        public void AddLayerResultTest()
        {
            var testDataBasePath = GetTestDataFilePath(SCENARIO);
            var mgr = new ConfigModelManager<Model>();

            Assert.AreEqual(
                Path.Combine(testDataBasePath, "MergeByAttribute1.yaml"),
                mgr.AddLayer(Path.Combine(testDataBasePath, "MergeByAttribute1.yaml")));

            Assert.ThrowsException<FileNotFoundException>(() =>
                mgr.AddLayer(Path.Combine(testDataBasePath, "_invalid_.yaml")));
        }

        [TestMethod]
        public void AddLayersResultTest()
        {
            var testDataBasePath = GetTestDataFilePath(SCENARIO);
            var mgr = new ConfigModelManager<Model>();

            CollectionAssert.AreEqual(
                new string[]
                {
                    Path.Combine(testDataBasePath, "MergeByInterface1.yaml"),
                    Path.Combine(testDataBasePath, "MergeByInterface2.yaml"),
                },
                mgr.AddLayers("MergeByInterface*.yaml", testDataBasePath));

            CollectionAssert.AreEqual(
                new string[]
                {
                    Path.Combine(testDataBasePath, "MergeByAttribute1.yaml"),
                    Path.Combine(testDataBasePath, "MergeByAttribute2.yaml"),
                    Path.Combine(testDataBasePath, "MergeByInterface1.yaml"),
                    Path.Combine(testDataBasePath, "MergeByInterface2.yaml"),
                },
                mgr.AddLayers("MergeBy*.yaml", testDataBasePath));
        }



        [MergableConfigModel]
        class Model
        {
            public string? A { get; set; }
            public string? B { get; set; }

            public Child? Child { get; set; }

            public MergableByAttribute? MergableByAttribute { get; set; }

            public MergableByInterface? MergableByInterface { get; set; }

            public IList<Child> ListDefault { get; set; } = new List<Child>();

            [MergeList(ListMergeMode.Clear)]
            public List<Child>? ListClear { get; set; }

            [MergeList(ListMergeMode.Append)]
            public List<Child>? ListAppend { get; set; }

            [MergeList(ListMergeMode.AppendDistinct)]
            public List<Child>? ListAppendDistinct { get; set; }

            [MergeList(ListMergeMode.Prepend)]
            public List<Child>? ListPrepend { get; set; }

            [MergeList(ListMergeMode.PrependDistinct)]
            public List<Child>? ListPrependDistinct { get; set; }

            [MergeList(ListMergeMode.ReplaceItem)]
            public List<Child>? ListReplace { get; set; }

            [MergeList(ListMergeMode.MergeItem)]
            public List<Child>? ListMerge { get; set; }


            public IDictionary<string, Child> DictDefault { get; set; } = new Dictionary<string, Child>();

            [MergeDictionary(DictionaryMergeMode.Clear)]
            public Dictionary<string, Child>? DictClear { get; set; }

            [MergeDictionary(DictionaryMergeMode.ReplaceValue)]
            public Dictionary<string, Child>? DictReplace { get; set; }

            [MergeDictionary(DictionaryMergeMode.MergeValue)]
            public Dictionary<string, Child>? DictMerge { get; set; }
        }

        class Child
        {
            public string? X { get; set; }
            public string? Y { get; set; }

            public Model? NestedModel { get; set; }
        }

        [MergableConfigModel]
        class MergableByAttribute
        {
            public string? X { get; set; }
            public string? Y { get; set; }
        }

        class MergableByInterface : IMergableConfigModel
        {
            public string? X { get; set; }
            public string? Y { get; set; }

            public void UpdateWith(object source, bool forceDeepMerge)
            {
                if (source == null) throw new ArgumentNullException(nameof(source));
                if (source is not MergableByInterface s) throw new ArgumentOutOfRangeException(nameof(source));
                X = s.X;
            }
        }
    }
}
