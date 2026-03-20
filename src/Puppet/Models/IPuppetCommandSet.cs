namespace Puppet.Models;

public interface IPuppetCommandSet
{
    IReadOnlyList<PuppetCommand> Commands { get; }
}