using UnityEngine;
using UnityEngine.UI;

public class ConsoleUI : MonoBehaviour
{
    [SerializeField] private ConsoleCommand[] _commands;
    [SerializeField] private GameObject _ui;
    [SerializeField] private InputField _inputField;

    private static ConsoleUI _Instance;

    private ConsoleLogic _consoleLogic;

    private ConsoleLogic ConsoleLogic => _consoleLogic ??= new ConsoleLogic(_commands);

    private void Awake()
    {
        if (_Instance != null && _Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _Instance = this;
    }

    public void Toggle()
    {
        if (_ui.activeSelf)
        {
            _ui.SetActive(false);
        }
        else
        {
            _ui.SetActive(true);
            _inputField.ActivateInputField();
            _inputField.text = string.Empty;
        }
    }

    public void ProcessCommand()
    {
        ConsoleLogic.ProcessCommand(_inputField.text);

        _inputField.text = string.Empty;
    }
}