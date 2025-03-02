using System;
using Balatron.Models;
using MoonSharp.Interpreter;

namespace Balatron.Services
{
    public static class LuaParser
    {
        // Parses a Lua string (which starts with "return") into a LuaNode tree.
        public static LuaNode Parse(string text)
        {
            // Initialize the MoonSharp interpreter
            Script script = new Script();
            // Execute the Lua script; the file should begin with "return { ... }"
            DynValue ret = script.DoString(text);
            if (ret.Type != DataType.Table)
                throw new Exception("Lua file did not return a table.");

            Table luaTable = ret.Table;
            // Create a dummy root node and convert the Lua table into our tree
            LuaNode root = new LuaNode { Key = "Root" };
            ConvertTable(luaTable, root);
            return root;
        }

        // Recursively converts a MoonSharp Table into a LuaNode hierarchy.
        private static void ConvertTable(Table table, LuaNode parent)
        {
            foreach (var pair in table.Pairs)
            {
                // Convert the key to a string. You may adjust formatting as needed.
                string key = pair.Key.ToPrintString();
                LuaNode node = new LuaNode { Key = key, Parent = parent };

                if (pair.Value.Type == DataType.Table)
                {
                    ConvertTable(pair.Value.Table, node);
                }
                else if (pair.Value.Type == DataType.String)
                {
                    // Use .String so that numeric-looking strings remain strings.
                    node.Value = pair.Value.String;
                }
                else
                {
                    // For other types (Number, Boolean, etc.), use ToPrintString.
                    node.Value = pair.Value.ToPrintString();
                }
                parent.Children.Add(node);
            }
        }
    }
}