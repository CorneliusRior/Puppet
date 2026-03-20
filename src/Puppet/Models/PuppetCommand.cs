using Puppet.Tools;
using System.Diagnostics.CodeAnalysis;

namespace Puppet.Models;

public class PuppetCommand
{
    public required string Name { get; init; }
    public string Address { get; internal set; } = "";
    public IReadOnlyList<string> Aliases { get; init; }
    public IReadOnlyList<PuppetCommand> Children { get; init; }
    
    // Function:
    public Func<PuppetContext, IReadOnlyList<string>, CancellationToken, Task>? ExecuteAsync { get; init; }
    public bool CanExecute => ExecuteAsync is not null;

    public Func<PuppetContext, IReadOnlyList<string>, CancellationToken, Task<bool>>? TestAsync { get; init; }
    public bool CanTest => TestAsync is not null;


    public Func<PuppetContext, object, CancellationToken, Task>? ExecuteJsonAsync { get; init; }
    public bool CanExecuteJson => ExecuteJsonAsync is not null;

    public Func<PuppetContext, object, CancellationToken, Task<bool>>? TestJsonAsync { get; init; }
    public bool CanTestJson => TestJsonAsync is not null;

    public Type? JsonPayloadType { get; init; }


    public string? Usage { get; init; }
    public string? Description { get; init; }
    public IReadOnlyList<string> Examples { get; init; }
    public string? LongDescription { get; init; }
    public string? Remarks { get; init; } // You can just put whatever you want here, not used.

    [SetsRequiredMembers]
    public PuppetCommand(
        string name,
        Func<PuppetContext, IReadOnlyList<string>, CancellationToken, Task>? executeAsync = null,
        Func<PuppetContext, IReadOnlyList<string>, CancellationToken, Task<bool>>? testAsync = null,
        Func<PuppetContext, object, CancellationToken, Task>? executeJsonAsync = null,
        Func<PuppetContext, object, CancellationToken, Task<bool>>? testJsonAsync = null,
        IReadOnlyList<string>? aliases = null,
        string? usage = null,
        string? description = null,
        IReadOnlyList<string>? examples = null,
        string? longDescription = null,
        string? remarks = null,
        IReadOnlyList<PuppetCommand>? children = null
    )
    {
        Name = name;
        ExecuteAsync = executeAsync;
        TestAsync = testAsync;
        ExecuteJsonAsync = executeJsonAsync;
        TestJsonAsync = testJsonAsync;
        Aliases = aliases ?? Array.Empty<string>();
        Usage = usage;
        Description = description;
        Examples = examples ?? Array.Empty<string>();
        LongDescription = longDescription;
        Remarks = remarks;
        Children = children ?? Array.Empty<PuppetCommand>();
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