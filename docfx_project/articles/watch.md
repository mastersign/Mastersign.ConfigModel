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

In some scenarios, watching for changes by listening to file system events does not work properly.
One example is changing a file in a Docker volume mount from the host.
For such cases, the environment variable `DOTNET_USE_POLLING_FILE_WATCHER`
can be set to `1` or `true` to switch from file system events to polling.
Currently the polling interval can not be specified and is propably set to 4 seconds.
