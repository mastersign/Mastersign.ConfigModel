using System.Data;
using static Mastersign.ConfigModel.Test.TestData;

namespace Mastersign.ConfigModel.Test
{
    [TestClass]
    public class ReadErrors
    {
        private static readonly string SCENARIO = "ReadErrors";

        [TestMethod]
        public void FileBlockedTest()
        {
            var modelFile = GetTestDataFilePath(SCENARIO, "BlockedFile.yaml");
            using var block = File.Open(modelFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

            var mgr = new ConfigModelManager<Model>();
            mgr.AddLayer(modelFile);

            try
            {
                mgr.LoadModel();
                Assert.Fail("Should throw " + nameof(ConfigModelLayerLoadException));
            }
            catch (ConfigModelLayerLoadException ex)
            {
                Console.WriteLine(ex.Message);
                Assert.AreEqual(modelFile, ex.FileName);
            }
        }

        [TestMethod]
        public void IncludeBlockedTest()
        {
            var modelFile = GetTestDataFilePath(SCENARIO, "WithBlockedInclude.yaml");
            var includeFile = GetTestDataFilePath(SCENARIO, "BlockedFile.yaml");
            using var block = File.Open(includeFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

            var mgr = new ConfigModelManager<Model>();
            mgr.AddLayer(modelFile);

            try
            {
                mgr.LoadModel();
                Assert.Fail("Should throw " + nameof(ConfigModelIncludeLoadException));
            }
            catch (ConfigModelIncludeLoadException ex)
            {
                Console.WriteLine(ex.Message);
                Assert.AreEqual(modelFile, ex.ModelFile);
                Assert.AreEqual(includeFile, ex.FileName);
            }
        }

        [TestMethod]
        public void StringSourceBlockedTest()
        {
            var modelFile = GetTestDataFilePath(SCENARIO, "WithBlockedStringSource.yaml");
            var stringSourceFile = GetTestDataFilePath(SCENARIO, Path.Combine("Strings", "BlockedFile.txt"));
            using var block = File.Open(stringSourceFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

            var mgr = new ConfigModelManager<Model>();
            mgr.AddLayer(modelFile);

            try
            {
                mgr.LoadModel();
                Assert.Fail("Should throw " + nameof(ConfigModelStringSourceLoadException));
            }
            catch (ConfigModelStringSourceLoadException ex)
            {
                Console.WriteLine(ex.Message);
                Assert.AreEqual(modelFile, ex.ModelFile);
                Assert.AreEqual(stringSourceFile, ex.FileName);
            }
        }

        [TestMethod]
        public void FileSyntaxErrorTest()
        {
            var modelFile = GetTestDataFilePath(SCENARIO, "SyntaxError.yaml");

            var mgr = new ConfigModelManager<Model>();
            mgr.AddLayer(modelFile);

            try
            {
                mgr.LoadModel();
                Assert.Fail("Should throw " + nameof(ConfigModelLayerLoadException));
            }
            catch (ConfigModelLayerLoadException ex)
            {
                Console.WriteLine(ex.Message);
                Assert.AreEqual(modelFile, ex.FileName);
                Assert.IsInstanceOfType(ex.InnerException, typeof(YamlDotNet.Core.SyntaxErrorException));
            }
        }

        [TestMethod]
        public void FileSemanticErrorTest()
        {
            var modelFile = GetTestDataFilePath(SCENARIO, "SemanticError.yaml");

            var mgr = new ConfigModelManager<Model>();
            mgr.AddLayer(modelFile);

            try
            {
                mgr.LoadModel();
                Assert.Fail("Should throw " + nameof(ConfigModelLayerLoadException));
            }
            catch (ConfigModelLayerLoadException ex)
            {
                Console.WriteLine(ex.Message);
                Assert.AreEqual(modelFile, ex.FileName);
                Assert.IsInstanceOfType(ex.InnerException, typeof(YamlDotNet.Core.SemanticErrorException));
            }
        }

        [TestMethod]
        public void FileInvalidAnchorReferenceTest()
        {
            var modelFile = GetTestDataFilePath(SCENARIO, "InvalidAnchor.yaml");

            var mgr = new ConfigModelManager<Model>();
            mgr.AddLayer(modelFile);

            try
            {
                mgr.LoadModel();
                Assert.Fail("Should throw " + nameof(ConfigModelLayerLoadException));
            }
            catch (ConfigModelLayerLoadException ex)
            {
                Console.WriteLine(ex.Message);
                Assert.AreEqual(modelFile, ex.FileName);
                Assert.IsInstanceOfType(ex.InnerException, typeof(YamlDotNet.Core.AnchorNotFoundException));
            }
        }

        [TestMethod]
        public void FileSyntaxErrorInIncludeTest()
        {
            var modelFile = GetTestDataFilePath(SCENARIO, "WithIncludedSyntaxError.yaml");

            var mgr = new ConfigModelManager<Model>();
            mgr.AddLayer(modelFile);

            try
            {
                mgr.LoadModel();
                Assert.Fail("Should throw " + nameof(ConfigModelIncludeLoadException));
            }
            catch (ConfigModelIncludeLoadException ex)
            {
                Console.WriteLine(ex.Message);
                Assert.AreEqual(modelFile, ex.ModelFile);
                Assert.AreEqual(GetTestDataFilePath(SCENARIO, "SyntaxError.yaml"), ex.FileName);
                Assert.IsInstanceOfType(ex.InnerException, typeof(YamlDotNet.Core.SyntaxErrorException));
            }
        }

        [TestMethod]
        public void FileSemanticErrorInIncludeTest()
        {
            var modelFile = GetTestDataFilePath(SCENARIO, "WithIncludedSemanticError.yaml");

            var mgr = new ConfigModelManager<Model>();
            mgr.AddLayer(modelFile);

            try
            {
                mgr.LoadModel();
                Assert.Fail("Should throw " + nameof(ConfigModelIncludeLoadException));
            }
            catch (ConfigModelIncludeLoadException ex)
            {
                Console.WriteLine(ex.Message);
                Assert.AreEqual(modelFile, ex.ModelFile);
                Assert.AreEqual(GetTestDataFilePath(SCENARIO, "SemanticError.yaml"), ex.FileName);
                Assert.IsInstanceOfType(ex.InnerException, typeof(YamlDotNet.Core.SemanticErrorException));
            }
        }

        [TestMethod]
        public void FileInvalidAnchorReferenceInIncludeTest()
        {
            var modelFile = GetTestDataFilePath(SCENARIO, "WithIncludedInvalidAnchor.yaml");

            var mgr = new ConfigModelManager<Model>();
            mgr.AddLayer(modelFile);

            try
            {
                mgr.LoadModel();
                Assert.Fail("Should throw " + nameof(ConfigModelIncludeLoadException));
            }
            catch (ConfigModelIncludeLoadException ex)
            {
                Console.WriteLine(ex.Message);
                Assert.AreEqual(modelFile, ex.ModelFile);
                Assert.AreEqual(GetTestDataFilePath(SCENARIO, "InvalidAnchor.yaml"), ex.FileName);
                Assert.IsInstanceOfType(ex.InnerException, typeof(YamlDotNet.Core.AnchorNotFoundException));
            }
        }

        [MergableConfigModel]
        class Model : ConfigModelBase
        {
            public string? X { get; set; }
        }
    }
}
