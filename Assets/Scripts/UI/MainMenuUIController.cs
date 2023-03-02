using UnityEngine;
using UnityEngine.UIElements;

namespace Immerse.BfhClient.UI
{
    public class MainMenuUIController : MonoBehaviour
    {
        private Button _joinButton;
        public void CreateButtonPressed()
        {
            SceneManager.Load(SceneManager.Instance.JoinMenu);
        }
        
        public void JoinButtonPressed()
        {
            SceneManager.Load(SceneManager.Instance.JoinMenu);
        }
        
        public void ExitButtonPressed()
        {
            Application.Quit();
        }
    }
}
