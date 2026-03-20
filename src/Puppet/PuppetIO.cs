

using Puppet.Models;
using Puppet.Tools;

namespace Puppet;

public sealed partial class Puppet
{
    public event Action<string>? OutputRequested;
    public event Action<string>? InlineOutputRequested;
    public Func<string, Task<string>>? InputRequestedAsync { get; set; }
    internal Task<string> ReadLineAsync(string prompt)
    {
        if (InputRequestedAsync is null) throw new InvalidOperationException("Input requested callback is not set");
        return InputRequestedAsync(prompt);
    }
    
    /// <summary>
    /// Writes a new line in output. Invokes OutputRequested.
    /// </summary>
    /// <param name="msg">Message to print.</param>
    internal void WriteLine(string msg = "") => OutputRequested?.Invoke(msg);

    /// <summary>
    /// Writes in output without new line. Invokes InlineOutputRequested.
    /// </summary>
    /// <param name="msg">Message to print.</param>
    internal void Write(string msg) => InlineOutputRequested?.Invoke(msg);

    private int _lastStatusLength = 0;

    /// <summary>
    /// Used for updating or animating a single line. Replaces the last line. Use ClearStatus() afterwards to clear.
    /// </summary>
    /// <param name="msg">Message to print.</param>
    internal void WriteStatus(string msg)
    {
        int padLength = Math.Max(0, _lastStatusLength - msg.Length);
        string output = "\r" + msg + new string(' ', padLength);
        _lastStatusLength = msg.Length;
        Write(output);
    }

    internal void WriteStatusSample(string msg, int length = 150)
    {
        msg = msg.Truncate(length);
        int padLength = Math.Max(0, length - msg.Length);
        string output = "\r" + "[ " + msg + new string(' ', padLength) + " ]";
        _lastStatusLength = output.Length;
        Write(output);
    }

    /// <summary>
    /// Clears a status/updated text.
    /// </summary>
    /// <param name="msg">Optional message to print in place of status.</param>
    internal void ClearStatus(string msg = "")
    {
        if (_lastStatusLength <= 0) return;
        Write("\r" + new string(' ', _lastStatusLength) + "\r");
        _lastStatusLength = 0;
        if (!string.IsNullOrWhiteSpace(msg)) WriteLine(msg);
    }

    /// <summary>
    /// Uses WriteStatus() to update last line to make a loading animation while awaiting an async task. 
    /// </summary>
    /// <example>
    /// // To await a function with only 1 CancellationToken as an argument:
    /// public int MyFunction(CancellationToken ct) { (...) }
    /// int result = await WithWaiterAsync(MyFunction, (...) }
    /// 
    /// // To await a function which takes more arguments, wrap it in a lambda:
    /// public int MyFunction(string name, bool real) { (...) }
    /// int result = await WithWaiterAsync(token => MyFunction("John", true, token), (...) }
    /// 
    /// // You can also just define a new method inline:
    /// int result = await WithWaiterAsync(
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
    public async Task<T> WithWaiterAsync<T>(Func<CancellationToken, Task<T>> action, string prefix = "Loading", string suffix = "", string finish = "Done", int frameTime = 100, CancellationToken ct = default, params string[] frames)
    {
        using CancellationTokenSource waitCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        if (frames.Length == 0) frames = WaitAnimation.Spinner.GetFrames();

        Task waitTask = Task.Run(async () =>
        {
            int framePadding = frames.Max(f => f.Length);
            int i = 0;
            while (!waitCts.Token.IsCancellationRequested)
            {
                WriteStatus(prefix + frames[i++ % frames.Length].PadRight(framePadding) + suffix);
                try { await Task.Delay(frameTime, waitCts.Token); }
                catch (OperationCanceledException) { break; }
            }
        }, waitCts.Token);

        try { return await action(ct); }
        finally
        {
            waitCts.Cancel();
            ClearStatus(finish);
            try { await waitTask; }
            catch (OperationCanceledException) { }
        }
    }

    /// <summary>
    /// Overload of <see cref="WithWaiterAsync{T}(Func{CancellationToken, Task{T}}, string, string, string, int, CancellationToken, string[])"/>
    /// that uses a predefined <see cref="WaitAnimation"/>
    /// </summary>
    public async Task<T> WithWaiterAsync<T>(Func<CancellationToken, Task<T>> action, string prefix = "Loading", string suffix = "", string finish = "Done", int frameTime = 100, CancellationToken ct = default, WaitAnimation animation = WaitAnimation.Spinner) => await WithWaiterAsync(action, prefix, suffix, finish, frameTime, ct, animation.GetFrames());

    /// <summary>
    /// Overload of <see cref="WithWaiterAsync{T}(Func{CancellationToken, Task{T}}, string, string, string, int, CancellationToken, string[])"/>
    /// that can return <see cref="Task"/> instead of <see cref="Task{T}"/>
    /// </summary>
    public async Task WithWaiterAsync(Func<CancellationToken, Task> action, string prefix = "Loading", string suffix = "", string finish = "Done", int frameTime = 100, CancellationToken ct = default, params string[] frames) =>
        await WithWaiterAsync(async token =>
        {
            await action(token);
            return true;
        },
        prefix, suffix, finish, frameTime, ct, frames);

    /// <summary>
    /// Overload of <see cref="WithWaiterAsync{T}(Func{CancellationToken, Task{T}}, string, string, string, int, CancellationToken, string[])"/>
    /// that can return <see cref="Task"/> instead of <see cref="Task{T}"/> and uses a predefined <see cref="WaitAnimation"/>
    /// </summary>
    public async Task WithWaiterAsync(Func<CancellationToken, Task> action, string prefix = "Loading", string suffix = "", string finish = "Done", int frameTime = 100, CancellationToken ct = default, WaitAnimation animation = WaitAnimation.Spinner) =>
        await WithWaiterAsync(async token =>
        {
            await action(token);
            return true;
        },
        prefix, suffix, finish, frameTime, ct, animation.GetFrames());

}
