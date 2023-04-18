using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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

        private static readonly bool _isMergable;

        static ConfigModelManager() 
        {
            _isMergable = typeof(TRootModel) is IMergableConfigModel
                || typeof(TRootModel).GetCustomAttribute<MergableConfigModelAttribute>() != null;
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
            PropertyNameHandling propertyNameHandling = PropertyNameHandling.CamelCase)
        {
            _filenameComparison = filenameComparison;
            _propertyNamingConvention = NamingConventionFor(propertyNameHandling);
        }

        public string AddLayer(string filename)
        {
            if (!_isMergable && _layers.Count > 0)
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
            if (!_isMergable)
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
            throw new NotImplementedException();
        }

        private Dictionary<Type, Tuple<string, Dictionary<string, Type>>> GetTypeDiscriminationsByPropertyValue()
        {
            throw new NotImplementedException();
        }

        private void Merge(object a, object b)
        {
            if (a == null) throw new ArgumentNullException(nameof(a));
            if (b == null) throw new ArgumentNullException(nameof(b));
            var t = a.GetType();
            if (b.GetType() != t) throw new ArgumentException("Different types in given layers");
            if (t is IMergableConfigModel)
            {
                ((IMergableConfigModel)a).UpdateWith(b);
            }
            else
            {
                // TODO implement merging via reflection
                throw new NotImplementedException();
            }
        }

        public TRootModel LoadModel(YamlDeserializerBuildCustomizer customizer = null)
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

            TRootModel root = _isMergable ? new TRootModel() : null;
            foreach (var layerSource in _layers)
            {
                TRootModel layer;
                using (var s = File.Open(layerSource, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var r = new StreamReader(s))
                {
                    layer = deserializer.Deserialize<TRootModel>(r);
                }
                // TODO load includes
                // TODO load sources
                if (_isMergable)
                {
                    Merge(root, layer);
                }
            }

            throw new NotImplementedException();
            return root;
        }

        public event ModelChangeHandler<TRootModel> ModelChanged;

        public void WatchForChanges()
        {

        }
    }
}
