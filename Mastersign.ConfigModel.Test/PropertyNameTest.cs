using static Mastersign.ConfigModel.Test.TestData;

namespace Mastersign.ConfigModel.Test
{
    [TestClass]
    public class PropertyNameTest
    {
        private static readonly string SCENARIO = "PropertyNames";

        [TestMethod]
        public void PascalCaseTest()
        {
            var mgr = new ConfigModelManager<RootModel>(propertyNameHandling: PropertyNameHandling.PascalCase);
            var modelFile = GetFilePath(SCENARIO, "PascalCase.yaml");
            mgr.AddLayer(modelFile);
            var result = mgr.LoadModel();
            Assert.IsNotNull(result);
            Assert.AreEqual("Name", result.Name);
            Assert.AreEqual("Prop A", result.PropA);
            Assert.IsNull(result.PropB);
            Assert.AreEqual("Prop D", result.PropD);
        }

        [TestMethod]
        public void CamelCaseTest()
        {
            var mgr = new ConfigModelManager<RootModel>(propertyNameHandling: PropertyNameHandling.CamelCase);
            var modelFile = GetFilePath(SCENARIO, "CamelCase.yaml");
            mgr.AddLayer(modelFile);
            var result = mgr.LoadModel();
            Assert.IsNotNull(result);
            Assert.AreEqual("Name", result.Name);
            Assert.AreEqual("Prop A", result.PropA);
            Assert.IsNull(result.PropB);
            Assert.AreEqual("Prop D", result.PropD);
        }

        [TestMethod]
        public void UnderscoredTest()
        {
            var mgr = new ConfigModelManager<RootModel>(propertyNameHandling: PropertyNameHandling.Underscored);
            var modelFile = GetFilePath(SCENARIO, "Underscored.yaml");
            mgr.AddLayer(modelFile);
            var result = mgr.LoadModel();
            Assert.IsNotNull(result);
            Assert.AreEqual("Name", result.Name);
            Assert.AreEqual("Prop A", result.PropA);
            Assert.IsNull(result.PropB);
            Assert.AreEqual("Prop D", result.PropD);
        }

        [TestMethod]
        public void HyphenatedTest()
        {
            var mgr = new ConfigModelManager<RootModel>(propertyNameHandling: PropertyNameHandling.Hyphenated);
            var modelFile = GetFilePath(SCENARIO, "Hyphenated.yaml");
            mgr.AddLayer(modelFile);
            var result = mgr.LoadModel();
            Assert.IsNotNull(result);
            Assert.AreEqual("Name", result.Name);
            Assert.AreEqual("Prop A", result.PropA);
            Assert.IsNull(result.PropB);
            Assert.AreEqual("Prop D", result.PropD);
        }
    }
}