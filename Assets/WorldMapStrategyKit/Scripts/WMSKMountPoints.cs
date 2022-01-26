// World Strategy Kit for Unity - Main Script
// (C) 2016-2020 by Ramiro Oliva (Kronnect)
// Don't modify this script - changes could be lost if you upgrade to a more recent version of WMSK
// ***************************************************************************
// This is the public API file - every property or public method belongs here
// ***************************************************************************

using System;
using System.Collections.Generic;
using UnityEngine;

namespace WorldMapStrategyKit
{
	/* Public WPM Class */
	public partial class WMSK : MonoBehaviour
	{
		/// <summary>
		/// Complete list of mount points.
		/// </summary>
		[NonSerialized] public List<MountPoint> mountPoints;

		#region Public API area

		/// <summary>
		/// Clears any mount point highlighted (color changed) and resets them to default city color (used from Editor)
		/// </summary>
		public void HideMountPointHighlights()
		{
			if (mountPointsLayer == null)
				return;
			var rr = mountPointsLayer.GetComponentsInChildren<Renderer>(true);
			for (var k = 0; k < rr.Length; k++)
				rr[k].sharedMaterial = mountPointsMat;
		}

		/// <summary>
		/// Toggles the mount point highlight.
		/// </summary>
		/// <param name="mountPointIndex">Moint point index in the mount points collection.</param>
		/// <param name="color">Color.</param>
		/// <param name="highlighted">If set to <c>true</c> the color of the mount point will be changed. If set to <c>false</c> the color of the mount point will be reseted to default color</param>
		public void ToggleMountPointHighlight(int mountPointIndex, Color color, bool highlighted)
		{
			if (mountPointsLayer == null)
				return;
			var t = mountPointsLayer.transform.Find(mountPointIndex.ToString());
			if (t == null)
				return;
			var rr = t.gameObject.GetComponent<Renderer>();
			if (rr == null)
				return;
			Material mat;
			if (highlighted)
			{
				mat = Instantiate(rr.sharedMaterial);
				mat.name = rr.sharedMaterial.name;
				if (disposalManager != null)
					disposalManager.MarkForDisposal(mat); //mat.hideFlags = HideFlags.DontSave;
				mat.color = color;
				rr.sharedMaterial = mat;
			}
			else
				rr.sharedMaterial = mountPointsMat;
		}

		/// <summary>
		/// Returns an array with the mount points names.
		/// </summary>
		public string[] GetMountPointNames() => GetMountPointNames(-1, -1);

		/// <summary>
		/// Returns an array with the mount points names.
		/// </summary>
		public string[] GetMountPointNames(int countryIndex) => GetMountPointNames(countryIndex, -1);

		/// <summary>
		/// Returns an array with the mount points names.
		/// </summary>
		public string[] GetMountPointNames(int countryIndex, int provinceIndex)
		{
			var c = new List<string>(20);
			for (var k = 0; k < mountPoints.Count; k++)
				if ((mountPoints[k].countryIndex == countryIndex || countryIndex == -1) &&
				    (mountPoints[k].provinceIndex == provinceIndex || provinceIndex == -1))
					c.Add(mountPoints[k].name + " (" + k + ")");
			c.Sort();
			return c.ToArray();
		}

		/// <summary>
		/// Gets the mount point index with that unique Id.
		/// </summary>
		public int GetMountPointIndex(int uniqueId)
		{
			if (mountPoints == null)
				return -1;
			for (var k = 0; k < mountPoints.Count; k++)
				if (mountPoints[k].uniqueId == uniqueId)
					return k;
			return -1;
		}

		/// <summary>
		/// Returns the index of a mount point in the global mount points collection. Note that country (and optionally province) index can be supplied due to repeated mount point names.
		/// Pass -1 to countryIndex or provinceIndex to ignore filters.
		/// </summary>
		public int GetMountPointIndex(int countryIndex, int provinceIndex, string mountPointName)
		{
			if (mountPoints == null)
				return -1;
			for (var k = 0; k < mountPoints.Count; k++)
				if ((mountPoints[k].countryIndex == countryIndex || countryIndex == -1) &&
				    (mountPoints[k].provinceIndex == provinceIndex || provinceIndex == -1) &&
				    mountPoints[k].name.Equals(mountPointName))
					return k;
			return -1;
		}

		/// <summary>
		/// Returns the mount point index by screen position.
		/// </summary>
		public bool GetMountPointIndex(Ray ray, out int mountPointIndex)
		{
			var hitCount = Physics.RaycastNonAlloc(ray, tempHits, 5000, layerMask);
			if (hitCount > 0)
				for (var k = 0; k < hitCount; k++)
					if (tempHits[k].collider.gameObject == gameObject)
					{
						var localHit = transform.InverseTransformPoint(tempHits[k].point);
						var c = GetMountPointNearPoint(localHit);
						if (c >= 0)
						{
							mountPointIndex = c;
							return true;
						}
					}
			mountPointIndex = -1;
			return false;
		}

		/// <summary>
		/// Deletes all mount points of current selected country's continent
		/// </summary>
		public void MountPointsDeleteFromSameContinent(string continentName)
		{
			HideMountPointHighlights();
			var k = -1;
			while (++k < mountPoints.Count)
			{
				var cindex = mountPoints[k].countryIndex;
				if (cindex >= 0)
				{
					var mpContinent = _countries[cindex].continent;
					if (mpContinent.Equals(continentName))
					{
						mountPoints.RemoveAt(k);
						k--;
					}
				}
			}
		}

		public void MountPointAdd(Vector2 location, string name, int countryIndex, int provinceIndex,
			int type)
		{
			if (mountPoints == null)
				mountPoints = new List<MountPoint>();
			var uniqueId = GetUniqueId(new List<IExtendableAttribute>(mountPoints.ToArray()));
			var newMountPoint =
				new MountPoint(name, countryIndex, provinceIndex, location, uniqueId, type);
			mountPoints.Add(newMountPoint);
		}

		/// <summary>
		/// Returns a list of mount points whose attributes matches predicate
		/// </summary>
		public List<MountPoint> GetMountPoints(AttribPredicate predicate)
		{
			var selectedMountPoints = new List<MountPoint>();
			var mountPointCount = mountPoints.Count;
			for (var k = 0; k < mountPointCount; k++)
			{
				var mountPoint = mountPoints[k];
				if (predicate(mountPoint.attrib))
					selectedMountPoints.Add(mountPoint);
			}
			return selectedMountPoints;
		}

		/// <summary>
		/// Returns a list of mount points contained in a given region
		/// </summary>
		public List<MountPoint> GetMountPoints(Region region)
		{
			var mpCount = mountPoints.Count;
			var cc = new List<MountPoint>();
			for (var k = 0; k < mpCount; k++)
				if (region.Contains(mountPoints[k].unity2DLocation))
					cc.Add(mountPoints[k]);
			return cc;
		}

		#endregion

		#region IO functions area

		/// <summary>
		/// Exports the geographic data in packed string format.
		/// </summary>
		public string GetMountPointsGeoData()
		{
			var json = new JSONObject();
			for (var k = 0; k < mountPoints.Count; k++)
			{
				var mp = mountPoints[k];
				var mpJSON = new JSONObject();
				mpJSON.AddField("Name", DataEscape(mp.name));
				var provinceUniqueID = -1;
				if (mp.provinceIndex >= 0 && mp.provinceIndex < provinces.Length)
					provinceUniqueID = provinces[mp.provinceIndex].uniqueId;
				mpJSON.AddField("Province", provinceUniqueID);
				var countryUniqueID = -1;
				if (mp.countryIndex >= 0 && mp.countryIndex < countries.Length)
					countryUniqueID = countries[mp.countryIndex].uniqueId;
				mpJSON.AddField("Country", countryUniqueID);
				mpJSON.AddField("Type", mp.type);
				mpJSON.AddField("X", mp.unity2DLocation.x);
				mpJSON.AddField("Y", mp.unity2DLocation.y);
				mpJSON.AddField("Id", mp.uniqueId);
				mpJSON.AddField("Attrib", mp.attrib);
				json.Add(mpJSON);
			}
			return json.Print(true);
		}

		/// <summary>
		/// Reads the mount points data from a packed string.
		/// </summary>
		public void SetMountPointsGeoData(string s)
		{
			if (s.IndexOf('{') >= 0)
			{
				ReadMountPointsXML(s);
				return;
			}
			var mountPointsList = s.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
			var mountPointsCount = mountPointsList.Length;
			mountPoints = new List<MountPoint>(mountPointsCount);

			for (var k = 0; k < mountPointsCount; k++)
			{
				var mountPointInfo = mountPointsList[k].Split(new[] { '$' });
				if (mountPointInfo.Length < 6)
					continue;
				var name = mountPointInfo[0];
				var country = mountPointInfo[2];
				var countryIndex = GetCountryIndex(country);
				if (countryIndex >= 0)
				{
					var province = mountPointInfo[1];
					var provinceIndex = GetProvinceIndex(countryIndex, province);
					var type = int.Parse(mountPointInfo[3], Misc.InvariantCulture);
					var x = float.Parse(mountPointInfo[4], Misc.InvariantCulture);
					var y = float.Parse(mountPointInfo[5], Misc.InvariantCulture);
					var tags = new Dictionary<string, string>();
					for (var t = 6; t < mountPointInfo.Length; t++)
					{
						var tag = mountPointInfo[t];
						var tagInfo = tag.Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries);
						if (tagInfo != null && tagInfo.Length > 1)
						{
							var key = tagInfo[0];
							var value = tagInfo[1];
							if (!tags.ContainsKey(key))
								tags.Add(key, value);
						}
					}
					int uniqueId;
					if (mountPointInfo.Length >= 7)
						uniqueId = int.Parse(mountPointInfo[6], Misc.InvariantCulture);
					else
						uniqueId = GetUniqueId(new List<IExtendableAttribute>(_countries));
					var mountPoint = new MountPoint(name, countryIndex, provinceIndex, new Vector2(x, y),
						type, uniqueId);
					mountPoints.Add(mountPoint);
				}
			}
		}

		/// <summary>
		/// Gets XML attributes of all mount points in jSON format.
		/// </summary>
		public string GetMountPointsAttributes(bool prettyPrint = true) =>
			GetMountPointsAttributes(new List<MountPoint>(mountPoints), prettyPrint);

		/// <summary>
		/// Gets XML attributes of provided mount points in jSON format.
		/// </summary>
		public string GetMountPointsAttributes(List<MountPoint> mountPoints, bool prettyPrint = true)
		{
			var composed = new JSONObject();
			var mountPointCount = mountPoints.Count;
			for (var k = 0; k < mountPointCount; k++)
			{
				var mountPoint = mountPoints[k];
				composed.AddField(mountPoint.uniqueId.ToString(), mountPoint.attrib);
			}
			return composed.Print(prettyPrint);
		}

		/// <summary>
		/// Sets mount points attributes from a jSON formatted string.
		/// </summary>
		public void SetMountPointsAttributes(string jSON)
		{
			var composed = new JSONObject(jSON);
			if (composed.keys == null)
				return;
			var keyCount = composed.keys.Count;
			for (var k = 0; k < keyCount; k++)
			{
				var uniqueId = int.Parse(composed.keys[k]);
				var mountPointIndex = GetMountPointIndex(uniqueId);
				if (mountPointIndex >= 0)
					mountPoints[mountPointIndex].attrib = composed[k];
			}
		}

		#endregion
	}
}