using UnityEngine;
using UnityEngine.SceneManagement;

namespace Immerse.BfhClient.UI
{
    public class SceneManager : MonoBehaviour
    {
        public record SceneState(string SceneName)
        {
            public string SceneName { get; } = SceneName;
        }
        public static SceneManager Instance { get; private set; }
        public SceneState MainMenu { get; } = new("MainMenu");
        public SceneState JoinMenu { get; } = new("JoinMenu");
        public SceneState Game { get; } = new("Game");

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public static void Load(SceneState state) => UnityEngine.SceneManagement.SceneManager.LoadScene(state.SceneName);
    }
}