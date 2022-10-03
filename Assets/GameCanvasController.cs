using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Immerse.BfHClient
{
    public class GameCanvasController : MonoBehaviour
    {
        [SerializeField] private ActionPopup _spawnPopup;
        [SerializeField] private ActionPopup _movePopup;
        
        // Start is called before the first frame update
        void Start() => RegionSelector.Instance.RegionSelected += RegionSelected;

        void RegionSelected(Region region)
        {
            _movePopup.SetActions();

            Vector2 screenPoint = Input.mousePosition;
            Vector2 worldPosition = Camera.main.ScreenToWorldPoint(screenPoint);
            _spawnPopup.Position = worldPosition;
            
            if (region != null && true) {//region.IsLand) {
                if (region.IsDockable) _spawnPopup.SetActions("Foot", "Horse", "Tower", "Boat");
                else _spawnPopup.SetActions("Foot", "Horse", "Tower");
            } else _spawnPopup.SetActions();
            
        }
    }
}
