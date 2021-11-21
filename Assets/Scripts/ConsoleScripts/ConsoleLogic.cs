using System.Collections.Generic;
using System.Linq;

public class ConsoleLogic
{
    private readonly IEnumerable<ConsoleCommand> commands;

    public ConsoleLogic(IEnumerable<ConsoleCommand> commands)
    {
        this.commands = commands;
    }


    public void ProcessCommand(string inputValue)
    {
        string[] inputSplit = inputValue.Split(' ');

        string commandName = inputSplit[0];
        string[] args = (string[])inputSplit.Skip(1);

        foreach (var command in commands)
        {
            if (commandName != command.commandWord)
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
