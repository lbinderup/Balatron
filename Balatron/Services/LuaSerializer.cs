using System.Text;
using Balatron.Models;

namespace Balatron.Services
{
    public static class LuaSerializer
    {
        // Serializes the LuaNode tree (ignoring the dummy "Root" node) into a Lua table string.
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
                
                sb.Append("[\"");
                sb.Append(EscapeString(node.Key));
                sb.Append("\"]=");
                if (node.Children.Count > 0)
                {
                    sb.Append(SerializeNodes(node.Children));
                }
                else
                {
                    if (double.TryParse(node.Value, out _) || node.Value == "true" || node.Value == "false")
                    {
                        sb.Append(node.Value);
                    }
                    else
                    {
                        sb.Append("\"");
                        sb.Append(EscapeString(node.Value));
                        sb.Append("\"");
                    }
                }
            }
            sb.Append("}");
            return sb.ToString();
        }
        
        private static string SerializeNodesReadable(System.Collections.ObjectModel.ObservableCollection<LuaNode> nodes, int indent = 0)
        {
            var sb = new StringBuilder();
            string indentStr = new string(' ', indent * 2);
            sb.Append("{\n");
            foreach (var node in nodes)
            {
                sb.Append(indentStr);
                sb.Append("  [\"");
                sb.Append(EscapeString(node.Key));
                sb.Append("\"]=");
                if (node.Children.Count > 0)
                {
                    sb.Append(SerializeNodesReadable(node.Children, indent + 1));
                }
                else
                {
                    if (double.TryParse(node.Value, out _) || node.Value == "true" || node.Value == "false")
                    {
                        sb.Append(node.Value);
                    }
                    else
                    {
                        sb.Append("\"");
                        sb.Append(EscapeString(node.Value));
                        sb.Append("\"");
                    }
                }
                sb.Append(",\n");
            }
            sb.Append(indentStr);
            sb.Append("}");
            return sb.ToString();
        }
        
        private static string EscapeString(string input)
        {
            return input?.Replace("\"", "\\\"") ?? "";
        }
    }
}