using UnityEngine;

namespace Hermannia
{
	public class RegionSelector : MonoBehaviour
	{
		private ColorChanger _colorChanger;
		private Region _currentRegion;
		private RegionHandler _regionHandler;
		[SerializeField] private TextAsset regionData;
		

		private void Awake()
		{
			_regionHandler = new RegionHandler(regionData);
			_colorChanger = new ColorChanger(GetComponent<SpriteRenderer>());
			_colorChanger.OnColorSelected += HandleColorChanged;
		}

		private void OnMouseDown() => _colorChanger.HandleSpriteClicked();

		private void HandleColorChanged(Color color)
		{
			_currentRegion = _regionHandler.GetRegionFromColor(color);
			/*
			print("Switched to region: " + _currentRegion.Name);
			print("Regions neighbours: " );
			foreach (var neighbour in _currentRegion.Neighbours)
			{
				print(neighbour.Name);
			}
		*/
		}
	}
}