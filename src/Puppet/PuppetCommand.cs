using System.Diagnostics.CodeAnalysis;

namespace Puppet;

public class PuppetCommand
{
    public required string Name { get; init; }
    public string? Address;
    public IReadOnlyList<string> Aliases { get; init; }
    public IReadOnlyList<PuppetCommand> Children { get; init; }
    
    public Func<PuppetContext, IReadOnlyList<string>, CancellationToken, Task>? ExecuteAsync { get; init; }
    public bool CanExecute => ExecuteAsync is not null;

    public Func<PuppetContext, IReadOnlyList<string>, CancellationToken, Task>? TestAsync { get; init; }
    public bool CanTest => ExecuteAsync is not null;

    public string? Usage { get; init; }
    public string? Description { get; init; }
    public IReadOnlyList<string> Examples { get; init; }
    public string? LongDescription { get; init; }

    [SetsRequiredMembers]
    public PuppetCommand(
        string name,
        Func<PuppetContext, IReadOnlyList<string>, CancellationToken, Task>? executeAsync = null,
        IReadOnlyList<string>? aliases = null,
        string? usage = null,
        string? description = null,
        IReadOnlyList<string>? examples = null,
        string? longDescription = null,
        IReadOnlyList<PuppetCommand>? children = null
    )
    {
        Name = name;
        Aliases = aliases ?? Array.Empty<string>();
        Children = children ?? Array.Empty<PuppetCommand>();
        ExecuteAsync = executeAsync ?? null;
        Usage = usage ?? null;
        Description = description ?? null;
        Examples = examples ?? Array.Empty<string>();
        LongDescription = longDescription ?? null;
    }

    public string PrintShort(int col1space, int col2space, HelpAttribute help, bool oneline = true)
    {
        string col1 = $"{Address}:".PadRight(col1space);        
        string col2 = help switch
        {
            HelpAttribute.Aliases           => $"[ {string.Join(", ", Aliases)} ]",
            HelpAttribute.Usage             => Usage ?? "",
            HelpAttribute.Description       => Description ?? "",
            HelpAttribute.Examples          => Examples.ToList().AlignList(col1space, col2space),
            HelpAttribute.LongDescription   => LongDescription ?? "",
            _ => "-"
        };
        if (oneline) return col1 + col2.Truncate(col2space);
        else return col1 + col2;
    }

    public string PrintLong() =>
        "*" + Address + "\n" +
        (Aliases.Count > 0 ? $"Aliases: [ {string.Join(", ", Aliases)} ]\n" : "") +
        (Usage is not null ? $"Usage: {Usage}\n" : "") +
        (Description is not null ? $"Description: {Description}\n" : "") +
        (Examples.Count > 0 ? $"Examples:\n - \"{string.Join("\"\n - \"", Examples)}\"\n" : "") +
        LongDescription ?? "";
}