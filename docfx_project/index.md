# Mastersign.ConfigModel

> YAML based configuration model for .NET applications

[![NuGet Package](https://img.shields.io/nuget/v/Mastersign.ConfigModel?logo=nuget&style=flat-square)](https://www.nuget.org/packages/Mastersign.ConfigModel)

Repository: <https://github.com/mastersign/Mastersign.ConfigModel>

This library is designed for the following use case:
You have an application needing a rather large configuration.
And you typically write the configuration by hand or modify
only parts of it with scripts, or by the application itself.
To keep the configuration lucid, you like to spread it over
multiple easy to manage files in a fitting directory structure.
You like to automatically reload the configuration,
when one file of the configuration changes.

What this library **not** supports is a full round-trip for a configuration.
Where you load it from file(s), modify it during the runtime
of the application and write it back to disk.

## Features

- Includes
- String sourcing from text files
- Multiple layers
- Optional merge or replace for items in dictionaries / lists
- Auto reload on file change

See [Documentation](articles/intro.md) and [API](api/index.md) for more details.

## Example

This example demonstrates a small configuration model
with two classes: `ProjectModel` and `DataModel`.

The `ProjectModel` has three public readable and writable properties.
One has the type `DataModel` as example for a nested data structure.
The `DataModel` class has a property with a dictionary with string keys
as an example for a generic map structure.

```cs
[MergableConfigModel]
class ProjectModel : ConfigModelBase
{
    public string? ProjectName { get; set; }

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

The following code demonstrates creating a manager
for the configuration model, loading the model,
and watching for changes for automatic reload.

The model is loaded by adding the two layers `config\1-defaults.yaml`,
and `config\2-main.yaml` with a globbing pattern `*.yaml` in the root folder `config`.

```cs
using Mastersign.ConfigModel;
using System.IO;

static class Program
{
    static string ConfigurationFolder => Path.Combine(Environment.CurrentDirectory, "config");

    static ConfigModelManager modelManager;

    static ProjectModel Model { get; set; }

    static void HandleModelReload(object sender, ConfigModelReloadEventArgs ea)
    {
        Model = ea.NewModel;
        Console.WriteLine("Model reloaded.");
    }

    static void Main()
    {
        modelManager = new ConfigModelManager<ProjectModel>();

        manager.AddLayers("*.yaml", ConfigurationFolder);
        Model = manager.LoadModel();

        manager.ModelReload += HandleModelReload;
        manager.WatchAndReload();

        // your application logic here
    }
}
```

You could use the following configuration files.
The directory structure is arbitrary, and chosen for this example
to demonstrate various features of the library.
Feel free to design your own directory structure for your configuration.

* `config`
    + `includes`
        - `data.yaml`
        - `user.inc.yaml`
    + `strings`
        - `description.txt`
    + `1-defaults.yaml`
    + `2-main.yaml`

**config\1-defaults.yaml:**

```yaml
Project: Unnamed
Description: No Description
Child:
  Version: 1
```

**config\2-main.yaml:**

```yaml
$includes:
  - includes/*.inc.yaml

Data:
  $includes:
    - includes\data.yaml
```

**config\includes\user.inc.yaml:**

```yaml
Project: My Project

$sources:
  Description: ../strings/description.txt
```

**config\includes\data.yaml:**

```yaml
Values:
  x: 100
  y: 200
```

**config\strings\description.txt:**

```txt
A long project description
with multiple lines and more text,
then you would like to have in your YAML files.
```

The loaded model would contain the following data:

```yaml
Project: My Project
Description: A long project description...
Data:
  Version: 1
  Values:
    x: 100
    y: 200
```

## License

This project is licensed under the MIT license.  
Copyright by Tobias Kiertscher <dev@mastersign.de>.
