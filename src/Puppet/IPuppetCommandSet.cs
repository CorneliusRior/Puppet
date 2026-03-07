namespace Puppet;

public interface IPuppetCommandSet
{
    IReadOnlyList<PuppetCommand> Commands { get; }
}