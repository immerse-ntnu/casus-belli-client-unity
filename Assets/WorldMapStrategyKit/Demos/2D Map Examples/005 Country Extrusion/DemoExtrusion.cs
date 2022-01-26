using UnityEngine;

namespace WorldMapStrategyKit
{
	public class DemoExtrusion : MonoBehaviour
	{
		private WMSK map;

		private void Start()
		{
			map = WMSK.instance;

			var USAIndex = map.GetCountryIndex("United States of America");
			var region = map.GetCountry(USAIndex).mainRegion;

			map.RegionGenerateExtrudeGameObject("Extruded USA", region, 1f, Color.gray);

			map.FlyToCountry(USAIndex, 4f, 0.2f);
		}
	}
}