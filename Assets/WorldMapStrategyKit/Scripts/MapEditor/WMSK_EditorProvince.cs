using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace WorldMapStrategyKit
{
	public partial class WMSK_Editor : MonoBehaviour
	{
		public int provinceIndex = -1, provinceRegionIndex = -1;
		public int GUIProvinceIndex;
		public string GUIProvinceName = "";
		public string GUIProvinceNewName = "";
		public string GUIProvinceToNewCountryName = "";
		public int GUIProvinceTransferToCountryIndex = -1;

		public int GUIProvinceTransferToProvinceIndex = -1;

		// if there's any pending change to be saved
		public bool provinceChanges;

		// if there's any pending change to be saved
		public bool provinceAttribChanges;
		public List<Province> selectedProvinces = new();
		public bool showProvinceNames = true;

		private int lastProvinceCount = -1;
		private string[] _provinceNames;
		private string[] emptyStringArray = new string[0];

		public string[] provinceNames
		{
			get
			{
				if (countryIndex == -1 || map.countries[countryIndex].provinces == null)
					return emptyStringArray;
				if (lastProvinceCount != map.countries[countryIndex].provinces.Length)
				{
					provinceIndex = -1;
					ReloadProvinceNames();
				}
				return _provinceNames;
			}
		}

		#region Editor functionality

		public void ClearProvinceSelection()
		{
			map.HideProvinceRegionHighlights(true);
			map.HideProvinces();
			for (var k = 0; k < selectedProvinces.Count; k++)
			{
				var index = map.GetProvinceIndex(selectedProvinces[k]);
				map.ToggleProvinceSurface(index, false, map.provincesFillColor);
			}
			selectedProvinces.Clear();
			provinceIndex = -1;
			provinceRegionIndex = -1;
			GUIProvinceName = "";
			GUIProvinceNewName = "";
			GUIProvinceToNewCountryName = "";
			GUIProvinceIndex = -1;
		}

		public bool ProvinceSelectByScreenClick(Ray ray)
		{
			int targetProvinceIndex, targetRegionIndex;
			if (map.GetProvinceIndex(ray, out targetProvinceIndex, out targetRegionIndex))
			{
				if (countryIndex != map.provinces[targetProvinceIndex].countryIndex)
				{
					countryIndex = map.provinces[targetProvinceIndex].countryIndex;
					countryRegionIndex = -1; // 0map.countries[countryIndex].mainRegionIndex;
					CountryRegionSelect();
				}
				provinceIndex = targetProvinceIndex;
				if (provinceIndex >= 0 && countryIndex != map.provinces[provinceIndex].countryIndex)
				{
					// sanity check
					ClearSelection();
					countryIndex = map.provinces[provinceIndex].countryIndex;
					countryRegionIndex = -1;
					CountryRegionSelect();
				}
				var e = Event.current;
				if (e != null)
				{
					if (!e.control)
						selectedProvinces.Clear();
					selectedProvinces.Add(map.provinces[provinceIndex]);
				}
				provinceRegionIndex = targetRegionIndex;
				ProvinceRegionSelect();
				return true;
			}
			return false;
		}

		private bool GetProvinceIndexByGUISelection()
		{
			if (GUIProvinceIndex < 0 || GUIProvinceIndex >= provinceNames.Length)
				return false;
			var s = _provinceNames[GUIProvinceIndex].Split(new[]
			{
				'(',
				')'
			}, StringSplitOptions.RemoveEmptyEntries);
			if (s.Length >= 2)
			{
				GUIProvinceName = s[0].Trim();
				if (int.TryParse(s[1], out provinceIndex))
				{
					provinceRegionIndex = map.provinces[provinceIndex].mainRegionIndex;
					return true;
				}
			}
			return false;
		}

		public void ProvinceSelectByCombo(int selection)
		{
			GUIProvinceName = "";
			GUIProvinceIndex = selection;
			if (GetProvinceIndexByGUISelection())
				if (Application.isPlaying)
					map.BlinkProvince(provinceIndex, Color.black, Color.green, 1.2f, 0.2f);
			ProvinceRegionSelect();
		}

		public void ReloadProvinceNames()
		{
			if (map == null ||
			    map.provinces == null ||
			    countryIndex < 0 ||
			    countryIndex >= map.countries.Length)
				return;
			var oldProvinceTransferName = GetProvinceTransferIndexByGUISelection();
			var oldProvinceMergeTransferName = GetProvinceMergeIndexByGUISelection();
			_provinceNames = map.GetProvinceNames(countryIndex);
			lastProvinceCount = _provinceNames.Length;
			lastMountPointCount = -1;
			SyncGUIProvinceTransferSelection(oldProvinceTransferName);
			SyncGUIProvinceMergeSelection(oldProvinceMergeTransferName);
			ProvinceRegionSelect(); // refresh selection
		}

		public void ProvinceRegionSelect()
		{
			if (countryIndex < 0 ||
			    countryIndex >= map.countries.Length ||
			    provinceIndex < 0 ||
			    provinceIndex >= map.provinces.Length ||
			    editingMode != EDITING_MODE.PROVINCES)
				return;

			// Checks country selected is correct
			var province = map.provinces[provinceIndex];
			if (province.countryIndex != countryIndex)
			{
				ClearSelection();
				countryIndex = province.countryIndex;
				countryRegionIndex = map.countries[countryIndex].mainRegionIndex;
				CountryRegionSelect();
			}

			// Just in case makes GUICountryIndex selects appropiate value in the combobox
			GUIProvinceName = province.name;
			SyncGUIProvinceSelection();
			if (provinceIndex >= 0 && provinceIndex < map.provinces.Length)
			{
				GUIProvinceNewName = province.name;
				ProvinceHighlightSelection();
			}
		}

		private void ProvinceHighlightSelection()
		{
			if (highlightedRegions == null)
				highlightedRegions = new List<Region>();
			else
				highlightedRegions.Clear();
			map.HideProvinceRegionHighlights(true);

			if (provinceIndex < 0 ||
			    provinceIndex >= map.provinces.Length ||
			    countryIndex < 0 ||
			    countryIndex >= map.countries.Length ||
			    map.countries[countryIndex].provinces == null ||
			    provinceRegionIndex < 0 ||
			    map.provinces[provinceIndex].regions == null ||
			    provinceRegionIndex >= map.provinces[provinceIndex].regions.Count)
				return;

			if (selectedProvinces.Count > 0) // Multi-select
				for (var k = 0; k < selectedProvinces.Count; k++)
				{
					var pindex = map.GetProvinceIndex(selectedProvinces[k]);
					if (pindex == provinceIndex)
						continue;
					map.ToggleProvinceSurface(pindex, true, map.provincesFillColor);
				}

			// Highlight current province
			for (var p = 0; p < map.countries[countryIndex].provinces.Length; p++)
			{
				var province = map.countries[countryIndex].provinces[p];
				if (province.regions == null)
					continue;
				// if province is current province then highlight it
				if (province.name.Equals(map.provinces[provinceIndex].name))
				{
					map.HighlightProvinceRegion(provinceIndex, provinceRegionIndex, false);
					highlightedRegions.Add(map.provinces[provinceIndex].regions[provinceRegionIndex]);
				}
				else // if this province belongs to the country but it's not current province, add to the collection of highlighted (not colorize because country is already colorized and that includes provinces area)
					highlightedRegions.Add(province.regions[province.mainRegionIndex]);
			}

			shouldHideEditorMesh = true;
		}

		private void SyncGUIProvinceSelection()
		{
			// recover GUI country index selection
			if (GUIProvinceName.Length > 0 && provinceNames != null)
				for (var k = 0; k < _provinceNames.Length; k++)
					if (_provinceNames[k].TrimStart().StartsWith(GUIProvinceName))
					{
						GUIProvinceIndex = k;
						provinceIndex = map.GetProvinceIndex(countryIndex, GUIProvinceName);
						return;
					}
			GUIProvinceIndex = -1;
			GUIProvinceName = "";
		}

		private string GetProvinceTransferIndexByGUISelection()
		{
			if (GUIProvinceTransferToCountryIndex < 0 ||
			    GUIProvinceTransferToCountryIndex >= _countryNames.Length)
				return "";
			var s = _countryNames[GUIProvinceTransferToCountryIndex].Split(new[]
			{
				'(',
				')'
			}, StringSplitOptions.RemoveEmptyEntries);
			if (s.Length >= 2)
				return s[0].Trim();
			return "";
		}

		private void SyncGUIProvinceTransferSelection(string oldName)
		{
			// recover GUI province index selection
			if (oldName.Length > 0)
			{
				for (var k = 0;
					k < _countryNames.Length;
					k++) // don't use countryNames or the array will be reloaded again if grouped option is enabled causing an infinite loop
					if (_countryNames[k].TrimStart().StartsWith(oldName))
					{
						GUIProvinceTransferToCountryIndex = k;
						return;
					}
				SetInfoMsg("Country " + oldName + " not found in this geodata file.");
			}
			GUIProvinceTransferToCountryIndex = -1;
		}

		private string GetProvinceMergeIndexByGUISelection()
		{
			if (GUIProvinceTransferToProvinceIndex < 0 ||
			    GUIProvinceTransferToProvinceIndex >= _provinceNames.Length)
				return "";
			var s = _provinceNames[GUIProvinceTransferToProvinceIndex].Split(new[]
			{
				'(',
				')'
			}, StringSplitOptions.RemoveEmptyEntries);
			if (s.Length >= 2)
				return s[0].Trim();
			return "";
		}

		private void SyncGUIProvinceMergeSelection(string oldName)
		{
			// recover GUI province index selection
			if (oldName.Length > 0)
			{
				for (var k = 0;
					k < _provinceNames.Length;
					k++) // don't use provinceNames or the array will be reloaded again if grouped option is enabled causing an infinite loop
					if (_provinceNames[k].TrimStart().StartsWith(oldName))
					{
						GUIProvinceTransferToProvinceIndex = k;
						return;
					}
				SetInfoMsg("Province " + oldName + " not found in this geodata file.");
			}
			GUIProvinceTransferToProvinceIndex = -1;
		}

		public bool ProvinceRename()
		{
			if (countryIndex < 0 || provinceIndex < 0)
				return false;
			var prevName = map.provinces[provinceIndex].name;
			GUIProvinceNewName = GUIProvinceNewName.Trim();
			if (prevName.Equals(GUIProvinceNewName))
				return false;
			if (map.ProvinceRename(countryIndex, prevName, GUIProvinceNewName))
			{
				GUIProvinceName = GUIProvinceNewName;
				lastProvinceCount = -1;
				SyncGUIProvinceSelection();
				ProvinceHighlightSelection();
				provinceChanges = true;
				cityChanges = true;
				return true;
			}
			return false;
		}

		/// <summary>
		/// Deletes current region or province if this was the last region
		/// </summary>
		public void ProvinceDelete()
		{
			if (provinceIndex < 0 || provinceIndex >= map.provinces.Length)
				return;
			map.ProvinceDelete(provinceIndex);
			ClearProvinceSelection();
			map.OptimizeFrontiers();
			map.Redraw();
			provinceChanges = true;
		}

		/// <summary>
		/// Deletes current region or province if this was the last region
		/// </summary>
		public void ProvinceRegionDelete()
		{
			if (provinceIndex < 0 || provinceIndex >= map.provinces.Length)
				return;

			if (map.provinces[provinceIndex].regions != null &&
			    map.provinces[provinceIndex].regions.Count > 1)
			{
				map.provinces[provinceIndex].regions.RemoveAt(provinceRegionIndex);
				map.RefreshProvinceDefinition(provinceIndex, false);
			}
			ClearProvinceSelection();
			RedrawFrontiers();
			provinceChanges = true;
		}

		/// <summary>
		/// Delete all provinces of current country. Called from DeleteCountry.
		/// </summary>
		private void mDeleteCountryProvinces()
		{
			if (map.provinces == null)
				return;
			if (countryIndex < 0)
				return;

			map.HideProvinceRegionHighlights(true);
			map.countries[countryIndex].provinces = new Province[0];
			map.CountryDeleteProvinces(countryIndex);
			provinceChanges = true;
		}

		public void DeleteCountryProvinces()
		{
			mDeleteCountryProvinces();
			ClearSelection();
			RedrawFrontiers();
			map.DrawMapLabels();
		}

		/// <summary>
		/// Creates a new province with the current shape
		/// </summary>
		public void ProvinceCreate()
		{
			if (newShape.Count < 3 || countryIndex < 0)
				return;

			provinceIndex = map.provinces.Length;
			provinceRegionIndex = 0;
			var newProvinceName = GetProvinceUniqueName("New Province");
			var newProvince = new Province(newProvinceName, countryIndex,
				map.GetUniqueId(new List<IExtendableAttribute>(map.provinces)));
			var region = new Region(newProvince, 0);
			region.points = newShape.ToArray();
			newProvince.regions = new List<Region>();
			newProvince.regions.Add(region);
			map.ProvinceAdd(newProvince);
			map.RefreshProvinceDefinition(provinceIndex, false);
			lastProvinceCount = -1;
			GUIProvinceName = newProvince.name;
			SyncGUIProvinceSelection();
			ProvinceRegionSelect();
			provinceChanges = true;
		}

		/// <summary>
		/// Adds a new province to current province
		/// </summary>
		public void ProvinceRegionCreate()
		{
			if (newShape.Count < 3 || provinceIndex < 0)
				return;

			var province = map.provinces[provinceIndex];
			if (province.regions == null)
				province.regions = new List<Region>();
			provinceRegionIndex = province.regions.Count;
			var region = new Region(province, provinceRegionIndex);
			region.points = newShape.ToArray();
			if (province.regions == null)
				province.regions = new List<Region>();
			province.regions.Add(region);
			map.RefreshProvinceDefinition(provinceIndex, false);
			provinceChanges = true;
			ProvinceRegionSelect();
		}

		/// <summary>
		/// Creates a new province with the given region
		/// </summary>
		public void ProvinceCreate(Region region)
		{
			if (region == null)
				return;

			// Remove region from source entity
			var entity = region.entity;
			if (entity != null)
			{
				entity.regions.Remove(region);
				Country country;
				// Refresh entity definition
				if (region.entity is Country)
				{
					var countryIndex = _map.GetCountryIndex((Country)region.entity);
					country = _map.countries[countryIndex];
					_map.RefreshCountryGeometry(country);
				}
				else
				{
					var provinceIndex = map.GetProvinceIndex((Province)region.entity);
					country = _map.countries[_map.provinces[provinceIndex].countryIndex];
					_map.RefreshProvinceGeometry(provinceIndex);
				}
			}

			provinceIndex = map.provinces.Length;
			provinceRegionIndex = 0;
			var newProvinceName = GetProvinceUniqueName("New Province");
			var newProvince = new Province(newProvinceName, countryIndex,
				map.GetUniqueId(new List<IExtendableAttribute>(map.provinces)));
			var newRegion = new Region(newProvince, 0);
			newRegion.UpdatePointsAndRect(region.points);
			newProvince.regions = new List<Region>();
			newProvince.regions.Add(newRegion);
			map.ProvinceAdd(newProvince);
			map.RefreshProvinceDefinition(provinceIndex, false);

			// Update cities
			var cities = _map.GetCities(region);
			var citiesCount = cities.Count;
			if (citiesCount > 0)
				for (var k = 0; k < citiesCount; k++)
					if (cities[k].province != newProvinceName)
					{
						cities[k].province = newProvinceName;
						cityChanges = true;
					}

			lastProvinceCount = -1;
			GUIProvinceName = newProvince.name;
			SyncGUIProvinceSelection();
			ProvinceRegionSelect();
			provinceChanges = true;
		}

		/// <summary>
		/// Checks province's polygon points quality and fix artifacts.
		/// </summary>
		/// <returns><c>true</c>, if province was changed, <c>false</c> otherwise.</returns>
		public bool ProvinceSanitize()
		{
			var changes = _map.ProvinceSanitize(provinceIndex, 5);
			if (changes)
				provinceChanges = true;
			return changes;
		}

		/// <summary>
		/// Changes province's owner to specified country
		/// </summary>
		public void ProvinceTransferTo()
		{
			if (provinceIndex < 0 ||
			    GUIProvinceTransferToCountryIndex < 0 ||
			    GUIProvinceTransferToCountryIndex >= countryNames.Length)
				return;

			// Get target country
			// recover GUI country index selection
			var targetCountryIndex = -1;
			var s = countryNames[GUIProvinceTransferToCountryIndex].Split(new[]
			{
				'(',
				')'
			}, StringSplitOptions.RemoveEmptyEntries);
			if (s.Length >= 2)
				if (!int.TryParse(s[1], out targetCountryIndex))
					return;

			map.HideCountryRegionHighlights(true);
			map.HideProvinceRegionHighlights(true);
			_map.CountryTransferProvinceRegion(targetCountryIndex,
				map.provinces[provinceIndex].regions[provinceRegionIndex], true);

			countryChanges = true;
			provinceChanges = true;
			countryIndex = targetCountryIndex;
			countryRegionIndex = map.countries[targetCountryIndex].mainRegionIndex;
			ProvinceRegionSelect();
		}

		/// <summary>
		/// Merges province with target province
		/// </summary>
		public void ProvinceMerge()
		{
			if (provinceIndex < 0 ||
			    GUIProvinceTransferToProvinceIndex < 0 ||
			    GUIProvinceTransferToProvinceIndex >= provinceNames.Length)
				return;

			// Get target province
			// recover GUI country index selection
			var targetProvinceIndex = -1;
			var s = _provinceNames[GUIProvinceTransferToProvinceIndex].Split(new[]
			{
				'(',
				')'
			}, StringSplitOptions.RemoveEmptyEntries);
			if (s.Length >= 2)
				if (!int.TryParse(s[1], out targetProvinceIndex))
					return;

			Province targetProvince;
			if (targetProvinceIndex >= 0)
				targetProvince = map.provinces[targetProvinceIndex];
			else
				return;
			map.HideCountryRegionHighlights(true);
			map.HideProvinceRegionHighlights(true);
			if (_map.ProvinceTransferProvinceRegion(targetProvinceIndex,
				map.provinces[provinceIndex].regions[provinceRegionIndex], true))
			{
				GC.Collect();
				countryChanges = true;
				provinceChanges = true;
				cityChanges = true;
				mountPointChanges = true;
				provinceIndex = map.GetProvinceIndex(targetProvince);
				provinceRegionIndex = map.provinces[provinceIndex].mainRegionIndex;
				CountryRegionSelect();
				ProvinceRegionSelect();
			}
		}

		/// <summary>
		/// Converts current province in a new country
		/// </summary>
		public void ProvinceToNewCountry()
		{
			if (map.GetCountryIndex(GUIProvinceToNewCountryName) >= 0)
			{
				Debug.LogError("Country name is already in use.");
				return;
			}
			var sourceProvince = map.provinces[provinceIndex];
			var newCountryIndex = map.ProvinceToCountry(sourceProvince, GUIProvinceToNewCountryName);
			GC.Collect();
			countryIndex = newCountryIndex;
			countryRegionIndex = 0;
			countryChanges = true;
			provinceChanges = true;
			cityChanges = true;
			mountPointChanges = true;
			CountryRegionSelect();
			ProvinceRegionSelect();
		}

		/// <summary>
		/// Merges province with target province
		/// </summary>
		public void ProvincesMerge()
		{
			if (selectedProvinces.Count < 2)
				return;
			var targetProvinceIndex = map.GetProvinceIndex(selectedProvinces[0]);
			var targetProvince = map.provinces[targetProvinceIndex];
			for (var k = 1; k < selectedProvinces.Count; k++)
			{
				_map.ProvinceTransferProvinceRegion(targetProvinceIndex, selectedProvinces[k].mainRegion,
					false);
				targetProvinceIndex = map.GetProvinceIndex(targetProvince);
				GC.Collect();
			}

			ClearProvinceSelection();
			countryChanges = true;
			provinceChanges = true;
			cityChanges = true;
			mountPointChanges = true;
			provinceIndex = map.GetProvinceIndex(targetProvince);
			provinceRegionIndex = map.provinces[provinceIndex].mainRegionIndex;
			map.Redraw(true);
			ProvinceRegionSelect();
		}

		/// <summary>
		/// Converts current province in a new country
		/// </summary>
		public void ProvincesToNewCountry()
		{
			if (map.GetCountryIndex(GUIProvinceToNewCountryName) >= 0)
			{
				Debug.LogError("Country name is already in use.");
				return;
			}
			var newCountryIndex = map.ProvincesToCountry(selectedProvinces, GUIProvinceToNewCountryName);
			GC.Collect();
			countryIndex = newCountryIndex;
			countryRegionIndex = 0;
			countryChanges = true;
			provinceChanges = true;
			cityChanges = true;
			mountPointChanges = true;
			CountryRegionSelect();
			ProvinceRegionSelect();
		}

		#endregion

		#region IO stuff

		/// <summary>
		/// Returns the file name corresponding to the current province data file
		/// </summary>
		public string GetProvinceGeoDataFileName() => "provinces10.txt";

		/// <summary>
		/// Merges all provinces in each country so their number fits a given range
		/// </summary>
		/// <param name="min">Minimum number of provinces.</param>
		/// <param name="max">Maximum number of provinces.</param>
		public void ProvincesEqualize(int min, int max, int countryIndex)
		{
			if (min < 1 || countryIndex < 0 || countryIndex >= map.countries.Length)
				return;
			if (max < min)
				max = min;

			map.showProvinces = true;
			map.drawAllProvinces = true;

			var country = map.countries[countryIndex];
			if (country == null || country.provinces == null)
				return;
			var targetProvCount = Random.Range(min, max);
			var provCount = country.provinces.Length;
			float provStartSize = 0;
			while (provCount > targetProvCount)
			{
				// Take the smaller province and merges with a neighbour
				var minAreaSize = float.MaxValue;
				var provinceIndex = -1;
				for (var p = 0; p < provCount; p++)
				{
					var prov = country.provinces[p];
					if (prov == null)
						continue;
					if (prov.regions == null)
						map.ReadProvincePackedString(prov);
					if (prov.regions == null ||
					    prov.regions.Count == 0 ||
					    prov.mainRegion.neighbours == null ||
					    prov.mainRegion.neighbours.Count == 0)
						continue;
					if (prov.regionsRect2DArea < minAreaSize && prov.regionsRect2DArea > provStartSize)
					{
						minAreaSize = prov.regionsRect2DArea;
						provinceIndex = map.GetProvinceIndex(prov);
					}
				}

				if (provinceIndex < 0)
					break;

				provStartSize = minAreaSize;

				// Get the smaller neighbour
				var neighbourIndex = -1;
				var province = map.provinces[provinceIndex];
				var neighbourCount = province.mainRegion.neighbours.Count;
				minAreaSize = float.MaxValue;
				for (var n = 0; n < neighbourCount; n++)
				{
					var neighbour = province.mainRegion.neighbours[n];
					var neighbourProvince = (Province)neighbour.entity;
					if (neighbourProvince != null &&
					    neighbourProvince != province &&
					    neighbourProvince.countryIndex == countryIndex &&
					    neighbour.rect2DArea < minAreaSize)
					{
						var neighbourProvIndex = map.GetProvinceIndex(neighbourProvince);
						if (neighbourProvIndex >= 0)
						{
							minAreaSize = neighbour.rect2DArea;
							neighbourIndex = neighbourProvIndex;
						}
					}
				}
				if (neighbourIndex < 0)
					continue;

				// Merges province into neighbour
				var provinceSource = map.provinces[provinceIndex].name;
				var provinceTarget = map.provinces[neighbourIndex].name;
				var prevProvCount = country.provinces.Length;
				if (!map.ProvinceTransferProvinceRegion(neighbourIndex,
					map.provinces[provinceIndex].mainRegion, false))
				{
					Debug.LogWarning("Country: " +
					                 map.countries[countryIndex].name +
					                 " => " +
					                 provinceSource +
					                 " failed merge into " +
					                 provinceTarget +
					                 ".");
					break;
				}
				provCount = country.provinces.Length;
				if (provCount == prevProvCount)
					break; // can't merge more provinces
			}

			provinceChanges = true;
			cityChanges = true;
			mountPointChanges = true;
		}

		#endregion
	}
}