using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Microsoft.Extensions.FileSystemGlobbing;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.BufferedDeserialization.TypeDiscriminators;
using YamlDotNet.Serialization.NamingConventions;

namespace Mastersign.ConfigModel
{
    public class ModelChangedEventArgs<T> : EventArgs
    {
        public T NewModel { get; }

        public ModelChangedEventArgs(T newModel)
        {
            NewModel = newModel;
        }
    }

    public delegate void ModelChangeHandler<T>(object sender, ModelChangedEventArgs<T> eventArgs);

    public class ModelReloadErrorEventArgs : EventArgs
    {
        public ConfigModelException Exception { get; }

        public ModelReloadErrorEventArgs(ConfigModelException exception)
        {
            Exception = exception;
        }
    }

    public delegate void ModelReloadErrorHandler(object sender, ModelReloadErrorEventArgs eventArgs);

    public enum PropertyNameHandling
    {
        CamelCase = 0,
        PascalCase = 1,
        LowerCase = 2,
        Underscored = 3,
        Hyphenated = 4,
    }

    public delegate DeserializerBuilder YamlDeserializerBuildCustomizer(DeserializerBuilder builder);

    /// <summary>
    /// The main class of the Mastersign.ConfigModel library.
    /// </summary>
    /// <typeparam name="TRootModel">The type of the root class of the configuration model tree.</typeparam>
    public class ConfigModelManager<TRootModel> : IDisposable
        where TRootModel : class, new()
    {
        private static INamingConvention NamingConventionFor(PropertyNameHandling nameHandling)
        {
            switch (nameHandling)
            {
                case PropertyNameHandling.CamelCase: return CamelCaseNamingConvention.Instance;
                case PropertyNameHandling.PascalCase: return PascalCaseNamingConvention.Instance;
                case PropertyNameHandling.LowerCase: return LowerCaseNamingConvention.Instance;
                case PropertyNameHandling.Underscored: return UnderscoredNamingConvention.Instance;
                case PropertyNameHandling.Hyphenated: return HyphenatedNamingConvention.Instance;
                default: throw new NotSupportedException();
            }
        }

        private readonly List<string> _layers = new List<string>();
        private readonly HashSet<string> _stringSources = new HashSet<string>();
        private readonly HashSet<string> _includeSources = new HashSet<string>();

        private readonly StringComparison _filenameComparison;
        private readonly INamingConvention _propertyNamingConvention;

        private readonly Dictionary<Type, Dictionary<string, Type>> _discriminationsByPropertyExistence;
        private readonly Dictionary<Type, Tuple<string, Dictionary<string, Type>>> _discriminationsByPropertyValue;

        private readonly TRootModel _defaultModel;
        private readonly YamlDeserializerBuildCustomizer _deserializationCustomizer;

        private ConfigModelManager(
            IReadOnlyDictionary<Type, IReadOnlyDictionary<string, Type>> typeDiscriminationsByPropertyExistence,
            IReadOnlyDictionary<Type, Tuple<string, IReadOnlyDictionary<string, Type>>> typeDiscriminationsByPropertyValue)
        {
            _discriminationsByPropertyExistence = ReflectionHelper.GetTypeDiscriminationsByPropertyExistence(typeof(TRootModel));
            if (typeDiscriminationsByPropertyExistence != null)
            {
                foreach (var kvp in typeDiscriminationsByPropertyExistence)
                {
                    if (!_discriminationsByPropertyExistence.TryGetValue(kvp.Key, out var discrimination))
                    {
                        discrimination = new Dictionary<string, Type>();
                        _discriminationsByPropertyExistence.Add(kvp.Key, discrimination);
                    }
                    foreach (var kvp2 in kvp.Value)
                    {
                        discrimination[kvp2.Key] = kvp2.Value;
                    }
                }
            }
            _discriminationsByPropertyValue = ReflectionHelper.GetTypeDiscriminationsByPropertyValue(typeof(TRootModel));
            if (typeDiscriminationsByPropertyValue != null)
            {
                foreach (var kvp in typeDiscriminationsByPropertyValue)
                {
                    if (!_discriminationsByPropertyValue.TryGetValue(kvp.Key, out var discrimination) ||
                        discrimination.Item1 != kvp.Value.Item1)
                    {
                        discrimination = Tuple.Create(
                            kvp.Value.Item1,
                            kvp.Value.Item2.ToDictionary(p => p.Key, p => p.Value));
                        _discriminationsByPropertyValue[kvp.Key] = discrimination;
                    }
                    else
                    {
                        foreach (var kvp2 in kvp.Value.Item2)
                        {
                            discrimination.Item2[kvp2.Key] = kvp2.Value;
                        }
                    }
                }
            }
        }

        public ConfigModelManager(
            StringComparison filenameComparison,
            INamingConvention propertyNamingConvention,
            TRootModel defaultModel,
            YamlDeserializerBuildCustomizer deserializationCustomizer,
            IReadOnlyDictionary<Type, IReadOnlyDictionary<string, Type>> typeDiscriminationsByPropertyExistence,
            IReadOnlyDictionary<Type, Tuple<string, IReadOnlyDictionary<string, Type>>> typeDiscriminationsByPropertyValue)
            : this(typeDiscriminationsByPropertyExistence, typeDiscriminationsByPropertyValue)
        {
            _filenameComparison = filenameComparison;
            _propertyNamingConvention = propertyNamingConvention ?? PascalCaseNamingConvention.Instance;
            _defaultModel = defaultModel;
            _deserializationCustomizer = deserializationCustomizer;
        }

        public ConfigModelManager(
            StringComparison filenameComparison = StringComparison.Ordinal,
            PropertyNameHandling propertyNameHandling = PropertyNameHandling.PascalCase,
            TRootModel defaultModel = null,
            YamlDeserializerBuildCustomizer deserializationCustomizer = null,
            IReadOnlyDictionary<Type, IReadOnlyDictionary<string, Type>> typeDiscriminationsByPropertyExistence = null,
            IReadOnlyDictionary<Type, Tuple<string, IReadOnlyDictionary<string, Type>>> typeDiscriminationsByPropertyValue = null)
            : this(
                  filenameComparison,
                  NamingConventionFor(propertyNameHandling),
                  defaultModel,
                  deserializationCustomizer,
                  typeDiscriminationsByPropertyExistence,
                  typeDiscriminationsByPropertyValue)
        {
        }

        public string AddLayer(string fileName)
        {
            if (!typeof(TRootModel).IsMergable() && _layers.Count > 0)
            {
                throw new NotSupportedException("The root model type is not mergable. Therefore, only one layer is supported.");
            }
            var filepath = PathHelper.GetCanonicalPath(fileName);
            _layers.Add(filepath);
            return filepath;
        }

        public string[] AddLayers(string filePattern, string rootPath = null)
        {
            if (!typeof(TRootModel).IsMergable())
            {
                throw new NotSupportedException("The root model type is not mergable. Therefore, only one layer is supported.");
            }
            var matcher = new Matcher(_filenameComparison);
            rootPath = rootPath ?? Environment.CurrentDirectory;
            var glob = PathHelper.PrepareGlobbingPattern(filePattern, rootPath);
            matcher.AddInclude(glob.Item2);
            var newLayers = new List<string>(matcher
                .GetResultsInFullPath(glob.Item1)
                .Select(p => PathHelper.GetCanonicalPath(p)));
            newLayers.Sort(StringComparerLookup.From(_filenameComparison));
            _layers.AddRange(newLayers);
            return newLayers.ToArray();
        }

        public string[] GetLayerPaths() => _layers.ToArray();

        public string[] GetIncludePaths() => _includeSources.ToArray();

        public string[] GetStringSourcePaths() => _stringSources.ToArray();

        private void LoadStringSource(ConfigModelBase model, string modelFile, string referencePath, PropertyInfo p)
        {
            if (p.GetValue(model) != null) return;
            var propName = p.Name;
            var yamlMemberAttr = p.GetCustomAttribute<YamlMemberAttribute>();
            if (yamlMemberAttr?.Alias != null) propName = yamlMemberAttr.Alias;
            if (yamlMemberAttr?.ApplyNamingConventions != false) propName = _propertyNamingConvention.Apply(propName);
            if (model.StringSources.TryGetValue(propName, out var sourceFile))
            {
                if (!Path.IsPathRooted(sourceFile)) sourceFile = Path.Combine(referencePath, sourceFile);
                sourceFile = PathHelper.GetCanonicalPath(sourceFile);
                _stringSources.Add(sourceFile);
                try
                {
                    var s = File.ReadAllText(sourceFile, Encoding.UTF8);
                    p.SetValue(model, s);
                }
                catch (FileNotFoundException ex)
                {
                    throw new ConfigModelStringSourceNotFoundException(
                        $"Could not find string source file '{sourceFile}' for model '{modelFile}'.",
                        modelFile, sourceFile, ex);
                }
                catch (DirectoryNotFoundException ex)
                {
                    throw new ConfigModelStringSourceNotFoundException(
                        $"Could not find a part of the path to string source file '{sourceFile}' for model '{modelFile}'.",
                        modelFile, sourceFile, ex);
                }
                catch (IOException ex)
                {
                    throw new ConfigModelStringSourceLoadException(
                        $"Can not read string source file in model '{modelFile}': {ex.Message}",
                        modelFile, sourceFile, ex);
                }
            }
        }

        private ConfigModelBase LoadStringSources(ConfigModelBase model, string modelFile, string referencePath)
        {
            if (model?.StringSources == null) return model;
            foreach (var p in model.GetType().GetModelProperties()
                .Where(p => p.PropertyType == typeof(string)))
            {
                LoadStringSource(model, modelFile, referencePath, p);
            }
            model.StringSources = null;
            return model;
        }

        private static readonly StringComparison FILENAME_COMPARISON
            = Environment.OSVersion.Platform == PlatformID.Win32NT
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal;

        private T LoadInclude<T>(string modelFile, string referencePath, string includePath,
            IDeserializer deserializer, List<string> includeStack, bool forceDeepMerge)
        {
            includePath = PathHelper.GetCanonicalPath(includePath, referencePath);
            _includeSources.Add(includePath);

            if (includeStack.Any(p => string.Equals(p, includePath, FILENAME_COMPARISON)))
            {
                throw new ConfigModelIncludeCycleException(includeStack
                    .SkipWhile(p => !string.Equals(p, includePath))
                    .Concat(new[] { includePath })
                    .ToArray());
            }

            includeStack.Add(includePath);

            T model;
            try
            {
                using (var s = File.Open(includePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var r = new StreamReader(s))
                {
                    model = deserializer.Deserialize<T>(r);
                }
            }
            catch (FileNotFoundException ex)
            {
                throw new ConfigModelIncludeNotFoundException(
                    $"Could not find included file '{includePath}' in model '{modelFile}'.",
                    modelFile, includePath, ex);
            }
            catch (DirectoryNotFoundException ex)
            {
                throw new ConfigModelIncludeNotFoundException(
                    $"Could not find a part of the path to included file '{includePath}' in model '{modelFile}'.",
                    modelFile, includePath, ex);
            }
            catch (IOException ex)
            {
                throw new ConfigModelIncludeLoadException(
                    $"Can not read include file in model file '{modelFile}': {ex.Message}",
                    modelFile, includePath, ex);
            }
            catch (SyntaxErrorException ex)
            {
                throw new ConfigModelIncludeLoadException(
                    $"Syntax error in included file '{includePath}' at {ex.Start}: {ex.Message}",
                    modelFile, includePath, ex);
            }
            catch (SemanticErrorException ex)
            {
                throw new ConfigModelIncludeLoadException(
                    $"Semantic error in included file '{includePath}' at {ex.Start}: {ex.Message}",
                    modelFile, includePath, ex);
            }
            catch (AnchorNotFoundException ex)
            {
                throw new ConfigModelIncludeLoadException(
                    $"Anchor not found in included file '{includePath}' at {ex.Start}: {ex.Message}",
                    modelFile, includePath, ex);
            }
            catch (ForwardAnchorNotSupportedException ex)
            {
                throw new ConfigModelIncludeLoadException(
                    $"Forward anchor not supported in included file '{includePath}' at {ex.Start}: {ex.Message}",
                    modelFile, includePath, ex);
            }
            catch (MaximumRecursionLevelReachedException ex)
            {
                throw new ConfigModelIncludeLoadException(
                    $"Maximum recursion level reached in included file '{includePath}' at {ex.Start}: {ex.Message}",
                    modelFile, includePath, ex);
            }
            catch (YamlException ex)
            {
                throw new ConfigModelIncludeLoadException(
                    $"Failed to load YAML for include from '{includePath}' at {ex.Start}: {ex.Message}",
                    modelFile, includePath, ex);
            }
            catch (Exception ex)
            {
                throw new ConfigModelIncludeLoadException(
                    $"Failed to load YAML for include from '{includePath}': {ex.Message}",
                    modelFile, includePath, ex);
            }

            var includeReferencePath = Path.GetDirectoryName(includePath);

            model = (T)ModelWalking.WalkConfigModel<ConfigModelBase>(model,
                m => LoadStringSources(m, includePath, includeReferencePath));

            model = (T)ModelWalking.WalkConfigModel<ConfigModelBase>(model,
                m => LoadIncludes(m, modelFile, includeReferencePath, deserializer, includeStack, forceDeepMerge));

            includeStack.RemoveAt(includeStack.Count - 1);

            return model;
        }

        private ConfigModelBase LoadIncludes(ConfigModelBase model, string modelFile, string referencePath,
            IDeserializer deserializer, List<string> includeStack, bool forceDeepMerge = false)
        {
            if (model?.Includes == null) return model;
            var t = model.GetType();
            var result = (ConfigModelBase)Activator.CreateInstance(t);

            var loadMethodInfo = typeof(ConfigModelManager<TRootModel>).GetMethod(
                nameof(LoadInclude), BindingFlags.Instance | BindingFlags.NonPublic);
            var loader = loadMethodInfo.MakeGenericMethod(t);

            void Include(string includePath)
            {
                object include;
                try
                {
                    include = loader.Invoke(this, new object[] {
                        modelFile, referencePath, includePath,
                        deserializer, includeStack, forceDeepMerge,
                    });
                }
                catch (TargetInvocationException ex)
                {
                    throw ex.InnerException;
                }
                Merging.MergeObject(result, include, forceRootMerge: true, forceDeepMerge);
            }

            foreach (var includePath in model.Includes)
            {
                if (includePath.Contains('*'))
                {
                    var matcher = new Matcher(_filenameComparison);
                    var glob = PathHelper.PrepareGlobbingPattern(includePath, referencePath);
                    matcher.AddInclude(glob.Item2);
                    var matches = new List<string>(matcher
                        .GetResultsInFullPath(glob.Item1)
                        .Select(p => PathHelper.GetCanonicalPath(p)));
                    matches.Sort(StringComparerLookup.From(_filenameComparison));
                    foreach (var match in matches)
                    {
                        Include(match);
                    }
                }
                else
                {
                    Include(includePath);
                }
            }

            Merging.MergeObject(result, model, forceRootMerge: true, forceDeepMerge);

            return result;
        }

        private IDeserializer BuildDeserializer()
        {
            var builder = new DeserializerBuilder();
            builder = builder.IgnoreUnmatchedProperties();
            builder = builder.WithNamingConvention(_propertyNamingConvention);
            builder = builder.WithNodeTypeResolver(new ClrTypeFromTagNodeTypeResolver());

            if (_discriminationsByPropertyExistence.Count > 0 ||
                _discriminationsByPropertyValue.Count > 0)
            {
                builder = builder.WithTypeDiscriminatingNodeDeserializer(o =>
                {
                    foreach (var discrimination in _discriminationsByPropertyExistence)
                    {
                        o.AddTypeDiscriminator(new UniqueKeyTypeDiscriminator(
                            discrimination.Key, discrimination.Value.ToDictionary(
                                kvp => _propertyNamingConvention.Apply(kvp.Key),
                                kvp => kvp.Value)));
                    }
                    foreach (var discrimination in _discriminationsByPropertyValue)
                    {
                        o.AddTypeDiscriminator(new KeyValueTypeDiscriminator(
                            discrimination.Key, 
                            _propertyNamingConvention.Apply(discrimination.Value.Item1),
                            discrimination.Value.Item2));
                    }
                });
            }
            if (_deserializationCustomizer != null)
            {
                builder = _deserializationCustomizer(builder);
            }
            var deserializer = builder.Build();
            return deserializer;
        }

        public TRootModel LoadModel()
        {
            if (_disposed) throw new ObjectDisposedException(typeof(ConfigModelManager<TRootModel>).FullName);

            _stringSources.Clear();
            _includeSources.Clear();

            IDeserializer deserializer = BuildDeserializer();

            TRootModel root = null;
            var rootIsMergable = typeof(TRootModel).IsMergable();
            if (rootIsMergable)
            {
                root = new TRootModel();
                if (_defaultModel != null)
                {
                    Merging.MergeObject(root, _defaultModel);
                }
            }
            else if (_defaultModel != null)
            {
                throw new NotSupportedException("The root model is not mergable. Therefore, a default model is not supported.");
            }
            foreach (var layerPath in _layers)
            {
                TRootModel layer;
                try
                {
                    using (var s = File.Open(layerPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    using (var r = new StreamReader(s))
                    {
                        layer = deserializer.Deserialize<TRootModel>(r);
                    }
                }
                catch (FileNotFoundException ex)
                {
                    throw new ConfigModelLayerNotFoundException(
                        $"Could not find model file '{layerPath}'.",
                        layerPath, ex);
                }
                catch (DirectoryNotFoundException ex)
                {
                    throw new ConfigModelLayerNotFoundException(
                        $"Could not find a part of the path to model file '{layerPath}'."
                        , layerPath, ex);
                }
                catch (IOException ex)
                {
                    throw new ConfigModelLayerLoadException(
                        $"Failed to open model file: {ex.Message}",
                        layerPath, ex);
                }
                catch (SyntaxErrorException ex)
                {
                    throw new ConfigModelLayerLoadException(
                        $"Syntax error in model file '{layerPath}' at {ex.Start}: {ex.Message}",
                        layerPath, ex);
                }
                catch (SemanticErrorException ex)
                {
                    throw new ConfigModelLayerLoadException(
                        $"Semantic error in model file '{layerPath}' at {ex.Start}: {ex.Message}",
                        layerPath, ex);
                }
                catch (AnchorNotFoundException ex)
                {
                    throw new ConfigModelLayerLoadException(
                        $"Anchor not found in model file '{layerPath}' at {ex.Start}: {ex.Message}",
                        layerPath, ex);
                }
                catch (ForwardAnchorNotSupportedException ex)
                {
                    throw new ConfigModelLayerLoadException(
                        $"Forward anchor not supported in model file '{layerPath}' at {ex.Start}: {ex.Message}"
                        , layerPath, ex);
                }
                catch (MaximumRecursionLevelReachedException ex)
                {
                    throw new ConfigModelLayerLoadException(
                        $"Maximum recursion level reached in model file '{layerPath}' at {ex.Start}: {ex.Message}",
                        layerPath, ex);
                }
                catch (YamlException ex)
                {
                    throw new ConfigModelLayerLoadException(
                        $"Failed to load YAML from '{layerPath}' at {ex.Start}: {ex.Message}",
                        layerPath, ex);
                }
                catch (Exception ex)
                {
                    throw new ConfigModelLayerLoadException(
                        $"Failed to load YAML from '{layerPath}': {ex.Message}",
                        layerPath, ex);
                }

                var referencePath = Path.GetDirectoryName(layerPath);
                var includeStack = new List<string> { layerPath };

                layer = (TRootModel)ModelWalking.WalkConfigModel<ConfigModelBase>(layer,
                    m => LoadStringSources(m, layerPath, referencePath));

                layer = (TRootModel)ModelWalking.WalkConfigModel<ConfigModelBase>(layer,
                    m => LoadIncludes(m, layerPath, referencePath, deserializer, includeStack, forceDeepMerge: false));

                if (rootIsMergable)
                    Merging.MergeObject(root, layer);
                else
                    root = layer;
            }

            return root;
        }

        public event ModelChangeHandler<TRootModel> ModelChanged;

        private void OnModelChanged(TRootModel model)
            => ModelChanged?.Invoke(this, new ModelChangedEventArgs<TRootModel>(model));

        public event ModelReloadErrorHandler ModelReloadFailed;

        private void OnModelReloadFailed(ConfigModelException exception)
            => ModelReloadFailed?.Invoke(this, new ModelReloadErrorEventArgs(exception));

        public TimeSpan ReloadDelay { get; set; } = TimeSpan.FromMilliseconds(250);

        private readonly List<FileSystemWatcher> _watchers = new List<FileSystemWatcher>();

        private Timer _delayTimer = null;

        private void OnReloadTimer(object _)
        {
            TRootModel model;
            try
            {
                model = LoadModel();
            }
            catch (ConfigModelException ex)
            {
                OnModelReloadFailed(ex);
                return;
            }
            OnModelChanged(model);
        }

        private void TriggerReloadTimer() => _delayTimer.Change(ReloadDelay, Timeout.InfiniteTimeSpan);

        private static readonly NotifyFilters WATCH_NOTIFY_FILTER =
            NotifyFilters.CreationTime
            | NotifyFilters.DirectoryName
            | NotifyFilters.FileName
            | NotifyFilters.LastWrite
            | NotifyFilters.Size;

        public void WatchAndReload()
        {
            if (_disposed) throw new ObjectDisposedException(typeof(ConfigModelManager<TRootModel>).FullName);
            StopWatching();

            var files = _layers.Concat(_includeSources).Concat(_stringSources).Distinct().ToList();
            var directories = files
                .GroupBy(Path.GetDirectoryName)
                .ToDictionary(
                    g => g.Key,
                    g => new HashSet<string>(g.Select(Path.GetFileName)));

            _delayTimer = new Timer(OnReloadTimer);
            _delayTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

            foreach (var group in directories)
            {
                var dir = group.Key;
                var groupFiles = group.Value;
                var watcher = new FileSystemWatcher(group.Key);
                watcher.NotifyFilter = WATCH_NOTIFY_FILTER;
                if (groupFiles.Count == 1)
                {
                    watcher.Filter = Path.GetFileName(groupFiles.First());
                }
                else
                {
                    var extensions = groupFiles.Select(Path.GetExtension).Distinct().ToList();
                    if (extensions.Count == 1)
                    {
                        watcher.Filter = "*" + extensions[0];
                    }
                }

                void Handler(object s, FileSystemEventArgs ea)
                {
                    if (groupFiles.Count > 1 && !groupFiles.Contains(ea.Name)) return;
                    TriggerReloadTimer();
                }
                void RenamedHandler(object s, RenamedEventArgs ea)
                {
                    if (groupFiles.Count > 1 && !groupFiles.Contains(ea.Name) && !groupFiles.Contains(ea.OldName)) return;
                    TriggerReloadTimer();
                }

                watcher.Changed += Handler;
                watcher.Created += Handler;
                watcher.Deleted += Handler;
                watcher.Renamed += RenamedHandler;
                _watchers.Add(watcher);
                watcher.EnableRaisingEvents = true;
            }
        }

        public void StopWatching()
        {
            foreach (var watcher in _watchers)
            {
                watcher.Dispose();
            }
            _watchers.Clear();
        }

        private bool _disposed = false;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            StopWatching();
            GC.SuppressFinalize(this);
        }

        ~ConfigModelManager()
        {
            Dispose();
        }
    }
}
