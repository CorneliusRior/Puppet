using Puppet.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Puppet.Scripting
{
    public sealed class ScriptException : Exception
    {
        public string Location { get; }
        public ScriptException(string message, int? scriptLineNumber = null, [CallerMemberName] string member = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0) : base(message)
        {
            Location = $"{Path.GetFileName(file)} (line {line}) {member}(), Script Line='{(scriptLineNumber.ToString() ?? "unspecified")}':";
        }
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
