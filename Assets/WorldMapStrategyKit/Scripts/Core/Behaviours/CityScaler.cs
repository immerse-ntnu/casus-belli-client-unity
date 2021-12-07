using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace WorldMapStrategyKit
{
	/// <summary>
	/// City scaler. Checks the city icons' size is always appropiate
	/// </summary>
	public class CityScaler : MonoBehaviour
	{
		private const float CITY_SIZE_ON_SCREEN = 10.0f;
		private Vector3 lastCamPos, lastPos;
		private float lastIconSize;
		private float lastCustomSize;
		private float lastOrtographicSize;

		[NonSerialized] public WMSK
			map;

		private void Start()
		{
			ScaleCities();
		}

		// Update is called once per frame
		private void Update()
		{
			if (lastPos != transform.position ||
			    lastCamPos != map.currentCamera.transform.position ||
			    lastIconSize != map.cityIconSize ||
			    map.currentCamera.orthographic &&
			    map.currentCamera.orthographicSize != lastOrtographicSize)
				ScaleCities();
		}

		public void ScaleCities()
		{
			if (map == null ||
			    map.currentCamera == null ||
			    map.currentCamera.pixelWidth == 0 ||
			    gameObject == null)
				return; // Camera pending setup

			try
			{
				// annotate current values 
				lastPos = transform.position;
				lastCamPos = map.currentCamera.transform.position;
				lastIconSize = map.cityIconSize;
				lastOrtographicSize = map.currentCamera.orthographicSize;

				var plane = new Plane(transform.forward, transform.position);
				var dist = plane.GetDistanceToPoint(lastCamPos);
				var centerPos = lastCamPos - transform.forward * dist;
				var a = map.currentCamera.WorldToScreenPoint(centerPos);
				var b = new Vector3(a.x, a.y + CITY_SIZE_ON_SCREEN, a.z);
				if (map.currentCamera.pixelWidth == 0)
					return; // Camera pending setup
				var aa = map.currentCamera.ScreenToWorldPoint(a);
				var bb = map.currentCamera.ScreenToWorldPoint(b);
				var scale = (aa - bb).magnitude * map.cityIconSize;
				if (map.currentCamera.orthographic)
					scale /= 1 +
					         map.currentCamera.orthographicSize *
					         map.currentCamera.orthographicSize *
					         (0.1f / map.transform.localScale.x);
				else
					scale /= 1 + dist * dist * (0.1f / map.transform.localScale.x);
				var newScale = new Vector3(scale / WMSK.mapWidth, scale / WMSK.mapHeight, 1.0f);
				map.currentCityScale = newScale;

				// check if scale has changed
				var tNormalCities = transform.Find("Normal Cities");
				var needRescale = false;
				Transform tChild;
				if (tNormalCities != null && tNormalCities.childCount > 0)
				{
					tChild = tNormalCities.GetChild(0);
					if (tChild != null)
						if (tChild.localScale != newScale)
							needRescale = true;
				}
				var tRegionCapitals = transform.Find("Region Capitals");
				if (!needRescale && tRegionCapitals != null && tRegionCapitals.childCount > 0)
				{
					tChild = tRegionCapitals.GetChild(0);
					if (tChild != null)
						if (tChild.localScale != newScale)
							needRescale = true;
				}
				var tCountryCapitals = transform.Find("Country Capitals");
				if (!needRescale && tCountryCapitals != null && tCountryCapitals.childCount > 0)
				{
					tChild = tCountryCapitals.GetChild(0);
					if (tChild != null)
						if (tChild.localScale != newScale)
							needRescale = true;
				}

				if (!needRescale)
					return;
				// apply scale to all cities children
				foreach (Transform t in tNormalCities)
					t.localScale = newScale;
				foreach (Transform t in tRegionCapitals)
					t.localScale = newScale * 1.75f;
				foreach (Transform t in tCountryCapitals)
					t.localScale = newScale * 2.0f;
			}
			catch { }
		}

		public void ScaleCities(float customSize)
		{
			if (customSize == lastCustomSize)
				return;
			lastCustomSize = customSize;
			var newScale = new Vector3(customSize / WMSK.mapWidth, customSize / WMSK.mapHeight, 1);
			foreach (Transform t in transform.Find("Normal Cities"))
				t.localScale = newScale;
			foreach (Transform t in transform.Find("Region Capitals"))
				t.localScale = newScale * 1.75f;
			foreach (Transform t in transform.Find("Country Capitals"))
				t.localScale = newScale * 2.0f;
		}
	}
}