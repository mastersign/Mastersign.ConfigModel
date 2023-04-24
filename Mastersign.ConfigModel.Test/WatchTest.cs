using static Mastersign.ConfigModel.Test.TestData;

namespace Mastersign.ConfigModel.Test
{
    [TestClass]
    public class WatchTest
    {
        private static readonly string SCENARIO = "Watching";

        private static void Backup(string fileName)
        {
            var bakFileName = fileName + ".bak";
            if (File.Exists(bakFileName)) File.Delete(bakFileName);
            File.Copy(fileName, bakFileName);
        }

        private static void Restore(string fileName)
        {
            var bakFileName = fileName + ".bak";
            if (!File.Exists(bakFileName)) return;
            if (File.Exists (fileName)) File.Delete(fileName);
            File.Move(bakFileName, fileName);
        }

        [TestMethod]
        public void RootModelChangeTest()
        {
            var mgr = new ConfigModelManager<Model>();
            var modelFile = GetTestDataFilePath(SCENARIO, "Root.yaml");
            var bakFile = modelFile + ".bak";
            mgr.AddLayer(modelFile);
            mgr.ReloadDelay = TimeSpan.FromMilliseconds(150);

            var m1 = mgr.LoadModel();

            Model? m2 = null;
            var count = 0;

            mgr.ModelChanged += (s, e) =>
            {
                count++;
                m2 = e.NewModel;
            };

            mgr.WatchAndReload();
            try
            {
                Backup(modelFile);
                File.AppendAllText(modelFile, "\r\nX: Changed X\r\n");
                Thread.Sleep(200);
                Assert.IsNotNull(m2);
                Assert.AreEqual(1, count);
                Assert.AreEqual("Changed X", m2.X);
            }
            finally
            {
                Restore(modelFile);
            }
        }

        [MergableConfigModel]
        class Model : ConfigModelBase
        {
            public string? X { get; set; }

            public ChildModel? Child { get; set; }
        }

        [MergableConfigModel]
        class ChildModel : ConfigModelBase
        {
            public string? Y { get; set; }
        }
    }
}
