using UnityEngine;
using UnityEngine.UI;

public class ConsoleUI : MonoBehaviour
{
    [SerializeField] private ConsoleCommand[] commands;
    [SerializeField] private GameObject ui;
    [SerializeField] private InputField inputField;

    private static ConsoleUI _Instance;

    private ConsoleLogic _consoleLogic;

    private ConsoleLogic ConsoleLogic => _consoleLogic ??= new ConsoleLogic(commands);

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
        if (ui.activeSelf)
            ui.SetActive(false);
        else
        {
            ui.SetActive(true);
            inputField.ActivateInputField();
            inputField.text = string.Empty;
        }
    }

    public void ProcessCommand()
    {
        ConsoleLogic.ProcessCommand(inputField.text);

        inputField.text = string.Empty;
    }
}