using UnityEngine;

namespace WorldMapStrategyKit
{
	public class CustomBorders : MonoBehaviour
	{
		public Texture2D borderTexture;

		private WMSK map;
		private GameObject outline;

		private void Start()
		{
			// Get a reference to the World Map API:
			map = WMSK.instance;

			// Color and add outline to Algeria
			var countryIndex = map.GetCountryIndex("Algeria");
			map.ToggleCountrySurface(countryIndex, true, new Color(1, 1, 1, 0.25f));
			map.ToggleCountryOutline(countryIndex, true, borderTexture, 0.5f, Color.gray);

			// Color and add outline to Niger
			countryIndex = map.GetCountryIndex("Niger");
			map.ToggleCountrySurface(countryIndex, true, new Color(0, 1, 0, 0.25f));
			map.ToggleCountryOutline(countryIndex, true, borderTexture, 0.5f, Color.green);

			// Merge three country regions and add a common border (Spain + France + Germany)
			DrawOutline();

			// Zoom into the zone
			map.FlyToCountry(countryIndex, 0, 0.3f);
		}

		public void DrawOutline()
		{
			Region area = null;
			var mapCountries = new Country[3];
			mapCountries[0] = map.GetCountry("France");
			mapCountries[1] = map.GetCountry("Spain");
			mapCountries[2] = map.GetCountry("Germany");
			foreach (var cData in mapCountries)
				if (area == null)
					area = cData.mainRegion;
				else
					area = map.RegionMerge(area, cData.mainRegion);

			if (outline != null)
				Destroy(outline);

			outline = map.DrawRegionOutline("PlayerRealm", area, borderTexture, 1f, Color.blue);
		}
	}
}