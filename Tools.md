# Puppet Tools

`Puppet.Tools` contains tools to help build command systems easily.

## CommandBuilder

Command builder is a tool for building commands. It also allows for the definition of `ExecuteJsonAsync` and associated properties, which the default constructor does not.

### How to use:

Type `using Puppet.Tools.CmdBuiler` at the top of a file. `CommandBuilder` can now be accessed with `Cmd(string name)`. Alternatively, every `CommandBuilder` declaration can be written as `CommandBuilder.Command(string name)`. This creates a `CommandBuilder` instance which can be added to with extensions, such as `.Exec()`, `.Usage()` and `.Description()`. One a `CommandBuilder` is complete, use `.Build()` to convert it to a `PuppetCommand`.

As every other field for a `PuppetCommand` is optional, extention methods can be added only as required. Once a command is defined with a name, and the command set is added to `Puppet` on construction, it will be registered and will appear on the `help` list.

Here is an example of a command definition using every extension:

```csharp

using Puppet.Tools.CmdBuilder;

public sealed class MyCommandSet : IPuppetCommandSet
{
	public IReadOnlyList<PuppetCommand> Commands =>
	[
		Cmd("MyCommand).
			.Aliases("mc", "SampleCommand", "MyCmd")
			.AddAlias("SampleCmd")
			.Exec(MyCommandAsync)
			.Test(MyCommandTestAsync)
			.ExecJson<MyCommandPayload>(MyCommandJsonAsync)
			.TestJson<MyCommandPayload>(MyCommandTestJsonAsync)
			.Usage("MyCommand <int MyInt> <string MyString> [bool MyBool] [double? MyDouble]")
			.Description("Sample command.")
			.LongDescription("Sample command showing use of all CmdBuilder methods.")
			.Examples(
				"MyCommand 20 \"My String\" true",
				"""
				MyCommand
				{
					"MyInt": 20
					"MyString": "My String"
					"MyBool": true
					"MyDouble": null
				}
				"""
			)
			.AddExample("MyCommand 50 puppet false 10.5")
			.Remarks("This is just a place to keep notes.")
			.Children(
				Cmd("FirstSubCommand")
					// ...
				.Build(),

				Cmd("SecondSubcommand")
					// ...
				.Build()

			)
			.AddChild(
				Cmd("ThirdSubCommand")
					//...
				.Build()

			)
		.Build()
	]
}

```

### Formatting:

`CommandBuilder` definitions can be formatted in any way, but it is recommended that this style be followed.
- `Cmd(string name)` should be on its own line.
- Every extension should be on its own line and indented once.
- `Build()` should have the same indentation as the `Cmd()` statement.
- Extentions should be in this general order (identical order to `PuppetCommand` constructor).
- `Children()` or `AddChild()` should always be the last extension used (if defined).
- Every subcommand definition should have a one-line gap afterwards.

Multiple extensions can also be used on the same line for more compact code.

## ArgumentHelpers

`ArgumentHelpers` is a static class of methods used to help parse arguments for "human input" (as opposed to "machine input", i.e. JSON formatting). The tokenizer used to process these is hosted here.

### Argument Extractors

Arguments are passed to commands through an `IReadOnlyList<string>`, usually called `args`. While individual arguments can be accessed via the index (i.e. `args[i]`), it can become tedious and complicated to implement input validation, exception handling, and the handling of optional arguments. Argument extractor methods make this easier.

All argument extraction methods are extentions of `IReadOnlyList<string>` and take arguments `int index` and `string name`. They are named after the return variable but with the case of the first letter changed (e.g. bool > `Bool()`, double > `Double()`, DateTime > `dateTime()`). Variants can exist (e.g. `Double()`, `DoubleOr()`, `DoubleOrNullable()`, `DoubleOrNull()`. The `index` argument indicates the position of the argument in the `args` list, and the `name` argument is used in error handling for easier bug-fixing.

Using index, here is how we would extract the arguments for a command with arguments `<int Id> [DateTime Date] [double Value]`, where no entry for `Date`, or just "_", indicates today, and `Value` is nullable:

```csharp

private Task Execute(PuppetContext ctx, IReadOnlyList<string> args, CancellationToken ct)
{
	if (args.Count == 0) throw new PuppetUserException($"Not enough arguments, missing int 'Id'.");
	if (!int.TryParse(args[0], out int id) throw new PuppetUserException($"Cannot parse int 'Id': '{args[0]}'.");

	DateTime date;
	if (args.Count > 1) 
	{
		if (args[1] == "_") date = DateTime.Today;
		if (!DateTime.TryParse(args[1], out date) throw new PuppetUserException($"Cannot parse DateTime 'Date': '{args[1]}'.");
	}
	else date = DateTime.Today;

	double? value;
	if (args.Count > 2) if (!double.TryPase(args[2], out value)) throw new PuppetUserException($"Cannot parse double `Value`: `{args[2]}`.");
	else value = null;

	// ...
}
```

Here is how we can define the same thing it using argument extractor methods:

```csharp
private Task Execute(PuppetContext cts, IReadOnlyList<string> args, CancellationToken ct)
{
	int id = args.Int(0, "Id");
	DateTime date = args.DateTimeOr(0, "Date", DateTime.Today);
	double? value = args.DoubleOrNull(2, "Value");

	// ...
}
```

Current list of extractor methods are:

- `String()` Returns specified string.
- `StringOr()` Returns specified string or default if not present.
- `StringOrNull()`Returns specified string, or null if not present.
- `StringOrDefault()` Returns specified string or non-nullable default if not present or equal to '_'.
- `StringNullableOrDefault()` Returns specified string or nullable default if not present or equal to '_'.
- `Bool()` Returns specified bool.
- `BoolOr()` Returns specified bool or default if not present.
- `BoolOrNull()` Returns specified bool or null if not present.
- `Double()` Returns specified double.
- `DoubleOr()` Returns specified double or non-nullable default if not present or equal to '_'.
- `DoubleOrNull()` Returns specified double, or null if not present.
- `DoubleOrNullable()` Returns specified double or nullable default if not present or equal to '_'.
- `Int()` Returns specified integer.
- `IntOr()` Returns specified integer, or non-nullable default if not present or equal to '_'.
- `IntOrNull()` Returns specified integer, or nullable default if not present or equal to '_'.
- `dateTime()` Returns specified DateTime.
- `dateTimeOr()` Returns specified DateTime, or default if not present.
- `dateTimeOrNull()` Returns specified DateTime, or null if not present.

## StringHelpers

`Stringhelpers` is a static class of methods used to help handle strings for printing. Current methods are:

- `ToSingleLine()` Replaces new lines with spaces.
- `ToSingleLineNullable()` Previous, but can handle null strings.
- `Unindent()` Removes indentation.
- `Truncate()` Truncates string down to a specified length.
- `TruncateNullable()` Previous, but can handle null strings.
- `ToStringTruncate()` Truncates double or int down to a specified length.
- `Checked()` Represent bool as a string, default is True > `[x]`, False > `[ ]`.
- `AlignList()` Align items of a list.
- `ToBox()` Returns a multiline string of input string enclosed in a box made with characters `┌` `┐` `└` `┘` `│` `─`.
- `ToDoubleBox()` Previous, but uses characters `╔` `╗` `╚` `╝` `║` `═`.