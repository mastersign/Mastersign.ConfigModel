using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace Mastersign.ConfigModel
{
    public abstract class ConfigModelBase
    {
        [NoMerge]
        [YamlMember(Alias = "$includes",
            DefaultValuesHandling = DefaultValuesHandling.OmitNull,
            ApplyNamingConventions = false)]
        public List<string> ConfigModelIncludes { get; set; }

        [NoMerge]
        [YamlMember(Alias = "$sources",
            DefaultValuesHandling = DefaultValuesHandling.OmitNull,
            ApplyNamingConventions = false)]
        public Dictionary<string, string> ConfigModelStringSources { get; set; }

        private readonly List<string> _layers = new List<string>();

        internal void PushLayer(string filename) => _layers.Add(filename);

        public string[] GetLayers() => _layers.ToArray();
    }
}
