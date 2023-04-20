using YamlDotNet.Serialization;
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
            var mgr = new ConfigModelManager<Model>(propertyNameHandling: PropertyNameHandling.PascalCase);
            var modelFile = GetTestDataFilePath(SCENARIO, "PascalCase.yaml");
            mgr.AddLayer(modelFile);
            var result = mgr.LoadModel();
            Assert.IsNotNull(result);
            Assert.AreEqual("Prop A", result.PropA);
            Assert.IsNull(result.PropB);
            Assert.AreEqual("Prop D", result.PropD);
        }

        [TestMethod]
        public void CamelCaseTest()
        {
            var mgr = new ConfigModelManager<Model>(propertyNameHandling: PropertyNameHandling.CamelCase);
            var modelFile = GetTestDataFilePath(SCENARIO, "CamelCase.yaml");
            mgr.AddLayer(modelFile);
            var result = mgr.LoadModel();
            Assert.IsNotNull(result);
            Assert.AreEqual("Prop A", result.PropA);
            Assert.IsNull(result.PropB);
            Assert.AreEqual("Prop D", result.PropD);
        }

        [TestMethod]
        public void UnderscoredTest()
        {
            var mgr = new ConfigModelManager<Model>(propertyNameHandling: PropertyNameHandling.Underscored);
            var modelFile = GetTestDataFilePath(SCENARIO, "Underscored.yaml");
            mgr.AddLayer(modelFile);
            var result = mgr.LoadModel();
            Assert.IsNotNull(result);
            Assert.AreEqual("Prop A", result.PropA);
            Assert.IsNull(result.PropB);
            Assert.AreEqual("Prop D", result.PropD);
        }

        [TestMethod]
        public void HyphenatedTest()
        {
            var mgr = new ConfigModelManager<Model>(propertyNameHandling: PropertyNameHandling.Hyphenated);
            var modelFile = GetTestDataFilePath(SCENARIO, "Hyphenated.yaml");
            mgr.AddLayer(modelFile);
            var result = mgr.LoadModel();
            Assert.IsNotNull(result);
            Assert.AreEqual("Prop A", result.PropA);
            Assert.IsNull(result.PropB);
            Assert.AreEqual("Prop D", result.PropD);
        }

        class Model
        {
            public string? PropA { get; set; }
            public string? PropB { get; set; }
            public string? PropC { get; set; }

            [YamlMember(Alias = "prop_dee", ApplyNamingConventions = false)]
            public string? PropD { get; set; }
        }
    }
}