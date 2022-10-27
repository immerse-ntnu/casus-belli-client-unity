using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Immerse.BfhClient
{
	public class RegionSelector : MonoBehaviour
	{
		public static RegionSelector Instance { get; private set; }
		public Region CurrentRegion { get; private set; }

		public event Action<Region> RegionSelected;

		private static readonly int Region = Shader.PropertyToID("_Region");
		private RegionColorHandler _regionColorHandler;
		private RegionLookUp _regionLookUp;
		private Material _material;
		[SerializeField] private TextAsset regionData;

		private void Awake()
		{
			Instance = this;
			var spriteRenderer = GetComponent<SpriteRenderer>();
			_material = spriteRenderer.material;
			_regionColorHandler = new RegionColorHandler(spriteRenderer);
			_regionLookUp = new RegionLookUp(regionData.text);
			
			_material.SetColor(Region, Color.white);
		}

		private void OnMouseDown()
		{
			if (EventSystem.current.IsPointerOverGameObject())
				return;
			var clickedColor = _regionColorHandler.GetSpritePixelColorUnderMousePointer();

			Region newRegion = null;
			if (clickedColor != Color.black)
				newRegion = _regionLookUp.GetRegionFromColor(clickedColor);

			if (newRegion == CurrentRegion || clickedColor == Color.black)
			{  
				newRegion = null;
				clickedColor = Color.white;
			}
			_material.SetColor(Region, clickedColor);
			CurrentRegion = newRegion;

			RegionSelected?.Invoke(CurrentRegion);
		}
	}
}