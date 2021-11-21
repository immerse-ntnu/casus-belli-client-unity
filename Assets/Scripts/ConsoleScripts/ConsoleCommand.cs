using UnityEngine;

public abstract class ConsoleCommand : ScriptableObject
{
    [SerializeField] private string _commandWord = "";

    public string CommandWord => _commandWord;

    public abstract bool Execute(string[] args);
}
