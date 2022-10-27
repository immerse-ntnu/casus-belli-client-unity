using UnityEngine;
using UnityEngine.UIElements;

namespace Immerse.BfhClient.UI
{
    public class JoinLobbyUIController : MonoBehaviour
    {
        private Button _joinButton;
        private Button _backButton;
        private TextField _codeField;

        private void Start()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;

            _joinButton = root.Q<Button>("Join");
            _backButton = root.Q<Button>("Back");
            _codeField = root.Q<TextField>("Code");

            _joinButton.clicked += JoinButtonPressed;
            _backButton.clicked += BackButtonPressed;
        }

        private void JoinButtonPressed()
        {
            Debug.Log("Attempt to join lobby " + _codeField.text);
            SceneController.Load(SceneController.Instance.Game);
        }

        private void BackButtonPressed()
        {
            SceneController.Load(SceneController.Instance.MainMenu);
        }
    }
}
