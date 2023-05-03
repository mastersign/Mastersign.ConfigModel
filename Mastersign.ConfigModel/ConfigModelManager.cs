using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Reflection;
using System.Text;
using System.Threading;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.Primitives;
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

        private readonly List<string> _layerPatterns = new List<string>();
        private readonly List<string> _loadedLayerPaths = new List<string>();
        private readonly HashSet<string> _loadedStringSourcePaths = new HashSet<string>();
        private readonly HashSet<string> _includePatterns = new HashSet<string>();
        private readonly HashSet<string> _loadedIncludePaths = new HashSet<string>();

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

        public void AddLayer(string fileName)
        {
            if (!typeof(TRootModel).IsMergable() && _layerPatterns.Count > 0)
            {
                throw new NotSupportedException("The root model type is not mergable. Therefore, only one layer is supported.");
            }
            if (fileName.Contains('*'))
            {
                throw new ArgumentException("The file name must not contain wildcards", nameof(fileName));
            }
            fileName = PathHelper.GetCanonicalPath(fileName, Environment.CurrentDirectory);
            _layerPatterns.Add(fileName);
        }

        public void AddLayers(string filePattern, string rootPath = null)
        {
            if (!typeof(TRootModel).IsMergable())
            {
                throw new NotSupportedException("The root model type is not mergable. Therefore, only one layer is supported.");
            }
            filePattern = PathHelper.GetCanonicalPath(filePattern, rootPath ?? Environment.CurrentDirectory);
            _layerPatterns.Add(filePattern);
            /*
            var matcher = new Matcher(_filenameComparison);
            rootPath = rootPath ?? Environment.CurrentDirectory;
            var glob = PathHelper.PrepareGlobbingPattern(filePattern, rootPath);
            matcher.AddInclude(glob.Item2);
            var newLayers = new List<string>(matcher
                .GetResultsInFullPath(glob.Item1)
                .Select(p => PathHelper.GetCanonicalPath(p)));
            newLayers.Sort(StringComparerLookup.From(_filenameComparison));
            */
        }

        public string[] GetLayerPatterns() => _layerPatterns.ToArray();
        public string[] GetLoadedLayerPaths() => _loadedLayerPaths.ToArray();

        public string[] GetIncludePatterns() => _includePatterns.ToArray();
        public string[] GetLoadedIncludePaths() => _loadedIncludePaths.ToArray();

        public string[] GetLoadedStringSourcePaths() => _loadedStringSourcePaths.ToArray();

        private void LoadStringSource(ConfigModelBase model, string modelFile, string referencePath, PropertyInfo p)
        {
            if (p.GetValue(model) != null) return;
            var propName = p.Name;
            var yamlMemberAttr = p.GetCustomAttribute<YamlMemberAttribute>();
            if (yamlMemberAttr?.Alias != null) propName = yamlMemberAttr.Alias;
            if (yamlMemberAttr?.ApplyNamingConventions != false) propName = _propertyNamingConvention.Apply(propName);
            if (model.StringSources.TryGetValue(propName, out var sourceFile))
            {
                sourceFile = PathHelper.GetCanonicalPath(sourceFile, referencePath);
                _loadedStringSourcePaths.Add(sourceFile);
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
            _loadedIncludePaths.Add(includePath);

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
                _includePatterns.Add(PathHelper.GetCanonicalPath(includePath, referencePath));

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

        private List<(string Path, bool Required)> GetLayerPaths()
        {
            var paths = new List<(string, bool)>();
            foreach (var pattern in _layerPatterns)
            {
                if (pattern.Contains('*'))
                {
                    var matcher = new Matcher(_filenameComparison);
                    var glob = PathHelper.PrepareGlobbingPattern(pattern);
                    matcher.AddInclude(glob.Item2);
                    var newLayers = new List<string>(matcher
                        .GetResultsInFullPath(glob.Item1)
                        .Select(p => PathHelper.GetCanonicalPath(p)));
                    newLayers.Sort(StringComparerLookup.From(_filenameComparison));
                    paths.AddRange(newLayers.Select(p => (p, false)));
                }
                else
                {
                    paths.Add((pattern, true));
                }
            }
            return paths;
        }

        public TRootModel LoadModel()
        {
            if (_disposed) throw new ObjectDisposedException(typeof(ConfigModelManager<TRootModel>).FullName);

            _loadedLayerPaths.Clear();
            _loadedStringSourcePaths.Clear();
            _includePatterns.Clear();
            _loadedIncludePaths.Clear();

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
            foreach (var (layerPath, required) in GetLayerPaths())
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
                    if (required)
                    {
                        throw new ConfigModelLayerNotFoundException(
                            $"Could not find model file '{layerPath}'.",
                            layerPath, ex);
                    }
                    else continue;
                }
                catch (DirectoryNotFoundException ex)
                {
                    if (required)
                    {
                        throw new ConfigModelLayerNotFoundException(
                        $"Could not find a part of the path to model file '{layerPath}'.",
                        layerPath, ex);
                    }
                    else continue;
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

                _loadedLayerPaths.Add(layerPath);

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

            if (IsWatching) WatchAndReload();

            return root;
        }

        public event ModelChangeHandler<TRootModel> ModelChanged;

        private void OnModelChanged(TRootModel model)
            => ModelChanged?.Invoke(this, new ModelChangedEventArgs<TRootModel>(model));

        public event ModelReloadErrorHandler ModelReloadFailed;

        private void OnModelReloadFailed(ConfigModelException exception)
            => ModelReloadFailed?.Invoke(this, new ModelReloadErrorEventArgs(exception));

        public TimeSpan ReloadDelay { get; set; } = TimeSpan.FromMilliseconds(250);

        private readonly List<PhysicalFileProvider> _pfProviders = new List<PhysicalFileProvider>();

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

        private void ChangeTokenHandler(object state)
        {
            TriggerReloadTimer();
            var (pfProvider, subPath) = (Tuple<PhysicalFileProvider, string>)state;
            var token = pfProvider.Watch(subPath);
            token.RegisterChangeCallback(ChangeTokenHandler, state);
        }

        private static readonly NotifyFilters WATCH_NOTIFY_FILTER =
            NotifyFilters.CreationTime
            | NotifyFilters.DirectoryName
            | NotifyFilters.FileName
            | NotifyFilters.LastWrite
            | NotifyFilters.Size;

        public bool IsWatching => _pfProviders.Any();

        /// <summary>
        /// Starts watching for changes on any files, matching the glob patterns
        /// added model layers, includes, and string sources.
        /// Every time a change is detected the <see cref="ReloadDelay"/> is waited,
        /// to filter out multiple change events in a short time,
        /// then the model is reloaded.
        /// When the reload was successful, the <see cref="ModelChanged"/> event is fired.
        /// If the reload failed, the <see cref="ModelReloadFailed"/> event is fired.
        /// </summary>
        /// <remarks>
        ///     The environment variable <c>DOTNET_USE_POLLING_FILE_WATCHER</c> can be set to <c>1</c> or <c>true</c>
        ///     to use polling for the watcher.
        ///     But keep in mind, that polling is slow: Probably around 4 seconds interval.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">Is thrown if the mamager was disposed.</exception>
        public void WatchAndReload()
        {
            if (_disposed) throw new ObjectDisposedException(typeof(ConfigModelManager<TRootModel>).FullName);
            StopWatching();

            var patterns = _layerPatterns
                .Concat(_includePatterns)
                .Concat(_loadedStringSourcePaths)
                .Distinct(StringComparerLookup.From(_filenameComparison))
                .Select(p => PathHelper.PrepareGlobbingPattern(p))
                .ToList();
            var directories = patterns
                .GroupBy(p => p.Item1)
                .ToDictionary(
                    g => g.Key,
                    g => new HashSet<string>(g.Select(p => p.Item2)));

            _delayTimer = new Timer(OnReloadTimer);
            _delayTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

            foreach (var group in directories)
            {
                var dir = group.Key;
                var groupFiles = group.Value;

                var pfProvider = new PhysicalFileProvider(dir);
                _pfProviders.Add(pfProvider);

                foreach (var subPattern in groupFiles)
                {
                    var changeToken = pfProvider.Watch(subPattern);
                    changeToken.RegisterChangeCallback(
                        ChangeTokenHandler,
                        Tuple.Create(pfProvider, subPattern));
                }
            }
        }

        public void StopWatching()
        {
            foreach (var pfProvider in _pfProviders) pfProvider.Dispose();
            _pfProviders.Clear();
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
