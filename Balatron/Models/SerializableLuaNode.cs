using System.Collections.Generic;

namespace Balatron.Models
{
    public class SerializableLuaNode
    {
        public string Key { get; set; }
        public bool ForceQuotedKey { get; set; }
        public bool IsTable { get; set; }
        public string Value { get; set; }
        public List<SerializableLuaNode> Children { get; set; } = new();
    }
}
