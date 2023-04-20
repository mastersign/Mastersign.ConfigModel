using static Mastersign.ConfigModel.Test.TestData;


namespace Mastersign.ConfigModel.Test
{
    [TestClass]
    public class StringSourcingTest
    {
        private static readonly string SCENARIO = "StringSourcing";

        [TestMethod]
        public void SimpleSourcing()
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
        public void Precedence()
        {
            var mgr = new ConfigModelManager<ChildWithSources>();
            var modelFile = GetTestDataFilePath(SCENARIO, "Precedence.yaml");
            mgr.AddLayer(modelFile);
            var result = mgr.LoadModel();
            Assert.IsNotNull(result);
            Assert.AreEqual("Precedence X", result.X);
            Assert.AreEqual("Y from File", result.Y);
        }

        [TestMethod]
        public void SourcingInChildren()
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
        }
    }
}
