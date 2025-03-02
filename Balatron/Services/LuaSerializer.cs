using System.Text;
using Balatron.Models;

namespace Balatron.Services
{
    public static class LuaSerializer
    {
        // Serializes the LuaNode tree (ignoring the dummy "Root" node) into a compact Lua table string.
        public static string Serialize(LuaNode root, bool readable = false)
        {
            var sb = new StringBuilder();
            sb.Append("return ");
            sb.Append(readable ? SerializeNodesReadable(root.Children) : SerializeNodes(root.Children));
            return sb.ToString();
        }

        private static string SerializeNodes(System.Collections.ObjectModel.ObservableCollection<LuaNode> nodes)
        {
            var sb = new StringBuilder();
            sb.Append("{");
            bool first = true;
            foreach (var node in nodes)
            {
                if (!first)
                    sb.Append(",");
                else
                    first = false;
                
                // Write key: if numeric, output without quotes; otherwise, escape and quote.
                sb.Append("[");
                if (double.TryParse(node.Key, out _))
                    sb.Append(node.Key);
                else
                {
                    sb.Append("\"");
                    sb.Append(node.Key);
                    sb.Append("\"");
                }
                sb.Append("]=");
                
                if (node.Children.Count > 0)
                {
                    sb.Append(SerializeNodes(node.Children));
                }
                else
                {
                    if (node.IsTable)
                    {
                        sb.Append("{}");
                    }
                    else
                    {
                        sb.Append(node.Value);
                    }
                }
            }
            // Add trailing comma if any element was written.
            if (!first)
                sb.Append(",");
            sb.Append("}");
            return sb.ToString();
        }
        
        private static string SerializeNodesReadable(System.Collections.ObjectModel.ObservableCollection<LuaNode> nodes, int indent = 0)
        {
            var sb = new StringBuilder();
            string indentStr = new string(' ', indent * 2);
            sb.Append("{\n");
            bool first = true;
            foreach (var node in nodes)
            {
                if (!first)
                    sb.Append(",\n");
                else
                    first = false;
                sb.Append(indentStr);
                sb.Append("  [");
                if (double.TryParse(node.Key, out _))
                    sb.Append(node.Key);
                else
                {
                    sb.Append("\"");
                    sb.Append(node.Key);
                    sb.Append("\"");
                }
                sb.Append("]=");
                if (node.Children.Count > 0)
                {
                    sb.Append(SerializeNodesReadable(node.Children, indent + 1));
                }
                else
                {
                    if (node.IsTable)
                    {
                        sb.Append("{}");
                    }
                    else
                    {
                        sb.Append(node.Value);
                    }
                }
            }
            if (!first)
            {
                sb.Append(",\n");
                sb.Append(indentStr);
            }
            sb.Append("}");
            return sb.ToString();
        }
    }
}
