using UnityEngine;

namespace Immerse.BfhClient.Game
{
    public class GameCanvasController : MonoBehaviour
    {
        [SerializeField] private ActionPopup _spawnPopup;
        [SerializeField] private ActionPopup _movePopup;
        
        private void Start() => RegionSelector.Instance.RegionSelected += RegionSelected;

        private void OnEnable()
        {
            MoveButton.Clicked += MoveButtonClicked;
        }

        private void OnDisable()
        {
            MoveButton.Clicked -= MoveButtonClicked;
        }

        public void MoveButtonClicked(MoveButton.MoveAction action)
        {
			_movePopup.SetActions("");
            
        }
        private void RegionSelected(Region region)
        {
            if (region == null)
            {
                _movePopup.gameObject.SetActive(false);
                _spawnPopup.gameObject.SetActive(false);
                return;
            }
            Vector2 screenPoint = Input.mousePosition;
            Vector2 worldPosition = Camera.main.ScreenToWorldPoint(screenPoint);
            bool spawning = region.Unit == null;
            _movePopup.gameObject.SetActive(!spawning);
            _spawnPopup.gameObject.SetActive(spawning);
            if (spawning)
            {  // Spawn menu
                _spawnPopup.transform.position = worldPosition;
                
                if (region.IsDockable) 
                    _spawnPopup.SetActions("Foot", "Horse", "Tower", "Boat");
                else 
                    _spawnPopup.SetActions("Foot", "Horse", "Tower");
            }
            else
            {  // Move menu
                _movePopup.transform.position = worldPosition;
                
                
            }

        }
    }
}
