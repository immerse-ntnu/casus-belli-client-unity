using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class MainmenuUIController : MonoBehaviour
{
    private Button createButton;
    private Button joinButton;

    // Start is called before the first frame update
    void Start()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        createButton = root.Q<Button>("Create");
        joinButton = root.Q<Button>("Join");

        createButton.clicked += CreateButtonPressed;
        joinButton.clicked += JoinButtonPressed;
    }

    void CreateButtonPressed()
    {
        //SceneController.Instance.Load(SceneController.Scene.Joinlobby);
    }
    void JoinButtonPressed()
    {
        SceneController.Instance.Load(SceneController.Scene.Joinlobby);
    }
}
