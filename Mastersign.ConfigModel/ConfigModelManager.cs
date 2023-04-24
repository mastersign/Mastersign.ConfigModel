using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.FileSystemGlobbing;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.BufferedDeserialization.TypeDiscriminators;
using YamlDotNet.Serialization.NamingConventions;

namespace Mastersign.ConfigModel
{
    public delegate void ModelChangeHandler<T>(T newModel);

    public enum PropertyNameHandling
    {
        CamelCase = 0,
        PascalCase = 1,
        LowerCase = 2,
        Underscored = 3,
        Hyphenated = 4,
    }

    public delegate DeserializerBuilder YamlDeserializerBuildCustomizer(DeserializerBuilder builder);

    public class ConfigModelManager<TRootModel>
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

        public ConfigModelManager(StringComparison filenameComparison, INamingConvention propertyNamingConvention)
        {
            _filenameComparison = filenameComparison;
            _propertyNamingConvention = propertyNamingConvention;
        }

        public ConfigModelManager(
            StringComparison filenameComparison = StringComparison.Ordinal,
            PropertyNameHandling propertyNameHandling = PropertyNameHandling.PascalCase)
        {
            _filenameComparison = filenameComparison;
            _propertyNamingConvention = NamingConventionFor(propertyNameHandling);
        }

        public string AddLayer(string filename)
        {
            if (!typeof(TRootModel).IsMergable() && _layers.Count > 0)
            {
                throw new NotSupportedException("The root model type is not mergable. Therefore, only one layer is supported.");
            }
            var filepath = PathHelper.GetCanonicalPath(filename);
            _layers.Add(filepath);
            return filepath;
        }

        public string[] AddLayers(string filepattern, string rootPath = null)
        {
            if (!typeof(TRootModel).IsMergable())
            {
                throw new NotSupportedException("The root model type is not mergable. Therefore, only one layer is supported.");
            }
            var matcher = new Matcher(_filenameComparison);
            matcher.AddInclude(filepattern);
            rootPath = rootPath ?? Environment.CurrentDirectory;
            var newLayers = new List<string>(matcher
                .GetResultsInFullPath(rootPath)
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
                    $"Syntax error in included model file '{includePath}' at {ex.Start}: {ex.Message}",
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

            var includeReferencePath = Path.GetDirectoryName(includePath);

            model = (T)ModelWalking.WalkConfigModel<ConfigModelBase>(model,
                m => LoadIncludes(m, modelFile, includeReferencePath, deserializer, includeStack, forceDeepMerge));

            includeStack.RemoveAt(includeStack.Count - 1);

            model = (T)ModelWalking.WalkConfigModel<ConfigModelBase>(model,
                m => LoadStringSources(m, includePath, includeReferencePath));

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

            foreach (var includePath in model.Includes)
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

            model.Includes = null;

            Merging.MergeObject(result, model, forceRootMerge: true, forceDeepMerge);
            return result;
        }

        public TRootModel LoadModel(
        TRootModel defaultModel = null,
        YamlDeserializerBuildCustomizer customizer = null)
        {
            var builder = new DeserializerBuilder();
            builder = builder.IgnoreUnmatchedProperties();
            builder = builder.WithNamingConvention(_propertyNamingConvention);
            builder = builder.WithNodeTypeResolver(new ClrTypeFromTagNodeTypeResolver());

            var discriminationsByPropertyExistence = ReflectionHelper.GetTypeDiscriminationsByPropertyExistence(typeof(TRootModel));
            var discriminationsByPropertyValue = ReflectionHelper.GetTypeDiscriminationsByPropertyValue(typeof(TRootModel));
            if (discriminationsByPropertyExistence.Count > 0 ||
                discriminationsByPropertyValue.Count > 0)
            {
                builder = builder.WithTypeDiscriminatingNodeDeserializer(o =>
                {
                    foreach (var discrimination in discriminationsByPropertyExistence)
                    {
                        o.AddTypeDiscriminator(new UniqueKeyTypeDiscriminator(
                            discrimination.Key, discrimination.Value));
                    }
                    foreach (var discrimination in discriminationsByPropertyValue)
                    {
                        o.AddTypeDiscriminator(new KeyValueTypeDiscriminator(
                            discrimination.Key, discrimination.Value.Item1, discrimination.Value.Item2));
                    }
                });
            }
            if (customizer != null)
            {
                builder = customizer(builder);
            }
            var deserializer = builder.Build();

            TRootModel root = null;
            var rootIsMergable = typeof(TRootModel).IsMergable();
            if (rootIsMergable)
            {
                root = new TRootModel();
                if (defaultModel != null)
                {
                    Merging.MergeObject(root, defaultModel);
                }
            }
            else if (defaultModel != null)
            {
                throw new NotSupportedException("Root model is not mergable. Therefore, a default model is not supported.");
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

                var referencePath = Path.GetDirectoryName(layerPath);
                var includeStack = new List<string> { layerPath };

                layer = (TRootModel)ModelWalking.WalkConfigModel<ConfigModelBase>(layer,
                    m => LoadIncludes(m, layerPath, referencePath, deserializer, includeStack, forceDeepMerge: false));

                layer = (TRootModel)ModelWalking.WalkConfigModel<ConfigModelBase>(layer,
                    m => LoadStringSources(m, layerPath, referencePath));

                if (rootIsMergable)
                    Merging.MergeObject(root, layer);
                else
                    root = layer;
            }

            return root;
        }

        public event ModelChangeHandler<TRootModel> ModelChanged;

        public void WatchForChanges()
        {

        }
    }
}
