using static Mastersign.ConfigModel.Test.TestData;

namespace Mastersign.ConfigModel.Test
{
    [TestClass]
    public class IndirectTypeDiscriminationByIndicatorPropertyTest
    {
        private static readonly string SCENARIO = "TypeDiscrimination";


        [TestMethod]
        public void IndirectTypeDiscriminationByExistenceTest()
        {
            var mgr = new ConfigModelManager<RootModel>();
            var modelFile = GetTestDataFilePath(SCENARIO, "IndirectByPropertyExistence.yaml");
            mgr.AddLayer(modelFile);
            var root = mgr.LoadModel();
            var result = root.Base as ModelA;
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result.Child, typeof(ChildModelA));
        }

        class RootModel
        {
            public BaseModel? Base { get; set; }
        }

        abstract class BaseModel
        {
        }

        class ModelA : BaseModel
        {
            [TypeIndicator]
            public string? A { get; set; }

            public ChildBaseModel? Child { get; set; }
        }

        abstract class ChildBaseModel
        {
        }

        class ChildModelA : ChildBaseModel
        {
            [TypeIndicator]
            public string? ChildA { get; set; }
        }
    }
}
