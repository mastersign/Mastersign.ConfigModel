using static Mastersign.ConfigModel.Test.TestData;

namespace Mastersign.ConfigModel.Test
{
    [TestClass]
    public class LayerCombinationTest
    {
        private static readonly string SCENARIO = "LayerCombination";

        [TestMethod]
        public void GetLayerPatternsAndLoadedPathsTest()
        {
            var mgr = new ConfigModelManager<Model>();
            var canonicalBasePath = GetTestDataFilePath(SCENARIO);
            var basePath = Path.Combine(canonicalBasePath, "..", SCENARIO, ".");

            Assert.AreEqual(0, mgr.GetLayerPatterns().Length);
            Assert.AreEqual(0, mgr.GetLoadedLayerPaths().Length);

            mgr.AddLayer(Path.Combine(basePath, "MergeByInterface1.yaml"));
            Assert.AreEqual(1, mgr.GetLayerPatterns().Length);
            Assert.AreEqual(
                Path.Combine(canonicalBasePath, "MergeByInterface1.yaml"),
                mgr.GetLayerPatterns()[0]);


            mgr.AddLayers("MergeByAttribute*.yaml", basePath);
            CollectionAssert.AreEqual(
                new[]
                {
                    Path.Combine(canonicalBasePath, "MergeByInterface1.yaml"),
                    Path.Combine(canonicalBasePath, "MergeByAttribute*.yaml"),
                },
                mgr.GetLayerPatterns());

            Assert.AreEqual(0, mgr.GetLoadedLayerPaths().Length);

            mgr.LoadModel();

            CollectionAssert.AreEqual(
                new[]
                {
                    Path.Combine(canonicalBasePath, "MergeByInterface1.yaml"),
                    Path.Combine(canonicalBasePath, "MergeByAttribute1.yaml"),
                    Path.Combine(canonicalBasePath, "MergeByAttribute2.yaml"),
                },
                mgr.GetLoadedLayerPaths());
        }


        [TestMethod]
        public void AddGetLoadedLayerPathsTest()
        {
            var testDataBasePath = GetTestDataFilePath(SCENARIO);
            var mgr = new ConfigModelManager<Model>();

            mgr.AddLayers("MergeByInterface*.yaml", testDataBasePath);
            mgr.AddLayers("MergeBy*.yaml", testDataBasePath);

            mgr.LoadModel();

            CollectionAssert.AreEqual(
                new string[]
                {
                    Path.Combine(testDataBasePath, "MergeByInterface1.yaml"),
                    Path.Combine(testDataBasePath, "MergeByInterface2.yaml"),
                    Path.Combine(testDataBasePath, "MergeByAttribute1.yaml"),
                    Path.Combine(testDataBasePath, "MergeByAttribute2.yaml"),
                    Path.Combine(testDataBasePath, "MergeByInterface1.yaml"),
                    Path.Combine(testDataBasePath, "MergeByInterface2.yaml"),
                },
                mgr.GetLoadedLayerPaths());
        }

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
        public void LayerMergeByAttributeWithSkipTest()
        {
            var mgr = new ConfigModelManager<Model>();
            mgr.AddLayers("MergeWithSkip*.yaml", GetTestDataFilePath(SCENARIO));
            var result = mgr.LoadModel();
            Assert.IsNotNull(result);
            Assert.AreEqual("A2", result.A);
            Assert.IsNull(result.C);
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
        public void AddNonExistantLayerTest()
        {
            var testDataBasePath = GetTestDataFilePath(SCENARIO);
            var mgr = new ConfigModelManager<Model>();

            mgr.AddLayer(Path.Combine(testDataBasePath, "DoesNotExist.yaml"));
            Assert.AreEqual(1, mgr.GetLayerPatterns().Length);
        }

        [TestMethod]
        public void MergeListDefaultTest()
        {
            var mgr = new ConfigModelManager<Model>();
            mgr.AddLayers("MergeListDefault*.yaml", GetTestDataFilePath(SCENARIO));
            var result = mgr.LoadModel();

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.ListDefault);
            Assert.AreEqual(2, result.ListDefault.Count);

            Assert.AreEqual("X 3.2", result.ListDefault[0].X);
            Assert.IsNull(result.ListDefault[0].Y);
            Assert.IsNotNull(result.ListDefault[0].NestedModel);
            Assert.AreEqual("A 3.2", result.ListDefault[0].NestedModel?.A);
            Assert.IsNull(result.ListDefault[0].NestedModel?.B);

            Assert.AreEqual("X 4.2", result.ListDefault[1].X);
            Assert.IsNull(result.ListDefault[1].Y);
            Assert.IsNotNull(result.ListDefault[1].NestedModel);
            Assert.AreEqual("A 4.2", result.ListDefault[1].NestedModel?.A);
            Assert.IsNull(result.ListDefault[1].NestedModel?.B);
        }

        [TestMethod]
        public void MergeListClearTest()
        {
            var mgr = new ConfigModelManager<Model>();
            mgr.AddLayers("MergeListClear*.yaml", GetTestDataFilePath(SCENARIO));
            var result = mgr.LoadModel();

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.ListClear);
            Assert.AreEqual(2, result.ListClear.Count);

            Assert.AreEqual("X 4.2", result.ListClear[0].X);
            Assert.IsNull(result.ListClear[0].Y);

            Assert.AreEqual("X 5.2", result.ListClear[1].X);
            Assert.IsNull(result.ListClear[1].Y);
        }

        [TestMethod]
        public void MergeListAppendTest()
        {
            var mgr = new ConfigModelManager<Model>();
            mgr.AddLayers("MergeListAppendIndistinct*.yaml", GetTestDataFilePath(SCENARIO));
            var result = mgr.LoadModel();

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.ListAppend);
            Assert.AreEqual(5, result.ListAppend.Count);

            Assert.AreEqual("X 1.1", result.ListAppend[0].X);
            Assert.AreEqual("X 2.1", result.ListAppend[1].X);
            Assert.AreEqual("X 3.1", result.ListAppend[2].X);
            Assert.AreEqual("X 4.2", result.ListAppend[3].X);
            Assert.AreEqual("X 5.2", result.ListAppend[4].X);
        }

        [TestMethod]
        public void MergeListPrependTest()
        {
            var mgr = new ConfigModelManager<Model>();
            mgr.AddLayers("MergeListPrependIndistinct*.yaml", GetTestDataFilePath(SCENARIO));
            var result = mgr.LoadModel();

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.ListPrepend);
            Assert.AreEqual(5, result.ListPrepend.Count);

            Assert.AreEqual("X 4.2", result.ListPrepend[0].X);
            Assert.AreEqual("X 5.2", result.ListPrepend[1].X);
            Assert.AreEqual("X 1.1", result.ListPrepend[2].X);
            Assert.AreEqual("X 2.1", result.ListPrepend[3].X);
            Assert.AreEqual("X 3.1", result.ListPrepend[4].X);
        }

        [TestMethod]
        public void MergeListAppendDistinctTest()
        {
            var mgr = new ConfigModelManager<Model>();
            mgr.AddLayers("MergeListAppendDistinct*.yaml", GetTestDataFilePath(SCENARIO));
            var result = mgr.LoadModel();

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.ListAppendDistinct);
            Assert.AreEqual(4, result.ListAppendDistinct.Count);

            Assert.AreEqual("A", result.ListAppendDistinct[0].X);
            Assert.AreEqual("B", result.ListAppendDistinct[1].X);
            Assert.AreEqual("C", result.ListAppendDistinct[2].X);
            Assert.AreEqual("D", result.ListAppendDistinct[3].X);
        }

        [TestMethod]
        public void MergeListPrependDistinctTest()
        {
            var mgr = new ConfigModelManager<Model>();
            mgr.AddLayers("MergeListPrependDistinct*.yaml", GetTestDataFilePath(SCENARIO));
            var result = mgr.LoadModel();

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.ListPrependDistinct);
            Assert.AreEqual(4, result.ListPrependDistinct.Count);

            Assert.AreEqual("D", result.ListPrependDistinct[0].X);
            Assert.AreEqual("A", result.ListPrependDistinct[1].X);
            Assert.AreEqual("B", result.ListPrependDistinct[2].X);
            Assert.AreEqual("C", result.ListPrependDistinct[3].X);
        }

        [TestMethod]
        public void MergeListReplaceTest()
        {
            var mgr = new ConfigModelManager<Model>();
            mgr.AddLayers("MergeListReplace*.yaml", GetTestDataFilePath(SCENARIO));
            var result = mgr.LoadModel();

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.ListReplace);
            Assert.AreEqual(3, result.ListReplace.Count);

            Assert.AreEqual("X 1.2", result.ListReplace[0].X);
            Assert.IsNull(result.ListReplace[0].Y);

            Assert.AreEqual("X 2.2", result.ListReplace[1].X);
            Assert.IsNull(result.ListReplace[1].Y);

            Assert.AreEqual("X 3.1", result.ListReplace[2].X);
            Assert.AreEqual("Y 3.1", result.ListReplace[2].Y);
        }

        [TestMethod]
        public void MergeListMergeTest()
        {
            var mgr = new ConfigModelManager<Model>();
            mgr.AddLayers("MergeListMerge*.yaml", GetTestDataFilePath(SCENARIO));
            var result = mgr.LoadModel();

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.ListMerge);
            Assert.AreEqual(3, result.ListMerge.Count);

            Assert.AreEqual("X 1.2", result.ListMerge[0].X);
            Assert.AreEqual("Y 1.1", result.ListMerge[0].Y);
            Assert.IsNotNull(result.ListMerge[0].NestedModel);
            Assert.AreEqual("A 1.2", result.ListMerge[0].NestedModel?.A);
            Assert.AreEqual("B 1.2", result.ListMerge[0].NestedModel?.B);

            Assert.AreEqual("X 2.2", result.ListMerge[1].X);
            Assert.AreEqual("Y 2.1", result.ListMerge[1].Y);
            Assert.IsNotNull(result.ListMerge[1].NestedModel);
            Assert.AreEqual("A 2.2", result.ListMerge[1].NestedModel?.A);
            Assert.AreEqual("B 2.1", result.ListMerge[1].NestedModel?.B);

            Assert.AreEqual("X 3.2", result.ListMerge[2].X);
            Assert.IsNull(result.ListMerge[2].Y);
            Assert.IsNotNull(result.ListMerge[2].NestedModel);
            Assert.AreEqual("A 3.2", result.ListMerge[2].NestedModel?.A);
            Assert.AreEqual("B 3.2", result.ListMerge[2].NestedModel?.B);
        }

        [TestMethod]
        public void MergeDictDefaultTest()
        {
            var mgr = new ConfigModelManager<Model>();
            mgr.AddLayers("MergeDictDefault*.yaml", GetTestDataFilePath(SCENARIO));
            var result = mgr.LoadModel();

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.DictDefault);
            Assert.AreEqual(3, result.DictDefault.Count);

            Assert.AreEqual("X a.2", result.DictDefault["a"].X);
            Assert.IsNull(result.DictDefault["a"].Y);
            Assert.IsNotNull(result.DictDefault["a"].NestedModel);
            Assert.AreEqual("A a.2", result.DictDefault["a"].NestedModel?.A);
            Assert.IsNull(result.DictDefault["a"].NestedModel?.B);

            Assert.AreEqual("X b.1", result.DictDefault["b"].X);
            Assert.AreEqual("Y b.1", result.DictDefault["b"].Y);
            Assert.IsNotNull(result.DictDefault["b"].NestedModel);
            Assert.AreEqual("A b.1", result.DictDefault["b"].NestedModel?.A);
            Assert.AreEqual("B b.1", result.DictDefault["b"].NestedModel?.B);

            Assert.AreEqual("X c.2", result.DictDefault["c"].X);
            Assert.IsNull(result.DictDefault["c"].Y);
            Assert.IsNotNull(result.DictDefault["c"].NestedModel);
            Assert.AreEqual("A c.2", result.DictDefault["c"].NestedModel?.A);
            Assert.IsNull(result.DictDefault["c"].NestedModel?.B);
        }

        [TestMethod]
        public void MergeDictClearTest()
        {
            var mgr = new ConfigModelManager<Model>();
            mgr.AddLayers("MergeDictClear*.yaml", GetTestDataFilePath(SCENARIO));
            var result = mgr.LoadModel();

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.DictClear);
            Assert.AreEqual(2, result.DictClear.Count);

            Assert.AreEqual("X a.2", result.DictClear["a"].X);
            Assert.IsNull(result.DictClear["a"].Y);
            Assert.IsNotNull(result.DictClear["a"].NestedModel);
            Assert.AreEqual("A a.2", result.DictClear["a"].NestedModel?.A);
            Assert.IsNull(result.DictClear["a"].NestedModel?.B);

            Assert.IsFalse(result.DictClear.ContainsKey("b"));

            Assert.AreEqual("X c.2", result.DictClear["c"].X);
            Assert.IsNull(result.DictClear["c"].Y);
            Assert.IsNotNull(result.DictClear["c"].NestedModel);
            Assert.AreEqual("A c.2", result.DictClear["c"].NestedModel?.A);
            Assert.IsNull(result.DictClear["c"].NestedModel?.B);
        }

        [TestMethod]
        public void MergeDictReplaceTest()
        {
            var mgr = new ConfigModelManager<Model>();
            mgr.AddLayers("MergeDictReplace*.yaml", GetTestDataFilePath(SCENARIO));
            var result = mgr.LoadModel();

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.DictReplace);
            Assert.AreEqual(3, result.DictReplace.Count);

            Assert.AreEqual("X a.2", result.DictReplace["a"].X);
            Assert.IsNull(result.DictReplace["a"].Y);
            Assert.IsNotNull(result.DictReplace["a"].NestedModel);
            Assert.AreEqual("A a.2", result.DictReplace["a"].NestedModel?.A);
            Assert.IsNull(result.DictReplace["a"].NestedModel?.B);

            Assert.AreEqual("X b.1", result.DictReplace["b"].X);
            Assert.AreEqual("Y b.1", result.DictReplace["b"].Y);
            Assert.IsNotNull(result.DictReplace["b"].NestedModel);
            Assert.AreEqual("A b.1", result.DictReplace["b"].NestedModel?.A);
            Assert.AreEqual("B b.1", result.DictReplace["b"].NestedModel?.B);

            Assert.AreEqual("X c.2", result.DictReplace["c"].X);
            Assert.IsNull(result.DictReplace["c"].Y);
            Assert.IsNotNull(result.DictReplace["c"].NestedModel);
            Assert.AreEqual("A c.2", result.DictReplace["c"].NestedModel?.A);
            Assert.IsNull(result.DictReplace["c"].NestedModel?.B);
        }


        [TestMethod]
        public void MergeDictMergeTest()
        {
            var mgr = new ConfigModelManager<Model>();
            mgr.AddLayers("MergeDictMerge*.yaml", GetTestDataFilePath(SCENARIO));
            var result = mgr.LoadModel();

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.DictMerge);
            Assert.AreEqual(3, result.DictMerge.Count);

            Assert.AreEqual("X a.2", result.DictMerge["a"].X);
            Assert.AreEqual("Y a.1", result.DictMerge["a"].Y);
            Assert.IsNotNull(result.DictMerge["a"].NestedModel);
            Assert.AreEqual("A a.2", result.DictMerge["a"].NestedModel?.A);
            Assert.AreEqual("B a.1", result.DictMerge["a"].NestedModel?.B);

            Assert.AreEqual("X b.1", result.DictMerge["b"].X);
            Assert.AreEqual("Y b.1", result.DictMerge["b"].Y);
            Assert.IsNotNull(result.DictMerge["b"].NestedModel);
            Assert.AreEqual("A b.1", result.DictMerge["b"].NestedModel?.A);
            Assert.AreEqual("B b.1", result.DictMerge["b"].NestedModel?.B);

            Assert.AreEqual("X c.2", result.DictMerge["c"].X);
            Assert.IsNull(result.DictMerge["c"].Y);
            Assert.IsNotNull(result.DictMerge["c"].NestedModel);
            Assert.AreEqual("A c.2", result.DictMerge["c"].NestedModel?.A);
            Assert.IsNull(result.DictMerge["c"].NestedModel?.B);
        }

        [TestMethod]
        public void ThrowsModelNotFoundExceptionFileNotFoundTest()
        {
            var mgr = new ConfigModelManager<Model>();
            var modelFile = GetTestDataFilePath(SCENARIO, "DoesNotExist.yaml");
            mgr.AddLayer(modelFile);
            try
            {
                mgr.LoadModel();
                Assert.Fail("Should throw " + nameof(ConfigModelLayerNotFoundException));
            }
            catch (ConfigModelLayerNotFoundException ex)
            {
                Assert.AreEqual(modelFile, ex.FileName);
                Console.WriteLine(ex.GetType().Name + ": " + ex.Message);
                if (ex.InnerException != null)
                {
                    Console.WriteLine(ex.InnerException.GetType().Name + ": " + ex.InnerException.Message);
                }
                Assert.IsInstanceOfType(ex.InnerException, typeof(FileNotFoundException));
            }
        }

        [TestMethod]
        public void ThrowsModelNotFoundExceptionDirectoryNotFoundTest()
        {
            var mgr = new ConfigModelManager<Model>();
            var modelFile = GetTestDataFilePath("Missing", "DoesNotExist.yaml");
            mgr.AddLayer(modelFile);
            try
            {
                mgr.LoadModel();
                Assert.Fail("Should throw " + nameof(ConfigModelLayerNotFoundException));
            }
            catch (ConfigModelLayerNotFoundException ex)
            {
                Assert.AreEqual(modelFile, ex.FileName);
                Console.WriteLine(ex.GetType().Name + ": " + ex.Message);
                if (ex.InnerException != null)
                {
                    Console.WriteLine(ex.InnerException.GetType().Name + ": " + ex.InnerException.Message);
                }
                Assert.IsInstanceOfType(ex.InnerException, typeof(DirectoryNotFoundException));
            }
        }

        [MergableConfigModel]
        class Model
        {
            public string? A { get; set; }
            public string? B { get; set; }

            [NoMerge]
            public string? C { get; set; }

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

            public override bool Equals(object? obj) => obj is Child child && X == child.X;
            public override int GetHashCode() => HashCode.Combine(X);
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
