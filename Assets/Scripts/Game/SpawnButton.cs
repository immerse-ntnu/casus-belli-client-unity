using UnityEngine;
using UnityEngine.UI;

namespace Immerse.BfhClient.Game
{
    public class SpawnButton : MonoBehaviour
    {
        [SerializeField] private GameObject unit;

        private void Awake() => 
            GetComponent<Button>().onClick.AddListener(SpawnUnit);

        public void SpawnUnit()
        {
            Region region = RegionSelector.Instance.CurrentRegion;
            var spawnedUnit = Instantiate(unit,region.transform);
            region.Unit = spawnedUnit;
        }
    }
}
