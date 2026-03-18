namespace Puppet;

public sealed class PuppetContext
{
    private readonly Puppet _puppet;
    internal PuppetContext(Puppet puppet)
    {
        _puppet = puppet;
    }
    public void WriteLine(string message) => _puppet.WriteLine(message);
    public Task<string> ReadLineAsync(string prompt) => _puppet.ReadLineAsync(prompt);

    
    public Dictionary<string, PuppetCommand> CommandIndex => _puppet.CommandIndex;
    public Dictionary<string, PuppetCommand> AliasIndex => _puppet.AliasIndex;
    
    public int OneLineMaxWidth => _puppet.OneLineMaxWidth;

    // Commands used to call other commands:
    public Task ExecuteCommandAsync(string commandHead, IReadOnlyList<string> args, CancellationToken ct = default) => _puppet.ExecuteCommandAsync(commandHead, args, ct);
    public Task<bool> TestCommandAsync(string commandHead, IReadOnlyList<string> args, CancellationToken ct = default) => _puppet.TestCommandAsync(commandHead, args, ct);

    // Json:
    public Task ExecuteJsonAsync(string commandHead, string json, CancellationToken ct = default) => _puppet.ExecuteJsonAsync(commandHead, json, ct);
    public Task<bool> TestJsonAsync(string commandHead, string json, CancellationToken ct = default) => _puppet.TestJsonAsync(commandHead, json, ct);

    // Script:
    public Task ExecuteScriptAsync(Script script, CancellationToken ct = default) => _puppet.ExecuteScriptAsync(script, ct);
    public Task TestScriptAsync(Script script, CancellationToken ct = default) => _puppet.TestScriptAsync(script, ct);
}