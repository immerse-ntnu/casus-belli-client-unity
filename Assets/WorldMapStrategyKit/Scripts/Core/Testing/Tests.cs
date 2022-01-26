#if UNITY_EDITOR

using UnityEngine;
using WorldMapStrategyKit.ClipperLib;

namespace WorldMapStrategyKit
{
	public partial class WMSK : MonoBehaviour
	{
		// Use this for initialization
		public void ExecuteTests()
		{
//								ProvinceMigrationTests();
			ClearAll();
			RegionMergeTest();
		}

		private void RegionMergeTest()
		{
			ClearAll();

			var country = new Country("", "", 1);
			var r1 = new Region(country, 0);
			showGrid = true;
			//								Vector2[] pp1 = new Vector2[] { new Vector2(2,1), new Vector2(3,1), new Vector2(4,2), new Vector2(1,2) };
			//								r1.UpdatePointsAndRect(pp1);
			var cell1 = GetCell(10, 10);
			r1.UpdatePointsAndRect(cell1.points);
			var r2 = new Region(country, 1);
			//								Vector2[] pp2 = new Vector2[] { new Vector2(2,1), new Vector2(1,0), new Vector2(4,0), new Vector2(3,1) };
			//								r2.UpdatePointsAndRect(pp2);
			var cell2 = GetCell(10, 11);
			r2.UpdatePointsAndRect(cell2.points);

//												PolygonClipper pc = new PolygonClipper (r1, r2);
//												pc.Compute (PolygonOp.UNION, null);

			var clipper = new Clipper();
			clipper.AddPath(r1, PolyType.ptSubject);
			clipper.AddPath(r2, PolyType.ptClip);
			clipper.Execute(ClipType.ctUnion);

			country.regions.Add(r1);
			var countryIndex = CountryAdd(country);
			RefreshCountryDefinition(countryIndex, null);
			Redraw();
			GenerateCountryRegionSurface(countryIndex, 0, GetColoredTexturedMaterial(Color.red, null));
			Debug.Log("ok");
		}

		//				void ProvinceMigrationTests() {
		//								// Test Case 1:
		//								//												int ci = map.GetCityIndex("Barcelona", "Spain");
		//								//												if (ci >= 0)
		//								//												{
		//								//																Province prov = map.GetProvince(map.cities[ci].province, "Spain");
		//								//																if (prov != null)
		//								//																{
		//								//																				map.ProvinceToCountry(prov, "New Spain");
		//								//																}
		//								//												}
		//
		//								// Test Case 2:
		//								//												int ci = map.GetCityIndex("Edinburgh", "United Kingdom");
		//								//												if (ci >= 0)
		//								//												{
		//								//																Province prov = map.GetProvince(map.cities[ci].province, "United Kingdom");
		//								//																if (prov != null)
		//								//																{
		//								//																				map.ProvinceToCountry(prov, "New Scotland");
		//								//																}
		//								//												}
		//
		//								// Test Case 3:
		//								//												int ci = map.GetCityIndex ("Glasgow", "United Kingdom");
		//								//												if (ci >= 0) {
		//								//																Province prov = map.GetProvince (map.cities [ci].province, "United Kingdom");
		//								//																if (prov != null) {
		//								//																				map.ProvinceToCountry (prov, "New Scotland");
		//								//																}
		//								//												}
		//				}

//		void CitiesFix() {
		//			List<City> fixedCities;
		//			ReadCitiesPackedString ("cities10 2");
		//			fixedCities = cities;
		//
		//			int cityCount = cities.Length;
		//			int fixedCount = fixedcities.Length;
		//			for (int k = 0; k < fixedCount; k++) {
		//				City fixedCity = fixedCities [k];
		//				for (int c=0;c<cityCount;c++) {
		//					City city = cities [c];
		//					if (city.unity2DLocation == fixedCity.unity2DLocation) {
		//						if (city.name != fixedCity.name) {
		//							city.name = fixedCity.name;
		//						}
		//					}
		//
		//				}
		//			}
		//			Debug.Log (cities.Length + " " + fixedcities.Length);
		//
		//			string fullPathName = "Assets/WorldMapStrategyKit/Resources/WMSK/Geodata/cities10";
		//			File.WriteAllText(fullPathName, GetCityGeoData(), System.Text.Encoding.UTF8);
//		}
	}
}

#endif