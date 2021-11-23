using UnityEngine;

public abstract class ConsoleCommand : ScriptableObject
{
	[SerializeField] private string commandWord = "";
    public abstract bool Execute(string[] args);

    public bool IsCommand(string commandName) => commandName == commandWord;
}
