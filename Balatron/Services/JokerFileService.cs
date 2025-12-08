using System.IO;
using System.Text.Json;
using Balatron.Models;

namespace Balatron.Services
{
    public static class JokerFileService
    {
        public static void ExportJoker(LuaNode jokerNode, string filePath)
        {
            if (jokerNode == null)
                return;

            var serializable = ToSerializableNode(jokerNode);
            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(filePath, JsonSerializer.Serialize(serializable, options));
        }

        public static LuaNode ImportJoker(string filePath)
        {
            if (!File.Exists(filePath))
                return null;

            var content = File.ReadAllText(filePath);
            var data = JsonSerializer.Deserialize<SerializableLuaNode>(content);
            return data == null ? null : FromSerializableNode(data, null);
        }

        private static SerializableLuaNode ToSerializableNode(LuaNode node)
        {
            var serializable = new SerializableLuaNode
            {
                Key = node.Key,
                ForceQuotedKey = node.ForceQuotedKey,
                IsTable = node.IsTable,
                Value = node.Value
            };

            foreach (var child in node.Children)
            {
                serializable.Children.Add(ToSerializableNode(child));
            }

            return serializable;
        }

        private static LuaNode FromSerializableNode(SerializableLuaNode data, LuaNode parent)
        {
            var node = new LuaNode
            {
                Key = data.Key,
                ForceQuotedKey = data.ForceQuotedKey,
                IsTable = data.IsTable,
                Value = data.Value,
                Parent = parent
            };

            foreach (var child in data.Children)
            {
                var converted = FromSerializableNode(child, node);
                node.Children.Add(converted);
            }

            return node;
        }
    }
}
