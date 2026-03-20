using Puppet.Models;
using System.Text;

namespace Puppet.Tools;

/// <summary>
/// Methods for helping handle arguments in PuppetCommands (Human Input). Contains:
///  - Tokenize
///  - Argument extractors
/// </summary>
public static class ArgumentHelpers
{
    public static List<string> Tokenize(this string input) 
    {
        List<string> tokens = new();
        if (string.IsNullOrWhiteSpace(input)) return tokens;

        StringBuilder sb = new();
        bool inQuotes = false;
        bool lastCharSlash = false;

        foreach (char c in input.Trim())
        {
            if (c == '"' && !lastCharSlash)
            {
                inQuotes = !inQuotes;
                continue;
            }
            if (c == '\\' && !lastCharSlash)
            {
                lastCharSlash = true;
                sb.Append(c);
                continue;
            }
            if (char.IsWhiteSpace(c) && !inQuotes)
            {
                if (sb.Length > 0) 
                {
                    tokens.Add(sb.ToString());
                    sb.Clear();
                }
                lastCharSlash = false;
                continue;
            }
            sb.Append(c);
            lastCharSlash = false;
        }
        if (inQuotes) throw new FormatException("Unmatched quotes");
        if (sb.Length > 0) tokens.Add(sb.ToString());
        return tokens;
    }

    // Argument extractors:

    /// <summary>
    /// Returns specified string, or throws exception if none present.
    /// </summary>
    /// <param name="index">Position of desired argument in "args"e</param>
    /// <param name="name">Assigned name for string, included in exception message</param>
    public static string String(this IReadOnlyList<string> args, int index, string name)
    {
        if (index >= args.Count) throw new PuppetUserException($"Not enough arguments, missing string '{name}'.");
        return args[index];
    }

    /// <summary>
    /// Gets the specified string, or returns the default if not present. "Fallback" is not nullable, use StringOrNull if you want nullable return.
    /// </summary>
    /// <param name="index">Position of desired argument in "args"</param>
    /// <param name="name">Assigned name for string, included in exception message</param>
    /// <param name="fallBack">Default return value</param>
    public static string StringOr(this IReadOnlyList<string> args, int index, string name, string fallBack)
    {
        if (index >= args.Count) return fallBack;
        return args[index];
    }

    /// <summary>
    /// Gets the specified string, or returns the default/fallBack if it is not present or equal to '_', use this for when you want to maintain defaults.
    /// </summary>
    /// <param name="index">Position of desired argument in "args"</param>
    /// <param name="name">Assigned name for string, included in exception message</param>
    /// <param name="fallBack">Default return value</param>
    public static string StringOrDefault(this IReadOnlyList<string> args, int index, string name, string fallBack)
    {
        if (index >= args.Count) return fallBack;
        if (args[index] == "_") return fallBack;
        return args[index];
    }

    /// <summary>
    /// Gets the specified string, or returns the default/fallBack if it is not present or equal to '_', fallBack can be null. Use this for when you want to maintain nullable defaults. 
    /// </summary>
    /// <param name="index">Position of desired argument in "args"</param>
    /// <param name="name">Assigned name for string, included in exception message</param>
    /// <param name="fallBack">Default return value</param>
    public static string? StringNullableOrDefault(this IReadOnlyList<string> args, int index, string name, string? fallBack)
    {
        if (index >= args.Count) return fallBack;
        if (args[index] == "_") return fallBack;
        return args[index];
    }

    /// <summary>
    /// Gets the specified string, or returns null.
    /// </summary>
    /// <param name="index">Position of desired argument in "args"</param>
    /// <param name="name">Assigned name for string, included in exception message</param>
    public static string? StringOrNull(this IReadOnlyList<string> args, int index, string name)
    {
        if (index >= args.Count) return null;
        if (string.IsNullOrWhiteSpace(args[index])) return null;
        return args[index];
    }

    /// <summary>
    /// Gets the specified bool, throws exception if not present or cannot parse.
    /// </summary>
    /// <param name="index">Position of desired argument in "args"</param>
    /// <param name="name">Assigned name for string, included in exception message</param>
    public static bool Bool(this IReadOnlyList<string> args, int index, string name)
    {
        if (index >= args.Count) throw new PuppetUserException($"Not enough arguments, missing bool '{name}'.");
        if (!bool.TryParse(args[index], out bool v)) throw new PuppetUserException($"Cannot parse bool '{name}': '{args[index]}'.");
        else return v;
    }

    /// <summary>
    /// Gets the specified bool, returns fallback if not present or equal to '_', and throws exception if cannot parse.
    /// </summary>
    /// <param name="index">Position of desired argument in "args"</param>
    /// <param name="name">Assigned name for string, included in exception message</param>
    /// <param name="fallBack">Default return value</param>
    public static bool BoolOr(this IReadOnlyList<string> args, int index, string name, bool fallBack)
    {
        if (index >= args.Count) return fallBack;
        if (args[index] == "_") return fallBack;
        if (!bool.TryParse(args[index], out bool v)) throw new PuppetUserException($"Cannot parse bool '{name}': '{args[index]}'.");
        else return v;
    }

    /// <summary>
    /// Gets the specified bool, returns null if not present and exception if cannot parse.
    /// </summary>
    /// <param name="index">Position of desired argument in "args"</param>
    /// <param name="name">Assigned name for string, included in exception message</param>
    public static bool? BoolOrNull(this IReadOnlyList<string> args, int index, string name)
    {
        if (index >= args.Count) return null;
        if (!bool.TryParse(args[index], out bool v)) throw new PuppetUserException($"Cannot parse bool '{name}': '{args[index]}'.");
        else return v;
    }

    /// <summary>
    /// Gets the specified double from a list of arguments, throws an exception if not present or cannot parse.
    /// </summary>
    /// <param name="index">Position of desired argument in "args"</param>
    /// <param name="name">Assigned name for string, included in exception message</param>
    public static double Double(this IReadOnlyList<string> args, int index, string name)
    {
        if (index >= args.Count) throw new PuppetUserException($"Not enough arguments, missing double '{name}'.");
        if (!double.TryParse(args[index], out double v)) throw new PuppetUserException($"Cannot parse double '{name}': '{args[index]}'.");
        return v;
    }

    /// <summary>
    /// Gets the specified double from a list of arguments, returns fallback if not present or is equal to '_', throws exception if cannot parse.
    /// </summary>
    /// <param name="index">Position of desired argument in "args"</param>
    /// <param name="name">Assigned name for string, included in exception message</param>
    /// <param name="fallBack">Default return value</param>
    public static double DoubleOr(this IReadOnlyList<string> args, int index, string name, double fallBack)
    {
        if (index >= args.Count) return fallBack;
        if (args[index] == "_") return fallBack;
        if (!double.TryParse(args[index], out double v)) throw new PuppetUserException($"Cannot parse double '{name}': '{args[index]}'.");
        return v;
    }

    /// <summary>
    /// Gets the specified double from a list of arguments, returns fallback if not present or is equal to '_', fallback is nullable. throws exception if cannot parse.
    /// </summary>
    /// <param name="index">Position of desired argument in "args"</param>
    /// <param name="name">Assigned name for string, included in exception message</param>
    /// <param name="fallBack">Default return value</param>
    public static double? DoubleOrNullable(this IReadOnlyList<string> args, int index, string name, double? fallBack)
    {
        if (index >= args.Count) return fallBack;
        if (args[index] == "_") return fallBack;
        if (!double.TryParse(args[index], out double v)) throw new PuppetUserException($"Cannot parse double '{name}': '{args[index]}'.");
        return v;
    }

    /// <summary>
    /// Gets the specified double from a list of arguments, returns null if not present, throws exception if cannot parse.
    /// </summary>
    /// <param name="index">Position of desired argument in "args"</param>
    /// <param name="name">Assigned name for string, included in exception message</param>
    /// <returns></returns>
    /// <exception cref="PuppetUserException"></exception>
    public static double? DoubleOrNull(this IReadOnlyList<string> args, int index, string name)
    {
        if (index >= args.Count) return null;
        if (!double.TryParse(args[index], out double v)) throw new PuppetUserException($"Cannot parse double '{name}': '{args[index]}'.");
        return v;
    }

    /// <summary>
    /// Gets the specified integer, throws exception if not present or cannot parse.
    /// </summary>
    /// <param name="index">Position of desired argument in "args"</param>
    /// <param name="name">Assigned name for string, included in exception message</param>
    public static int Int(this IReadOnlyList<string> args, int index, string name)
    {
        if (index >= args.Count) throw new PuppetUserException($"Not enough arguments, missing int '{name}'");
        if (!int.TryParse(args[index], out int v)) throw new PuppetUserException($"Cannot parse int '{name}': .{args[index]}'.");
        return v;
    }

    /// <summary>
    /// Gets the specified integer, returns fallback if not oresent or is equal to '_', throws exception if cannot parse.
    /// </summary>
    /// <param name="index">Position of desired argument in "args"</param>
    /// <param name="name">Assigned name for string, included in exception message</param>
    /// <param name="fallBack">Default return value</param>
    public static int IntOr(this IReadOnlyList<string> args, int index, string name, int fallBack)
    {
        if (index >= args.Count) return fallBack;
        if (args[index] == "_") return fallBack;
        if (!int.TryParse(args[index], out int v)) throw new PuppetUserException($"Cannot parse int '{name}': .{args[index]}'.");
        return v;
    }

    /// <summary>
    /// Gets the specified integer, returns null if not present, throws exception if cannot parse.
    /// </summary>
    /// <param name="index">Position of desired argument in "args"</param>
    /// <param name="name">Assigned name for string, included in exception message</param>
    public static int? IntOrNull(this IReadOnlyList<string> args, int index, string name)
    {
        if (index >= args.Count) return null;
        if (!int.TryParse(args[index], out int v)) throw new PuppetUserException($"Cannot parse int '{name}': .{args[index]}'.");
        return v;
    }

    /// <summary>
    /// Returns the specified integer, returns fallback if not present or is equal to '_', throw exception if cannot parse.
    /// </summary>
    /// <param name="index">Position of desired argument in "args"</param>
    /// <param name="name">Assigned name for string, included in exception message</param>
    /// <param name="fallBack">Default return value</param>
    public static int? IntOrNullable(this IReadOnlyList<string> args, int index, string name, int? fallBack)
    {
        if (index >= args.Count) return fallBack;
        if (args[index] == "_") return fallBack;
        if (!int.TryParse(args[index], out int v)) throw new PuppetUserException($"Cannot parse int '{name}': .{args[index]}'.");
        return v;
    }

    /// <summary>
    /// Returns specified DateTime from a list of arguments, throws an exception if not present or cannot parse.
    /// </summary>
    /// <param name="index">Position of desired argument in "args"</param>
    /// <param name="name">Assigned name for string, included in exception message</param>
    public static DateTime dateTime(this IReadOnlyList<string> args, int index, string name)
    {
        if (index >= args.Count) throw new PuppetUserException($"Not enough arguments, missing DateTme '{name}'");
        if (!DateTime.TryParse(args[index], out DateTime v)) throw new PuppetUserException($"Cannot Parse DateTIme '{name}': '{args[index]}'.");
        return v;
    }

    /// <summary>
    /// Returns specified DateTime from a list of arguments, returns fallabck if not present or equal to '_' and exception if cannot parse.
    /// </summary>
    /// <param name="index">Position of desired argument in "args"</param>
    /// <param name="name">Assigned name for string, included in exception message</param>
    /// <param name="fallBack">Default return value</param>
    public static DateTime dateTimeOr(this IReadOnlyList<string> args, int index, string name, DateTime fallBack)
    {
        if (index >= args.Count) return fallBack;
        if (args[index] == "_") return fallBack;
        if (!DateTime.TryParse(args[index], out DateTime v)) throw new PuppetUserException($"Cannot parse DateTime '{name}': '{args[index]}'.");
        return v;
    }

    /// <summary>
    /// Returns specified DateTime from a list of arguments, returns null if not present and exception if cannot parse.
    /// </summary>
    /// <param name="index">Position of desired argument in "args"</param>
    /// <param name="name">Assigned name for string, included in exception message</param>
    public static DateTime? dateTimeOrNull(this IReadOnlyList<string> args, int index, string name)
    {
        if (index >= args.Count) return null;
        if (!DateTime.TryParse(args[index], out DateTime v)) throw new PuppetUserException($"Cannot parse DateTime '{name}': '{args[index]}'.");
        return v;
    }

    public static bool ParseConfirmation(this string input, bool? fallBack = null)
    {
        string[] yesString = ["yes", "y", "affirmative", "1", "true", "+"];
        string[] noString = ["no", "n", "negative", "0", "false", "-"];
        if (yesString.Contains(input.ToLowerInvariant().Trim())) return true;
        if (noString.Contains(input.ToLowerInvariant().Trim())) return false;
        if (fallBack is null) throw new PuppetUserException($"Cannot parse {input} to bool.");
        return fallBack.Value;
    }
}