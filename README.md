# Puppet
Puppet is a  C# class library for building REPL-style command systems. Commands are registered as a hierarchical tree, and can be executed via string input, or directly within commands.

## Overview
Puppet is designed quickly and easily create interactive command environments, scripts, and automation tools.

The `Puppet` class manages input parsing, prompts, command address assignment, command aliases, execution, and testing.

Commands are added by defining `PuppetCommand` objects inside command sets implementing `IPuppetCommandSet`, which are provided to `Puppet` during construction. 


## Features
- Hierarchical command structure (`command.subcommand`, `help.list`, &c.).
- Command aliases.
- Asynchronous command execution.
- Test methods for validating command inputs without executing them.
- Built-in input/output abstraction.
- Easy argument parsing and user input exceptions.

## Usage

### Setup
To set up a Puppet command system:

1. Create the Puppet object, assigning command sets as constructor parameters (these can be left blank initially). 
2. Assign input & output handlers. 
3. Create input method.

Here is an example for a simple console app:

```csharp
// Define Puppet object, assign commandsets:
Puppet puppet = new(
	new MyCommands(),
	// ...
);

// Assign input & output handlers:
puppet.OutputRequested += msg => Console.WriteLine(msg);
puppet.InputRequestedAsync = prompt =>
{
	Console.WriteLine(prompt);
	Console.Write("> ");
	string input = Console.ReadLine() ?? "";
	return Task.FromResult(input);
};

while (true)
{
	Console.Write("> ");
	string? line = Console.ReadLine();
	if (string.IsNullOrWhiteSpace(line)) continue;
	await puppet.ExecuteAsync(line);
}
``` 

### Command definition

Commands are defined in `IPuppetCommandSet`:

```csharp
public class MyCommands : IPuppetCommandSet
{
	public IReadOnlyList<PuppetCommand> Commands =>
	[
		new(name "MyCommand"
			executeAsync: MyCommandAsync
		)
	];

	private Task MyCommandAsync(PuppetContext ctx, IReadOnlyList<string> args, CancellationToken ct)
	{
		// ...
	}
}
```
See `PuppetCommand` documentation for more details.

## Prerequisites
- .NET 8

## Project status
Personal project for use in other projects, still evolving. API may change as features are refined.

## Planned features
- Run commands with Json formatting

- Import, parse, test and run scripts.