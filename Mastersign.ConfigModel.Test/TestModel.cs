﻿using YamlDotNet.Serialization;

namespace Mastersign.ConfigModel.Test
{
    internal class RootModel : ConfigModelBase
    {
        public string? Name { get; set; }

        public string? PropA { get; set; }
        public string? PropB { get; set; }
        public string? PropC { get; set; }

        [YamlMember(Alias = "prop_dee")]
        public string? PropD { get; set; }

        public ChildBaseByPropertyValue? ChildByPropertyValue { get; set; }

        public ChildBaseByPropertyExistence? ChildByPropertyExistence { get; set; }

        public ChildSimple? SimpleChild { get; set; }

        public ChildMergableByAttribute? MergableByAttribute { get; set; }

        public ChildMergableByInterface? MergableByInterface { get; set; }

        public ChildWithSources? WithSources { get; set; }

        public ChildWithSourcesMergableByAttribute? WithSourcesMergableByAttribute { get; set; }

        public ChildWithSourcesMergableByInterface? WithSourcesMergableByInterface { get; set; }
    }

    internal abstract class ChildBaseByPropertyValue
    {
        [TypeDescriminator]
        public string? Class { get; set; }
    }

    [TypeDescriminationValue("Class A")]
    internal class ChildClass1A : ChildBaseByPropertyValue
    {

    }

    [TypeDescriminationValue("Class B")]
    internal class ChildClass1B : ChildBaseByPropertyValue
    {

    }

    internal abstract class ChildBaseByPropertyExistence
    {

    }

    internal abstract class ChildBaseByPropertyExistenceIntermediate : ChildBaseByPropertyExistence
    {

    }

    internal class ChildClass2A : ChildBaseByPropertyExistenceIntermediate
    {
        [TypeIndicator]
        public int A { get; set; }
    }

    internal class ChildClass2B : ChildBaseByPropertyExistenceIntermediate
    {
        [TypeIndicator]
        public int B { get; set; }
    }

    internal class ChildSimple
    {
        public string? Caption { get; set; }

        public string? Victim { get; set; }

        public DateTimeOffset? Timestamp { get; set; }
    }

    internal class MergableBaseClass
    {
        public string? X { get; set; }
        public string? Y { get; set; }
    }

    [MergableConfigModel]
    internal class ChildMergableByAttribute : MergableBaseClass
    {
    }

    internal class ChildMergableByInterface : MergableBaseClass, IMergableConfigModel
    {
        public string? Z { get; set; }

        public void UpdateWith(object layer)
        {
            X = (layer as ChildMergableByInterface)?.X;
            Y = (layer as ChildMergableByInterface)?.Y;
            Z = Z ?? (layer as ChildMergableByInterface)?.Z;
        }
    }

    internal class ChildWithSources : ConfigModelBase
    {
        public string? Caption { get; set; }
        public string? Victim { get; set; }
    }

    [MergableConfigModel]
    internal class ChildWithSourcesMergableByAttribute : ConfigModelBase
    {
        public string? X { get; set; }
        public string? Y { get; set; }
    }

    internal class ChildWithSourcesMergableByInterface : ConfigModelBase, IMergableConfigModel
    {
        public string? X { get; set; }
        public string? Y { get; set; }
        public string? Z { get; set; }

        public override void UpdateWith(object layer)
        {
            X = (layer as ChildWithSourcesMergableByInterface)?.X;
            Y = (layer as ChildWithSourcesMergableByInterface)?.Y;
            Z = Z ?? (layer as ChildWithSourcesMergableByInterface)?.Z;
        }
    }

}
