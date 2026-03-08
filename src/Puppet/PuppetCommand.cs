using System.Diagnostics.CodeAnalysis;

namespace Puppet;

public class PuppetCommand
{
    public required string Name { get; init; }
    public IReadOnlyList<string> Address { get; internal set; } = Array.Empty<string>();
    public string AddressString { get; internal set; } = "";
    public IReadOnlyList<string> Aliases { get; init; }
    public IReadOnlyList<PuppetCommand> Children { get; init; }
    
    // Function:
    public Func<PuppetContext, IReadOnlyList<string>, CancellationToken, Task>? ExecuteAsync { get; init; }
    public bool CanExecute => ExecuteAsync is not null;

    public Func<PuppetContext, IReadOnlyList<string>, CancellationToken, Task<bool>>? TestAsync { get; init; }
    public bool CanTest => TestAsync is not null;


    public Func<PuppetContext, string, CancellationToken, Task>? ExecuteJsonAsync { get; init; }
    public bool CanExecuteJson => ExecuteJsonAsync is not null;

    public Func<PuppetContext, string, CancellationToken, Task<bool>>? TestJsonAsync { get; init; }
    public bool CanTestJson => TestJsonAsync is not null;


    public string? Usage { get; init; }
    public string? Description { get; init; }
    public IReadOnlyList<string> Examples { get; init; }
    public string? LongDescription { get; init; }

    [SetsRequiredMembers]
    public PuppetCommand(
        string name,
        Func<PuppetContext, IReadOnlyList<string>, CancellationToken, Task>? executeAsync = null,
        Func<PuppetContext, IReadOnlyList<string>, CancellationToken, Task<bool>>? testAsync = null,
        Func<PuppetContext, object, CancellationToken, Task>? ExecuteJsonAsync = null,
        Func<PuppetContext, object, CancellationToken, Task<bool>>? TestJsonAsync = null,
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
        TestAsync = testAsync ?? null;
        Usage = usage ?? null;
        Description = description ?? null;
        Examples = examples ?? Array.Empty<string>();
        LongDescription = longDescription ?? null;
    }

    public string PrintShort(int col1space, int col2space, HelpAttribute help, bool oneline = true)
    {
        string col1 = $"{AddressString}:".PadRight(col1space);        
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
        "*" + AddressString + "\n" +
        (Aliases.Count > 0 ? $"Aliases: [ {string.Join(", ", Aliases)} ]\n" : "") +
        (Usage is not null ? $"Usage: {Usage}\n" : "") +
        (Description is not null ? $"Description: {Description}\n" : "") +
        (Examples.Count > 0 ? $"Examples:\n - \"{string.Join("\"\n - \"", Examples)}\"\n" : "") +
        LongDescription ?? "";
}