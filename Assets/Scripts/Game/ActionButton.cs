using System;
using UnityEngine;
using UnityEngine.UI;

namespace Immerse.BfhClient.Game
{
    public class ActionButton : MonoBehaviour
    {
        [SerializeField] private GameObject unit;

        private void Awake() => 
            GetComponent<Button>().onClick.AddListener(SpawnUnit);

        public void SpawnUnit()
        {
            var spawnedUnit = Instantiate(unit,RegionSelector.Instance.CurrentRegion.transform);
            Debug.Log(unit.name);
        }
    }
}
