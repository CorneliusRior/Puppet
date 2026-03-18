using System.Text.Json;
using System.Text.Json.Serialization;

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
    private readonly JsonSerializerOptions _jsonOptions = new();

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
                if (!AliasIndex.TryAdd(alias, root)) throw new PuppetException($"Duplicate command or alias address: '{alias}' in '{root.Name}' ('{(root.AddressString ?? "Unknown address")}')");
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
            if (!AliasIndex.TryAdd(aliasAddress, command)) throw new PuppetException($"Duplocate command or alias address: `{aliasAddress}` in '{command.Name}' ('{command.AddressString ?? "Unknown address"}')");
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
        if (!cmd.CanExecute) throw new PuppetException($"Command '{commandHead}' has no ExecuteAsync method: cannot execute.");
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

    // Json input:
    public async Task ExecuteJsonAsync(string commandHead, string json, CancellationToken ct = default)
    {
        PuppetCommand cmd = GetCommand(commandHead);
        if (!cmd.CanExecuteJson) throw new PuppetException($"Command '{commandHead}' has no ExecuteJsonAsync method: cannot execute.");
        if (cmd.JsonPayloadType is null) throw new PuppetException("Null JSON Payload - this command cannot parse JSON.");

        object pl;
        pl = JsonSerializer.Deserialize(json, cmd.JsonPayloadType, _jsonOptions) ?? throw new PuppetUserException($"Invalid JSON: Cannot parse.");
        PuppetContext ctx = new(this);
        await cmd.ExecuteJsonAsync!(ctx, pl, ct);
    }

    public async Task<bool> TestJsonAsync(string commandHead, string json, CancellationToken ct = default)
    {
        PuppetCommand cmd = GetCommand(commandHead);
        if (!cmd.CanTestJson) throw new PuppetException($"Command '{commandHead}' has no TestJsonAsync method: cannot test.");
        if (cmd.JsonPayloadType is null) throw new PuppetException("Null JSON Payload - this command cannot parse JSON.");

        object pl;
        try{ pl = JsonSerializer.Deserialize(json, cmd.JsonPayloadType, _jsonOptions) ?? throw new PuppetUserException($"Invalid JSON: Cannot parse."); }
        catch (PuppetUserException ex)
        {
            WriteLine($"Command '{commandHead}' failed: '{ex.Message}'\n\"{json}\"");
            return false;
        }
        PuppetContext ctx = new(this);
        return await cmd.TestJsonAsync!(ctx, pl, ct);
    }

    // Scripting:
    public async Task ExecuteScriptAsync(Script script, CancellationToken ct = default)
    {
        // Test it first:
        if (/*await TestScriptAsync(script, ct)*/true)
        {
            WriteLine($"\n\n{new string('-', 32)}\n\nTesting complete, running script\n\n{new string('-', 32)}\n\n");
            foreach(ScriptStatement s in script.Statements)
            {
                await ExecuteJsonAsync(s.CommandHead, s.JsonPayload, ct);
                //WriteLine($"Executed statement {s.PrintRef()}");
            }
        }
    }

    public async Task<bool> TestScriptAsync(Script script, CancellationToken ct = default)
    {
        WriteLine($"Testing script {script.PrintInfo()}");
        bool ok = true;
        List<ScriptStatement> error = new();
        PuppetContext ctx = new(this);
        foreach (ScriptStatement s in script.Statements)
        {
            try
            {
                PuppetCommand cmd = GetCommand(s.CommandHead);

                // Test that it can even run:
                if (!cmd.CanExecuteJson) throw new PuppetException("No ExecuteJsonAsync mathos - this command cannot run JSON");

                // Test that the payload can be parsed:
                if (cmd.JsonPayloadType is null) throw new PuppetException($"Null JSON Payload - this command cannot parse JSON.");
                object pl;
                pl = JsonSerializer.Deserialize(s.JsonPayload, cmd.JsonPayloadType, _jsonOptions) ?? throw new PuppetUserException($"Invalid JSON: Cannot parse.");

                // If there's no test method, call it a success:
                if (!cmd.CanTestJson)
                {
                    WriteLine($"[OK] (?) No TestJsonAsync method found for {s.PrintRef()}");
                    continue;
                }
                bool success = await cmd.TestJsonAsync!(ctx, pl, ct);
                if (success) WriteLine($"[OK] {s.PrintRef()}");
                else
                { 
                    WriteLine($"[ERROR] {s.PrintRef()}: Failed TestJsonAsync.");
                    ok = false;
                    error.Add(s);
                }
            }
            catch (PuppetUserException ex)
            {
                WriteLine($"[ERROR] {s.PrintRef()}: Input Error, {ex.Location} {ex.Message}");
                ok = false;
                error.Add(s);
            }
            catch (PuppetException ex)
            {
                WriteLine($"[ERROR] {s.PrintRef()}: Error in {ex.Location} {ex.Message}");
                ok = false;
                error.Add(s);
            }
            catch (Exception ex)
            {
                WriteLine($"[ERROR] {s.PrintRef()}: {ex.Message}");
                ok = false;
                error.Add(s);
            }            
        }
        if (ok) WriteLine($"\n\n------No errors found.------\n\n");
        else
        {
            WriteLine($"\n\n------ {error.Count} Error(s) found: ------\n");
            if (error.Count > 5) foreach (ScriptStatement est in error) est.PrintInfoShort();
            else foreach (ScriptStatement est in error) est.PrintInfo();
        }
        return ok;
    }

}
