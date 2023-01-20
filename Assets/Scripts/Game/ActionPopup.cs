using System.Linq;
using UnityEngine;

namespace Immerse.BfhClient.Game
{
    public class ActionPopup : MonoBehaviour
    {
        private SpawnButton[] _actionButtons;

        private void Awake()
        {
            _actionButtons = GetComponentsInChildren<SpawnButton>();
            foreach (var button in _actionButtons)
            {
                print(button.gameObject.name);
            }
        }

        public void SetActions(params string[] actions)
        {
            foreach (var actionButton in _actionButtons)
            {
                var buttonGO = actionButton.gameObject;
                var isVisible = actions.Contains(buttonGO.name);
                buttonGO.SetActive(isVisible);
            }
        }
    }
}
