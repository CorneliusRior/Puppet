using Puppet.Tools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Puppet.Scripting
{
    public static class ScriptParser
    {
        public async static Task<Script> ParseChars(string input)
        {
            bool inBlockComment = false;
            bool inLineComment = false;
            int statementIndex = 0;
            int lineIndex = 1;
            int charLength = input.Length;
            List<ScriptStatement> statements = new();

            for (int i  = 0; i < charLength; i++)
            {
                char c = input[i];
                if (c == '\n')
                {
                    lineIndex++;
                    inLineComment = false;
                    continue;
                }
                if (c == '#')
                {
                    inBlockComment = !inBlockComment;
                    continue;
                }
                if (inBlockComment || inLineComment) continue;
                if (c == '/')
                {
                    inLineComment = true;
                    continue;
                }
                if (char.IsWhiteSpace(c)) continue;

                // A statement is present, start building:
                int startLine = lineIndex;
                StringBuilder sb = new();
                while (true)
                {
                    sb.Append(input[i]);
                    i++;
                    if (i >= charLength) throw new ScriptException($"Statement #{statementIndex} (line {lineIndex}, at end of document) has no JSON arguments.", startLine);
                    if (char.IsWhiteSpace(input[i]) || input[i] == '\n' || input[i] == '{') break;
                }
                string commandHead = sb.ToString();
                sb.Clear();

                // Find JSON:
                while (true)
                {
                    if (input[i] == '{' && !inLineComment && !inBlockComment)
                    {
                        sb.Append(input[i]);
                        break;
                    }
                    i++;
                    if (i >= charLength) throw new ScriptException($"Statement #{statementIndex} (line {lineIndex}, at end of document) has no JSON arguments.", startLine);

                    // Repeat comment/whitespace filtering:
                    if (input[i] == '\n')
                    {
                        lineIndex++;
                        inLineComment = false;
                        continue;
                    }
                    if (input[i] == '#')
                    {
                        inBlockComment = !inBlockComment;
                    }
                    if (inBlockComment || inLineComment) continue;
                    if (c == '/')
                    {
                        inLineComment = true;
                        continue;
                    }
                    if (char.IsWhiteSpace(input[i])) continue;
                }

                // Build JSON:
                int braceDepth = 1;
                bool inString = false;
                bool escaped = false;
                while (braceDepth > 0)
                {
                    i++;
                    if (i >= charLength) throw new ScriptException($"No closing bracket ('}}') found for statement #{statementIndex} '{commandHead}' (Startline {startLine}).");
                    char ch = input[i];
                    sb.Append(ch);
                    if (ch == '\n') lineIndex++;

                    // Determine if in string:
                    if (inString)
                    {
                        if (escaped)
                        {
                            escaped = false;
                            continue;
                        }
                        if (ch == '\\')
                        {
                            escaped = true;
                            continue;
                        }
                        if (ch == '"')
                        {
                            inString = false;
                            continue;
                        }
                        continue;
                    }
                    if (ch == '"')
                    {
                        inString = true;
                        continue;
                    }

                    // Not in string, handle braceDepth:
                    if (ch == '{') braceDepth++;
                    if (ch == '}') braceDepth--;
                }

                string json = sb.ToString();
                if (json.TryParseJson()) statements.Add(new ScriptStatement(statementIndex++, startLine, lineIndex, commandHead, json));
                else throw new ScriptException($"Invalid JSON for statement #{statementIndex} '{commandHead}' (lines {startLine}-{lineIndex})");
            }

            if (statements.Count == 0) throw new ScriptException("Script contains no statements.");
            if (!statements[0].CommandHead.Equals("ScriptMetaData", StringComparison.OrdinalIgnoreCase)) throw new ScriptException($"First statement must be ScriptMetaData", statements[0].StartLine);

            ScriptMetaData metaData = statements[0].JsonPayload.ParseMetaData(statements[0].StartLine);
            return new Script(metaData, statements.Skip(1).ToList());                
        }        

        private static void p(string msg = "Reached.", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string name = "")
        {
            Debug.WriteLine(file + $"(Line {line})" + name + "(): " + msg);
        }

        public async static Task<Script> FromPath(string path)
        {
            /*
            string[] lines = File.ReadAllLines(path);
            return ParseLines(lines);
            */
            string chars = File.ReadAllText(path);
            return await ParseChars(chars);
        }

        private static bool TryParseJson(this string json)
        {
            try
            {
                using var _ = JsonDocument.Parse(json);
                return true;
            }
            catch (JsonException) { return false; }
        }

        public static ScriptMetaData ParseMetaData(this string input, int? line = null) =>
            JsonSerializer.Deserialize<ScriptMetaData>(input) ?? throw new ScriptException($"Could not parse metadata, first statement must be metadata.", line);
    }   
}
