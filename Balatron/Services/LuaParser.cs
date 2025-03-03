using System;
using Balatron.Models;

namespace Balatron.Services
{
    public static class LuaParser
    {
        public static LuaNode Parse(string text)
        {
            int pos = 0;
            SkipWhitespace(text, ref pos);
            if (text.Substring(pos).StartsWith("return"))
            {
                pos += "return".Length;
                SkipWhitespace(text, ref pos);
            }
            if (pos < text.Length && text[pos] == '{')
            {
                pos++; // Skip the opening brace
                var root = new LuaNode { Key = "Root", IsTable = true };
                ParseTable(text, ref pos, root);
                return root;
            }
            else
            {
                throw new Exception("Expected '{' at beginning of Lua table.");
            }
        }

        private static void ParseTable(string text, ref int pos, LuaNode parent)
        {
            while (pos < text.Length)
            {
                SkipWhitespace(text, ref pos);
                if (pos >= text.Length)
                    break;
                if (text[pos] == '}')
                {
                    pos++; // Consume the closing brace
                    return;
                }
                // Expect a key in the form: [ "key" ] or [number]
                if (text[pos] == '[')
                {
                    pos++; // skip '['
                    SkipWhitespace(text, ref pos);
                    string key = null;
                    bool quotedKey = false;
                    if (text[pos] == '"')
                    {
                        quotedKey = true;
                        pos++; // skip opening quote
                        int keyStart = pos;
                        while (pos < text.Length && text[pos] != '"')
                        {
                            pos++;
                        }
                        key = text.Substring(keyStart, pos - keyStart);
                        pos++; // skip closing quote
                    }
                    else
                    {
                        int keyStart = pos;
                        while (pos < text.Length && char.IsDigit(text[pos]))
                            pos++;
                        key = text.Substring(keyStart, pos - keyStart);
                    }
                    SkipWhitespace(text, ref pos);
                    if (pos < text.Length && text[pos] == ']')
                    {
                        pos++; // skip ']'
                    }
                    else
                    {
                        throw new Exception("Expected ']' after key");
                    }
                    SkipWhitespace(text, ref pos);
                    if (pos < text.Length && text[pos] == '=')
                    {
                        pos++; // skip '='
                    }
                    else
                    {
                        throw new Exception("Expected '=' after key");
                    }
                    SkipWhitespace(text, ref pos);
                    LuaNode child;
                    if (pos < text.Length && text[pos] == '{')
                    {
                        child = new LuaNode { Key = key, Parent = parent, IsTable = true, ForceQuotedKey = quotedKey };
                        pos++; // skip '{'
                        ParseTable(text, ref pos, child);
                    }
                    else
                    {
                        child = new LuaNode { Key = key, Parent = parent, ForceQuotedKey = quotedKey };
                        int valueStart = pos;
                        while (pos < text.Length && text[pos] != ',' && text[pos] != '}')
                        {
                            pos++;
                        }
                        string value = text.Substring(valueStart, pos - valueStart).Trim();
                        child.Value = value;
                    }
                    parent.Children.Add(child);
                    SkipWhitespace(text, ref pos);
                    if (pos < text.Length && text[pos] == ',')
                    {
                        pos++; // skip comma
                    }
                }
                else
                {
                    pos++;
                }
            }
        }

        private static void SkipWhitespace(string text, ref int pos)
        {
            while (pos < text.Length && char.IsWhiteSpace(text[pos]))
                pos++;
        }
    }
}
