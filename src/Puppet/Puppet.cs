namespace Puppet;

/// <summary>
/// Entry point for the Puppet library.
/// 
/// Instead of having a seperate "engine" class, we will just have this.
/// </summary>
public sealed class Puppet
{
    // CommandIndex:
    public Dictionary<string, PuppetCommand> CommandIndex = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, PuppetCommand> AliasIndex = new(StringComparer.OrdinalIgnoreCase);
       
    //Other variables:
    public int OneLineMaxWidth { get; set; } = 200;

    // Constructor:
    public Puppet(params IPuppetCommandSet[] commandSets)
    {
        List<PuppetCommand> rootCommands = new();
        rootCommands.AddRange(new BaseCommands().Commands);
        foreach (IPuppetCommandSet cs in commandSets) rootCommands.AddRange(cs.Commands);
        AssignCommandAddresses(rootCommands);
        BuildAliasDictionary(rootCommands);
    }

    // Set-up functions:
    public void AssignCommandAddresses(List<PuppetCommand> commandList)
    {
        commandList = commandList.OrderBy(c => c.Name).ToList();
        foreach (PuppetCommand root in commandList)
        {
            List<string> commandHead = new();            
            AssignChildAddress(root, commandHead);
        }
    }

    public void AssignChildAddress(PuppetCommand command, List<string> commandHead)
    {
        commandHead.Add(command.Name);
        command.Address = commandHead.ToArray();
        command.AddressString = string.Join('.', commandHead);
        CommandIndex.Add(command.AddressString, command);
        foreach (PuppetCommand child in command.Children.OrderBy(c => c.Name).ToList()) AssignChildAddress(child, commandHead);
        commandHead.RemoveAt(commandHead.Count - 1);
    }

    public void BuildAliasDictionary(List<PuppetCommand> rootCommandList)
    {
        foreach (PuppetCommand root in rootCommandList)
        {
            List<string> addresses = new();
            addresses.Add(root.Name);
            addresses.AddRange(root.Aliases);            
            foreach (string alias in addresses)
            {
                AliasIndex.Add(alias, root);
                foreach (PuppetCommand child in root.Children)
                {
                    AliasDictionaryAdd(alias, child);
                }
            }
        }
    }

    public void AliasDictionaryAdd(string parentAddress, PuppetCommand command)
    {
        List<string> addresses = new();
        addresses.Add(command.Name);
        addresses.AddRange(command.Aliases);
        foreach(string alias in addresses)
        {
            string aliasAddress = parentAddress + '.' + alias;
            AliasIndex.Add(aliasAddress, command);
            foreach (PuppetCommand child in command.Children)
            {
                AliasDictionaryAdd(aliasAddress, child);
            }
        }
    }

    // IO:
    public event Action<string>? OutputRequested;
    internal void WriteLine(string msg) => OutputRequested?.Invoke(msg);

    public Func<string, Task<string>>? InputRequestedAsync { get; set; }
    internal Task<string> ReadLineAsync(string prompt)
    {
        if (InputRequestedAsync is null) throw new InvalidOperationException("Input requested callback is not set");
        return InputRequestedAsync(prompt);
    }

    // Get command:
    public PuppetCommand GetCommand(string commandHead)
    {
        PuppetCommand cmd;
        if (CommandIndex.ContainsKey(commandHead)) cmd = CommandIndex[commandHead];
        else if (AliasIndex.ContainsKey(commandHead)) cmd = AliasIndex[commandHead];
        else throw new PuppetUserException($"Unknown command '{commandHead}': no command or alias found.");
        return cmd;
    }

    // Execute:

    /// <summary>
    /// Attempts to parse the command and run the ExecuteAsync method on specified command with specified arguments. 
    /// </summary>
    /// <param name="input"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task ExecuteAsync(string input, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(input)) return;
        try
        {
            List<string> tokens = input.Tokenize();
            string commandHead = tokens[0];
            IReadOnlyList<string> args = tokens.Skip(1).ToList();
            await ExecuteCommandAsync(commandHead, args, ct);
        }
        catch (PuppetUserException ex) { WriteLine($"Input Error, {ex.Location} {ex.Message}"); }
        catch (PuppetException ex) { WriteLine($"Error in {ex.Location} {ex.Message}"); }
        catch (Exception ex) { WriteLine($"Error: {ex.Message}"); }
    }

    /// <summary>
    /// Runs the ExecuteAsync method on the specified command with specified arguments. This is kept separate from ExecuteAsync so that commands can execute other commands directly.
    /// </summary>
    /// <param name="commandHead"></param>
    /// <param name="args"></param>
    public async Task ExecuteCommandAsync(string commandHead, IReadOnlyList<string> args, CancellationToken ct = default)
    {
        PuppetCommand cmd = GetCommand(commandHead);
        if (!cmd.CanExecute)
        {
            WriteLine($"Command {commandHead} as no ExecuteAsync method: cannot execute.");
            return;
        }
        PuppetContext ctx = new(this);
        await cmd.ExecuteAsync!(ctx, args, ct);
    }

    // Test:

    /// <summary>
    /// Attempts to parse the command and run the TestAsync method on specified command with specified arguments.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task TestAsync(string input, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(input)) return;
        try
        {
            List<string> tokens = input.Tokenize();
            string commandHead = tokens[0];
            IReadOnlyList<string> args = tokens.Skip(1).ToList();
            bool ok = await TestCommandAsync(commandHead, args, ct);
            if (ok) WriteLine($"[SUCCESS]: '{input}'.");
            else WriteLine($"[FAILURE]: '{input}'.");
        }
        catch (PuppetUserException ex) { WriteLine($"Input Error, {ex.Location} {ex.Message}"); }
        catch (PuppetException ex) { WriteLine($"Error in {ex.Location} {ex.Message}"); }
        catch (Exception ex) { WriteLine($"Error: {ex.Message}"); }
        
    }

    /// <summary>
    /// Runs the TestAsync method on the specified command with specified arguments. This is kept separate from TestAsync so that commands can test other commands directly. Returns true if there is no TestAsync method.
    /// </summary>
    /// <param name="commandHead"></param>
    /// <param name="args"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<bool> TestCommandAsync(string commandHead, IReadOnlyList<string> args, CancellationToken ct = default)
    {
        PuppetCommand cmd = GetCommand(commandHead);
        if (!cmd.CanTest)
        {
            WriteLine($"Command {commandHead} as no TestAsync method: cannot test.");
            return true;
        }
        PuppetContext ctx = new(this);       
        return await cmd.TestAsync!(ctx, args, ct);
    }

    // Test and run:

    /// <summary>
    /// Attempts to parse the command, runs TestAsync, if it returns true, run the command, otherwise, does nothing.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task TestAndExecuteAsync(string input, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(input)) return;
        try
        {
            List<string> tokens = input.Tokenize();
            string commandHead = tokens[0];
            IReadOnlyList<string> args = tokens.Skip(1).ToList();
            WriteLine($"Testing '{commandHead}'...");
            bool ok = await TestCommandAsync(commandHead, args, ct);
            if (!ok)
            {
                WriteLine($"[FAILURE]: Command '{input}' TestAsync returned false: not executing.");
                return;
            }            
            await ExecuteCommandAsync(commandHead, args, ct);
        }
        catch (PuppetUserException ex) { WriteLine($"Input Error, {ex.Location} {ex.Message}"); }
        catch (PuppetException ex) { WriteLine($"Error in {ex.Location} {ex.Message}"); }
        catch (Exception ex) { WriteLine($"Error: {ex.Message}"); }
    }
}
