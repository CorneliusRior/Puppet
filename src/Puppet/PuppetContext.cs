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
    public async Task<bool> ConfirmAsync(string prompt = $"(Y/N):", bool? fallBack = null) => (await ReadLineAsync(prompt)).ParseConfirmation(fallBack);

    public async Task<bool> ConfirmRequireAsync(string prompt = $"(Y/N)", string retryPrompt = "Could not parse, try again.")
    {
        while (true)
        {
            try { return await ConfirmAsync(prompt); }
            catch { WriteLine(retryPrompt); }
        }
    }

    public async Task<int> RequestIntAsync(string prompt, int? fallback = null)
    {
        string input = await ReadLineAsync(prompt);
        if (!int.TryParse(input, out int output))
        {
            if (fallback is not null) return fallback.Value;
            else throw new PuppetUserException($"Cannot parse '{input}' as int.");
        }
        return output;
    }
    public async Task<int> RequireIntAsync(string prompt, string retryPrompt = "Could not parse, try again.")
    {
        while (true)
        {
            try { return await RequestIntAsync(prompt); }
            catch { WriteLine(retryPrompt); }
        }
    }

    public async Task<double> RequestDoubleAsync(string prompt, double? fallBack = null)
    {
        string input = await ReadLineAsync(prompt);
        if (!double.TryParse(input, out double output))
        {
            if (fallBack is not null) return fallBack.Value;
            else throw new PuppetUserException($"Cannot parse '{input}' as int.");
        }
        return output;
    }
    public async Task<double> RequireDoubleAsync(string prompt, string retryPrompt = "Could not parse, try again.")
    {
        while (true)
        {
            try { return await RequestIntAsync(prompt); }
            catch { WriteLine(retryPrompt); }
        }
    }

    public IReadOnlyList<PuppetCommand> GetAllCommands() => _puppet.GetAllCommands();
    public IReadOnlyList<PuppetCommand> GetCommandAndDescendants(IReadOnlyList<string> commandHead) => _puppet.GetCommandAndDescendants(commandHead);
    public int OneLineMaxWidth => _puppet.OneLineMaxWidth;
    public Task ExecuteCommandAsync(
        IReadOnlyList<string> commandHead, 
        IReadOnlyList<string> args, 
        CancellationToken cancellationToken = default) => _puppet.ExecuteCommandAsync(commandHead, args, cancellationToken);
}