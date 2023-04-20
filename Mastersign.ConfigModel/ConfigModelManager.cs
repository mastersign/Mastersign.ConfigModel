using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.FileSystemGlobbing;
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

        private readonly StringComparison _filenameComparison;
        private readonly INamingConvention _propertyNamingConvention;

        public ConfigModelManager(StringComparison filenameComparison, INamingConvention propertyNamingConvention)
        {
            _filenameComparison = filenameComparison;
            _propertyNamingConvention = propertyNamingConvention;
        }

        public ConfigModelManager(
            StringComparison filenameComparison = StringComparison.InvariantCulture,
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
            var filepath = Path.GetFullPath(filename);
            if (File.Exists(filepath))
            {
                _layers.Add(filepath);
                return filepath;
            }
            throw new FileNotFoundException("The given file was not found", filepath);
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
            var newLayers = new List<string>(matcher.GetResultsInFullPath(rootPath));
            newLayers.Sort(StringComparerLookup.From(_filenameComparison));
            _layers.AddRange(newLayers);
            return newLayers.ToArray();
        }

        private void LoadStringSourcesInternal(ConfigModelBase model, string referenceFilename)
        {
            if (model?.StringSources == null) return;
            var rootPath = Path.GetDirectoryName(referenceFilename);
            foreach (var p in model.GetType().GetModelProperties()
                .Where(p => p.PropertyType == typeof(string)))
            {
                if (p.GetValue(model) != null) continue;
                var propName = p.Name;
                var yamlMemberAttr = p.GetCustomAttribute<YamlMemberAttribute>();
                if (yamlMemberAttr?.Alias != null) propName = yamlMemberAttr.Alias;
                if (yamlMemberAttr?.ApplyNamingConventions != false) propName = _propertyNamingConvention.Apply(propName);
                if (model.StringSources.TryGetValue(propName, out var sourceFile))
                {
                    if (!Path.IsPathRooted(sourceFile)) sourceFile = Path.Combine(rootPath, sourceFile);
                    var s = File.ReadAllText(sourceFile, Encoding.UTF8);
                    p.SetValue(model, s);
                }
            }
            model.StringSources = null;
        }

        private void LoadStringSourcesInList<T>(IList<T> list, string referenceFilename)
        {
            if (ReflectionHelper.IsAtomic(typeof(T))) return;
            for (var i = 0; i < list.Count; i++)
            {
                var x = list[i];
                LoadStringSources(x, referenceFilename);
                if (typeof(T).IsValueType) list[i] = x;
            }
        }

        private void LoadStringSourcesInDictionary<T>(IDictionary<string, T> dictionary, string referenceFilename)
        {
            if (ReflectionHelper.IsAtomic(typeof(T))) return;
            foreach (var kvp in dictionary)
            {
                var x = kvp.Value;
                LoadStringSources(x, referenceFilename);
                if (typeof(T).IsValueType) dictionary[kvp.Key] = x;
            }
        }

        private void LoadStringSources(object o, string referenceFilename)
        {
            if (o == null) return;
            var t = o.GetType();
            if (typeof(ConfigModelBase).IsAssignableFrom(t))
            {
                LoadStringSourcesInternal((ConfigModelBase)o, referenceFilename);
            }

            var mapValueType = ReflectionHelper.GetMapValueType(t);
            if (mapValueType != null)
            {
                var loadStringSourcesInDictionaryMethodGenericInfo = typeof(ConfigModelManager<TRootModel>).GetMethod(
                    nameof(LoadStringSourcesInDictionary), BindingFlags.Instance | BindingFlags.NonPublic);
                var loadStringSourcesInDictionaryMethod = loadStringSourcesInDictionaryMethodGenericInfo.MakeGenericMethod(typeof(string), mapValueType);
                loadStringSourcesInDictionaryMethod.Invoke(this, new[] { o, referenceFilename });
                return;
            }

            var listElementType = ReflectionHelper.GetListElementType(t);
            if (listElementType != null)
            {
                var loadStringSourcesInListMethodGenericInfo = typeof(ConfigModelManager<TRootModel>).GetMethod(
                    nameof(LoadStringSourcesInList), BindingFlags.Instance | BindingFlags.NonPublic);
                var loadStringSourcesInListMethod = loadStringSourcesInListMethodGenericInfo.MakeGenericMethod(listElementType);
                loadStringSourcesInListMethod.Invoke(this, new[] { o, referenceFilename });
                return;
            }

            foreach (var p in t.GetModelProperties())
            {
                if (ReflectionHelper.IsAtomic(p.PropertyType)) continue;
                var pv = p.GetValue(o);
                if (pv != null)
                {
                    LoadStringSources(pv, referenceFilename);
                    if (p.PropertyType.IsValueType) p.SetValue(o, pv);
                }
            }
        }

        public TRootModel LoadModel(
            TRootModel defaultModel = null,
            YamlDeserializerBuildCustomizer customizer = null)
        {
            var builder = new DeserializerBuilder();
            builder = builder.IgnoreUnmatchedProperties();
            builder = builder.WithNamingConvention(_propertyNamingConvention);

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
            foreach (var layerSource in _layers)
            {
                TRootModel layer;
                using (var s = File.Open(layerSource, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var r = new StreamReader(s))
                {
                    layer = deserializer.Deserialize<TRootModel>(r);
                }
                if (layer is ConfigModelBase)
                {
                    // TODO load includes
                    LoadStringSources(layer as ConfigModelBase, layerSource);
                }
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
