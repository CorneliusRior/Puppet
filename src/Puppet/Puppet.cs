namespace Puppet;

/// <summary>
/// Entry point for the Puppet library.
/// 
/// Instead of having a seperate "engine" class, we will just have this.
/// </summary>
public sealed class Puppet
{
    public readonly IReadOnlyList<PuppetCommand> Commands;
    public readonly IReadOnlyList<PuppetCommand> AllCommands;
    public event Action<string>? OutputRequested;
    public Func<string, Task<string>>? InputRequestedAsync { get; set; }
    
    public int OneLineMaxWidth { get; set; } = 300;

    public Puppet(params IPuppetCommandSet[] commandSets)
    {
        List<PuppetCommand> commands = new();
        commands.AddRange(new BaseCommands().Commands);
        foreach (IPuppetCommandSet commandSet in commandSets)
        {
            commands.AddRange(commandSet.Commands);
        }
        AssignAllAddress(commands);
        Commands = commands;
        AllCommands = GetAllCommands();
    }

    internal void WriteLine(string message) => OutputRequested?.Invoke(message);
    internal Task<string> ReadLineAsync(string prompt)
    {
        if (InputRequestedAsync is null) throw new InvalidOperationException("Input requested callback is not set");
        return InputRequestedAsync(prompt);
    }

    public async Task ExecuteAsync(string input, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input)) return;
        try
        {
            List<string> tokens = input.Tokenize();
            IReadOnlyList<string> commandHead = tokens[0].Split('.').ToList();
            IReadOnlyList<string> args = tokens.Skip(1).ToList();
            await ExecuteCommandAsync(commandHead, args, cancellationToken);
        }
        catch (PuppetUserException ex)
        {
            WriteLine($"Input Error, {ex.Location} {ex.Message}");
        }
        catch (PuppetException ex)
        {
            WriteLine($"Error in {ex.Location} {ex.Message}");
        }
        catch (Exception ex)
        {
            WriteLine($"Error: {ex.Message}");
        }
    }    

    public async Task ExecuteCommandAsync(IReadOnlyList<string> commandHead, IReadOnlyList<string> args, CancellationToken cancellationToken = default)
    {
        PuppetCommand command = FindCommand(commandHead) ?? throw new PuppetUserException($"Cannot find command '{string.Join('.', commandHead)}'");
        if (!command.CanExecute)
        {
            WriteLine($"Command {command.Name} cannot be executed.");
            return;
        }
        PuppetContext ctx = new(this);
        await command.ExecuteAsync!(ctx, args, cancellationToken);
    }

    public List<PuppetCommand> FindCommands(IReadOnlyList<string> commandHead, bool preferExecutable = true)
    {
        List<PuppetCommand> currentLevel = Commands.ToList();
        List<PuppetCommand> matched = new();

        foreach (string head in commandHead)
        {
            matched = currentLevel.Where(c => string.Equals(c.Name, head, StringComparison.OrdinalIgnoreCase)).ToList();
            if (matched.Count == 0) matched = currentLevel.Where(c => c.Aliases.Any(a => string.Equals(a, head, StringComparison.OrdinalIgnoreCase))).ToList();
            if (matched.Count == 0) return new List<PuppetCommand>();
            currentLevel = matched.SelectMany(c => c.Children).ToList();
        }
        List<PuppetCommand> executable = matched.Where(c => c.CanExecute).ToList();
        return preferExecutable && executable.Count > 0 ? executable : matched;
    }

    public PuppetCommand? FindCommand(IReadOnlyList<string> commandHead)
    {
        List<PuppetCommand> currentLevel = Commands.ToList();
        List<PuppetCommand> matched = new();

        foreach (string head in commandHead)
        {
            matched = currentLevel.Where(c => string.Equals(c.Name, head, StringComparison.OrdinalIgnoreCase)).ToList();
            if (matched.Count == 0) matched = currentLevel.Where(c => c.Aliases.Any(a => string.Equals(a, head, StringComparison.OrdinalIgnoreCase))).ToList();
            if (matched.Count == 0) return null;
            currentLevel = matched.SelectMany(c => c.Children).ToList();
        }
        if (matched.Count == 1) return matched[0];
        List<PuppetCommand> executable = matched.Where(c => c.CanExecute).ToList();
        if (executable.Count == 1) return executable[0];
        int minChild = executable.Min(c => c.Children.Count);
        List<PuppetCommand> minChildList = executable.Where(c => c.Children.Count == minChild).ToList();
        if (minChildList.Count == 1) return minChildList[0];
        else throw new PuppetException($"Cannot narrow commandhead down to one command: commandHead='{string.Join('.', commandHead)}', matched=[{string.Join(", ", matched.Select(c => c.Address))}].");
    }

    public IReadOnlyList<PuppetCommand> GetAllCommands()
    {
        List<PuppetCommand> results = new();
        foreach (PuppetCommand root in Commands) AddCommandAndDescendants(root, results);
        return results;
    }
    public static void AddCommandAndDescendants(PuppetCommand command, List<PuppetCommand> results)
    {
        results.Add(command);
        foreach (PuppetCommand child in command.Children) AddCommandAndDescendants(child, results);
    }

    public IReadOnlyList<PuppetCommand> GetCommandAndDescendants(IReadOnlyList<string> commandHead)
    {
        PuppetCommand? command = FindCommand(commandHead);
        if (command is null) return Array.Empty<PuppetCommand>();
        List<PuppetCommand> results = new();
        AddCommandAndDescendants(command, results);
        return results;
    }

    public void AssignAllAddress(List<PuppetCommand> commandList)
    {
        foreach (PuppetCommand root in commandList)
        {
            List<string> commandHead = new();
            //commandHead.Add(root.Name);
            AssignChildAdress(root, commandHead);
        }
    }

    public void AssignChildAdress(PuppetCommand command, List<string> commandHead)
    {
        commandHead.Add(command.Name);
        command.Address = string.Join('.', commandHead);
        foreach (PuppetCommand child in command.Children) AssignChildAdress(child, commandHead);
    }

}
