using UnityEngine;
using UnityEngine.UI;

public class ConsoleUI : MonoBehaviour
{
    [SerializeField] private ConsoleCommand[] _commands = new ConsoleCommand[0];
    [SerializeField] private GameObject _ui;
    [SerializeField] private InputField _inputField;

    private static ConsoleUI instance;

    private ConsoleLogic _consoleLogic;

    private ConsoleLogic ConsoleLogic 
    {
        get
        {
            if (_consoleLogic == null)
            {
                _consoleLogic = new ConsoleLogic(_commands);
            }
            return _consoleLogic;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
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