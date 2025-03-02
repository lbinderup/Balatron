using System.Text;
using Balatron.Models;

namespace Balatron.Services
{
    public static class LuaSerializer
    {
        // Serializes the LuaNode tree (ignoring the dummy "Root" node) into a Lua table string.
        public static string Serialize(LuaNode root)
        {
            var sb = new StringBuilder();
            sb.Append("return ");
            sb.Append(SerializeNodes(root.Children, 0));
            return sb.ToString();
        }

        private static string SerializeNodes(System.Collections.ObjectModel.ObservableCollection<LuaNode> nodes, int indent)
        {
            var sb = new StringBuilder();
            string indentStr = new string(' ', indent * 2);
            sb.Append("{\n");
            foreach (var node in nodes)
            {
                sb.Append(indentStr);
                sb.Append("  [\"");
                sb.Append(node.Key);
                sb.Append("\"]=");
                if (node.Children.Count > 0)
                {
                    sb.Append(SerializeNodes(node.Children, indent + 1));
                }
                else
                {
                    // If the value is not numeric or a boolean, wrap it in quotes.
                    if (double.TryParse(node.Value, out _) || node.Value == "true" || node.Value == "false")
                    {
                        sb.Append(node.Value);
                    }
                    else
                    {
                        sb.Append("\"" + node.Value + "\"");
                    }
                }
                sb.Append(",\n");
            }
            sb.Append(indentStr);
            sb.Append("}");
            return sb.ToString();
        }
    }
}