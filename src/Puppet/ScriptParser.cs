using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Puppet
{
    public static class ScriptParser
    {
        public static Script ParseChars(string input)
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
                    if (i >= charLength) throw new ScriptException($"No closing bracket ('}}') found for statement #{statementIndex} (Startline {startLine}.");
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
                            escaped = true;
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

        public static Script ParseLines(string[] lines)
        {
            bool inBlockComment = false;
            int statementIndex = 0;
            List<ScriptStatement> statements = new();

            for (int i = 0; i < lines.Length; i++)
            {
                string t = lines[i].Trim();
                if (t.StartsWith('#'))
                {
                    inBlockComment = !inBlockComment;
                    continue;
                }
                if (inBlockComment) continue;
                if (t.StartsWith("//")) continue;
                if (string.IsNullOrWhiteSpace(t)) continue;

                // A statement is present, start builing:
                string commandHead = t.Trim();
                int startLine = i + 1;

                // If it's the last line, no payload.
                if (startLine == lines.Length)
                {
                    statements.Add(new ScriptStatement(statementIndex++, startLine, startLine, commandHead, ""));
                    continue;
                }

                // If next doesn't start with '{', no payload.
                string next = lines[i + 1].Trim();
                if (!next.StartsWith('{'))
                {
                    statements.Add(new ScriptStatement(statementIndex++, startLine, startLine, commandHead, ""));
                    continue;
                }

                // Payload is present, start building:
                StringBuilder sb = new();
                int braceDepth = 0;

                while (true)
                {
                    i++;
                    if (i >= lines.Length) throw new ScriptException($"No closing bracket found for statement #{statementIndex}", startLine);

                    sb.AppendLine(lines[i]);
                    if (lines[i].Trim().StartsWith('{')) braceDepth++;
                    if (lines[i].Trim().EndsWith('}')) braceDepth--;

                    if (braceDepth == 0) break;
                }
                string json = sb.ToString();
                if (json.TryParseJson()) statements.Add(new ScriptStatement(statementIndex++, startLine, i + 1, commandHead, sb.ToString()));
                else throw new ScriptException($"Invalid JSON for statement #{statementIndex} '{commandHead}' (lines {startLine}-{i + 1})");
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

        public static Script FromPath(string path)
        {
            /*
            string[] lines = File.ReadAllLines(path);
            return ParseLines(lines);
            */
            string chars = File.ReadAllText(path);
            return ParseChars(chars);
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

    public sealed record Script(ScriptMetaData MetaData, IReadOnlyList<ScriptStatement> Statements)
    {
        public string PrintInfo() => MetaData.PrintInfo() + $" {Statements.Count} statements.";
        public string PrintFullInfo()
        {
            StringBuilder sb = new();
            sb.AppendLine(MetaData.PrintInfo());
            sb.AppendLine("\nAll Statements:");
            foreach (ScriptStatement s in Statements) sb.AppendLine($"\n{s.PrintInfo()}");
            return sb.ToString();
        }
    }

    public sealed record ScriptMetaData(string Format, string Name, string Author, DateTime Created)
    {
        public string PrintInfo() => $"{Name}.\nAuthor: {Author}, Created: {Created.ToString("G")}\nFormat: '{Format}'.";
    }
    

    public sealed record ScriptStatement(int Index, int StartLine, int EndLine, string CommandHead, string JsonPayload)
    {
        public string PrintInfo() => $"Statement #{Index} (lines {StartLine}-{EndLine}):\n{CommandHead} {JsonPayload.Replace("\n", " ").Replace("\r", " ").ToSingleLine().Unindent()}";
        public string PrintInfoShort(int truncateLength = 200) => $"#{Index} (line {StartLine}): {CommandHead} {JsonPayload.ToSingleLine().Unindent()}".Truncate(truncateLength);
        public string PrintRef() => $"#{Index} (lines {StartLine}-{EndLine}) '{CommandHead}'.";
    };


}
