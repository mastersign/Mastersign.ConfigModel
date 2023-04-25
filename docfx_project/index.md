# Mastersign.ConfigModel

> YAML based configuration model for .NET

## Features

- Includes
- String sourcing from text files
- Multiple layers
- Optional merge or replace for items in dictionaries / lists
- Auto reload on file change

See [Documentation](articles/intro.md) and [API](api/index.md).

## Example

```cs
using Mastersign.ConfigModel;
using System.IO;

[MergableConfigModel]
class RootModel : ConfigModelBase
{
    public string? A { get; set; }

    public string? B { get; set; }

    public ChildModel? Child { get; set; }
}

[MergableConfigModel]
class ChildModel : ConfigModelBase
{
    public int? X { get; set; }

    public Dictionary<string, int> Ys { get; set; }
}

static class Program
{
    static string ConfigurationFolder => Path.Combine(Environment.CurrentDirectory, "config");

    static ConfigModelManager modelManager;

    static RootModel Model { get; set; }

    static void HandleModelReload(object sender, ConfigModelReloadEventArgs ea)
    {
        Model = ea.NewModel;
        Console.WriteLine("Model reloaded.");
    }

    static void Main()
    {
        modelManager = new ConfigModelManager<RootModel>();

        manager.AddLayers("*.yaml", ConfigurationFolder);
        Model = manager.LoadModel();

        manager.ModelReload += HandleModelReload;
        manager.WatchAndReload();
    }
}
```

You could use the following configuration files:

* `config`
    + `includes`
        - `child.yaml`
        - `user.inc.yaml`
    + `strings`
        - `a.txt`
    + `1-default.yaml`
    + `2-main.yaml`

`config\1-defaults.yaml`:

```yaml
A: Default A
B: Default B
Child:
  X: 100
```

`config\2-main.yaml`:

```yaml
$includes:
  - includes/*.inc.yaml
$sources:
  A: strings/a.txt

Child:
  $includes:
    - includes\child.yaml
```

`config\includes\user.inc.yaml`

```yaml
B: User B
```

`config\includes\child.yaml`

```yaml
Ys:
  a: 1
  b: 2
```

`config\strings\a.txt`:

```txt
Value A From File
```

The loaded model would contain the following data:

```yaml
A: Value A From File
B: User B
Child:
  X: 100
  Ys:
    a: 1
    b: 2
```

## License

This project is licensed under the MIT license.  
Copyright by Tobias Kiertscher <dev@mastersign.de>.
