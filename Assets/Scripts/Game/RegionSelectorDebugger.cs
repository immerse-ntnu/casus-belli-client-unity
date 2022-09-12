using UnityEngine;

namespace Immerse.BfHClient
{
	public class RegionSelectorDebugger : MonoBehaviour
	{
		[SerializeField] private bool debugging = true;
		
		private void Start() => 
			GetComponent<RegionSelector>().RegionSelected += OnSelectedRegion;

		private void OnSelectedRegion(Region region)
		{
			if (!debugging)
				return;
			Debug.Log(region is null
				? "Deselected region"
				: $"Currently selected region is <color=green>{region.Name}</color>", this);
		}
	}
}