using Puppet;
using Puppet.Cli;

Puppet.Puppet puppet = new(new SampleCommands());
puppet.OutputRequested += msg => Console.WriteLine(msg);
puppet.InputRequestedAsync = prompt =>
{
    Console.WriteLine(prompt);
    Console.Write("> ");
    string input = Console.ReadLine() ?? "";
    return Task.FromResult(input);
};

Console.WriteLine("Puppet CLI. Type 'exit' to quit.");

while (true)
{
    Console.Write("> ");
    string? line = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(line)) continue;
    if (line.Equals("exit", StringComparison.OrdinalIgnoreCase)) break;
    await puppet.ExecuteAsync(line);
}




