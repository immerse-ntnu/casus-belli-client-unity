using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Immerse.BfHClient
{
	public class RegionSelector : MonoBehaviour
	{
		public static RegionSelector Instance { get; private set; }
		public Region CurrentRegion { get => _currentRegion; }
		public event Action<Region> RegionSelected;

		private static readonly int Region = Shader.PropertyToID("_Region");
		private RegionColorHandler _regionColorHandler;
		private RegionHandler _regionHandler;
		private Material _material;
		private Region _currentRegion;

		private void Awake()
		{
			Instance = this;
			var spriteRenderer = GetComponent<SpriteRenderer>();
			_material = spriteRenderer.material;
			_regionColorHandler = new RegionColorHandler(spriteRenderer);
			_regionHandler = GetComponent<RegionHandler>();
			
			_material.SetColor(Region, Color.white);
		}

		private void OnMouseDown()
		{
			if (EventSystem.current.IsPointerOverGameObject())
				return;
			var clickedColor = _regionColorHandler.GetSpritePixelColorUnderMousePointer();

			Region newRegion = null;
			if (clickedColor != Color.black)
				newRegion = _regionHandler.GetRegionFromColor(clickedColor);

			if (newRegion == _currentRegion || clickedColor == Color.black)
			{  
				newRegion = null;
				clickedColor = Color.white;
			}
			_material.SetColor(Region, clickedColor);
			_currentRegion = newRegion;

			RegionSelected?.Invoke(_currentRegion);
		}
	}
}