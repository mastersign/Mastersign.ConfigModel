using static Mastersign.ConfigModel.Test.TestData;

namespace Mastersign.ConfigModel.Test
{
    [TestClass]
    public class StringSourcingTest
    {
        private static readonly string SCENARIO = "StringSourcing";

        [TestMethod]
        public void SimpleSourcingTest()
        {
            var mgr = new ConfigModelManager<ChildWithSources>();
            var modelFile = GetTestDataFilePath(SCENARIO, "Simple.yaml");
            mgr.AddLayer(modelFile);
            var result = mgr.LoadModel();
            Assert.IsNotNull(result);
            Assert.AreEqual("X from File", result.X);
            Assert.AreEqual("Y from File", result.Y);
        }

        [TestMethod]
        public void PrecedenceTest()
        {
            var mgr = new ConfigModelManager<ChildWithSources>();
            var modelFile = GetTestDataFilePath(SCENARIO, "Precedence.yaml");
            mgr.AddLayer(modelFile);
            var result = mgr.LoadModel();
            Assert.IsNotNull(result);
            Assert.AreEqual("Precedence X", result.X);
            Assert.AreEqual("Y from File", result.Y);
            Assert.AreEqual("Z from Include", result.Z);
        }

        [TestMethod]
        public void SourcingInChildrenTest()
        {
            var mgr = new ConfigModelManager<Model>();
            var modelFile = GetTestDataFilePath(SCENARIO, "Child.yaml");
            mgr.AddLayer(modelFile);
            var result = mgr.LoadModel();
            Assert.IsNotNull(result);

            Assert.IsNotNull(result.Child);
            Assert.AreEqual("X from File", result.Child.X);
            Assert.AreEqual("Y from File", result.Child.Y);

            Assert.IsNotNull(result.List);
            Assert.AreEqual(2, result.List.Count);
            Assert.AreEqual("X from File", result.List[0].X);
            Assert.IsNull(result.List[0].Y);
            Assert.IsNull(result.List[1].X);
            Assert.AreEqual("Y from File", result.List[1].Y);

            Assert.IsNull(result.Dictionary);

            Assert.IsNotNull(result.Nested);
            Assert.IsNotNull(result.Nested.Child);
            Assert.AreEqual("X from File", result.Nested.Child.X);
            Assert.IsNull(result.Nested.Child.Y);

            Assert.IsNotNull(result.Nested.Dictionary);
            Assert.AreEqual(2, result.Nested.Dictionary.Count);
            Assert.AreEqual("X from File", result.Nested.Dictionary["A"].X);
            Assert.IsNull(result.Nested.Dictionary["A"].Y);
            Assert.IsNull(result.Nested.Dictionary["B"].X);
            Assert.AreEqual("Y from File", result.Nested.Dictionary["B"].Y);

            Assert.IsNull(result.Nested.List);
        }

        [TestMethod]
        public void GetStringSourcePathsTest()
        {
            var mgr = new ConfigModelManager<Model>();
            mgr.AddLayer(GetTestDataFilePath(SCENARIO, "Child.yaml"));
            Assert.AreEqual(0, mgr.GetLoadedStringSourcePaths().Length);
            mgr.LoadModel();
            CollectionAssert.AreEquivalent(
                new[]
                {
                    GetTestDataFilePath(SCENARIO, "Strings", "x.txt"),
                    GetTestDataFilePath(SCENARIO, "Strings", "y.txt"),
                },
                mgr.GetLoadedStringSourcePaths());
        }

        [TestMethod]
        public void ThrowsStringSourceNotFoundExceptionTest()
        {
            var mgr = new ConfigModelManager<ChildWithSources>();
            var modelFile = GetTestDataFilePath(SCENARIO, "NotFound.yaml");
            mgr.AddLayer(modelFile);
            try
            {
                mgr.LoadModel();
                Assert.Fail("Should throw " + nameof(ConfigModelStringSourceNotFoundException));
            }
            catch (ConfigModelStringSourceNotFoundException ex)
            {
                Assert.AreEqual(modelFile, ex.ModelFile);
                Console.WriteLine(ex.GetType().Name + ": " + ex.Message);
                Assert.AreEqual(GetTestDataFilePath(SCENARIO, "Strings", "DoesNotExist.txt"), ex.FileName);
                Assert.IsInstanceOfType(ex.InnerException, typeof(FileNotFoundException));
            }
        }

        [TestMethod]
        public void GenericDictionaryTest()
        {
            var mgr = new ConfigModelManager<Dictionary<string, object>>();
            var modelFile = GetTestDataFilePath(SCENARIO, "Dictionary.yaml");
            mgr.AddLayer(modelFile);
            var result = mgr.LoadModel();

            Assert.IsNotNull(result);
            Assert.IsFalse(result.ContainsKey("$sources"));
            Assert.AreEqual("X from File", result["X"]);
            Assert.AreEqual("Dictionary Y", result["Y"]);
        }

        class Model
        {
            public ChildWithSources? Child { get; set; }

            public List<ChildWithSources>? List { get; set; }

            public Dictionary<string, ChildWithSources>? Dictionary { get; set; }

            public Model? Nested { get; set; }
        }

        class ChildWithSources : ConfigModelBase
        {
            public string? X { get; set; }
            public string? Y { get; set; }
            public string? Z { get; set; }
        }
    }
}
