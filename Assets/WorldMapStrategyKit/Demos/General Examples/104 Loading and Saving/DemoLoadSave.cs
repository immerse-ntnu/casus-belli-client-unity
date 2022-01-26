using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WorldMapStrategyKit
{
	public class DemoLoadSave : MonoBehaviour
	{
		/// <summary>
		/// Here we'll store the data - you can save these strings to file or database (ie. System.IO.File.WriteAllText(path, countryGeoData)
		/// </summary>
		private string countryGeoData,
			countryAttributes,
			provinceGeoData,
			provinceAttributes,
			cityGeoData,
			cityAttributes,
			mountPointGeoData,
			mountPointAttributes;

		private int state;
		private GUIStyle buttonStyle;
		private WMSK map;

		private void Start()
		{
			map = WMSK.instance;

			// setup GUI resizer - only for the demo
			GUIResizer.Init(800, 500);

			// setup GUI styles - only for the demo
			buttonStyle = new GUIStyle();
			buttonStyle.alignment = TextAnchor.MiddleCenter;
			buttonStyle.normal.background = Texture2D.whiteTexture;
			buttonStyle.normal.textColor = Color.black;

#if UNITY_EDITOR
			EditorUtility.DisplayDialog("Load/Save Demo",
				"In this demo scene, a map change is simulated (North America is collapsed into one single country), then saved and loaded.",
				"Ok");
#endif
		}

		private void OnGUI()
		{
			// Do autoresizing of GUI layer
			GUIResizer.AutoResize();

			switch (state)
			{
				case 0:
					if (GUI.Button(new Rect(10, 10, 160, 30), "Merge North America", buttonStyle))
					{
						var countryUSA = map.GetCountryIndex("United States of America");
						var countryCanada = map.GetCountryIndex("Canada");
						map.CountryTransferCountry(countryUSA, countryCanada, true);

						countryUSA = map.GetCountryIndex("United States of America");
						var countryMexico = map.GetCountryIndex("Mexico");
						map.CountryTransferCountry(countryUSA, countryMexico, true);
						state++;
					}
					break;
				case 1:
					if (GUI.Button(new Rect(10, 10, 160, 30), "Save Current Frontiers", buttonStyle))
					{
						SaveAllData();
						state++;
#if UNITY_EDITOR
						EditorUtility.DisplayDialog("Saved Data",
							"Data stored in temporary variables. You can now click reset frontiers to simulate a game restart and then Load Saved Data to restore the saved data.",
							"Ok");
#endif
					}
					break;
				case 2:
					if (GUI.Button(new Rect(10, 10, 160, 30), "Reset Frontiers", buttonStyle))
					{
						map.ReloadData();
						map.Redraw();
						state++;
					}
					break;
				case 3:
					if (GUI.Button(new Rect(10, 10, 160, 30), "Load Saved Data", buttonStyle))
					{
						LoadAllData();
						state++;
#if UNITY_EDITOR
						EditorUtility.DisplayDialog("Data Loaded!",
							"Data has been loaded from the temporary variables.", "Ok");
#endif
					}
					break;
				case 4:
					if (GUI.Button(new Rect(10, 50, 160, 30), "Reset Frontiers", buttonStyle))
					{
						map.ReloadData();
						map.Redraw();
						state = 0;
					}
					break;
			}
		}

		private void SaveAllData()
		{
			// Store current countries information and frontiers data in string variables
			countryGeoData = map.GetCountryGeoData();
			countryAttributes = map.GetCountriesAttributes();

			// Same for provinces. This wouldn't be neccesary if you are not using provinces in your app.
			provinceGeoData = map.GetProvinceGeoData();
			provinceAttributes = map.GetProvincesAttributes();

			// Same for cities. This wouldn't be neccesary if you are not using cities in your app.
			cityGeoData = map.GetCityGeoData();
			cityAttributes = map.GetCitiesAttributes();

			// Same for mount points. This wouldn't be neccesary if you are not using mount points in your app.
			mountPointGeoData = map.GetMountPointsGeoData();
			mountPointAttributes = map.GetMountPointsAttributes();
		}

		private void LoadAllData()
		{
			// Load country information from a string.
			map.SetCountryGeoData(countryGeoData);
			map.SetCountriesAttributes(countryAttributes);

			// Same for provinces. This wouldn't be neccesary if you are not using provinces in your app.
			map.SetProvincesGeoData(provinceGeoData);
			map.SetProvincesAttributes(provinceAttributes);

			// Same for cities. This wouldn't be neccesary if you are not using cities in your app.
			map.SetCityGeoData(cityGeoData);
			map.SetCitiesAttributes(cityAttributes);

			// Same for mount points. This wouldn't be neccesary if you are not using mount points in your app.
			map.SetMountPointsGeoData(mountPointGeoData);
			map.SetMountPointsAttributes(mountPointAttributes);

			// Redraw everything
			map.Redraw();
		}
	}
}