# Auto Reload

After the model was loaded the first time, you can call
@Mastersign.ConfigModel.ConfigModelManager`1.WatchAndReload()
to watch for changes on all files of the model.

Changes are detected on all layers, added by
@Mastersign.ConfigModel.ConfigModelManager`1.AddLayer(System.String),
or @Mastersign.ConfigModel.ConfigModelManager`1.AddLayers(System.String,System.String).
On all included model files and on all string sources.

The events @Mastersign.ConfigModel.ConfigModelManager`1.ModelChanged,
and @Mastersign.ConfigModel.ConfigModelManager`1.ModelReloadFailed are fired
when changes occur.

```cs
using Mastersign.ConfigModel;

using var manager = new ConfigModelManager<RootModel>();
manager.ReloadDelay = TimeSpan.FromMilliseconds(500); // default is 250ms

RootModel model;

manager.ModelChanged += (s, e) => {
    model = e.NewModel;
    // update your application base on the new model
};
manager.ModelReloadFailed += (s, e) => {
    Console.WriteLine($"Automatic reloading the config model failed: {e.Exception.Message}");
};

model = manager.LoadModel();
```

Globbing of includes is currently not considered in the watch.
Which means, that if a file included by a glob pattern is changed or deleted,
the model is reloaded.
But if a new file is created, that matches a glob pattern,
the model is not reloaded.
