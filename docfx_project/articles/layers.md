# Configuration Layers

If you need default values or multiple layers of configuration,
and includes are not viable, because e. g. the paths to the layers
are build at runtime or given as a command line argument,
configuration layers are a solution.

You can add any number of layers to the model manager
before loading the model.
You can even use globbing for adding multiple layers at once.

```cs
using Mastersign.ConfigModel;

var compileTimeDefaults = new Model()
{
    A: "Basic A",
    B: "Basic B",
};

// Use an instance of your root model class
// for compile time defaults.
// This layer has less precedence then all file layers added later.
var manager = new ConfigModelManager<RootModel>(
    defaultModel: compileTimeDefaults);

// Add one or multiple layers with relative or absolute paths.
// Relative paths are resolved relative to the current working directory.
manager.AddLayer("res/default.yaml");
manager.AddLayer("config.yaml");

// Use glob patterns to add multiple layers at once.
manager.AddLayers("user/*.config.yaml");

// load the model with all layers merged
var config = manager.LoadModel();
```

Globbed layer files are sorted alphabetically.
The globbing is performed only once when calling `AddLayers()`.
Alle files, matched during the one globbing, must
be present every time the model is (re)loaded.

If you need the globbing to be performed every time the model is reloaded,
consider using includes.

The layers are merged in order they where added.
The layer added last, has the highest precedence.
