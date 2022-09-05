using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class JoinlobbyUIController : MonoBehaviour
{
    private Button joinButton;
    private Button backButton;
    private TextField codeField;

    // Start is called before the first frame update
    void Start()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        joinButton = root.Q<Button>("Join");
        backButton = root.Q<Button>("Back");
        codeField = root.Q<TextField>("Code");

        joinButton.clicked += JoinButtonPressed;
        backButton.clicked += BackButtonPressed;
    }

    void JoinButtonPressed()
    {
        Debug.Log("Attempt to join lobby " + codeField.text);
        SceneController.Instance.Load(SceneController.Scene.Hermannia);
    }
    void BackButtonPressed()
    {
        SceneController.Instance.Load(SceneController.Scene.Mainmenu);
    }
}