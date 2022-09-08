using System;
using UnityEngine;

namespace Hermannia
{
	public class RegionSelector : MonoBehaviour
	{
		public event Action SelectedRegion;
		private static readonly int Region = Shader.PropertyToID("_Region");
		[SerializeField] private TextAsset regionData;
		private RegionColorHandler _regionColorHandler;
		private Region _currentRegion;
		private RegionHandler _regionHandler;
		private Material _material;

		private void Awake()
		{
			var spriteRenderer = GetComponent<SpriteRenderer>();
			_material = spriteRenderer.material;
			_regionColorHandler = new RegionColorHandler(spriteRenderer);
			_regionHandler = new RegionHandler(regionData.text);
			_material.SetColor(Region, Color.white);
		}

		private void OnMouseDown()
		{
			//Todo make this a manager-class
			//This logic probably should be delegated to another class if this becomes a manager-class
			var clickedColor = _regionColorHandler.GetSpritePixelColorUnderMousePointer();
			_currentRegion = null;
			if (clickedColor != Color.black)
			{
				_material.SetColor(Region, clickedColor);
				_currentRegion = _regionHandler.GetRegionFromColor(clickedColor);
			}
			
			SelectedRegion?.Invoke();
		}
	}
}