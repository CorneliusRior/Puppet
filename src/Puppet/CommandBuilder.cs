using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Puppet
{
    public sealed class CommandBuilder
    {
        private readonly string _name;
        private readonly List<string> _aliases = [];
        private readonly List<string> _examples = [];
        private readonly List<PuppetCommand> _children = [];

        private Func<PuppetContext, IReadOnlyList<string>, CancellationToken, Task>? _executeAsync;
        private Func<PuppetContext, IReadOnlyList<string>, CancellationToken, Task<bool>>? _testAsync;

        private Func<PuppetContext, object, CancellationToken, Task>? _executeJsonAsync;
        private Func<PuppetContext, object, CancellationToken, Task<bool>>? _testJsonAsync;
        private Type? _jsonPayloadType;

        private string? _usage;
        private string? _description;
        private string? _longDescription;
        private string? _remarks;

        public CommandBuilder(string name)
        {
            _name = name;
        }

        public PuppetCommand Build()
        {
            return new PuppetCommand(
                name:               _name,
                executeAsync:       _executeAsync,
                testAsync:          _testAsync,
                executeJsonAsync:   _executeJsonAsync,
                testJsonAsync:      _testJsonAsync,
                aliases:            _aliases,
                usage:              _usage,
                description:        _description,
                longDescription:    _longDescription,
                remarks:            _remarks,
                children:           _children
            )
            {
                JsonPayloadType = _jsonPayloadType
            };
        }
        public static CommandBuilder Command(string name) => new(name);

        public CommandBuilder Aliases(params string[] aliases)
        {
            _aliases.AddRange(aliases);
            return this;
        }

        public CommandBuilder Exec(Func<PuppetContext, IReadOnlyList<string>, CancellationToken, Task> executeAsync)
        {
            _executeAsync = executeAsync;
            return this;
        }

        public CommandBuilder Test(Func<PuppetContext, IReadOnlyList<string>, CancellationToken, Task<bool>> testAsync)
        {
            _testAsync = testAsync;
            return this;
        }

        public CommandBuilder ExecJson<TPayload>(Func<PuppetContext, TPayload, CancellationToken, Task> executeJsonAsync)
        {
            _jsonPayloadType = typeof(TPayload);
            _executeJsonAsync = (ctx, payload, ct) => executeJsonAsync(ctx, (TPayload)payload, ct);
            return this;
        }

        public CommandBuilder TestJson<TPayload>(Func<PuppetContext, TPayload, CancellationToken, Task<bool>> testJsonAsync)
        {
            _jsonPayloadType = typeof(TPayload);
            _testJsonAsync = (ctx, payload, ct) => testJsonAsync(ctx, (TPayload)payload, ct);
            return this;
        }

        public CommandBuilder Usage(string usage)
        {
            _usage = usage;
            return this;
        }

        public CommandBuilder Description(string description)
        {
            _description = description;
            return this;
        }

        public CommandBuilder LongDescription(string longDescription)
        {
            _longDescription = longDescription;
            return this;
        }

        public CommandBuilder Examples(params string[] examples)
        {
            _examples.AddRange(examples);
            return this;
        }

        public CommandBuilder Remarks(string remarks)
        {
            _remarks = remarks;
            return this;
        }

        public CommandBuilder Children(params PuppetCommand[] children)
        {
            _children.AddRange(children);
            return this;
        }
    }

    public static class CmdBuilder
    {
        public static CommandBuilder Cmd(string name) => new(name);
    }
}
