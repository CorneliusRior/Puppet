using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Puppet;

public sealed class PuppetUserException : Exception
{
    public string Location { get; }
    public PuppetUserException(string message, [CallerMemberName] string member = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0) : base (message)
    {
        Location = $"{Path.GetFileName(file)} (line {line}) {member}():";
    }    
}

public sealed class PuppetException : Exception
{
    public string Location { get; }
    public PuppetException(string message, [CallerMemberName] string member = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0) : base(message)
    {
        Location = $"{Path.GetFileName(file)} (line {line}) {member}():";
    }
}

public sealed class ScriptException : Exception
{
    public string Location { get; }
    public ScriptException(string message, int? scriptLineNumber = null, [CallerMemberName] string member = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0) : base (message)
    {
        Location = $"{Path.GetFileName(file)} (line {line}) {member}(), Script Line='{(scriptLineNumber.ToString() ?? "unspecified")}':";
    }
}


public enum HelpAttribute
{
    Aliases,
    Usage,
    Description,
    Examples,
    LongDescription
}

public static class HelpAttributeExtensions
{
    public static bool TryParse(string input, out HelpAttribute output) => Enum.TryParse(input, true, out output);
}