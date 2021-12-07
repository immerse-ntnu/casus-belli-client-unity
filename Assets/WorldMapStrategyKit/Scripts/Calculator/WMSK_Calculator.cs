using UnityEngine;
using System;
using System.Collections;

namespace WorldMapStrategyKit
{
	public enum UNIT_TYPE
	{
		Degrees,
		DecimalDegrees,
		PlaneCoordinates
	}

	[Serializable, RequireComponent(typeof(WMSK))]
	public class WMSK_Calculator : MonoBehaviour
	{
		public UNIT_TYPE fromUnit = UNIT_TYPE.Degrees;

		// From: latitude (degree)
		public float fromLatDegrees;
		public int fromLatMinutes;

		public float fromLatSeconds;

		// From: longitude (degree)
		public float fromLonDegrees;
		public int fromLonMinutes;

		public float fromLonSeconds;

		// From: decimal degrees
		public float fromLatDec, fromLonDec;

		// From: plane coordinates
		public float fromX, fromY;

		// To: latitude (degree)
		public float toLatDegree;
		public int toLatMinute;

		public float toLatSeconds;

		// To: longitude (degree)
		public float toLonDegree;
		public int toLonMinute;

		public float toLonSecond;

		// To: decimal degrees
		public float toLatDec, toLonDec;

		// To: spherical coordinates
		public float toX, toY;
		public bool captureCursor;
		public bool isDirty;

		public int cityDistanceFrom = -1, cityDistanceTo = -1;
		public string cityDistanceResult = "";

		public WMSK map => GetComponent<WMSK>();

		public Vector3 toPlaneLocation => new Vector2(toX, toY);

		/// <summary>
		/// Returns either "N" or "S" depending on the converted latitude
		/// </summary>
		public string toLatCardinal => toLatDec >= 0 ? "N" : "S";

		/// <summary>
		/// Returns either "W" or "E" depending on the converted longitude
		/// </summary>
		public string toLonCardinal => toLonDec >= 0 ? "E" : "W";

		private Vector3 lastCursorPos;
		public string errorMsg;

		private void Update()
		{
			if (captureCursor)
			{
				if (map != null && map.cursorLocation != lastCursorPos)
				{
					lastCursorPos = map.cursorLocation;
					fromX = map.cursorLocation.x;
					fromY = map.cursorLocation.y;
					Convert();
				}
				if (Input.GetKey(KeyCode.C))
					captureCursor = false;
			}
		}

		public bool Convert()
		{
			errorMsg = "";
			try
			{
				if (fromUnit == UNIT_TYPE.Degrees)
				{
					toLatDegree = fromLatDegrees;
					toLatMinute = fromLatMinutes;
					toLatSeconds = fromLatSeconds;
					toLonDegree = fromLonDegrees;
					toLonMinute = fromLonMinutes;
					toLonSecond = fromLonSeconds;
					toLatDec = fromLatDegrees + fromLatMinutes / 60.0f + fromLatSeconds / 3600.0f;
					toLonDec = fromLonDegrees + fromLonMinutes / 60.0f + fromLonSeconds / 3600.0f;
					toX = (toLonDec + 180) / 360 - 0.5f;
					toY = toLatDec / 180;
				}
				else if (fromUnit == UNIT_TYPE.DecimalDegrees)
				{
					toLatDec = fromLatDec;
					toLonDec = fromLonDec;
					toLatDegree = (int)fromLatDec;
					toLatMinute = (int)(Mathf.Abs(fromLatDec) * 60) % 60;
					toLatSeconds = Mathf.Abs(fromLatDec) * 3600 % 60;
					toLonDegree = (int)fromLonDec;
					toLonMinute = (int)(Mathf.Abs(fromLonDec) * 60) % 60;
					toLonSecond = Mathf.Abs(fromLonDec) * 3600 % 60;
					toX = (toLonDec + 180) / 360 - 0.5f;
					toY = toLatDec / 180;
				}
				else if (fromUnit == UNIT_TYPE.PlaneCoordinates)
				{
					toLatDec = 180.0f * fromY;
					toLonDec = 360.0f * (fromX + 0.5f) - 180.0f;
					toLatDegree = (int)toLatDec;
					toLatMinute = (int)(Mathf.Abs(toLatDec) * 60) % 60;
					toLatSeconds = Mathf.Abs(toLatDec) * 3600 % 60;
					toLonDegree = (int)toLonDec;
					toLonMinute = (int)(Mathf.Abs(toLonDec) * 60) % 60;
					toLonSecond = Mathf.Abs(toLonDec) * 3600 % 60;
					toX = fromX;
					toY = fromY;
				}
			}
			catch (ApplicationException ex)
			{
				errorMsg = ex.Message;
			}
			isDirty = true;
			return errorMsg.Length == 0;
		}

		/// <summary>
		/// Returns a formatted lat/lon coordinates string based on the current cursor position
		/// </summary>
		/// <value>The pretty current lat lon.</value>
		public string prettyCurrentLatLon
		{
			get
			{
				fromUnit = UNIT_TYPE.PlaneCoordinates;
				Vector2 cursor = map.cursorLocation;
				fromX = cursor.x;
				fromY = cursor.y;
				Convert();
				return string.Format("{0}°{1}'{2:F2}\"{3} {4}°{5}'{6:F2}\"{7}", Mathf.Abs(toLatDegree),
					toLatMinute, toLatSeconds, toLatCardinal, Mathf.Abs(toLonDegree), toLonMinute,
					toLonSecond, toLonCardinal);
			}
		}

		/// <summary>
		/// Returns distance in meters from two lat/lon coordinates
		/// </summary>
		public float Distance(float latDec1, float lonDec1, float latDec2, float lonDec2)
		{
			float R = 6371000; // metres
			var phi1 = latDec1 * Mathf.Deg2Rad;
			var phi2 = latDec2 * Mathf.Deg2Rad;
			var deltaPhi = (latDec2 - latDec1) * Mathf.Deg2Rad;
			var deltaLambda = (lonDec2 - lonDec1) * Mathf.Deg2Rad;

			var a = Mathf.Sin(deltaPhi / 2) * Mathf.Sin(deltaPhi / 2) +
			        Mathf.Cos(phi1) *
			        Mathf.Cos(phi2) *
			        Mathf.Sin(deltaLambda / 2) *
			        Mathf.Sin(deltaLambda / 2);
			var c = 2.0f * Mathf.Atan2(Mathf.Sqrt(a), Mathf.Sqrt(1.0f - a));
			return R * c;
		}

		public float Distance(City city1, City city2) =>
			Distance(city1.unity2DLocation, city2.unity2DLocation);

		public float Distance(Vector2 position1, Vector2 position2)
		{
			var latDec1 = 180.0f * position1.y;
			var lonDec1 = 360.0f * (position1.x + 0.5f) - 180.0f;
			var latDec2 = 180.0f * position2.y;
			var lonDec2 = 360.0f * (position2.x + 0.5f) - 180.0f;
			return Distance(latDec1, lonDec1, latDec2, lonDec2);
		}
	}
}