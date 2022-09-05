using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class JoinLobbyUIController : MonoBehaviour
{
    private Button _joinButton;
    private Button _backButton;
    private TextField _codeField;

    // Start is called before the first frame update
    void Start()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        _joinButton = root.Q<Button>("Join");
        _backButton = root.Q<Button>("Back");
        _codeField = root.Q<TextField>("Code");

        _joinButton.clicked += JoinButtonPressed;
        _backButton.clicked += BackButtonPressed;
    }

    void JoinButtonPressed()
    {
        Debug.Log("Attempt to join lobby " + _codeField.text);
        SceneController.Instance.Load(SceneController.Instance.Game);
    }
    void BackButtonPressed()
    {
        SceneController.Instance.Load(SceneController.Instance.MainMenu);
    }
}
