using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class MainMenuUIController : MonoBehaviour
{
    private Button _joinButton;
    
    void Start()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        
        _joinButton = root.Q<Button>("Join");
        _joinButton.clicked += JoinButtonPressed;
    }
    void JoinButtonPressed()
    {
        SceneController.Instance.Load(SceneController.Instance.JoinMenu);
    }
}
