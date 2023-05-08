namespace Mastersign.ConfigModel.Test
{
    [TestClass]
    public class MergeDictionaryTest
    {
        private static Dictionary<string, T> CloneDict<T>(Dictionary<string, T> d)
            where T : ICloneable
            => new(d.Select(kvp => KeyValuePair.Create(kvp.Key, (T)kvp.Value.Clone())));

        private static Dictionary<string, T> CopyDict<T>(Dictionary<string, T> d)
            where T : struct
            => new(d);

        [TestMethod]
        public void ClearTest()
        {
            var target = new Dictionary<string, int> { { "X", 1 }, { "Y", 2 }, { "Z", 3 } };
            var source = new Dictionary<string, int> { { "X", 4 }, { "Y", 5 } };
            var sourceCopy = CopyDict(source);
            var expected = new Dictionary<string, int> { { "X", 4 }, { "Y", 5 } };
            Merging.MergeDictionary(target, source, typeof(int), DictionaryMergeMode.Clear);
            CollectionAssert.AreEqual(sourceCopy, source);
            CollectionAssert.AreEqual(expected, target);
        }

        [TestMethod]
        public void ReplaceValuesTest()
        {
            var target = new Dictionary<string, MergableTestModel> {
                { "X", new() { A = "XA", B = "XB", C = "XC" } },
                { "Y", new() { A = "YA", B = "YB", C = "YC" } },
                { "Z", new() { A = "ZA", B = "ZB", C = "ZC" } },
            };
            var source = new Dictionary<string, MergableTestModel> {
                { "X", new() { A = "Xa", B = "Xb", C = "Xc" } },
                { "Y", new() { A = "Ya" } },
                { "Z", new() { B = "Zb", C = "Zc" } },
            };
            var sourceCopy = CloneDict(source);
            var expected = new Dictionary<string, MergableTestModel> {
                { "X", new() { A = "Xa", B = "Xb", C = "Xc" } },
                { "Y", new() { A = "Ya" } },
                { "Z", new() { B = "Zb", C = "Zc" } },
            };
            Merging.MergeDictionary(target, source, typeof(MergableTestModel), DictionaryMergeMode.ReplaceValue);
            CollectionAssert.AreEqual(sourceCopy, source);
            CollectionAssert.AreEqual(expected, target);
        }

        [TestMethod]
        public void ReplaceValuesWithLessTest()
        {
            var target = new Dictionary<string, MergableTestModel> {
                { "X", new() { A = "XA", B = "XB", C = "XC" } },
                { "Y", new() { A = "YA", B = "YB", C = "YC" } },
                { "Z", new() { A = "ZA", B = "ZB", C = "ZC" } },
            };
            var source = new Dictionary<string, MergableTestModel> {
                { "X", new() { A = "Xa", B = "Xb", C = "Xc" } },
                { "Y", new() { A = "Ya" } },
            };
            var sourceCopy = CloneDict(source);
            var expected = new Dictionary<string, MergableTestModel> {
                { "X", new() { A = "Xa", B = "Xb", C = "Xc" } },
                { "Y", new() { A = "Ya" } },
                { "Z", new() { A = "ZA", B = "ZB", C = "ZC" } },
            };
            Merging.MergeDictionary(target, source, typeof(MergableTestModel), DictionaryMergeMode.ReplaceValue);
            CollectionAssert.AreEqual(sourceCopy, source);
            CollectionAssert.AreEqual(expected, target);
        }

        [TestMethod]
        public void ReplaceValuesWithMoreTest()
        {
            var target = new Dictionary<string, MergableTestModel> {
                { "X", new() { A = "XA", B = "XB", C = "XC" } },
                { "Y", new() { A = "YA", B = "YB", C = "YC" } },
            };
            var source = new Dictionary<string, MergableTestModel> {
                { "X", new() { A = "Xa", B = "Xb", C = "Xc" } },
                { "Y", new() { A = "Ya" } },
                { "Z", new() { B = "Zb", C = "Zc" } },
            };
            var sourceCopy = CloneDict(source);
            var expected = new Dictionary<string, MergableTestModel> {
                { "X", new() { A = "Xa", B = "Xb", C = "Xc" } },
                { "Y", new() { A = "Ya" } },
                { "Z", new() { B = "Zb", C = "Zc" } },
            };
            Merging.MergeDictionary(target, source, typeof(MergableTestModel), DictionaryMergeMode.ReplaceValue);
            CollectionAssert.AreEqual(sourceCopy, source);
            CollectionAssert.AreEqual(expected, target);
        }

        [TestMethod]
        public void ComplementsTest()
        {
            var target = new Dictionary<string, MergableTestModel> {
                { "X", new() { A = "XA", B = "XB", C = "XC" } },
                { "Y", new() { A = "YA", B = "YB", C = "YC" } },
            };
            var source = new Dictionary<string, MergableTestModel> {
                { "X", new() { A = "Xa", B = "Xb", C = "Xc" } },
                { "Y", new() { A = "Ya" } },
                { "Z", new() { B = "Zb", C = "Zc" } },
            };
            var sourceCopy = CloneDict(source);
            var expected = new Dictionary<string, MergableTestModel> {
                { "X", new() { A = "XA", B = "XB", C = "XC" } },
                { "Y", new() { A = "YA", B = "YB", C = "YC" } },
                { "Z", new() { B = "Zb", C = "Zc" } },
            };
            Merging.MergeDictionary(target, source, typeof(MergableTestModel), DictionaryMergeMode.Complement);
            CollectionAssert.AreEqual(sourceCopy, source);
            CollectionAssert.AreEqual(expected, target);
        }

        [TestMethod]
        public void MergeValuesTest()
        {
            var target = new Dictionary<string, TestModel> {
                { "X", new() { A = "XA", B = "XB", C = "XC" } },
                { "Y", new() { A = "YA", B = "YB", C = "YC" } },
                { "Z", new() { A = "ZA", B = "ZB", C = "ZC" } },
            };
            var source = new Dictionary<string, TestModel> {
                { "X", new() { A = "Xa", B = "Xb", C = "Xc" } },
                { "Y", new() { A = "Ya" } },
                { "Z", new() { B = "Zb", C = "Zc" } },
            };
            var sourceCopy = CloneDict(source);
            var expected = new Dictionary<string, TestModel> {
                { "X", new() { A = "Xa", B = "Xb", C = "Xc" } },
                { "Y", new() { A = "Ya", B = "YB", C = "YC" } },
                { "Z", new() { A = "ZA", B = "Zb", C = "Zc" } },
            };
            Merging.MergeDictionary(target, source, typeof(TestModel), DictionaryMergeMode.MergeValue);
            CollectionAssert.AreEqual(sourceCopy, source);
            CollectionAssert.AreEqual(expected, target);
        }

        [TestMethod]
        public void MergeValuesWithLessTest()
        {
            var target = new Dictionary<string, TestModel> {
                { "X", new() { A = "XA", B = "XB", C = "XC" } },
                { "Y", new() { A = "YA", B = "YB", C = "YC" } },
                { "Z", new() { A = "ZA", B = "ZB", C = "ZC" } },
            };
            var source = new Dictionary<string, TestModel> {
                { "X", new() { A = "Xa", B = "Xb", C = "Xc" } },
                { "Y", new() { A = "Ya" } },
            };
            var sourceCopy = CloneDict(source);
            var expected = new Dictionary<string, TestModel> {
                { "X", new() { A = "Xa", B = "Xb", C = "Xc" } },
                { "Y", new() { A = "Ya", B = "YB", C = "YC" } },
                { "Z", new() { A = "ZA", B = "ZB", C = "ZC" } },
            };
            Merging.MergeDictionary(target, source, typeof(TestModel), DictionaryMergeMode.MergeValue);
            CollectionAssert.AreEqual(sourceCopy, source);
            CollectionAssert.AreEqual(expected, target);
        }

        [TestMethod]
        public void MergeValuesWithMoreTest()
        {
            var target = new Dictionary<string, TestModel> {
                { "X", new() { A = "XA", B = "XB", C = "XC" } },
                { "Y", new() { A = "YA", B = "YB", C = "YC" } },
            };
            var source = new Dictionary<string, TestModel> {
                { "X", new() { A = "Xa", B = "Xb", C = "Xc" } },
                { "Y", new() { A = "Ya" } },
                { "Z", new() { B = "Zb", C = "Zc" } },
            };
            var sourceCopy = CloneDict(source);
            var expected = new Dictionary<string, TestModel> {
                { "X", new() { A = "Xa", B = "Xb", C = "Xc" } },
                { "Y", new() { A = "Ya", B = "YB", C = "YC" } },
                { "Z", new() { B = "Zb", C = "Zc" } },
            };
            Merging.MergeDictionary(target, source, typeof(TestModel), DictionaryMergeMode.MergeValue);
            CollectionAssert.AreEqual(sourceCopy, source);
            CollectionAssert.AreEqual(expected, target);
        }

        [TestMethod]
        public void MergeMergableValuesTest()
        {
            var target = new Dictionary<string, MergableTestModel> {
                { "X", new() { A = "XA", B = "XB", C = "XC" } },
                { "Y", new() { A = "YA", B = "YB", C = "YC" } },
                { "Z", new() { A = "ZA", B = "ZB", C = "ZC" } },
            };
            var source = new Dictionary<string, MergableTestModel> {
                { "X", new() { A = "Xa", B = "Xb", C = "Xc" } },
                { "Y", new() { B = "Yb", C = "Yc" } },
                { "Z", new() { A = "Za", C = "Zc" } },
            };
            var sourceCopy = CloneDict(source);
            var expected = new Dictionary<string, MergableTestModel> {
                { "X", new() { A = "Xa", B = "Xb", C = "XC" } },
                { "Y", new() { B = "Yb", C = "YC" } },
                { "Z", new() { A = "Za", C = "ZC" } },
            };
            Merging.MergeDictionary(target, source, typeof(MergableTestModel), DictionaryMergeMode.MergeValue);
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
