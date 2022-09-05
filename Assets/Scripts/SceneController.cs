using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    public static SceneController Instance { get; private set; }

    public SceneState MainMenu { get => _mainMenu; }
    public SceneState JoinMenu { get => _joinMenu; }
    public SceneState Game { get => _game; }
    
    private SceneState _mainMenu;
    private SceneState _joinMenu;
    private SceneState _game;
    private void Awake()
    {
        Instance = this;
        _game = new("Game");
        _joinMenu = new("JoinMenu");
        _mainMenu = new("MainMenu");
    }
    public void Load(SceneState state)
    {
        SceneManager.LoadScene(state.SceneName);
    }
}
public class SceneState
{
    public string SceneName { get => _sceneName; }
    
    private string _sceneName;
    public SceneState(string sceneName)
    {
        _sceneName = sceneName;
    }
}

