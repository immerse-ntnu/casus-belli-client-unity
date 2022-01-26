using UnityEngine;

namespace WorldMapStrategyKit
{
	public class DemoElevation : MonoBehaviour
	{
		private WMSK map;
		private GUIStyle labelStyle;

		private void Start()
		{
			// Get a reference to the World Map API:
			map = WMSK.instance;

			// UI Setup - non-important, only for this demo
			labelStyle = new GUIStyle();
			labelStyle.alignment = TextAnchor.MiddleCenter;
			labelStyle.normal.textColor = Color.white;

			map.OnCountryClick += (int countryIndex, int regionIndex, int buttonIndex) =>
			{
				map.RegionSetCustomElevation(map.GetCountry(countryIndex).regions, 0.7f);
			};
		}

		private void OnGUI()
		{
			GUIResizer.AutoResize();
			GUI.Box(new Rect(10, 10, 460, 40), "Click on a region to change its elevation", labelStyle);
		}
	}
}