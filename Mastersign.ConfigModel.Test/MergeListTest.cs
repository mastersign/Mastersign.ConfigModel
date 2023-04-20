namespace Mastersign.ConfigModel.Test
{
    [TestClass]
    public class MergeListTest
    {
        private static List<T> CloneList<T>(List<T> l)
            where T: ICloneable
            => l.Select(x => (T)x.Clone()).ToList();

        private static List<T> CopyList<T>(List<T> l)
            where T : struct
            => new(l);

        [TestMethod]
        public void Clear()
        {
            var target = new List<int> { 1, 2, 3 };
            var source = new List<int> { 4, 5 };
            var sourceCopy = CopyList(source);
            var expected = new List<int> { 4, 5 };
            Merging.MergeList(target, source, typeof(int), ListMergeMode.Clear);
            CollectionAssert.AreEqual(sourceCopy, source);
            CollectionAssert.AreEqual(expected, target);
        }

        [TestMethod]
        public void Append()
        {
            var target = new List<int> { 1, 2, 3 };
            var source = new List<int> { 4, 5 };
            var sourceCopy = CopyList(source);
            var expected = new List<int> { 1, 2, 3, 4, 5 };
            Merging.MergeList(target, source, typeof(int), ListMergeMode.Append);
            CollectionAssert.AreEqual(sourceCopy, source);
            CollectionAssert.AreEqual(expected, target);
        }

        [TestMethod]
        public void Prepend()
        {
            var target = new List<int> { 1, 2, 3 };
            var source = new List<int> { 4, 5 };
            var sourceCopy = CopyList(source);
            var expected = new List<int> { 4, 5, 1, 2, 3 };
            Merging.MergeList(target, source, typeof(int), ListMergeMode.Prepend);
            CollectionAssert.AreEqual(sourceCopy, source);
            CollectionAssert.AreEqual(expected, target);
        }

        [TestMethod]
        public void ReplaceItems()
        {
            var target = new List<int> { 1, 2, 3 };
            var source = new List<int> { 4, 5, 6 };
            var sourceCopy = CopyList(source);
            var expected = new List<int> { 4, 5, 6 };
            Merging.MergeList(target, source, typeof(int), ListMergeMode.ReplaceItem);
            CollectionAssert.AreEqual(sourceCopy, source);
            CollectionAssert.AreEqual(expected, target);
        }

        [TestMethod]
        public void ReplaceItemsWithLess()
        {
            var target = new List<int> { 1, 2, 3 };
            var source = new List<int> { 4, 5 };
            var sourceCopy = CopyList(source);
            var expected = new List<int> { 4, 5, 3 };
            Merging.MergeList(target, source, typeof(int), ListMergeMode.ReplaceItem);
            CollectionAssert.AreEqual(sourceCopy, source);
            CollectionAssert.AreEqual(expected, target);
        }

        [TestMethod]
        public void ReplaceItemsWithMore()
        {
            var target = new List<int> { 1, 2 };
            var source = new List<int> { 4, 5, 6 };
            var sourceCopy = CopyList(source);
            var expected = new List<int> { 4, 5, 6 };
            Merging.MergeList(target, source, typeof(int), ListMergeMode.ReplaceItem);
            CollectionAssert.AreEqual(sourceCopy, source);
            CollectionAssert.AreEqual(expected, target);
        }

        [TestMethod]
        public void MergeItems()
        {
            var target = new List<TestModel> {
                new TestModel { A = "A1", B = "B1", C = "C1" },
                new TestModel { A = "A2", B = "B2", C = "C2" },
                new TestModel { A = "A3", B = "B3", C = "C3" },
            };
            var source = new List<TestModel> {
                new TestModel { A = "a1", B = "b1", C = "c1" },
                new TestModel { B = "b2" },
                new TestModel { C = "c3" },
            };
            var sourceCopy = CloneList(source);
            var expected = new List<TestModel> {
                new TestModel { A = "a1", B = "b1", C = "c1" },
                new TestModel { A = "A2", B = "b2", C = "C2" },
                new TestModel { A = "A3", B = "B3", C = "c3" },
            };
            Merging.MergeList(target, source, typeof(TestModel), ListMergeMode.MergeItem);
            CollectionAssert.AreEqual(sourceCopy, source);
            CollectionAssert.AreEqual(expected, target);
        }

        [TestMethod]
        public void MergeItemsWithLess()
        {
            var target = new List<TestModel> {
                new TestModel { A = "A1", B = "B1", C = "C1" },
                new TestModel { A = "A2", B = "B2", C = "C2" },
                new TestModel { A = "A3", B = "B3", C = "C3" },
            };
            var source = new List<TestModel> {
                new TestModel { A = "a1", B = "b1", C = "c1" },
                new TestModel { B = "b2" },
            };
            var sourceCopy = CloneList(source);
            var expected = new List<TestModel> {
                new TestModel { A = "a1", B = "b1", C = "c1" },
                new TestModel { A = "A2", B = "b2", C = "C2" },
                new TestModel { A = "A3", B = "B3", C = "C3" },
            };
            Merging.MergeList(target, source, typeof(TestModel), ListMergeMode.MergeItem);
            CollectionAssert.AreEqual(sourceCopy, source);
            CollectionAssert.AreEqual(expected, target);
        }

        [TestMethod]
        public void MergeItemsWithMore()
        {
            var target = new List<TestModel> {
                new TestModel { A = "A1", B = "B1", C = "C1" },
                new TestModel { A = "A2", B = "B2", C = "C2" },
            };
            var source = new List<TestModel> {
                new TestModel { A = "a1", B = "b1", C = "c1" },
                new TestModel { B = "b2" },
                new TestModel { A = "a3", B = "b3", C = "c3" },
            };
            var sourceCopy = CloneList(source);
            var expected = new List<TestModel> {
                new TestModel { A = "a1", B = "b1", C = "c1" },
                new TestModel { A = "A2", B = "b2", C = "C2" },
                new TestModel { A = "a3", B = "b3", C = "c3" },
            };
            Merging.MergeList(target, source, typeof(TestModel), ListMergeMode.MergeItem);
            CollectionAssert.AreEqual(sourceCopy, source);
            CollectionAssert.AreEqual(expected, target);
        }

        [TestMethod]
        public void MergeMergableItems()
        {
            var target = new List<MergableTestModel> {
                new MergableTestModel { A = "A1", B = "B1", C = "C1" },
                new MergableTestModel { A = "A2", B = "B2", C = "C2" },
                new MergableTestModel { A = "A3", B = "B3", C = "C3" },
            };
            var source = new List<MergableTestModel> {
                new MergableTestModel { A = "a1", B = "b1", C = "c1" },
                new MergableTestModel { B = "b2", C = "c2" },
                new MergableTestModel { A = "a3", C = "c3" },
            };
            var sourceCopy = CloneList(source);
            var expected = new List<MergableTestModel> {
                new MergableTestModel { A = "a1", B = "b1", C = "C1" },
                new MergableTestModel { B = "b2", C = "C2" },
                new MergableTestModel { A = "a3", C = "C3" },
            };
            Merging.MergeList(target, source, typeof(MergableTestModel), ListMergeMode.MergeItem);
            CollectionAssert.AreEqual(sourceCopy, source);
            CollectionAssert.AreEqual(expected, target);
        }

        class TestModel : ICloneable
        {
            public string? A { get; set; }
            public string? B { get; set; }
            public string? C { get; set; }

            public virtual object Clone() => new TestModel { A = A, B = B, C = C };
            public override bool Equals(object? obj) => obj is TestModel model && A == model.A && B == model.B && C == model.C;
            public override int GetHashCode() => HashCode.Combine(A, B, C);
        }

        class MergableTestModel : TestModel, IMergableConfigModel
        {
            public void UpdateWith(object source, bool forceDeepMerge)
            {
                if (source == null) throw new ArgumentNullException(nameof(source));
                if (source is not MergableTestModel s) throw new ArgumentOutOfRangeException(nameof(source));
                A = s.A;
                B = s.B;
            }
            public override object Clone() => new MergableTestModel { A = A, B = B, C = C };
        }
    }
}
