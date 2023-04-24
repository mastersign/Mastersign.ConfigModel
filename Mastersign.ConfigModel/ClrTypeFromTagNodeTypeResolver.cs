using System;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Mastersign.ConfigModel
{
    public class ClrTypeFromTagNodeTypeResolver : INodeTypeResolver
    {
        public bool Resolve(NodeEvent nodeEvent, ref Type currentType)
        {
            if (nodeEvent.Tag.IsEmpty || nodeEvent.Tag.IsNonSpecific) return false;
            if (!nodeEvent.Tag.Value.StartsWith("!clr:")) return false;

            var netTypeName = nodeEvent.Tag.Value.Substring(5);
            var type = Type.GetType(netTypeName);
            if (type == null) return false;

            currentType = type;
            return true;
        }
    }
}
