// World Map Strategy Kit for Unity - Main Script
// (C) 2016-2020 by Ramiro Oliva (Kronnect)
// Don't modify this script - changes could be lost if you upgrade to a more recent version of WMSK

using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace WorldMapStrategyKit
{
	public partial class WMSK : MonoBehaviour
	{
		private const float MOUNT_POINT_HIT_PRECISION = 0.0015f;

		#region Internal variables

		// resources
		private Material mountPointsMat;
		private GameObject mountPointSpot, mountPointsLayer;

		#endregion

		#region System initialization

		private void ReadMountPointsPackedString()
		{
			var mountPointsCatalogFileName = _geodataResourcesPath + "/mountPoints";
			var ta = Resources.Load<TextAsset>(mountPointsCatalogFileName);
			if (ta != null)
			{
				var s = ta.text;
				SetMountPointsGeoData(s);
			}
			else
				mountPoints = new List<MountPoint>();
		}

		private void ReadMountPointsXML(string s)
		{
			var json = new JSONObject(s);
			var mountPointsCount = json.list.Count;
			mountPoints = new List<MountPoint>(mountPointsCount);
			for (var k = 0; k < mountPointsCount; k++)
			{
				var mpJSON = json[k];
				string name = mpJSON["Name"];
				int countryUniqueId = mpJSON["Country"];
				var countryIndex = GetCountryIndex(countryUniqueId);
				int provinceUniqueId = mpJSON["Province"];
				var provinceIndex = GetProvinceIndex(provinceUniqueId);
				float x = mpJSON["X"];
				float y = mpJSON["Y"];
				if (x == 0 && y == 0)
				{
					// workaround for string data: fixes old issue, no longer needed but kept for compatibility
					float.TryParse(mpJSON["X"], System.Globalization.NumberStyles.Float,
						Misc.InvariantCulture, out x);
					float.TryParse(mpJSON["Y"], System.Globalization.NumberStyles.Float,
						Misc.InvariantCulture, out y);
				}
				// Try to locate country and provinces in case data does not match
				var location = new Vector2(x, y);
				if (countryIndex < 0 && countryUniqueId > 0)
					countryIndex = GetCountryIndex(location);
				if (provinceIndex < 0 && provinceUniqueId > 0)
					provinceIndex = GetProvinceIndex(location);
				int uniqueId = mpJSON["Id"];
				int type = mpJSON["Type"];
				var mp = new MountPoint(name, countryIndex, provinceIndex, location, uniqueId, type);
				mp.attrib = mpJSON["Attrib"];
				mountPoints.Add(mp);
			}
		}

		#endregion

		#region Drawing stuff

		/// <summary>
		/// Redraws the mounts points but only in editor time. This is automatically called by Redraw(). Used internally by the Map Editor. You should not need to call this method directly.
		/// </summary>
		public void DrawMountPoints()
		{
			// Create mount points layer
			var t = transform.Find("Mount Points");
			if (t != null)
				DestroyImmediate(t.gameObject);
			if (Application.isPlaying || mountPoints == null)
				return;

			mountPointsLayer = new GameObject("Mount Points");
			mountPointsLayer.transform.SetParent(transform, false);

			// Draw mount points marks
			for (var k = 0; k < mountPoints.Count; k++)
			{
				var mp = mountPoints[k];
				var mpObj = Instantiate(mountPointSpot);
				mpObj.name = k.ToString();
				mpObj.transform.position = transform.TransformPoint(mp.unity2DLocation);
				if (disposalManager != null)
					disposalManager.MarkForDisposal(mpObj);
				mpObj.hideFlags |= HideFlags.HideInHierarchy;
				mpObj.transform.SetParent(mountPointsLayer.transform, true);
			}

			var mpScaler = mountPointsLayer.GetComponent<MountPointScaler>() ??
			               mountPointsLayer.AddComponent<MountPointScaler>();
			mpScaler.map = this;
			mpScaler.ScaleMountPoints();
		}

		#endregion

		#region Internal Cities API

		/// <summary>
		/// Returns any mousnep point near the point specified in local coordinates.
		/// </summary>
		public int GetMountPointNearPoint(Vector2 localPoint) =>
			GetMountPointNearPoint(localPoint, MOUNT_POINT_HIT_PRECISION);

		/// <summary>
		/// Returns any mount point near the point specified in local coordinates.
		/// </summary>
		/// <param name="separation">Distance threshold (minimum should be MOUNT_POINT_HIT_PRECISION constant).</param>
		public int GetMountPointNearPoint(Vector2 localPoint, float separation)
		{
			if (mountPoints == null)
				return -1;
			if (separation < MOUNT_POINT_HIT_PRECISION)
				separation = MOUNT_POINT_HIT_PRECISION;
			var separationSqr = separation * separation;
			var count = mountPoints.Count;
			for (var c = 0; c < count; c++)
			{
				var mpLoc = mountPoints[c].unity2DLocation;
				var distSqr =
					FastVector.SqrDistance(ref mpLoc,
						ref localPoint); // (mpLoc - localPoint).sqrMagnitude;
				if (distSqr < separationSqr)
					return c;
			}
			return -1;
		}

		private bool GetMountPointUnderMouse(int countryIndex, Vector2 localPoint, out int mountPointIndex)
		{
			var hitPrecission = MOUNT_POINT_HIT_PRECISION * _cityIconSize * 5.0f;
			for (var c = 0; c < mountPoints.Count; c++)
			{
				var mp = mountPoints[c];
				if (mp.countryIndex == countryIndex)
					if ((mp.unity2DLocation - localPoint).magnitude < hitPrecission)
					{
						mountPointIndex = c;
						return true;
					}
			}
			mountPointIndex = -1;
			return false;
		}

		/// <summary>
		/// Returns mount points belonging to a provided country.
		/// </summary>
		private List<MountPoint> GetMountPoints(int countryIndex)
		{
			var results = new List<MountPoint>(20);
			for (var c = 0; c < mountPoints.Count; c++)
				if (mountPoints[c].countryIndex == countryIndex)
					results.Add(mountPoints[c]);
			return results;
		}

		/// <summary>
		/// Returns mount points belonging to a provided country and province.
		/// </summary>
		private List<MountPoint> GetMountPoints(int countryIndex, int provinceIndex)
		{
			var results = new List<MountPoint>(20);
			for (var c = 0; c < mountPoints.Count; c++)
				if (mountPoints[c].countryIndex == countryIndex &&
				    mountPoints[c].provinceIndex == provinceIndex)
					results.Add(mountPoints[c]);
			return results;
		}

		/// <summary>
		/// Updates the mount points scale.
		/// </summary>
		public void ScaleMountPoints()
		{
			if (mountPointsLayer != null)
			{
				var scaler = mountPointsLayer.GetComponent<MountPointScaler>();
				if (scaler != null)
					scaler.ScaleMountPoints();
			}
		}

		#endregion
	}
}