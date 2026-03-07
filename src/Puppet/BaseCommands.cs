namespace Puppet;

public sealed class BaseCommands : IPuppetCommandSet
{  
    public IReadOnlyList<PuppetCommand> Commands =>
    [
        new PuppetCommand(
            name: "Help",
            executeAsync: HelpAsync,
            aliases: ["h", "?"],
            usage: "help (string HelpAttribute) (string CommandHeads)",
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
                    usage: "list (string HelpAttribute) (string CommandHeads)",
                    description: "List all commands and specified help attribute (description by default), or just for all commands with specified CommandHeads."
                ),
                new PuppetCommand(
                    name: "Full",
                    executeAsync: HelpFullAsync,
                    usage: "Help.Full (string CommandHeads)",
                    description: "Show full help for all commands, or all commands with specified CommandHeads."
                )
            ]            
        ),

        new PuppetCommand(
            name: "Commands",
            executeAsync: CommandsAsync,
            aliases: ["CommandList", "cmd"],
            description: "List all commands"
        ),
    ];   


    private Task HelpAsync(PuppetContext context, IReadOnlyList<string> args, CancellationToken cancellationToken)
    {
        if (args.Count == 0)
        {
            context.WriteLine("Printing all commands. Try 'help <Command>' for more information:");
            PrintShort(context, context.GetAllCommands(), HelpAttribute.Description);
            return Task.CompletedTask;
        }
        if (args.Count == 1)
        {
            string arg1 = args.String(0, "Help Attribute/Command Head");
            if (!Enum.TryParse<HelpAttribute>(arg1, true, out HelpAttribute help))
            {
                IReadOnlyList<PuppetCommand> filtered = context.GetCommandAndDescendants(arg1.Split('.'));
                if (filtered.Count == 0) context.WriteLine($"No commands found for '{arg1}'.");
                else PrintLong(context, filtered);
                return Task.CompletedTask;
            }
            context.WriteLine($"Printing all commands and corresponding {help.ToString()}");
            PrintShort(context, context.GetAllCommands(), help);
            return Task.CompletedTask;
        }
        else
        {
            string helpStr = args.String(0, "HelpAttribute");
            string headStr = args.String(1, "CommandHead");
            HelpAttribute helpAtt = Enum.Parse<HelpAttribute>(helpStr);
            IReadOnlyList<PuppetCommand> filtered = context.GetCommandAndDescendants(headStr.Split(' '));
            context.WriteLine($"Printing all commands containing CommandGead '{headStr}' and corresponding {helpAtt.ToString()}:");
            PrintShort(context, Commands, helpAtt);
            return Task.CompletedTask;
        }
        
    }

    private Task HelpListAsync(PuppetContext context, IReadOnlyList<string> args, CancellationToken cancellationToken)
    {
        if (args.Count == 0)
        {
            context.WriteLine("Printing all commands.");
            PrintShort(context, context.GetAllCommands(), HelpAttribute.Description);
            return Task.CompletedTask;
        }
        if (args.Count == 1)
        {
            string arg1 = args.String(0, "Help Attribute/Command Head");
            if (!Enum.TryParse<HelpAttribute>(arg1, true, out HelpAttribute help))
            {
                IReadOnlyList<PuppetCommand> filtered = context.GetCommandAndDescendants(arg1.Split('.'));
                if (filtered.Count == 0) context.WriteLine($"No commands found for '{arg1}'.");
                else PrintLong(context, filtered);
                return Task.CompletedTask;
            }
            context.WriteLine($"Printing all commands and corresponding {help.ToString()}");
            PrintShort(context, context.GetAllCommands(), help);
            return Task.CompletedTask;
        }
        else
        {
            string helpStr = args.String(0, "HelpAttribute");
            string headStr = args.String(1, "CommandHead");
            HelpAttribute helpAtt = Enum.Parse<HelpAttribute>(helpStr);
            IReadOnlyList<PuppetCommand> filtered = context.GetCommandAndDescendants(headStr.Split(' '));
            context.WriteLine($"Printing all commands containing CommandGead '{headStr}' and corresponding {helpAtt.ToString()}:");
            PrintLong(context, Commands);
            return Task.CompletedTask;
        }
    }

    private void PrintShort(PuppetContext context, IReadOnlyList<PuppetCommand> commands, HelpAttribute help, bool oneline = false)
    {
        context.WriteLine("");
        int col1space = Math.Min(commands.Max(c => c.Address!.Length) + 3, 100);
        int col2space = Math.Max(context.OneLineMaxWidth - col1space, 0);
        foreach (PuppetCommand c in commands) context.WriteLine(c.PrintShort(col1space, col2space, help, oneline));
    }

    private Task HelpFullAsync(PuppetContext context, IReadOnlyList<string> args, CancellationToken cancellationToken)
    {
        if (args.Count == 0) PrintLong(context, context.GetAllCommands());
        else PrintLong(context, context.GetCommandAndDescendants(args.String(0, "Command head").Split('.')));
        return Task.CompletedTask;
    }

    private void PrintLong(PuppetContext context, IReadOnlyList<PuppetCommand> commands)
    {
        context.WriteLine("");
        foreach (PuppetCommand c in commands) context.WriteLine(c.PrintLong());
    }

    private Task CommandsAsync(PuppetContext context, IReadOnlyList<string> args, CancellationToken cancellationToken)
    {
        context.WriteLine("All commands:");
        List<PuppetCommand> orderedCommands = context.GetAllCommands().OrderBy(c => c.Address).ToList();
        foreach (PuppetCommand c in orderedCommands) context.WriteLine(c.Address!);
        return Task.CompletedTask;
    }
}