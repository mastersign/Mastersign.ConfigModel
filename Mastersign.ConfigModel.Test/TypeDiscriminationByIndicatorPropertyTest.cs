using static Mastersign.ConfigModel.Test.TestData;

namespace Mastersign.ConfigModel.Test
{
    [TestClass]
    public class TypeDiscriminationByIndicatorPropertyTest
    {
        private static readonly string SCENARIO = "TypeDiscrimination";

        [TestMethod]
        public void BasicTest()
        {
            var mgrA = new ConfigModelManager<BaseModel>();
            var modelFileA = GetTestDataFilePath(SCENARIO, "ByPropertyExistenceA.yaml");
            mgrA.AddLayer(modelFileA);
            var resultA = mgrA.LoadModel();
            Assert.AreEqual(typeof(ModelA), resultA.GetType());

            var mgrB = new ConfigModelManager<BaseModel>();
            var modelFileB = GetTestDataFilePath(SCENARIO, "ByPropertyExistenceB.yaml");
            mgrB.AddLayer(modelFileB);
            var resultB = mgrB.LoadModel();
            Assert.AreEqual(typeof(ModelB), resultB.GetType());
        }

        [TestMethod]
        public void WithoutKeyTest()
        {
            var mgr = new ConfigModelManager<BaseModel>();
            var modelFile = GetTestDataFilePath(SCENARIO, "ByPropertyExistenceNone.yaml");
            mgr.AddLayer(modelFile);
            var result = mgr.LoadModel();
            Assert.AreEqual(typeof(BaseModel), result.GetType());
        }

        [TestMethod]
        public void WithMultipleKeysTest()
        {
            var mgr = new ConfigModelManager<BaseModel>();
            var modelFile = GetTestDataFilePath(SCENARIO, "ByPropertyExistenceMultiple.yaml");
            mgr.AddLayer(modelFile);
            var result = mgr.LoadModel();
            Assert.AreEqual(typeof(ModelB), result.GetType());
        }

        [TestMethod]
        public void WithAbstractBaseClassTest()
        {
            var mgr = new ConfigModelManager<RootModelForAbstract>();
            var modelFile = GetTestDataFilePath(SCENARIO, "ByPropertyExistenceAbstractC.yaml");
            mgr.AddLayer(modelFile);
            var result = mgr.LoadModel();
            Assert.AreEqual(typeof(ModelC), result.Abstract?.GetType());
        }

        [TestMethod]
        public void WithAbstractBaseWithoutKeyTest()
        {
            var mgr = new ConfigModelManager<RootModelForAbstract>();
            var modelFile = GetTestDataFilePath(SCENARIO, "ByPropertyExistenceAbstractNone.yaml");
            mgr.AddLayer(modelFile);
            RootModelForAbstract result;
            Assert.ThrowsException<YamlDotNet.Core.YamlException>(() => result = mgr.LoadModel());
        }

        class BaseModel
        {
            public string? X { get; set; }
        }

        class ModelA : BaseModel
        {
            [TypeIndicator]
            public string? A { get; set; }
        }

        class ModelB : BaseModel
        {
            [TypeIndicator]
            public string? B { get; set; }
        }

        class RootModelForAbstract
        {
            public AbstractBaseModel? Abstract { get; set; }
        }

        abstract class AbstractBaseModel
        {
            public string? X { get; set; }
        }

        class ModelC : AbstractBaseModel
        {
            [TypeIndicator]
            public string? C { get; set; }
        }
    }
}
