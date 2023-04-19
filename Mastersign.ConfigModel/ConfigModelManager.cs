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
using static Mastersign.ConfigModel.ReflectionHelper;

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

        private Dictionary<Type, Dictionary<string, Type>> GetTypeDiscriminationsByPropertyExistence()
        {
            var result = new Dictionary<Type, Dictionary<string, Type>>();
            foreach (var mt in TraverseModelTypes(typeof(TRootModel)))
            {
                foreach (var st in FindAllDerivedTypesInTheSameAssembly(mt))
                {
                    foreach (var p in st.GetModelProperties())
                    {
                        if (p.HasCustomAttribute<TypeIndicatorAttribute>())
                        {
                            if (!result.TryGetValue(mt, out var uniqueKeys))
                            {
                                uniqueKeys = new Dictionary<string, Type>();
                                result.Add(mt, uniqueKeys);
                            }
                            uniqueKeys.Add(p.Name, st);
                            break;
                        }
                    }
                }
            }

            return result;
        }

        private Dictionary<Type, Tuple<string, Dictionary<string, Type>>> GetTypeDiscriminationsByPropertyValue()
        {
            var result = new Dictionary<Type, Tuple<string, Dictionary<string, Type>>>();
            foreach (var mt in TraverseModelTypes(typeof(TRootModel)))
            {
                foreach (var p in mt.GetModelProperties())
                {
                    if (p.HasCustomAttribute<TypeDiscriminatorAttribute>())
                    {
                        if (!result.TryGetValue(mt, out var valueIndicator))
                        {
                            valueIndicator = Tuple.Create(p.Name, new Dictionary<string, Type>());
                            result.Add(mt, valueIndicator);
                        }
                        foreach (var st in FindAllDerivedTypesInTheSameAssembly(mt))
                        {
                            var discriminationValue = st.GetCustomAttribute<TypeDiscriminationValueAttribute>()?.Value;
                            if (!string.IsNullOrWhiteSpace(discriminationValue))
                            {
                                valueIndicator.Item2.Add(discriminationValue, st);
                            }
                        }
                        break;
                    }
                }
            }
            return result;
        }

        private static void MergeListGeneric<T>(IList<T> target, IList<T> source, ListMergeMode mode)
        {
            switch (mode)
            {
                case ListMergeMode.Clear:
                    target.Clear();
                    foreach (var x in source) target.Add(x);
                    break;
                case ListMergeMode.Append:
                    foreach (var x in source) target.Add(x);
                    break;
                case ListMergeMode.Prepend:
                    // inefficient, performance can definitely be improved
                    foreach (var x in source.Reverse()) target.Insert(0, x);
                    break;
                case ListMergeMode.AppendDistinct:
                    foreach (var x in source)
                    {
                        if (!target.Contains(x)) target.Add(x);
                    }
                    break;
                case ListMergeMode.PrependDistinct:
                    // inefficient, performance can definitely be improved
                    foreach (var x in source.Reverse())
                    {
                        if (!target.Contains(x)) target.Insert(0, x);
                    }
                    break;
                case ListMergeMode.ReplaceItem:
                    for (var i = 0; i < Math.Min(source.Count, target.Count); i++)
                    {
                        target[i] = source[i];
                    }
                    if (source.Count > target.Count)
                    {
                        for (var i = target.Count; i < source.Count; i++)
                        {
                            target.Add(source[i]);
                        }
                    }
                    break;
                case ListMergeMode.MergeItem:
                    for (var i = 0; i < Math.Min(source.Count, target.Count); i++)
                    {
                        Merge(target[i], source[i]);
                    }
                    if (source.Count > target.Count)
                    {
                        for (var i = target.Count; i < source.Count; i++)
                        {
                            target.Add(source[i]);
                        }
                    }
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        private static void MergeList(object target, object source, Type itemType, ListMergeMode mode)
        {
            var mergeMethodInfo = typeof(ConfigModelManager<TRootModel>).GetMethod(nameof(MergeListGeneric), BindingFlags.NonPublic | BindingFlags.Static);
            var genericMergeMethodInfo = mergeMethodInfo.MakeGenericMethod(itemType);
            genericMergeMethodInfo.Invoke(null, new object[] { target, source, mode });
        }

        private static void MergeDictionaryGeneric<T>(IDictionary<string, T> target, IDictionary<string, T> source, DictionaryMergeMode mode)
        {
            switch (mode)
            {
                case DictionaryMergeMode.Clear:
                    target.Clear(); 
                    foreach (var kvp in source) target.Add(kvp.Key, kvp.Value);
                    break;
                case DictionaryMergeMode.ReplaceValue:
                    target.Clear();
                    foreach (var kvp in source) target[kvp.Key] = kvp.Value;
                    break;
                case DictionaryMergeMode.MergeValue:
                    foreach (var kvp in source)
                    {
                        if (target.TryGetValue(kvp.Key, out var v))
                            Merge(v, kvp.Value);
                        else
                            target[kvp.Key] = kvp.Value;
                    }
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        private static void MergeDictionary(object target, object source, Type valueType, DictionaryMergeMode mode)
        {
            var mergeMethodInfo = typeof(ConfigModelManager<TRootModel>).GetMethod(nameof(MergeDictionaryGeneric), BindingFlags.NonPublic | BindingFlags.Static);
            var genericMergeMethodInfo = mergeMethodInfo.MakeGenericMethod(typeof(string), valueType);
            genericMergeMethodInfo.Invoke(null, new object[] { target, source, mode });
        }

        private static void Merge(object target, object source)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (source == null) throw new ArgumentNullException(nameof(source));
            var t = target.GetType();
            if (source.GetType() != t) throw new ArgumentException("Different types in source and target");
            if (t.IsMergableByInterface())
            {
                ((IMergableConfigModel)target).UpdateWith(source);
            }
            else
            {
                foreach (var p in t.GetModelProperties())
                {
                    if (!p.CanRead || !p.CanWrite) continue;
                    var sv = p.GetValue(source, null);
                    if (sv == null) continue;
                    var tv = p.GetValue(target, null);
                    if (tv == null)
                    {
                        p.SetValue(target, sv);
                        continue;
                    }
                    if (p.PropertyType.IsMergable())
                    {
                        Merge(tv, sv);
                        continue;
                    }
                    var listElementType = GetListElementType(p.PropertyType);
                    if (listElementType != null)
                    {
                        var listMergeMode = p.GetCustomAttribute<MergeListAttribute>()?.MergeMode ?? ListMergeMode.Clear;
                        MergeList(tv, sv, listElementType, listMergeMode);
                        continue;
                    }
                    var mapValueType = GetMapValueType(p.PropertyType);
                    if (mapValueType != null)
                    {
                        var mapMergeMode = p.GetCustomAttribute<MergeDictionaryAttribute>()?.MergeMode
                            ?? (mapValueType.IsMergable()
                                ? DictionaryMergeMode.MergeValue
                                : DictionaryMergeMode.ReplaceValue);
                        MergeDictionary(tv, sv, mapValueType, mapMergeMode);
                        continue;
                    }

                    p.SetValue(target, sv);
                }
            }
        }

        private void LoadStringSources(ConfigModelBase model, string filename)
        {
            if (model?.StringSources == null) return;
            var rootPath = Path.GetDirectoryName(filename);
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

        private void LoadStringSources(object o, string filename)
        {
            if (o == null) return;
            var t = o.GetType();
            if (typeof(ConfigModelBase).IsAssignableFrom(t))
            {
                LoadStringSources((ConfigModelBase)o, filename);
            }
            var listElementType = GetListElementType(t);
            if (listElementType != null)
            {
                throw new NotImplementedException();
            }
            var mapValueType = GetMapValueType(t);
            if (mapValueType != null)
            {
                throw new NotImplementedException();
            }

            foreach (var p in t.GetModelProperties())
            {
                throw new NotImplementedException();
            }
        }

        public TRootModel LoadModel(
            TRootModel defaultModel = null,
            YamlDeserializerBuildCustomizer customizer = null)
        {
            var builder = new DeserializerBuilder();
            builder = builder.IgnoreUnmatchedProperties();
            builder = builder.WithNamingConvention(_propertyNamingConvention);

            var discriminationsByPropertyExistence = GetTypeDiscriminationsByPropertyExistence();
            var discriminationsByPropertyValue = GetTypeDiscriminationsByPropertyValue();
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
                    Merge(root, defaultModel);
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
                    Merge(root, layer);
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
