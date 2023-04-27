# Type Discrimination

Sometimes the type of a model property or even the type of the root model
is an interface or the base class of a class hierarchy.
In that case, the YAML deserializer needs some hints,
which class to instantiate.

Mastersign.ConfigModel supports two ways of type discrimination directly
via property attributes.
It also supports YAML tags in the form of `!clr:<Full Type Name>` to
specify the type of a map inside the YAML.
Further, it is open for other ways for type discrimination supported by YamlDotNet.

## Discrimination by Property Existence

The first way of determining, which derived class should be instantiated,
is by the existence of a unique property in the derived class.

Example:

```cs
class RootModel
{
    public ChildBase Child { get; set; }
}

abstract class ChildBase { }

class ChildA
{
    [TypeIndicator]
    public string A { get; set; }
}

class ChildB
{
    [TypeIndicator]
    public string B { get; set; }
}
```

The following YAML model will lead to an instance of `ChildB`
for the property `RootModel.Child`:

```yaml
Child:
  B: discriminated
```

If you can not annotate the model classes, you can use the
`typeDiscriminationByPropertyExistence` parameter
of the constructor of @Mastersign.ConfigModel.ConfigModelManager.
The dictionary maps from a base type to a nested dictionary wich maps from
unique property names to derived types for instantiation.

The equivalent of the example above would be:

```cs
var discrimination = new Dictionary<Type, Dictionary<string, Type>>()
{
 {
    typeof(ChildBase),
    new Dictionary<string, Type> {
        { nameof(ChildA.A), typeof(ChildA) },
        { nameof(ChildB.B), typeof(ChildB) },
    },
 },
};

var manager = new ConfigModelManager(
    typeDiscriminationByPropertyExistence: discrimination);
```

## Discrimination by Property Value

The second way of determining, which derived class should be instantiated,
is by the value of a common property in the derived class.

Example:

```cs
class RootModel
{
    public ChildBase Child { get; set; }
}

abstract class ChildBase
{
    [TypeDiscriminator]
    public string? ChildType { get; set; }
}

[TypeDiscriminationValue("A")]
class ChildA { }

[TypeDiscriminationValue("B")]
class ChildB { }
```

The following YAML model will lead to an instance of `ChildA`
for the property `RootModel.Child`:

```yaml
Child:
  ChildType: A
```

If you can not annotate the model classes, you can use the
`typeDiscriminationByPropertyValue` parameter
of the constructor of @Mastersign.ConfigModel.ConfigModelManager.
The dictionary maps from a base type to a tuple with the indicator property name
as first item and a nested dictionary as second item,
wich maps from property values to derived types for instantiation.

The equivalent of the example above would be:

```cs
var discrimination = new Dictionary<Type, Tuple<string, Dictionary<string, Type>>>()
{
 {
    typeof(ChildBase),
    Tuple.Create(
        nameof(ChildBase.ChildType),
        new Dictionary<string, Type> {
            { "A", typeof(ChildA) },
            { "B", typeof(ChildB) },
        }
    )
 },
};

var manager = new ConfigModelManager(
    typeDiscriminationByPropertyValue: discrimination);
```

## YAML Tag

Because Mastersign.ConfigModel adds the @Mastersign.ConfigModel.ClrTypeFromTagNodeTypeResolver
to the YamlDotNet deserializer, you can specify the .NET class of a map in the YAML file.

```cs
namespace Mastersign.ConfigModel.Demo
{
    class RootModel
    {
        public object Child { get; set; }
    }
    class Child
    {
        public string? Caption { get; set; }
    }
}
```

The following YAML will lead to an instance of `Child`
for the property `RootModel.Child`:

```yaml
Child: !clr:Mastersign.ConfigModel.Demo.Child
  Caption: YAML tag example
```

## Custom Type Resolver

You can add custom type resolvers by passing a deserializer customizer
to the constructor of @Mastersign.ConfigModel.ConfigModelManager:

```cs
using Mastersign.ConfigModel;
using YamlDotNet.Serialization;

class MyTypeResolver : INodeTypeResolver
{
    ...
}

static class Program
{
    void Main()
    {
        var manager = new ConfigModelManager<RootModel>(
            deserializationCustomizer: builder => builder
                .WithNodeTypeResolver(new MyTypeResolver()));
    }
}
```
