namespace Mastersign.ConfigModel.Test
{
    [TestClass]
    public class ReflectionHelperTest
    {
        [TestMethod]
        public void GetListElementTypeTest()
        {
            Assert.AreEqual(
                typeof(int),
                ReflectionHelper.GetListElementType(typeof(IList<int>)));

            Assert.AreEqual(
                typeof(int),
                ReflectionHelper.GetListElementType(typeof(List<int>)));
        }

        [TestMethod]
        public void GetDictionaryValueTypeTest()
        {
            Assert.AreEqual(
                typeof(int),
                ReflectionHelper.GetDictionaryValueType(typeof(IDictionary<string, int>)));

            Assert.AreEqual(
                typeof(int),
                ReflectionHelper.GetDictionaryValueType(typeof(Dictionary<string, int>)));
        }

        [TestMethod]
        public void GetAssociatedModelTypesTest()
        {
            var discoveredTypes = ReflectionHelper.GetAssociatedModelTypes(typeof(ModelPropertyIntermediateClass)).ToList();
            var expectedTypes = new List<Type>
            {
                typeof(ModelPropertyChildAClass),
                typeof(ModelPropertyChildBClass),
            };
            CollectionAssert.AreEquivalent(expectedTypes, discoveredTypes);
        }

        [TestMethod]
        public void TraverseModelTest()
        {
            var discoveredTypes = ReflectionHelper.TraverseModelTypes(typeof(ModelPropertyRootClass)).ToList();
            var expectedTypes = new List<Type>
            {
                typeof(ModelPropertyRootClass),
                typeof(ModelPropertyIntermediateClass),
                typeof(ModelPropertyChildAClass),
                typeof(ModelPropertyChildBClass),
                typeof(int),
            };
            CollectionAssert.AreEquivalent(expectedTypes, discoveredTypes);
        }

        class ModelPropertyRootClass
        {
            public ModelPropertyRootClass? Loop { get; set; }

            public ModelPropertyIntermediateClass? Intermediate { get; set; }
        }

        class ModelPropertyIntermediateClass
        {
            public ModelPropertyChildAClass? ChildA { get; set; }

            public List<ModelPropertyChildBClass>? ChildrenB { get; set; }
        }

        class ModelPropertyChildAClass { }

        class ModelPropertyChildBClass
        {
            public Dictionary<string, int>? DictWithPrimitives { get; set; }
        }

        [TestMethod]
        public void FindAllDerivedTypesInTheSameAssemblyTest()
        {
            var discoveredTypes = ReflectionHelper.FindAllDerivedTypesInTheSameAssembly(typeof(DerivationBase));
            var expectedTypes = new List<Type>
            {
                typeof(DerivationIntermediate),
                typeof(DerivationChildA2),
                typeof(DerivationChildB),
            };
            CollectionAssert.AreEquivalent(expectedTypes, discoveredTypes);
        }

        [TestMethod]
        public void FindAllDerivedTypesInTheSameAssemblyIncludingAbstractClassesTest()
        {
            var discoveredTypes = ReflectionHelper.FindAllDerivedTypesInTheSameAssembly(typeof(DerivationBase), returnAbstractClasses: true);
            var expectedTypes = new List<Type>
            {
                typeof(DerivationIntermediate),
                typeof(DerivationChildA),
                typeof(DerivationChildA2),
                typeof(DerivationChildB),
            };
            CollectionAssert.AreEquivalent(expectedTypes, discoveredTypes);
        }

        class DerivationBase { }
        class DerivationIntermediate : DerivationBase { }
        abstract class DerivationChildA : DerivationIntermediate { }
        class DerivationChildA2 : DerivationChildA { }
        class DerivationChildB : DerivationIntermediate { }

        [TestMethod]
        public void GetDiscriminationsByPropertyExistence()
        {
            var discriminations = ReflectionHelper.GetTypeDiscriminationsByPropertyExistence(typeof(DiscriminationRoot));
            var expected = new Dictionary<Type, Dictionary<string, Type>>
            {
                {
                    typeof(DiscriminationBaseClassByExistence),
                    new Dictionary<string, Type>
                    {
                        { nameof(DiscriminationChildByExistenceA.A), typeof(DiscriminationChildByExistenceA) },
                        { nameof(DiscriminationChildByExistenceB.B), typeof(DiscriminationChildByExistenceB) },
                    }
                }
            };
            CollectionAssert.AreEquivalent(expected.Keys, discriminations.Keys);
            foreach (var baseType in expected.Keys)
            {
                var expectedDiscrimination = expected[baseType];
                var discoveredDiscrimination = discriminations[baseType];
                CollectionAssert.AreEquivalent(expectedDiscrimination.Keys, discoveredDiscrimination.Keys);
                foreach (var indicatorName in expectedDiscrimination.Keys)
                {
                    Assert.AreEqual(expectedDiscrimination[indicatorName], discoveredDiscrimination[indicatorName]);
                }
            }
        }

        [TestMethod]
        public void GetDiscriminationsByPropertyValue()
        {
            var discriminations = ReflectionHelper.GetTypeDiscriminationsByPropertyValue(typeof(DiscriminationRoot));
            var expected = new Dictionary<Type, Tuple<string, Dictionary<string, Type>>>
            {
                {
                    typeof(DiscriminationBaseClassByValue),
                    Tuple.Create(
                        nameof(DiscriminationBaseClassByValue.Type),
                        new Dictionary<string, Type>
                        {
                            { "Class A", typeof(DiscriminationChildByValueA) },
                            { "Class B", typeof(DiscriminationChildByValueB) },
                        })
                },
            };
            CollectionAssert.AreEquivalent(expected.Keys, discriminations.Keys);
            foreach (var baseType in expected.Keys)
            {
                var expectedDiscrimination = expected[baseType];
                var discoveredDiscrimination = discriminations[baseType];
                Assert.AreEqual(expectedDiscrimination.Item1, discoveredDiscrimination.Item1);
                CollectionAssert.AreEquivalent(expectedDiscrimination.Item2.Keys, discoveredDiscrimination.Item2.Keys);
                foreach (var indicatorName in expectedDiscrimination.Item2.Keys)
                {
                    Assert.AreEqual(expectedDiscrimination.Item2[indicatorName], discoveredDiscrimination.Item2[indicatorName]);
                }
            }
        }

        class DiscriminationRoot
        {
            public DiscriminationIntermediate? Intermediate { get; set; }
        }

        class DiscriminationIntermediate
        {
            public DiscriminationBaseClassByExistence? ByExistence { get; set; }

            public DiscriminationBaseClassByValue? ByValue { get; set; }
        }

        class DiscriminationBaseClassByExistence { }

        abstract class DiscriminationAbstractChildByExistence : DiscriminationBaseClassByExistence { }

        class DiscriminationChildByExistenceA : DiscriminationBaseClassByExistence
        {
            [TypeIndicator]
            public string? A { get; set; }
        }

        class DiscriminationChildByExistenceB : DiscriminationAbstractChildByExistence
        {
            [TypeIndicator]
            public string? B { get; set; }
        }

        class DiscriminationBaseClassByValue
        {
            [TypeDiscriminator]
            public string? Type { get; set; }
        }

        [TypeDiscriminationValue("Class A")]
        class DiscriminationChildByValueA : DiscriminationBaseClassByValue { }

        abstract class DiscriminationAbstractChildByValueA : DiscriminationBaseClassByValue { }

        [TypeDiscriminationValue("Class B")]
        class DiscriminationChildByValueB : DiscriminationAbstractChildByValueA { }
    }
}
