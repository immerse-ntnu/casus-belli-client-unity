using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    public class SceneState
    {
        public string SceneName { get => _sceneName; }
    
        private string _sceneName;
        public SceneState(string sceneName)
        {
            _sceneName = sceneName;
        }
    }
    public static SceneController Instance { get; private set; }
    public SceneState MainMenu => _mainMenu;
    public SceneState JoinMenu => _joinMenu;
    public SceneState Game => _game;
    
    private SceneState _mainMenu;
    private SceneState _joinMenu;
    private SceneState _game;
    private void Awake()
    {
        Instance = this;
        _game = new SceneState("Game");
        _joinMenu = new SceneState("JoinMenu");
        _mainMenu = new SceneState("MainMenu");
    }
    public void Load(SceneState state)
    {
        SceneManager.LoadScene(state.SceneName);
    }
}