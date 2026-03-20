using Puppet.Models;
using Puppet.Scripting;

namespace Puppet;

/// <summary>
/// Context class for commands to interact with Puppet: Request input, output, run scripts, and execution of other commands.
/// </summary>
public sealed class PuppetContext
{
    private readonly Puppet _puppet;
    internal PuppetContext(Puppet puppet)
    {
        _puppet = puppet;
    }

    // Command index & other:
    public Dictionary<string, PuppetCommand> CommandIndex => _puppet.CommandIndex;
    public Dictionary<string, PuppetCommand> AliasIndex => _puppet.AliasIndex;  
    public int OneLineMaxWidth => _puppet.OneLineMaxWidth;

    // IO:
    public Task<string> ReadLineAsync(string prompt) => _puppet.ReadLineAsync(prompt);
    public void WriteLine(string msg = "") => _puppet.WriteLine(msg);
    public void Write(string msg) => _puppet.Write(msg);
    public void WriteStatus(string msg) => _puppet.WriteStatus(msg);
    public void WriteStatusSample(string msg, int length = 150) => _puppet.WriteStatusSample(msg, length);
    public void ClearStatus(string msg = "") => _puppet.ClearStatus(msg);

    /// <summary>
    /// Uses WriteStatus() to update last line to make a loading animation while awaiting an async task. 
    /// </summary>
    /// <example>
    /// // To await a function with only 1 CancellationToken as an argument:
    /// public int MyFunction(CancellationToken ct) { (...) }
    /// int result = await ctx.WithWaiterAsync(MyFunction, (...) }
    /// 
    /// // To await a function which takes more arguments, wrap it in a lambda:
    /// public int MyFunction(string name, bool real) { (...) }
    /// int result = await ctx.WithWaiterAsync(token => MyFunction("John", true, token), (...) }
    /// 
    /// // You can also just define a new method inline:
    /// int result = await ctx.WithWaiterAsync(
    ///     async token =>
    ///     {
    ///         // ...
    ///     });
    /// <typeparam name="T">Return type</typeparam>
    /// <param name="action">Function which takes argument CancellationToken and returns Task<T></param>
    /// <param name="prefix">String shown before frames (e.g. "Loading").</param>
    /// <param name="suffix">String shown after frames.</param>
    /// <param name="finish">String shown in place when complete (e.g. "Done").</param>
    /// <param name="frameTime">Time in ms between updates</param>
    /// <param name="ct"></param>
    /// <param name="frames">Animation frames. Must contain at least one element.</param>
    /// <returns></returns>
    public Task<T> WithWaiterAsync<T>(Func<CancellationToken, Task<T>> action, string prefix = "Loading", string suffix = "", string finish = "Done", int frameTime = 100, CancellationToken ct = default, params string[] frames) => _puppet.WithWaiterAsync(action, prefix, suffix, finish, frameTime, ct, frames);

    /// <summary>
    /// Overload of <see cref="WithWaiterAsync{T}(Func{CancellationToken, Task{T}}, string, string, string, int, CancellationToken, string[])"/>
    /// that uses a predefined <see cref="WaitAnimation"/>
    /// </summary>
    public Task<T> WithWaiterAsync<T>(Func<CancellationToken, Task<T>> action, string prefix = "Loading", string suffix = "", string finish = "Done", int frameTime = 100, CancellationToken ct = default, WaitAnimation animation = WaitAnimation.Spinner) => _puppet.WithWaiterAsync(action, prefix, suffix, finish, frameTime, ct, animation);

    /// <summary>
    /// Overload of <see cref="WithWaiterAsync{T}(Func{CancellationToken, Task{T}}, string, string, string, int, CancellationToken, string[])"/>
    /// that can return <see cref="Task"/> instead of <see cref="Task{T}"/>
    /// </summary>
    public Task WithWaiterAsync(Func<CancellationToken, Task> action, string prefix = "Loading", string suffix = "", string finish = "Done", int frameTime = 100, CancellationToken ct = default, params string[] frames) => _puppet.WithWaiterAsync(action, prefix, suffix, finish, frameTime, ct, frames);

    /// <summary>
    /// Overload of <see cref="WithWaiterAsync{T}(Func{CancellationToken, Task{T}}, string, string, string, int, CancellationToken, string[])"/>
    /// that can return <see cref="Task"/> instead of <see cref="Task{T}"/> and uses a predefined <see cref="WaitAnimation"/>
    /// </summary>
    public Task WithWaiterAsync(Func<CancellationToken, Task> action, string prefix = "Loading", string suffix = "", string finish = "Done", int frameTime = 100, CancellationToken ct = default, WaitAnimation animation = WaitAnimation.Spinner) => _puppet.WithWaiterAsync(action, prefix, suffix, finish, frameTime, ct, animation);

    

    // Commands used to call other commands:
    public Task ExecuteCommandAsync(string commandHead, IReadOnlyList<string> args, CancellationToken ct = default) => _puppet.ExecuteCommandAsync(commandHead, args, ct);
    public Task<bool> TestCommandAsync(string commandHead, IReadOnlyList<string> args, CancellationToken ct = default) => _puppet.TestCommandAsync(commandHead, args, ct);

    // Json:
    public Task ExecuteJsonAsync(string commandHead, string json, CancellationToken ct = default) => _puppet.ExecuteJsonAsync(commandHead, json, ct);
    public Task<bool> TestJsonAsync(string commandHead, string json, CancellationToken ct = default) => _puppet.TestJsonAsync(commandHead, json, ct);

    // Script:
    public Task ExecuteScriptAsync(Script script, CancellationToken ct = default) => _puppet.ExecuteScriptAsync(script, ct);
    public Task<bool> TestScriptAsync(Script script, CancellationToken ct = default) => _puppet.TestScriptAsync(script, ct);
}