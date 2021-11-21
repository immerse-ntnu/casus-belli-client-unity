using UnityEngine;

public abstract class ConsoleCommand : ScriptableObject
{
    [SerializeField] private string _commandWord = "";

    public string commandWord => _commandWord;

    public abstract bool Execute(string[] args);
}
