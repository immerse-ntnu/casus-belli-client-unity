// World Strategy Kit for Unity - Main Script
// (C) 2016-2020 by Ramiro Oliva (Kronnect)
// Don't modify this script - changes could be lost if you upgrade to a more recent version of WMSK

using System;
using UnityEngine;

namespace WorldMapStrategyKit
{
	public static class Conversion
	{
		private const float EARTH_RADIUS = 6371000f;

		#region Public Conversion API area

		/// <summary>
		/// Returns local position from latitude and longitude
		/// </summary>
		public static Vector2 GetLocalPositionFromLatLon(float lat, float lon)
		{
			Vector2 p;
			p.x = (lon + 180f) / 360f - 0.5f;
			p.y = (lat + 90f) / 180f - 0.5f;
			return p;
		}

		/// <summary>
		/// Returns local position from latitude and longitude
		/// </summary>
		public static Vector3 GetLocalPositionFromLatLon(Vector2 latLon)
		{
			Vector3 p;
			p.x = (latLon.y + 180f) / 360f - 0.5f;
			p.y = (latLon.x + 90f) / 180f - 0.5f;
			p.z = 0;
			return p;
		}

		/// <summary>
		/// Returns lat/lon coordinates from local position
		/// </summary>
		public static Vector2 GetLatLonFromLocalPosition(Vector2 position)
		{
			var lon = position.x * 360f;
			var lat = position.y * 180f;
			return new Vector2(lat, lon);
		}

		/// <summary>
		/// Returns UV texture coordinates from latitude and longitude
		/// </summary>
		public static Vector2 GetUVFromLatLon(float lat, float lon)
		{
			Vector2 p;
			p.x = (lon + 180f) / 360f;
			p.y = (lat + 90f) / 180f;
			return p;
		}

		public static Vector2 GetLatLonFromBillboard(Vector2 position)
		{
			const float mapWidth = 200.0f;
			const float mapHeight = 100.0f;
			var lon = (position.x + mapWidth * 0.5f) * 360f / mapWidth - 180f;
			var lat = position.y * 180f / mapHeight;
			return new Vector2(lat, lon);
		}

		/// <summary>
		/// Gets the lat lon from UV coordinates (UV ranges from 0 to 1)
		/// </summary>
		/// <returns>The lat lon from U.</returns>
		/// <param name="uv">Uv.</param>
		public static Vector2 GetLatLonFromUV(Vector2 uv)
		{
			var lon = uv.x * 360f - 180f;
			var lat = (uv.y - 0.5f) * 2f * 90f;
			return new Vector2(lat, lon);
		}

		public static Vector2 GetBillboardPointFromLatLon(Vector2 latlon)
		{
			Vector2 p;
			var mapWidth = 200.0f;
			var mapHeight = 100.0f;
			p.x = (latlon.y + 180) * (mapWidth / 360f) - mapWidth * 0.5f;
			p.y = latlon.x * (mapHeight / 180f);
			return p;
		}

		public static Rect GetBillboardRectFromLatLonRect(Rect latlonRect)
		{
			var min = GetBillboardPointFromLatLon(latlonRect.min);
			var max = GetBillboardPointFromLatLon(latlonRect.max);
			return new Rect(min.x, min.y, Math.Abs(max.x - min.x), Mathf.Abs(max.y - min.y));
		}

		public static Rect GetUVRectFromLatLonRect(Rect latlonRect)
		{
			var min = GetUVFromLatLon(latlonRect.min.x, latlonRect.min.y);
			var max = GetUVFromLatLon(latlonRect.max.x, latlonRect.max.y);
			return new Rect(min.x, min.y, Math.Abs(max.x - min.x), Mathf.Abs(max.y - min.y));
		}

		public static Vector2 ConvertToTextureCoordinates(Vector3 localPos, int width, int height)
		{
			localPos.x = (int)((localPos.x + 0.5f) * width);
			localPos.y = (int)((localPos.y + 0.5f) * height);
			return localPos;
		}

		public static Vector2 GetBillboardPosFromSpherePoint(Vector3 p)
		{
			var u = 1.25f - (Mathf.Atan2(p.z, -p.x) / (2.0f * Mathf.PI) + 0.5f);
			if (u > 1)
				u -= 1.0f;
			var v = Mathf.Asin(p.y * 2.0f) / Mathf.PI;
			return new Vector2(u * 2.0f - 1.0f, v) * 100.0f;
		}

		/// <summary>
		/// Returns distance in meters between two lat/lon coordinates
		/// </summary>
		public static float Distance(float latDec1, float lonDec1, float latDec2, float lonDec2)
		{
			const float R = 6371000; // metres
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
			return c * R;
		}

		/// <summary>
		/// Get tile coordinate which contains a given latitude/longitude
		/// </summary>
		/// <param name="zoomLevel">Zoom level.</param>
		/// <param name="lat">Lat.</param>
		/// <param name="lon">Lon.</param>
		/// <param name="xtile">Xtile.</param>
		/// <param name="ytile">Ytile.</param>
		public static void GetTileFromLatLon(int zoomLevel, float lat, float lon, out int xtile,
			out int ytile)
		{
			lat = Mathf.Clamp(lat, -80f, 80f);
			xtile = (int)((lon + 180.0) / 360.0 * (1 << zoomLevel));
			ytile = (int)((1.0 -
			               Math.Log(
				               Math.Tan(lat * Math.PI / 180.0) + 1.0 / Math.Cos(lat * Math.PI / 180.0)) /
			               Math.PI) /
			              2.0 *
			              (1 << zoomLevel));
		}

		/// <summary>
		/// Gets latitude/longitude of top/left corner for a given map tile
		/// </summary>
		/// <returns>The lat lon from tile.</returns>
		/// <param name="x">The x coordinate.</param>
		/// <param name="y">The y coordinate.</param>
		/// <param name="zoomLevel">Zoom level.</param>
		public static Vector2 GetLatLonFromTile(float x, float y, int zoomLevel)
		{
			var n = Math.PI - 2.0 * Math.PI * y / Math.Pow(2.0, zoomLevel);
			var lat = 180.0 / Math.PI * Math.Atan(Math.Sinh(n));
			var lon = x / Math.Pow(2.0, zoomLevel) * 360.0 - 180.0;
			return new Vector2((float)lat, (float)lon);
		}

		/// <summary>
		/// Gets the map position for the center of a given tile defined by x,y and zoom level
		/// </summary>
		/// <returns>The lat lon from tile.</returns>
		/// <param name="x">The x coordinate.</param>
		/// <param name="y">The y coordinate.</param>
		/// <param name="zoomLevel">Zoom level.</param>
		public static Vector2 GetLocalPositionFromTile(float x, float y, int zoomLevel)
		{
			var latlon = GetLatLonFromTile(x, y, zoomLevel);
			return GetLocalPositionFromLatLon(latlon);
		}

		/// <summary>
		/// Convertes sphere to latitude/longitude coordinates
		/// </summary>
		public static void GetLatLonFromSpherePoint(Vector3 p, out float lat, out float lon)
		{
			var phi = Mathf.Asin(p.y * 2.0f);
			var theta = Mathf.Atan2(p.x, p.z);
			lat = phi * Mathf.Rad2Deg;
			lon = -theta * Mathf.Rad2Deg;
		}

		#endregion
	}
}