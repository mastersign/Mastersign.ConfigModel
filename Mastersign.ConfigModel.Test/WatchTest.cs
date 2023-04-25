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
            if (File.Exists(fileName)) File.Delete(fileName);
            File.Move(bakFileName, fileName);
        }

        [TestMethod]
        public void RootModelChangeTest()
        {
            var mgr = new ConfigModelManager<Model>();
            var modelFile = GetTestDataFilePath(SCENARIO, "Root.yaml");
            mgr.AddLayer(modelFile);
            mgr.ReloadDelay = TimeSpan.FromMilliseconds(150);

            var m1 = mgr.LoadModel();

            Assert.IsNotNull(m1);
            Assert.AreEqual("X from File", m1.X);
            Assert.IsNotNull(m1.Child);
            Assert.AreEqual("Y from File", m1.Child.Y);

            Model? m2 = null;
            var count = 0;
            mgr.ModelChanged += (s, e) =>
            {
                count++;
                m2 = e.NewModel;
            };

            Exception? ex = null;
            mgr.ModelReloadFailed += (s, e) =>
            {
                ex = e.Exception;
            };

            mgr.WatchAndReload();
            try
            {
                Backup(modelFile);
                File.AppendAllText(modelFile, "\r\nX: Changed X\r\n");
                Assert.AreEqual(0, count);
                Thread.Sleep(75);
                Assert.AreEqual(0, count);
                Thread.Sleep(100);
                Assert.IsNull(ex);
                Assert.IsNotNull(m2);
                Assert.AreEqual(1, count);
                Assert.AreEqual("Changed X", m2.X);
            }
            finally
            {
                Restore(modelFile);
            }
        }

        [TestMethod]
        public void StringSourceChangeTest()
        {
            var mgr = new ConfigModelManager<Model>();
            var modelFile = GetTestDataFilePath(SCENARIO, "Root.yaml");
            var stringSourceFile = GetTestDataFilePath(SCENARIO, "Strings", "x.txt");
            mgr.AddLayer(modelFile);
            mgr.ReloadDelay = TimeSpan.FromMilliseconds(150);

            var m1 = mgr.LoadModel();

            Assert.IsNotNull(m1);
            Assert.AreEqual("X from File", m1.X);
            Assert.IsNotNull(m1.Child);
            Assert.AreEqual("Y from File", m1.Child.Y);

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
                Backup(stringSourceFile);
                File.WriteAllText(stringSourceFile, "Changed X");
                Thread.Sleep(200);
                Assert.IsNotNull(m2);
                Assert.AreEqual(1, count);
                Assert.AreEqual("Changed X", m2.X);
            }
            finally
            {
                Restore(stringSourceFile);
            }
        }

        [TestMethod]
        public void IncludeChangeTest()
        {
            var mgr = new ConfigModelManager<Model>();
            var modelFile = GetTestDataFilePath(SCENARIO, "Root.yaml");
            var includeFile = GetTestDataFilePath(SCENARIO, "Include.yaml");
            mgr.AddLayer(modelFile);
            mgr.ReloadDelay = TimeSpan.FromMilliseconds(150);

            var m1 = mgr.LoadModel();

            Assert.IsNotNull(m1);
            Assert.AreEqual("X from File", m1.X);
            Assert.IsNotNull(m1.Child);
            Assert.AreEqual("Y from File", m1.Child.Y);

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
                Backup(includeFile);
                File.WriteAllText(includeFile, "Child:\r\n  Y: Changed Y");
                Thread.Sleep(200);
                Assert.IsNotNull(m2);
                Assert.AreEqual(1, count);
                Assert.IsNotNull(m2.Child);
                Assert.AreEqual("Changed Y", m2.Child.Y);
            }
            finally
            {
                Restore(includeFile);
            }
        }

        [TestMethod]
        public void NestedIncludeChangeTest()
        {
            var mgr = new ConfigModelManager<Model>();
            var modelFile = GetTestDataFilePath(SCENARIO, "Root.yaml");
            var nestedIncludeFile = GetTestDataFilePath(SCENARIO, "NestedInclude.yaml");
            mgr.AddLayer(modelFile);
            mgr.ReloadDelay = TimeSpan.FromMilliseconds(150);

            var m1 = mgr.LoadModel();

            Assert.IsNotNull(m1);
            Assert.AreEqual("X from File", m1.X);
            Assert.IsNotNull(m1.Child);
            Assert.AreEqual("Y from File", m1.Child.Y);

            Model? m2 = null;
            var count = 0;
            mgr.ModelChanged += (s, e) =>
            {
                count++;
                m2 = e.NewModel;
            };

            Exception? ex = null;
            mgr.ModelReloadFailed += (s, e) =>
            {
                ex = e.Exception;
            };

            mgr.WatchAndReload();
            try
            {
                Backup(nestedIncludeFile);
                File.WriteAllText(nestedIncludeFile, "Y: Changed Y");
                Thread.Sleep(200);
                Assert.IsNull(ex);
                Assert.IsNotNull(m2);
                Assert.AreEqual(1, count);
                Assert.IsNotNull(m2.Child);
                Assert.AreEqual("Changed Y", m2.Child.Y);
            }
            finally
            {
                Restore(nestedIncludeFile);
            }
        }

        [TestMethod]
        public void IncludedStringSourceChangeTest()
        {
            var mgr = new ConfigModelManager<Model>();
            var modelFile = GetTestDataFilePath(SCENARIO, "Root.yaml");
            var stringSourceFile = GetTestDataFilePath(SCENARIO, "Strings", "y.txt");
            mgr.AddLayer(modelFile);
            mgr.ReloadDelay = TimeSpan.FromMilliseconds(150);

            var m1 = mgr.LoadModel();

            Assert.IsNotNull(m1);
            Assert.AreEqual("X from File", m1.X);
            Assert.IsNotNull(m1.Child);
            Assert.AreEqual("Y from File", m1.Child.Y);

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
                Backup(stringSourceFile);
                File.WriteAllText(stringSourceFile, "Changed Y");
                Thread.Sleep(200);
                Assert.IsNotNull(m2);
                Assert.AreEqual(1, count);
                Assert.IsNotNull(m2.Child);
                Assert.AreEqual("Changed Y", m2.Child.Y);
            }
            finally
            {
                Restore(stringSourceFile);
            }
        }

        [TestMethod]
        public void ReadErrorBlockedChangeTest()
        {
            var mgr = new ConfigModelManager<Model>();
            var modelFile = GetTestDataFilePath(SCENARIO, "Root.yaml");
            var blockedStringSourceFile = GetTestDataFilePath(SCENARIO, "Strings", "x.txt");
            var changedStringSourceFile = GetTestDataFilePath(SCENARIO, "Strings", "y.txt");
            mgr.AddLayer(modelFile);
            mgr.ReloadDelay = TimeSpan.FromMilliseconds(150);

            var m1 = mgr.LoadModel();

            Assert.IsNotNull(m1);
            Assert.AreEqual("X from File", m1.X);
            Assert.IsNotNull(m1.Child);
            Assert.AreEqual("Y from File", m1.Child.Y);

            Model? m2 = null;
            var loadCount = 0;
            mgr.ModelChanged += (s, e) =>
            {
                loadCount++;
                m2 = e.NewModel;
            };

            var errorCount = 0;
            Exception? ex = null;
            mgr.ModelReloadFailed += (s, e) =>
            {
                errorCount++;
                ex = e.Exception;
            };

            using var block = File.Open(blockedStringSourceFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

            mgr.WatchAndReload();
            try
            {
                Backup(changedStringSourceFile);
                File.WriteAllText(changedStringSourceFile, "Changed Y");
                Thread.Sleep(200);
                Assert.AreEqual(1, errorCount);
                Assert.IsNotNull(ex);
                Assert.IsInstanceOfType(ex, typeof(ConfigModelStringSourceLoadException));
                Assert.AreEqual(0, loadCount);
                Assert.IsNull(m2);
            }
            finally
            {
                block.Dispose();
                Restore(changedStringSourceFile);
            }
        }

        [TestMethod]
        public void ReadErrorMissingFileTest()
        {
            var mgr = new ConfigModelManager<Model>();
            var modelFile = GetTestDataFilePath(SCENARIO, "Root.yaml");
            var includeFile = GetTestDataFilePath(SCENARIO, "Include.yaml");
            mgr.AddLayer(modelFile);
            mgr.ReloadDelay = TimeSpan.FromMilliseconds(150);

            var m1 = mgr.LoadModel();

            Assert.IsNotNull(m1);
            Assert.AreEqual("X from File", m1.X);
            Assert.IsNotNull(m1.Child);
            Assert.AreEqual("Y from File", m1.Child.Y);

            Model? m2 = null;
            var loadCount = 0;
            mgr.ModelChanged += (s, e) =>
            {
                loadCount++;
                m2 = e.NewModel;
            };

            var errorCount = 0;
            Exception? ex = null;
            mgr.ModelReloadFailed += (s, e) =>
            {
                errorCount++;
                ex = e.Exception;
            };

            mgr.WatchAndReload();
            try
            {
                Backup(includeFile);
                File.Delete(includeFile);
                Thread.Sleep(200);
                Assert.AreEqual(0, loadCount);
                Assert.IsNull(m2);
                Assert.AreEqual(1, errorCount);
                Assert.IsNotNull(ex);
                Assert.IsInstanceOfType(ex, typeof(ConfigModelIncludeNotFoundException));
            }
            finally
            {
                Restore(includeFile);
            }
        }

        [TestMethod]
        public void DeletionFastTest()
        {
            var mgr = new ConfigModelManager<Model>();
            var modelFile = GetTestDataFilePath(SCENARIO, "Deletion.yaml");
            var deletedFiles = (new[] { "Deleted1.yaml", "Deleted2.yaml" })
                .Select(f => GetTestDataFilePath(SCENARIO, f))
                .ToList();
            mgr.AddLayer(modelFile);
            mgr.ReloadDelay = TimeSpan.FromMilliseconds(150);

            var m1 = mgr.LoadModel();

            Assert.IsNotNull(m1);
            Assert.AreEqual("Deleted 2 X", m1.X);

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
                foreach (var f in deletedFiles) { Backup(f); }
                foreach (var f in deletedFiles) { File.Delete(f); }
                Assert.AreEqual(0, count);
                Thread.Sleep(75);
                Assert.AreEqual(0, count);
                Thread.Sleep(100);
                Assert.IsNotNull(m2);
                Assert.AreEqual(1, count);
                Assert.AreEqual("Base X", m2.X);
            }
            finally
            {
                foreach (var f in deletedFiles) { Restore(f); }
            }
        }


        [TestMethod]
        public void DeletionSnapshotsTest()
        {
            var mgr = new ConfigModelManager<Model>();
            var modelFile = GetTestDataFilePath(SCENARIO, "Deletion.yaml");
            var deletedFiles = (new[] { "Deleted1.yaml", "Deleted2.yaml" })
                .Select(f => GetTestDataFilePath(SCENARIO, f))
                .ToList();
            mgr.AddLayer(modelFile);
            mgr.ReloadDelay = TimeSpan.FromMilliseconds(150);

            var m1 = mgr.LoadModel();

            Assert.IsNotNull(m1);
            Assert.AreEqual("Deleted 2 X", m1.X);

            var snapshots = new List<Model>();

            mgr.ModelChanged += (s, e) =>
            {
                snapshots.Add(e.NewModel);
            };

            mgr.WatchAndReload();
            try
            {
                foreach (var f in deletedFiles) { Backup(f); }
                foreach (var f in deletedFiles.Reverse<string>()) { File.Delete(f); Thread.Sleep(200); }
                Assert.AreEqual(2, snapshots.Count);
                Assert.AreEqual("Deleted 1 X", snapshots[0].X);
                Assert.AreEqual("Base X", snapshots[1].X);
            }
            finally
            {
                foreach (var f in deletedFiles) { Restore(f); }
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
