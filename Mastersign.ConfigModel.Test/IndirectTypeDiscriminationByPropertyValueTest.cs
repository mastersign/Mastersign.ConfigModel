using static Mastersign.ConfigModel.Test.TestData;

namespace Mastersign.ConfigModel.Test
{
    [TestClass]
    public class IndirectTypeDiscriminationByPropertyValueTest
    {
        private static readonly string SCENARIO = "TypeDiscrimination";


        [TestMethod]
        public void IndirectTypeDiscriminationByValueTest()
        {
            var mgr = new ConfigModelManager<BaseModel>();
            var modelFile = GetTestDataFilePath(SCENARIO, "IndirectByPropertyValue.yaml");
            mgr.AddLayer(modelFile);
            var result = mgr.LoadModel() as ModelA;
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result.Child, typeof(ChildModelA));
        }

        class BaseModel
        {
            [TypeDiscriminator]
            public string? Type { get; set; }
        }

        [TypeDiscriminationValue("Class A")]
        class ModelA : BaseModel
        {
            public ChildBaseModel? Child { get; set; }
        }

        abstract class ChildBaseModel
        {
            [TypeDiscriminator]
            public string? ChildType { get; set; }
        }

        [TypeDiscriminationValue("Child Class A")]
        class ChildModelA : ChildBaseModel
        {
        }
    }
}
