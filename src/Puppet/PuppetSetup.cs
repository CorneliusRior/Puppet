
using Puppet.CommandSets;
using Puppet.Models;

namespace Puppet;
public sealed partial class Puppet
{
    // Constructor:
    public Puppet(params IPuppetCommandSet[] commandSets)
    {
        List<PuppetCommand> rootCommands = new();
        rootCommands.AddRange(new BaseCommands().Commands);
        foreach (IPuppetCommandSet cs in commandSets) rootCommands.AddRange(cs.Commands);
        AssignCommandAddresses(rootCommands);
        BuildAliasDictionary(rootCommands);
    }

    // Set-up functions:
    public void AssignCommandAddresses(List<PuppetCommand> commandList)
    {
        commandList = commandList.OrderBy(c => c.Name).ToList();
        foreach (PuppetCommand root in commandList)
        {
            List<string> commandHead = new();
            AssignChildAddress(root, commandHead);
        }
    }

    public void AssignChildAddress(PuppetCommand command, List<string> commandHead)
    {
        commandHead.Add(command.Name);        
        command.Address = string.Join('.', commandHead);
        CommandIndex.Add(command.Address, command);
        foreach (PuppetCommand child in command.Children.OrderBy(c => c.Name).ToList()) AssignChildAddress(child, commandHead);
        commandHead.RemoveAt(commandHead.Count - 1);
    }

    public void BuildAliasDictionary(List<PuppetCommand> rootCommandList)
    {
        foreach (PuppetCommand root in rootCommandList)
        {
            List<string> addresses = new();
            addresses.Add(root.Name);
            addresses.AddRange(root.Aliases);
            foreach (string alias in addresses)
            {
                if (!AliasIndex.TryAdd(alias, root)) throw new PuppetException($"Duplicate command or alias address: '{alias}' in '{root.Name}' ('{(root.Address ?? "Unknown address")}')");
                foreach (PuppetCommand child in root.Children)
                {
                    AliasDictionaryAdd(alias, child);
                }
            }
        }
    }

    public void AliasDictionaryAdd(string parentAddress, PuppetCommand command)
    {
        List<string> addresses = new();
        addresses.Add(command.Name);
        addresses.AddRange(command.Aliases);
        foreach (string alias in addresses)
        {
            string aliasAddress = parentAddress + '.' + alias;
            if (!AliasIndex.TryAdd(aliasAddress, command)) throw new PuppetException($"Duplocate command or alias address: `{aliasAddress}` in '{command.Name}' ('{command.Address ?? "Unknown address"}')");
            foreach (PuppetCommand child in command.Children)
            {
                AliasDictionaryAdd(aliasAddress, child);
            }
        }
    }
}