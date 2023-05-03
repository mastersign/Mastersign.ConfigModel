using System.Globalization;
using Mastersign.ConfigModel;

if (args.Length < 1)
{
    Console.WriteLine("Usage: Mastersign.ConfigModel.WatchTest.exe <model file> [<watch duration in secs>]");
    Environment.Exit(1);
}

var modelFile = args[0];
var targetDir = Path.GetDirectoryName(modelFile)!;

var watchDurationInSeconds = args.Length >= 2
    ? double.Parse(args[1],
        NumberStyles.Integer | NumberStyles.AllowDecimalPoint,
        CultureInfo.InvariantCulture)
    : 4.0;

var changes = 0;

var mgr = new ConfigModelManager<Model>();
mgr.AddLayer(modelFile);
mgr.ModelChanged += ModelChangedHandler;
mgr.ModelReloadFailed += ModelReloadFailedHandler;
mgr.LoadModel();
mgr.WatchAndReload();

Console.WriteLine($"Waiting {watchDurationInSeconds} seconds...");
Thread.Sleep(TimeSpan.FromSeconds(watchDurationInSeconds));
Console.WriteLine($"Finished after {changes} model reloads.");
Environment.Exit(0);

void ModelChangedHandler(object sender, ModelChangedEventArgs<Model> eventArgs)
{
    changes++;
    File.AppendAllLines(Path.Combine(targetDir, "changes.txt"), new string[] {
        changes.ToString(),
        eventArgs.NewModel.Value ?? "<null>",
    });
    Console.WriteLine($"Reload with value = '{eventArgs.NewModel.Value}'");
}

void ModelReloadFailedHandler(object sender, ModelReloadErrorEventArgs eventArgs)
{
    File.WriteAllText(Path.Combine(targetDir, "error.txt"), eventArgs.Exception.ToString());
    Console.Error.WriteLine(eventArgs.Exception);
}

class Model : ConfigModelBase
{
    public string? Value { get; set; }
}
