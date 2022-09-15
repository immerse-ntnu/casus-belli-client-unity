using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Immerse.BfHClient
{
    public class GameUIController : MonoBehaviour
    {
        public static GameUIController Instance { get; private set; }

        private VisualElement _spawnTroopGUI;
        private Button _spawnBoatButton;
        private Button _spawnFootButton;
        private Button _spawnHorseButton;
        private UIDocument _uiDocument;

        private void Awake()
        {
            Instance = this;
            _uiDocument = GetComponent<UIDocument>();
            var root = _uiDocument.rootVisualElement;
            _spawnTroopGUI = root.Q<VisualElement>("SpawnTroop");
            _spawnFootButton = root.Q<Button>("Foot");
            _spawnFootButton.clicked += SpawnFoot;
            _spawnHorseButton = root.Q<Button>("Horse");
            _spawnHorseButton.clicked += SpawnHorse;
            _spawnBoatButton = root.Q<Button>("Boat");
            _spawnBoatButton.clicked += SpawnBoat;

            SetActive(_spawnTroopGUI, false);
        }
        private void Start()
        {
            RegionSelector.Instance.RegionSelected += RegionSelected;
        }
        public bool IsPointerOverUI ( Vector2 screenPos )
        {  // Shamelessly stolen from https://answers.unity.com/questions/1881324/ui-toolkit-prevent-click-through-visual-element.html
            Vector2 pointerUiPos = new Vector2{ x = screenPos.x , y = Screen.height - screenPos.y };
            List<VisualElement> picked = new List<VisualElement>();
            _uiDocument.rootVisualElement.panel.PickAll( pointerUiPos , picked );
            foreach( var ve in picked )
                if( ve!=null )
                {
                    Color32 bcol = ve.resolvedStyle.backgroundColor;
                    if( bcol.a!=0 && ve.enabledInHierarchy )
                        return true;
                }
            return false;
        }
        private void SpawnFoot()
        {
            Debug.Log("Spawn foot");
        }
        private void SpawnHorse()
        {
            Debug.Log("Spawn horse");
        }
        private void SpawnBoat()
        {
            Debug.Log("Spawn boat");
        }
        private void RegionSelected(Region current)
        {
            bool show = current != null && !current.Name.Contains("Mare");
            
            SetActive(_spawnBoatButton, show && current.IsBeach);
            _spawnTroopGUI.transform.position = new Vector2(Input.mousePosition.x-Screen.width/2f, Screen.height/2f-Input.mousePosition.y);
            SetActive(_spawnTroopGUI, show);
        }
        private void SetActive(VisualElement element, bool show)
        {
            element.SetEnabled(show);
            element.visible = show;
        }
    }
}