using System.Collections.Generic;
using System.Linq;

public class ConsoleLogic
{
    private readonly IEnumerable<ConsoleCommand> _commands;

    public ConsoleLogic(IEnumerable<ConsoleCommand> commands) => this._commands = commands;

    public void ProcessCommand(string inputValue)
    {
        var inputSplit = inputValue.Split(' ');

        var commandName = inputSplit[0];
        var args = (string[])inputSplit.Skip(1);

        foreach (var command in _commands)
        {
            if (commandName != command.CommandWord)
            {
                continue;
            }

            if (command.Execute(args))
            {
                return;
            }
        }
    }
}
