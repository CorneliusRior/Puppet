using static Puppet.CmdBuilder;
using static Puppet.ScriptParser;

namespace Puppet;

public sealed class BaseCommands : IPuppetCommandSet
{  
    public IReadOnlyList<PuppetCommand> Commands =>
    [
        new PuppetCommand(
            name: "Help",
            executeAsync: HelpAsync,
            aliases: ["h", "?"],
            usage: "help [string HelpAttribute] [string CommandHeads]",
            description: "Lists all commands and specified help attribute (description by default), or shows full help for all commands with specified CommandHeads.",
            examples:
            [
                "help", 
                "help Diary.Add",
                "help usage Diary.Add",
                "help usage",                
            ],
            longDescription: @"Sharp brackets <> denote required arguments. Round brackets () denote optional arguments.
Help command on its own lists all commands and descriptions. If there is one argument, if it can be parsed as a help attribute, it will dieplay that attribute.
Help attirbutes include: Aliases, Usage, Description, Examples, LongDescription.
Otherwise, if it cannot be parsed as an attribute, it will interpret the argument as a CommandHead, and will show full help for all commands with those CommandHead elements.
If two arguments are given, the first argument will be interpreted as a Help Attribute, and the second argument will be interpreted as a CommandHead.",
            children:
            [
                new PuppetCommand(
                    name: "List",
                    executeAsync: HelpListAsync,
                    usage: "list [string HelpAttribute] [string CommandHeads]",
                    description: "List all commands and specified help attribute (description by default), or just for all commands with specified CommandHeads."
                ),
                new PuppetCommand(
                    name: "Full",
                    executeAsync: HelpFullAsync,
                    usage: "Help.Full [string CommandHeads]",
                    description: "Show full help for all commands, or all commands with specified CommandHeads."
                )
            ]            
        ),

        new PuppetCommand(
            name: "Commands",
            executeAsync: CommandsAsync,
            aliases: ["CommandList", "cmd", "Command"],
            description: "List all commands",
            children:
            [
                new PuppetCommand(
                    name: "Aliases",
                    executeAsync: CommandAliasesAsync,
                    aliases: ["All"],
                    description: "Lists all commands and aliases for each command."
                )
            ]
        ),

        new PuppetCommand(
            name: "Test",
            executeAsync: TestAsync,
            usage: "Tast <Command> (arguments ... )",
            description: "Runs the TestAsync method on specified command with specified arguments."
        ),

        Cmd("Json").Description("Commands for manual use of Json Commands").Children(
            Cmd("Run").Exec(RunJson).Description("Run a Json Command")
                .Usage("Json.Run <string CommandHead>")
                .Build(),
            Cmd("Test").Exec(TestJson).Description("Tests a JsonCommand")
                .Usage("Json.Test <string CommandHead>")
                .Build()
        ).Build(),

        Cmd("Script").Description("Commands for running scripts.").Children(
            Cmd("Run").Exec(ScriptTestAndRunAsync).Description("Runs a script from a file path. Tests first.")
                .Usage("Script.Run <string FilePath>")
                .Children(
                    Cmd("Force").Exec(ScriptRunAsync).Description("Runs a script from a file path without testing first.").Build()
                ).Build(),
            Cmd("Test").Exec(ScriptTestAsync).Description("Tests a script from a file path.")
                .Usage("Script.Test <string FilePath>")
                .Build()
        ).Build()
    ];   

    private Task HelpAsync(PuppetContext ctx, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0)
        {
            ctx.WriteLine("Printing all commands. Try 'help <Command>' for more information:");
            PrintShort(ctx, HelpAttribute.Description);
        }
        else if (args.Count == 1)
        {
            string arg1 = args.String(0, "Help Attribute/Command Head");
            if (!Enum.TryParse<HelpAttribute>(arg1, true, out HelpAttribute help))
            {
                ctx.WriteLine($"Printing all commands starting with '{arg1}':");
                PrintLong(ctx, arg1);
            }
            else
            {
                ctx.WriteLine($"Printing all commands and corresponding {help.ToString()}:");
                PrintShort(ctx, help);
            }
        }
        else
        {
            string helpStr = args.String(0, "HelpAttribute");
            string headStr = args.String(1, "CommandHead");
            HelpAttribute helpAtt = Enum.Parse<HelpAttribute>(helpStr);
            ctx.WriteLine($"Printing all commands starting with '{helpStr}' and corresponding {helpAtt.ToString()}:");
            PrintShort(ctx, helpAtt, headStr, false);            
        }
        return Task.CompletedTask;

    }

    private Task HelpListAsync(PuppetContext ctx, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0)
        {
            ctx.WriteLine("Printing all commands. Try 'help <Command>' for more information:");
            PrintShort(ctx, HelpAttribute.Description, "", true);            
        }
        else if (args.Count == 1)
        {
            string arg1 = args.String(0, "Help Attribute/Command Head");
            if (!Enum.TryParse<HelpAttribute>(arg1, true, out HelpAttribute help))
            {
                ctx.WriteLine($"Printing all commands starting with '{arg1}':");
                PrintShort(ctx, HelpAttribute.Description, arg1, true);                
            }
            else
            {
                ctx.WriteLine($"Printing all commands and corresponding {help.ToString()}:");
                PrintShort(ctx, help, "", true);
            }            
        }
        else
        {
            string helpStr = args.String(0, "HelpAttribute");
            string headStr = args.String(1, "CommandHead");
            HelpAttribute helpAtt = Enum.Parse<HelpAttribute>(helpStr);
            ctx.WriteLine($"Printing all commands starting with '{helpStr}' and corresponding {helpAtt.ToString()}:");
            PrintShort(ctx, helpAtt, headStr, true);
        }
        return Task.CompletedTask;
    }

    private void PrintShort(PuppetContext ctx, HelpAttribute help, string searchTerm = "", bool oneline = false)
    {
        ctx.WriteLine("");
        List<PuppetCommand> commands = ctx.SearchDictionary(searchTerm);
        int col1space = Math.Min(commands.Max(c => c.AddressString!.Length) + 3, 100);
        int col2space = Math.Max(ctx.OneLineMaxWidth - col1space, 0);
        foreach (PuppetCommand c in commands) ctx.WriteLine(c.PrintShort(col1space, col2space, help, oneline));
    }

    private Task HelpFullAsync(PuppetContext ctx, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0)
        {
            ctx.WriteLine("Printing full information for all commands:");
            PrintLong(ctx);
        }
        else
        {
            string searchTerm = args.String(0, "Search Term");
            ctx.WriteLine($"Printing full information for all commands starting with {searchTerm}:");
            PrintLong(ctx, searchTerm);

        }
        return Task.CompletedTask;
    }

    private void PrintLong(PuppetContext ctx, string searchTerm = "")
    {
        ctx.WriteLine("");
        List<PuppetCommand> commands = ctx.SearchDictionary(searchTerm);
        foreach (PuppetCommand c in commands) ctx.WriteLine(c.PrintLong());
    }

    private Task CommandsAsync(PuppetContext ctx, IReadOnlyList<string> args, CancellationToken ct)
    {
        ctx.WriteLine("Printing all commands. Try 'help <command>' for more information:");
        List<PuppetCommand> orderedCommands = ctx.SearchDictionary();
        foreach (PuppetCommand c in orderedCommands) ctx.WriteLine(c.AddressString!);
        return Task.CompletedTask;
    }

    private Task CommandAliasesAsync(PuppetContext ctx, IReadOnlyList<string> args, CancellationToken ct)
    {
        int col = Math.Min(ctx.AliasIndex.Max(kv => kv.Key.Length), (ctx.OneLineMaxWidth - 10) / 2);
        ctx.WriteLine("Printing all commands and aliases. Try 'help <command> for more information:");
        ctx.WriteLine("");
        foreach (var kv in ctx.AliasIndex.OrderBy(kv => kv.Value.AddressString)) ctx.WriteLine($"{kv.Key.Truncate(col).PadRight(col)} ({kv.Value.AddressString.Truncate(col)})");
        return Task.CompletedTask;
    }

    private async Task TestAsync(PuppetContext ctx, IReadOnlyList<string> args, CancellationToken ct)
    {
        string commandHead = args[0];
        IReadOnlyList<string> testArgs = args.Skip(1).ToList();
        bool success = await ctx.TestCommandAsync(commandHead, args, ct);
        if (success) ctx.WriteLine($"No issues found: '{string.Join(' ', args)}'.");
        else ctx.WriteLine($"Failed test: '{string.Join(' ', args)}'.");
    }

    private async Task RunJson(PuppetContext ctx, IReadOnlyList<string> args, CancellationToken ct)
    {
        string commandHead = args.String(0, "CommandHead");
        string json = await ctx.ReadLineAsync("Please enter Json argument:");
        await ctx.ExecuteJsonAsync(commandHead, json);
    }

    private async Task TestJson(PuppetContext ctx, IReadOnlyList<string> args, CancellationToken ct)
    {
        string commandHead = args[0];
        string json = await ctx.ReadLineAsync("Please enter Json argument:");
        bool success = await ctx.TestJsonAsync(commandHead, json, ct);
        if (success) ctx.WriteLine($"No issues found: '{string.Join(' ', args)}'.");
        else ctx.WriteLine($"Failed test: '{string.Join(' ', args)}'.");
    }

    private async Task ScriptRunAsync(PuppetContext ctx, IReadOnlyList<string> args, CancellationToken ct)
    {
        string path = args.StringOrNull(0, "FilePath") ?? await ctx.ReadLineAsync("Please enter filepath:");
        ctx.WriteLine($"Parsing file '{Path.GetFileName(path)}'...");
        await ctx.ExecuteScriptAsync(FromPath(path), ct);
    }

    private async Task ScriptTestAsync(PuppetContext ctx, IReadOnlyList<string> args, CancellationToken ct)
    {
        string path = args.StringOrNull(0, "File Path") ?? await ctx.ReadLineAsync("Please enter filepath:");
        ctx.WriteLine($"Parsing file '{Path.GetFileName(path)}'...");
        await ctx.TestScriptAsync(FromPath(path), ct);
    }

    private async Task ScriptTestAndRunAsync(PuppetContext ctx, IReadOnlyList<string> args, CancellationToken ct)
    {
        string path = args.StringOrNull(0, "File Path") ?? await ctx.ReadLineAsync("Please enter filepath:");
        ctx.WriteLine($"Parsing file '{Path.GetFileName(path)}'...");
        Script script = FromPath(path);
        if (await ctx.TestScriptAsync(script, ct)) await ctx.ExecuteScriptAsync(script, ct);
    }
}