using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Puppet
{
    public static class ScriptParser
    {
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

        public static Script FromPath(string path)
        {
            string[] lines = File.ReadAllLines(path);
            return ParseLines(lines);
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
