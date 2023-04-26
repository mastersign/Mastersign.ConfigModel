# Introduction

Mastersign.ConfigModel is based on the YamlDotNet deserializer
and adds features for loading the configuration of an app from YAML files.

## Features

The library currently supports the following features:

- Includes
- String sourcing from text files
- Multiple layers
- Optional merge or replace for items in dictionaries / lists
- Auto reload on file change

## Example Model

The following model is used in most of the examples:

```cs
using Mastersign.ConfigModel;

[MergableConfigModel]
class ProjectModel : ConfigModelBase
{
    public string? Project { get; set; }

    public string? Description { get; set; }

    public DataModel? Data { get; set; }
}

[MergableConfigModel]
class DataModel : ConfigModelBase
{
    public int? Version { get; set; }

    public Dictionary<string, int> Values { get; set; }
}
```

The base class @Mastersign.ConfigModel.ConfigModelBase adds support for includes and string sourcing to the model.
The class attribute @Mastersign.ConfigModel.MergableConfigModelAttribute indicates,
that the annotated class can be automatically merged by assigning or merging each readable and writable public property.

Therefore, both classes `ProjectModel` and `DataModel` do support includes and string sourcing.
And instances of these classes are automatically merged by merging their public properties.
