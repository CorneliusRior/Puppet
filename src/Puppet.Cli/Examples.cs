using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Puppet.Example;

/// <summary>
/// GPT generated this so that we can test some things. 
/// </summary>
public sealed class CounterCommands : IPuppetCommandSet
{
    private readonly Dictionary<string, int> _counters = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<PuppetCommand> Commands =>
    [
        CommandBuilder.Command("Counter")
            .Aliases("ctr", "count")
            .Description("Commands for manipulating temporary in-memory counters.")
            .Children(
                CommandBuilder.Command("Create")
                    .Aliases("new", "add")
                    .Description("Creates a new counter.")
                    .Usage("Counter.Create")
                    .Examples("""
Counter.Create
{
    "name": "Apples",
    "initialValue": 10
}
""")
                    .ExecJson<CreatePayload>(CounterCreateAsync)
                    .TestJson<CreatePayload>(CounterCreateTestAsync)
                    .Build(),

                CommandBuilder.Command("Increment")
                    .Aliases("inc", "+")
                    .Description("Increments a counter by a specified amount.")
                    .Usage("Counter.Increment")
                    .Examples("""
Counter.Increment
{
    "name": "Apples",
    "amount": 5
}
""")
                    .ExecJson<IncrementPayload>(CounterIncrementAsync)
                    .TestJson<IncrementPayload>(CounterIncrementTestAsync)
                    .Build(),

                CommandBuilder.Command("Decrement")
                    .Aliases("dec", "-")
                    .Description("Decrements a counter by a specified amount.")
                    .Usage("Counter.Decrement")
                    .Examples("""
Counter.Decrement
{
    "name": "Apples",
    "amount": 2
}
""")
                    .ExecJson<DecrementPayload>(CounterDecrementAsync)
                    .TestJson<DecrementPayload>(CounterDecrementTestAsync)
                    .Build(),

                CommandBuilder.Command("Multiply")
                    .Aliases("mul", "x")
                    .Description("Multiplies a counter by a specified factor.")
                    .Usage("Counter.Multiply")
                    .Examples("""
Counter.Multiply
{
    "name": "Apples",
    "factor": 3
}
""")
                    .ExecJson<MultiplyPayload>(CounterMultiplyAsync)
                    .TestJson<MultiplyPayload>(CounterMultiplyTestAsync)
                    .Build(),

                CommandBuilder.Command("Rename")
                    .Aliases("mv", "move")
                    .Description("Renames a counter.")
                    .Usage("Counter.Rename")
                    .Examples("""
Counter.Rename
{
    "name": "Apples",
    "newName": "GreenApples"
}
""")
                    .ExecJson<RenamePayload>(CounterRenameAsync)
                    .TestJson<RenamePayload>(CounterRenameTestAsync)
                    .Build(),

                CommandBuilder.Command("Delete")
                    .Aliases("rm", "remove")
                    .Description("Deletes a counter.")
                    .Usage("Counter.Delete")
                    .Examples("""
Counter.Delete
{
    "name": "Apples"
}
""")
                    .ExecJson<DeletePayload>(CounterDeleteAsync)
                    .TestJson<DeletePayload>(CounterDeleteTestAsync)
                    .Build(),

                CommandBuilder.Command("Get")
                    .Aliases("show", "view")
                    .Description("Prints the current value of a counter.")
                    .Usage("Counter.Get")
                    .Examples("""
Counter.Get
{
    "name": "Apples"
}
""")
                    .ExecJson<GetPayload>(CounterGetAsync)
                    .TestJson<GetPayload>(CounterGetTestAsync)
                    .Build(),

                CommandBuilder.Command("List")
                    .Aliases("ls")
                    .Description("Lists all counters and their values.")
                    .Usage("Counter.List")
                    .Examples("""
Counter.List
{
}
""")
                    .ExecJson<ListPayload>(CounterListAsync)
                    .Build(),

                CommandBuilder.Command("Reset")
                    .Aliases("clear")
                    .Description("Resets a counter back to zero.")
                    .Usage("Counter.Reset")
                    .Examples("""
Counter.Reset
{
    "name": "Apples"
}
""")
                    .ExecJson<ResetPayload>(CounterResetAsync)
                    .TestJson<ResetPayload>(CounterResetTestAsync)
                    .Build()
            )
            .Build()
    ];

    private Task<bool> CounterCreateTestAsync(PuppetContext ctx, CreatePayload payload, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(payload.Name))
        {
            ctx.WriteLine("Counter.Create failed: 'name' is required.");
            return Task.FromResult(false);
        }

        if (_counters.ContainsKey(payload.Name))
        {
            ctx.WriteLine($"Counter.Create failed: counter '{payload.Name}' already exists.");
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    private Task CounterCreateAsync(PuppetContext ctx, CreatePayload payload, CancellationToken ct)
    {
        int initialValue = payload.InitialValue ?? 0;
        _counters.Add(payload.Name, initialValue);
        ctx.WriteLine($"Created counter '{payload.Name}' with initial value {initialValue}.");
        return Task.CompletedTask;
    }

    private Task<bool> CounterIncrementTestAsync(PuppetContext ctx, IncrementPayload payload, CancellationToken ct)
    {
        return RequireExistingCounter(ctx, payload.Name);
    }

    private Task CounterIncrementAsync(PuppetContext ctx, IncrementPayload payload, CancellationToken ct)
    {
        int amount = payload.Amount ?? 1;
        _counters[payload.Name] += amount;
        ctx.WriteLine($"Incremented '{payload.Name}' by {amount}. New value: {_counters[payload.Name]}.");
        return Task.CompletedTask;
    }

    private Task<bool> CounterDecrementTestAsync(PuppetContext ctx, DecrementPayload payload, CancellationToken ct)
    {
        return RequireExistingCounter(ctx, payload.Name);
    }

    private Task CounterDecrementAsync(PuppetContext ctx, DecrementPayload payload, CancellationToken ct)
    {
        int amount = payload.Amount ?? 1;
        _counters[payload.Name] -= amount;
        ctx.WriteLine($"Decremented '{payload.Name}' by {amount}. New value: {_counters[payload.Name]}.");
        return Task.CompletedTask;
    }

    private Task<bool> CounterMultiplyTestAsync(PuppetContext ctx, MultiplyPayload payload, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(payload.Name))
        {
            ctx.WriteLine("Counter.Multiply failed: 'name' is required.");
            return Task.FromResult(false);
        }

        if (!_counters.ContainsKey(payload.Name))
        {
            ctx.WriteLine($"Counter.Multiply failed: counter '{payload.Name}' does not exist.");
            return Task.FromResult(false);
        }

        if (payload.Factor is null)
        {
            ctx.WriteLine("Counter.Multiply failed: 'factor' is required.");
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    private Task CounterMultiplyAsync(PuppetContext ctx, MultiplyPayload payload, CancellationToken ct)
    {
        _counters[payload.Name] *= payload.Factor!.Value;
        ctx.WriteLine($"Multiplied '{payload.Name}' by {payload.Factor.Value}. New value: {_counters[payload.Name]}.");
        return Task.CompletedTask;
    }

    private Task<bool> CounterRenameTestAsync(PuppetContext ctx, RenamePayload payload, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(payload.Name))
        {
            ctx.WriteLine("Counter.Rename failed: 'name' is required.");
            return Task.FromResult(false);
        }

        if (string.IsNullOrWhiteSpace(payload.NewName))
        {
            ctx.WriteLine("Counter.Rename failed: 'newName' is required.");
            return Task.FromResult(false);
        }

        if (!_counters.ContainsKey(payload.Name))
        {
            ctx.WriteLine($"Counter.Rename failed: counter '{payload.Name}' does not exist.");
            return Task.FromResult(false);
        }

        if (_counters.ContainsKey(payload.NewName))
        {
            ctx.WriteLine($"Counter.Rename failed: counter '{payload.NewName}' already exists.");
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    private Task CounterRenameAsync(PuppetContext ctx, RenamePayload payload, CancellationToken ct)
    {
        int value = _counters[payload.Name];
        _counters.Remove(payload.Name);
        _counters[payload.NewName] = value;
        ctx.WriteLine($"Renamed counter '{payload.Name}' to '{payload.NewName}'.");
        return Task.CompletedTask;
    }

    private Task<bool> CounterDeleteTestAsync(PuppetContext ctx, DeletePayload payload, CancellationToken ct)
    {
        return RequireExistingCounter(ctx, payload.Name);
    }

    private Task CounterDeleteAsync(PuppetContext ctx, DeletePayload payload, CancellationToken ct)
    {
        int value = _counters[payload.Name];
        _counters.Remove(payload.Name);
        ctx.WriteLine($"Deleted counter '{payload.Name}' (previous value: {value}).");
        return Task.CompletedTask;
    }

    private Task<bool> CounterGetTestAsync(PuppetContext ctx, GetPayload payload, CancellationToken ct)
    {
        return RequireExistingCounter(ctx, payload.Name);
    }

    private Task CounterGetAsync(PuppetContext ctx, GetPayload payload, CancellationToken ct)
    {
        ctx.WriteLine($"Counter '{payload.Name}' = {_counters[payload.Name]}");
        return Task.CompletedTask;
    }

    private Task CounterListAsync(PuppetContext ctx, ListPayload payload, CancellationToken ct)
    {
        if (_counters.Count == 0)
        {
            ctx.WriteLine("No counters exist.");
            return Task.CompletedTask;
        }

        foreach ((string name, int value) in _counters.OrderBy(x => x.Key))
            ctx.WriteLine($"{name} = {value}");

        return Task.CompletedTask;
    }

    private Task<bool> CounterResetTestAsync(PuppetContext ctx, ResetPayload payload, CancellationToken ct)
    {
        return RequireExistingCounter(ctx, payload.Name);
    }

    private Task CounterResetAsync(PuppetContext ctx, ResetPayload payload, CancellationToken ct)
    {
        _counters[payload.Name] = 0;
        ctx.WriteLine($"Reset counter '{payload.Name}' to 0.");
        return Task.CompletedTask;
    }

    private Task<bool> RequireExistingCounter(PuppetContext ctx, string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            ctx.WriteLine("'name' is required.");
            return Task.FromResult(false);
        }

        if (!_counters.ContainsKey(name))
        {
            ctx.WriteLine($"Counter '{name}' does not exist.");
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    private sealed record CreatePayload(string Name, int? InitialValue);
    private sealed record IncrementPayload(string Name, int? Amount);
    private sealed record DecrementPayload(string Name, int? Amount);
    private sealed record MultiplyPayload(string Name, int? Factor);
    private sealed record RenamePayload(string Name, string NewName);
    private sealed record DeletePayload(string Name);
    private sealed record GetPayload(string Name);
    private sealed record ResetPayload(string Name);
    private sealed record ListPayload();
}