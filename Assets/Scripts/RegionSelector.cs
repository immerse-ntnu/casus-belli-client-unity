using System.Collections.Generic;
using UnityEngine;

namespace Hermannia
{
	public class RegionSelector : MonoBehaviour
	{
		private ColorChanger _colorChanger;
		private Region _currentRegion;
		private RegionHandler _regionHandler;

		private void Awake()
		{
			_colorChanger = new ColorChanger(GetComponent<SpriteRenderer>());
			_colorChanger.OnColorSelected += HandleColorChanged;
		}

		private void HandleColorChanged(Color color)
		{
			_currentRegion = _regionHandler.GetRegionFromColor(color);
		}

		private void OnMouseDown() => _colorChanger.HandleSpriteClicked();
	}

	public class RegionHandler
	{
		private Dictionary<Color, Region> _regions;
		public Region GetRegionFromColor(Color color) => _regions[color];

		public RegionHandler()
		{
		}
	}

	public class Region { }
}