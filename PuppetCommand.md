# PuppetCommand

The `PuppetCommand` class is the building block of a Puppet command system. Commands are defined inside command sets implementing `IPuppetCommandSet`, and privided to `Puppet` during construction.

The `PuppetCommand` constructor has only one required property, `Name`. When `Puppet` is built, each assigned command is automatically given an address `AddressString`, structured lke `parent.command.child`, which we call the "Command Head". The command can then be called by inputting the command head, followed by arguments seperated by spaces, stored in an IReadOnlyList\<string\> `args`.

## Properties:

- `Name`:Canonical name of the command. The only required property. AddressString is assigned based on this parameter. Name must be unique in its respective level and have no siblings with identical names. 
- `AddressString`:
String by which this command is called. Consists of the root command's name followed by descendent names seperated by '.' (e.g. `Help.List`).
- `Aliases`: A list of aliases which can be used instead of `Name`. Canonical name takes priority in all searches. Every alias must be unique in its respective level and have no siblings with identical aliases.
- `Children`: Child commands of this command which are given `AddressString` "ThisName.ChildName".
- `ExecuteAsync`: Main execution method of the command. Method does not need to be defined in the same command set class. Methods must return `Task`, and accept arguments `(PuppetContext ctx, IReadOnlyList<string> args, CancellationToken ct)`. They do not need to be `async`.
- `CanExecute`: Returns true if `ExecuteAsync` is not null.
- `TestAsync`: Testing method of the command. Methods must return `Task<bool>` and accept the same arguments as `ExecuteAsync`: They do not need to be `async`. These should be constructed to parse arguments identically `ExecuteAsync`, and further validation can be made too.
- `CanTest`: Returns true if `TestAsync` is not null.
- `Usage`: String showing how the command is used. Format as "Command.Head \<type RequiredArgument\> [type OptionalArgument]".
- `Description`: String describing what this command does. This is the default "Help" parameter shown when the `help` command is called.
- `Examples`: Examples showing how to use the command.
- `LongDescription`: String describing the command.

## Methods:
- `PuppetCommand.PrintShort(int col1space, intcol2space, HelpAttribute help, bool oneline = true)`: Prints the command address and specified help parameter. `HelpAttribute` is an Enum, with options [ Aliases, Usage, Description, Examples, LongDescription ]. Truncates to specified column length and down to one line if `oneline` is set true.
- `PuppetCommand.PrintLong()`: Prints all help parameters. Shown when `help` command is called on a specific command, or `help.full` is called.

## Formatting:
Puppet internal systems are generally case insensitive, so names and aliases can be written in any way. However, for the same of consistency, write names and aliases starting with a capital letter. Canonical names should only contain letters, no numbers or symbols. 

## Implementation:
Create an implementation of `IPuppetCommand`, and add new command(s) to the list `Commands`. You can create placeholder commands with just `Name`, these will be registered in `Puppet` and will be listed in `help` and `commands` commands, but will have no functionality.

```csharp
public class MyCommands : IPuppetCommandSet
{
	public IReadOnlyList<PuppetCommand> Commands =>
	[
		new(name: "MyCommand",
			children:
			[
				new(name: "MySubCommand")
			];
	];
}
```

It is recommended that you at least add a description, and usage if this is intended to be an executable command instead of a command category.

To make an executable command, assign a suitable method to `ExecuteAsync`. Arguments are passed in the form of the IReadOnlyList\<string\> `args`. These can be parsed with `ArgumentHelpers` extensions, which can handle for varying argument counts, parsing and input errors. A `TestAsync` method can be made just be wrapping the input in `try { }`, returning false if a `PuppetUserException` is caught, true otherwise:

```csharp
public class MyCommands : IPuppetCommandSet
{
	public IReadOnlyList<PuppetCommand> Commands =>
	[
		new(name: "MyCommand",
			executeAsync: MyCommandAsync,
			testAsync: MyCommandTestAsync,
			usage: "MyCommand <int MyInt> <string MyString> [bool MyBool] [double? MyDouble]"
	];

	private Task MyCommandAsync(PuppetContext ctx, IReadOnlyList<string> args, CancellationToken ct)
	{
		string myString	 = args.String(1, "My String");		// Throws exception if absent
		int myInt	 = args.Int(0, "My Int");		// Throws exception if absent or cannot parse
		bool myBool	 = args.BoolOr(2, "My Bool", true);	// Returns default if absent
		double? myDouble = args.DoubleOrNull(3, "My Double");	// Returns null if absent
		// ...
	}

	private Task<bool> MyCommandTestAsync(PuppetContext ctx, IReadOnlyList<string> args, CancellationToken ct)
	{
		try
		{
			string myString	 = args.String(1, "My String");
			int myInt	 = args.Int(0, "My Int");		
			bool myBool	 = args.BoolOr(2, "My Bool", true);	
			double? myDouble = args.DoubleOrNull(3, "My Double");	
		}
		catch (PuppetUserException ex) { return false; }
		return true;
	}
}
```
### Prompts:
Continuous input can be given using an `async` execution method and `PuppetContext` methods. Useful methods include:

- `PuppetContext.ConfirmAsync()`: (Y/N) confirmation.
- `PuppetContext.ConfirmRequireAsync()`: (Y/N) confirmation which loops if not parsed.
- `PuppetContext.RequestAsync()`: Request entry of a certain type.
- `PuppetContext.RequireAsync()`: Request entry of a certain type and loops if not parsed or cancelled.

```csharp
private async Task MyPromptAsync(PuppetContext cts, IReadOnlyList<string> args, CancellationToken ct)
{
	string myString = await ctx.ReadLineAsync("Enter MyString: "); // Accepts regardless.
	
	int myInt = await ctx.RequireAsync(	// Will not accept any answer which cannot be parsed
		"Enter MyInt: "
		s => (int.TryParse(s, out int v), v),
		"Could not parse, please try again."
		);

	bool myBool = await ctx.ConfirmAsync("Enter MyBool: ", true) // returns fallback: "true" if cannot parse

	double myDouble = await ctx.RequireAsync(		// Will not accept any answer which cannot be parsed,
		"Enter MyDouble: "				// But will revery to default "0" if one of the 
		s => (double.TryParse(s, out double v), v),	// specified "defaultStrings" is entered.
		"Could not parse, please try again.",
		0, "Default", "Fallback", "Zero")
}
```
