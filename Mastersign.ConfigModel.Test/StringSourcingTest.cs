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
            var modelFile = GetFilePath(SCENARIO, "Simple.yaml");
            mgr.AddLayer(modelFile);
            var result = mgr.LoadModel();
            Assert.IsNotNull(result);
            Assert.AreEqual("Simple X from File", result.X);
            Assert.AreEqual("Simple Y from File", result.Y);
        }

        [TestMethod]
        public void Precedence()
        {
            var mgr = new ConfigModelManager<ChildWithSources>();
            var modelFile = GetFilePath(SCENARIO, "Precedence.yaml");
            mgr.AddLayer(modelFile);
            var result = mgr.LoadModel();
            Assert.IsNotNull(result);
            Assert.AreEqual("Precedence X", result.X);
            Assert.AreEqual("Precedence Y from File", result.Y);
        }

        [TestMethod]
        public void SourcingInChildren()
        {
            var mgr = new ConfigModelManager<RootModel>();
            var modelFile = GetFilePath(SCENARIO, "Child.yaml");
            mgr.AddLayer(modelFile);
            var result = mgr.LoadModel();
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.WithSources);
            Assert.AreEqual("Child WithSources X from File", result.WithSources.X);
        }
    }
}
