using System.Text;

namespace Puppet;

public static class StringHelpers
{
    public static string ToSingleLine(this string input) => input.Replace("\r\n", " ").Replace("\r", " ").Replace("\n", " ");
    public static string? ToSingleLineNullable(this string? input) => input is null ? null : input.Replace("\r\n", " ").Replace("\r", " ").Replace("\n", " ");
    public static string Unindent(this string input) => input.Replace("\t", "");

    public static string Truncate(this string input, int length, string truncateString = "…")
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        input = input.Replace("\r\n", " ").Replace("\r", " ").Replace("\n", " ");
        length = Math.Abs(length);
        if (length <= truncateString.Length) return truncateString[..(Math.Max(0, length))];
        if (input.Length <= length) return input;
        return input[..(length - truncateString.Length)] + truncateString;
    }

    public static string? TruncateNullable(this string? input, int length, string truncateString = "…")
    {
        if (input is null) return null;
        return input.Truncate(length, truncateString);
    }

    public static string ToStringTruncate(this double input, int length, string format = "0.#", string prefix = "", string suffix = "", string truncateString = "…")
    {
        string output = prefix + input.ToString(format) + suffix;
        output = output.Truncate(length, truncateString);
        return output;
    }

    public static string ToStringTruncate(this int input, int length, string prefix = "", string suffix = "", string truncateString = "…")
    {
        string output = prefix + input.ToString() + suffix;
        output = output.Truncate(length, truncateString);
        return output;
    }

    /// <summary>
    /// Returns a string depending on if the bool is true or false. Default return value for true is "[x]". amd false if "[ ]". If invert is set true, inverts return. Used to display bools as strings. Checked and UnChecked strings can be set as anything with no character limits.
    /// </summary>
    /// <param name="check">Bool to represent as string.</param>
    /// <param name="invert">Inverts the return.</param>
    /// <param name="checkedString">String returned if "check" is true (unless invert)</param>
    /// <param name="unCheckedString">String returned if "check" is false (unless invert)</param>
    public static string Checked(this bool check, bool invert = false, string checkedString = "[x]", string unCheckedString = "[ ]" )
    {
        if (invert) return check ? unCheckedString : checkedString;
        else return check ? checkedString : unCheckedString;
    }

    /// <summary>
    /// This is for very specific kind of case. For listing things out.
    /// 
    /// Does this basically:
    /// Header: ListItem1
    ///         ListItem2
    ///         ListItem3
    /// </summary>
    /// <param name="input"></param>
    /// <param name="leftMargin"></param>
    /// <returns></returns>
    public static string AlignList(this List<string> input, int leftMargin, int? rightMargin = null)
    {
        if (input.Count == 0) return "";

        List<string> inter = new();
        if (rightMargin is not null) foreach (string l in input) inter.Add(l.Truncate(rightMargin.Value));
        else inter = input;

        if (inter.Count == 1) return inter[1];
        StringBuilder sb = new();
        sb.AppendLine(inter[0]);
        foreach (string l in inter.Skip(1)) sb.AppendLine(new string(' ', leftMargin) + l);
        return (sb.ToString());
    }

    public static string ToBox(this string msg)
    {
        if (string.IsNullOrWhiteSpace(msg)) return "┌─┐\n└─┘";

        string[] lines = msg.Split(new[] {"\r\n", "\n" }, StringSplitOptions.None);
        int msgWidth = lines.Max(s => s.Length);
        int msgHeight = lines.Length;

        string vert = new string('─', msgWidth + 2);
        StringBuilder sb = new();
        sb.AppendLine('┌' + vert + '┐');
        foreach (string l in lines) sb.AppendLine("│ " + l.PadRight(msgWidth) + " │");
        sb.AppendLine('└' + vert + '┘');
        return sb.ToString();
    }      
    
    public static string ToDoubleBox(this string msg)
    {
        if (string.IsNullOrWhiteSpace(msg)) return "╔═╗\n╚═╝";

        string[] lines = msg.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        int msgWidth = lines.Max(s => s.Length);
        int msgHeight = lines.Length;

        string vert = new string('═', msgWidth + 2);
        StringBuilder sb = new();
        sb.AppendLine('╔' + vert + '╗');
        foreach (string l in lines) sb.AppendLine("║ " + l.PadRight(msgWidth) + " ║");
        sb.AppendLine('╚' + vert + '╝');
        return sb.ToString();
    }
}                 