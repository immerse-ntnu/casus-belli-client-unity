using UnityEngine;

namespace Immerse.BfhClient.Game
{
    public class GameCanvasController : MonoBehaviour
    {
        [SerializeField] private ActionPopup _spawnPopup;
        [SerializeField] private ActionPopup _movePopup;
        
        private void Start() => RegionSelector.Instance.RegionSelected += RegionSelected;

        private void RegionSelected(Region region)
        {
            Vector2 screenPoint = Input.mousePosition;
            Vector2 worldPosition = Camera.main.ScreenToWorldPoint(screenPoint);
            _spawnPopup.transform.position = worldPosition;
            
            if (region != null) {
                if (region.IsDockable) 
                    _spawnPopup.SetActions("Foot", "Horse", "Tower", "Boat");
                else 
                    _spawnPopup.SetActions("Foot", "Horse", "Tower");
            } else _spawnPopup.SetActions();
        }
    }
}
