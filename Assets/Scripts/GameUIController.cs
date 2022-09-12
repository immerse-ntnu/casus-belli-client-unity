using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Hermannia
{
    public class GameUIController : MonoBehaviour
    {
        private VisualElement spawnTroopGUI;

        private void Awake()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            spawnTroopGUI = root.Q<VisualElement>("SpawnTroop");
            root.Q<Button>("Foot").clicked += SpawnFoot;
            root.Q<Button>("Horse").clicked += SpawnHorse;
            root.Q<Button>("Boat").clicked += SpawnBoat;

            SetActive(spawnTroopGUI, false);
        }
        private void Start()
        {
            // Not done in Awake because it depends on other MonoBehaviours
            RegionSelector.Instance.RegionSelected += RegionSelected;
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
        private void RegionSelected()
        {
            Region current = RegionSelector.Instance.CurrentRegion;
            SetActive(spawnTroopGUI, current != null);
        }
        private void SetActive(VisualElement element, bool show)
        {
            element.SetEnabled(show);
            element.visible = show;
        }
    }

}