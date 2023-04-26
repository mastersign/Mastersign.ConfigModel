# Getting Started

This example uses the class model from the [Introduction](intro.md).

At first we need a YAML file with our application configuration.

`config.yaml`:

```yaml
Project: My Project
Description: An Example
Data:
  Version: 2
  Values:
    a: 1
    b: 2
```

To load the model once, use the following code:

```cs
using Mastersign.ConfigModel;

// create a config model manager with your root model as type parameter
var manager = new ConfigModelManager<RootModel>();

// if a relative path is given for a layer,
// it is combined with the current working directory
manager.AddLayer("config.yaml");

// load the model once
var config = manager.LoadModel();
```
