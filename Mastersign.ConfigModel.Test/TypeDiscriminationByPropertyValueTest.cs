﻿using static Mastersign.ConfigModel.Test.TestData;

namespace Mastersign.ConfigModel.Test
{
    [TestClass]
    public class TypeDiscriminationByPropertyValueTest
    {
        private static readonly string SCENARIO = "TypeDiscrimination";

        [TestMethod]
        public void BasicTest()
        {
            var mgrA = new ConfigModelManager<BaseModel>();
            var modelFileA = GetTestDataFilePath(SCENARIO, "ByPropertyValueA.yaml");
            mgrA.AddLayer(modelFileA);
            var resultA = mgrA.LoadModel();
            Assert.IsInstanceOfType(resultA, typeof(ModelA));

            var mgrB = new ConfigModelManager<BaseModel>();
            var modelFileB = GetTestDataFilePath(SCENARIO, "ByPropertyValueB.yaml");
            mgrB.AddLayer(modelFileB);
            var resultB = mgrB.LoadModel();
            Assert.IsInstanceOfType(resultB, typeof(ModelB));
        }

        [TestMethod]
        public void WithNamingConventionTest()
        {
            var mgrA = new ConfigModelManager<BaseModel>(
                propertyNameHandling: PropertyNameHandling.CamelCase);
            var modelFileA = GetTestDataFilePath(SCENARIO, "ByPropertyValueWithNamingConvention.yaml");
            mgrA.AddLayer(modelFileA);
            var resultA = mgrA.LoadModel();
            Assert.IsInstanceOfType(resultA, typeof(ModelA));
        }

        [TestMethod]
        public void WithoutKeyTest()
        {
            var mgr = new ConfigModelManager<BaseModel>();
            var modelFile = GetTestDataFilePath(SCENARIO, "ByPropertyValueNone.yaml");
            mgr.AddLayer(modelFile);
            var result = mgr.LoadModel();
            Assert.IsInstanceOfType(result, typeof(BaseModel));
        }

        [TestMethod]
        public void WithInvalidKeyTest()
        {
            var mgr = new ConfigModelManager<BaseModel>();
            var modelFile = GetTestDataFilePath(SCENARIO, "ByPropertyValueInvalid.yaml");
            mgr.AddLayer(modelFile);
            var result = mgr.LoadModel();
            Assert.IsInstanceOfType(result, typeof(BaseModel));
        }

        [TestMethod]
        public void WithAbstractBaseClassTest()
        {
            var mgr = new ConfigModelManager<RootModelForAbstract>();
            var modelFile = GetTestDataFilePath(SCENARIO, "ByPropertyValueAbstractC.yaml");
            mgr.AddLayer(modelFile);
            var result = mgr.LoadModel();
            Assert.IsInstanceOfType(result.Abstract, typeof(ModelC));
        }

        [TestMethod]
        public void WithAbstractBaseWithInvalidKeyTest()
        {
            var mgr = new ConfigModelManager<RootModelForAbstract>();
            var modelFile = GetTestDataFilePath(SCENARIO, "ByPropertyValueAbstractInvalid.yaml");
            mgr.AddLayer(modelFile);
            RootModelForAbstract result;
            Assert.ThrowsException<ConfigModelLayerLoadException>(() => result = mgr.LoadModel());
        }

        [TestMethod]
        public void WithAbstractBaseWithoutKeyTest()
        {
            var mgr = new ConfigModelManager<RootModelForAbstract>();
            var modelFile = GetTestDataFilePath(SCENARIO, "ByPropertyValueAbstractNone.yaml");
            mgr.AddLayer(modelFile);
            RootModelForAbstract result;
            Assert.ThrowsException<ConfigModelLayerLoadException>(() => result = mgr.LoadModel());
        }

        class BaseModel
        {
            [TypeDiscriminator]
            public string? Type { get; set; }

            public string? X { get; set; }
        }

        [TypeDiscriminationValue("Class A")]
        class ModelA : BaseModel { }

        [TypeDiscriminationValue("Class B")]
        class ModelB : BaseModel { }

        class RootModelForAbstract
        {
            public AbstractBaseModel? Abstract { get; set; }
        }

        abstract class AbstractBaseModel
        {
            [TypeDiscriminator]
            public string? Type { get; set; }

            public string? X { get; set; }
        }

        [TypeDiscriminationValue("Class C")]
        class ModelC : AbstractBaseModel { }
    }
}
