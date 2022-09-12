using System;
using UnityEngine;

namespace Immerse.BfHClient
{
	public class RegionSelector : MonoBehaviour
	{
		public event Action<Region> RegionSelected;
		private static readonly int Region = Shader.PropertyToID("_Region");
		[SerializeField] private TextAsset regionData;
		private RegionColorHandler _regionColorHandler;
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
			var clickedColor = _regionColorHandler.GetSpritePixelColorUnderMousePointer();
			Region currentRegion = null;
			if (clickedColor != Color.black)
			{
				currentRegion = _regionHandler.GetRegionFromColor(clickedColor);
				_material.SetColor(Region, currentRegion is not null ? clickedColor : Color.white);
			}
			
			RegionSelected?.Invoke(currentRegion);
		}
	}
}