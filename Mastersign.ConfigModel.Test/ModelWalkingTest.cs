namespace Mastersign.ConfigModel.Test
{
    [TestClass]
    public class ModelWalkingTest
    {
        [TestMethod]
        public void WalkModelTest()
        {
            var rootA = new TargetA(1);
            var rootBA = new TargetA(2);
            var rootB = new TargetB(3) { A = rootBA };
            var intermediateA = new TargetA(4);
            var intermediateB = new TargetB(5);
            var intermediateDictX = new TargetA(6);
            var intermediateDictY = new TargetA(7);
            var rootListA1 = new TargetA(8);
            var rootListA2 = new TargetA(9);

            var originalTargets = new List<ITarget> {
                rootA, 
                rootBA, rootB, // depth first
                intermediateA, intermediateB,
                intermediateDictX, intermediateDictY,
                rootListA1, rootListA2,
            };

            var root = new Model
            {
                A = rootA,
                B = rootB,
                Intermediate = new Intermediate
                {
                    A = intermediateA,
                    B = intermediateB,
                    Dict = new Dictionary<string, TargetA>
                    {
                        { "X", intermediateDictX },
                        { "Y", intermediateDictY },
                    },
                },
                List = new List<TargetA> {
                    rootListA1,
                    rootListA2,
                },
            };

            // check that all targets are visited in breadth first order
            var visitedBreadthFirst = new List<ITarget>();
            var rootResult = ModelWalking.WalkConfigModel<ITarget>(root, t =>
            {
                visitedBreadthFirst.Add(t);
                return t;
            });
            Assert.AreSame(root, rootResult);
            CollectionAssert.AreEqual(originalTargets, visitedBreadthFirst);

            // check that all targets are replaced
            var replaced = new List<ITarget>();
            rootResult = ModelWalking.WalkConfigModel<ITarget>(root, t =>
            {
                replaced.Add(t);
                return t.Clone();
            });
            Assert.AreSame(root, rootResult);
            CollectionAssert.AreEqual(originalTargets, replaced);
            var replacements = new List<ITarget>();
            ModelWalking.WalkConfigModel<ITarget>(root, t =>
            {
                replacements.Add(t);
                return t;
            });
            Assert.AreEqual(originalTargets.Count, replacements.Count);
            for (var i = 0; i < originalTargets.Count; i++)
            {
                var original = originalTargets[i];
                var replacement = replacements[i];
                Assert.IsFalse(original.Cloned);
                Assert.AreNotSame(original, replacement);
                Assert.AreEqual(original.Id, replacement.Id);
                Assert.IsTrue(replacement.Cloned);
            }
        }

        interface ITarget
        {
            int Id { get; }
            bool Cloned { get; }
            ITarget Clone();
        }

        class Model
        {
            public TargetA? A { get; set; }

            public TargetB? B { get; set; }

            public Intermediate? Intermediate { get; set; }

            public IList<TargetA>? List { get; set; }
        }

        class Intermediate
        {
            public TargetA? A { get; set; }

            public TargetB? B { get; set; }

            public Dictionary<string, TargetA>? Dict { get; set; }
        }

        class TargetA : ITarget
        {
            public int Id { get; }
            public bool Cloned { get; protected set; }
            public TargetA(int id) { Id = id; }
            public virtual ITarget Clone() => new TargetA(Id) { Cloned = true };
        }

        class TargetB : TargetA 
        {
            public TargetB(int id) : base(id) { }
            public override ITarget Clone() => new TargetB(Id) { Cloned = true, A = A };

            public TargetA? A { get; set; }
        }
    }
}
