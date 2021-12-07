using UnityEngine;
using System.Text;
using System.Collections;
using System.Collections.Generic;

namespace WorldMapStrategyKit
{
	public partial class WMSK_Editor : MonoBehaviour
	{
		public int GUICountryIndex;
		public string GUICountryName = "";
		public string GUICountryNewName = "";
		public string GUICountryNewContinent = "";
		public string GUICountryNewFIPS10_4 = "";
		public string GUICountryNewISO_A2 = "";
		public string GUICountryNewISO_A3 = "";
		public string GUICountryNewISO_N3 = "";
		public int GUICountryTransferToCountryIndex = -1;
		public bool groupByParentAdmin = true;
		public int countryIndex = -1, countryRegionIndex = -1;

		public bool countryChanges;

		// if there's any pending change to be saved
		public bool countryAttribChanges;

		[SerializeField] private bool _GUICountryHidden;

		public bool GUICountryHidden
		{
			get => _GUICountryHidden;
			set
			{
				if (_GUICountryHidden != value)
				{
					_GUICountryHidden = value;
					countryChanges = true;
					if (countryIndex >= 0 && _map.countries[countryIndex].hidden != _GUICountryHidden)
					{
						_map.countries[countryIndex].hidden = _GUICountryHidden;
						ClearSelection();
						_map.OptimizeFrontiers();
						_map.Redraw();
					}
				}
			}
		}

		// private fields
		private int lastCountryCount = -1;
		private string[] _countryNames;
		private Dictionary<string, List<Country>> groupedCountries;

		public string[] countryNames
		{
			get
			{
				if (map.countries != null && lastCountryCount != map.countries.Length)
				{
					countryIndex = -1;
					ReloadCountryNames();
				}
				return _countryNames;
			}
		}

		#region Editor functionality

		public bool CountryRename()
		{
			if (countryIndex < 0)
				return false;
			var prevName = map.countries[countryIndex].name;
			GUICountryNewName = GUICountryNewName.Trim();
			if (prevName.Equals(GUICountryNewName))
				return false;
			if (map.CountryRename(prevName, GUICountryNewName))
			{
				GUICountryName = GUICountryNewName;
				lastCountryCount = -1;
				ReloadCountryNames();
				map.DrawMapLabels();
				countryChanges = true;
				provinceChanges = true;
				cityChanges = true;
				mountPointChanges = true;
				return true;
			}
			return false;
		}

		/// <summary>
		/// Updates all countries within same continent to new country name
		/// </summary>
		public bool ContinentRename()
		{
			if (countryIndex < 0)
				return false;

			var currentContinent = map.countries[countryIndex].continent;
			for (var k = 0; k < map.countries.Length; k++)
				if (map.countries[k].continent.Equals(currentContinent))
					map.countries[k].continent = GUICountryNewContinent;
			countryChanges = true;
			return true;
		}

		/// <summary>
		/// Checks country's polygon points quality and fix artifacts.
		/// </summary>
		/// <returns><c>true</c>, if country was changed, <c>false</c> otherwise.</returns>
		public bool CountrySanitize()
		{
			var changes = _map.CountrySanitize(countryIndex, 5);
			if (changes)
				countryChanges = true;
			return changes;
		}

		public void CountrySelectByCombo(int selection)
		{
			GUICountryName = "";
			GUICountryIndex = selection;
			if (GetCountryIndexByGUISelection())
				if (Application.isPlaying)
					map.BlinkCountry(countryIndex, Color.black, Color.green, 1.2f, 0.2f);
			CountryRegionSelect();
		}

		private bool GetCountryIndexByGUISelection()
		{
			if (GUICountryIndex < 0 || GUICountryIndex >= countryNames.Length)
				return false;
			var s = countryNames[GUICountryIndex].Split(new char[]
			{
				'(',
				')'
			}, System.StringSplitOptions.RemoveEmptyEntries);
			if (s.Length >= 2)
			{
				GUICountryName = s[0].Trim();
				if (int.TryParse(s[1], out countryIndex))
				{
					countryRegionIndex = map.countries[countryIndex].mainRegionIndex;
					return true;
				}
			}
			return false;
		}

		public void CountryRegionSelect()
		{
			if (countryIndex < 0 || countryIndex > map.countries.Length)
				return;

			// Just in case makes GUICountryIndex selects appropiate value in the combobox
			GUICountryName = map.countries[countryIndex].name;
			SyncGUICountrySelection();
			GUICountryNewName = map.countries[countryIndex].name;
			GUICountryNewContinent = map.countries[countryIndex].continent;
			GUICountryHidden = map.countries[countryIndex].hidden;
			GUICountryNewFIPS10_4 = map.countries[countryIndex].fips10_4;
			GUICountryNewISO_A2 = map.countries[countryIndex].iso_a2;
			GUICountryNewISO_A3 = map.countries[countryIndex].iso_a3;
			GUICountryNewISO_N3 = map.countries[countryIndex].iso_n3;
			if (editingMode == EDITING_MODE.COUNTRIES)
				CountryHighlightSelection();
			else if (editingMode == EDITING_MODE.PROVINCES)
			{
				map.HighlightCountryRegion(countryIndex, countryRegionIndex, false, true);
				ProvinceHighlightSelection(); // add province names
				map.DrawProvinces(countryIndex, false, false, false);
			}
			shouldHideEditorMesh = true;
			lastProvinceCount = -1;
			lastMountPointCount = -1;
			ClearCitySelection();
			lastCityCount = -1;
			ReloadCityNames();
		}

		public bool CountrySelectByScreenClick(Ray ray)
		{
			int targetCountryIndex, targetRegionIndex;
			if (map.GetCountryIndex(ray, out targetCountryIndex, out targetRegionIndex))
			{
				countryIndex = targetCountryIndex;
				countryRegionIndex = targetRegionIndex;
				CountryRegionSelect();
				return true;
			}
			return false;
		}

		private void CountryHighlightSelection()
		{
			CountryHighlightSelection(null);
		}

		private void CountryHighlightSelection(List<Region> filterRegions)
		{
			if (highlightedRegions == null)
				highlightedRegions = new List<Region>();
			else
				highlightedRegions.Clear();
			if (countryIndex < 0 || countryIndex >= map.countries.Length)
				return;
			if (countryRegionIndex >= map.countries[countryIndex].regions.Count)
			{
				countryRegionIndex = map.countries[countryIndex].mainRegionIndex;
				if (countryRegionIndex >= map.countries[countryIndex].regions.Count)
					return;
			}

			// Colorize neighours
			var color = new Color(1, 1, 1, 0.4f);
			map.HideCountryRegionHighlights(true);
			var country = map.countries[countryIndex];
			if (country.regions != null &&
			    countryRegionIndex >= 0 &&
			    countryRegionIndex < country.regions.Count)
			{
				var region = country.regions[countryRegionIndex];
				for (var cr = 0; cr < region.neighbours.Count; cr++)
				{
					var neighbourRegion = region.neighbours[cr];
					if (filterRegions == null || filterRegions.Contains(neighbourRegion))
					{
						var c = map.GetCountryIndex((Country)neighbourRegion.entity);
						map.ToggleCountryRegionSurfaceHighlight(c, neighbourRegion.regionIndex, color,
							true);
						highlightedRegions.Add(
							neighbourRegion.entity.regions[neighbourRegion.regionIndex]);
					}
				}
				map.HighlightCountryRegion(countryIndex, countryRegionIndex, false, true);
				highlightedRegions.Add(region);
			}

			shouldHideEditorMesh = true;
		}

		public void ReloadCountryNames()
		{
			if (map == null || map.countries == null)
			{
				lastCountryCount = -1;
				return;
			}
			lastCountryCount =
				map.countries.Length; // check this size, and not result from GetCountryNames
			var oldCountryTransferName = GetCountryTransferIndexByGUISelection();
			_countryNames = map.GetCountryNames(groupByParentAdmin);
			lastProvinceCount = -1;
			lastMountPointCount = -1;
			lastCityCount = -1;
			SyncGUICountrySelection();
			SyncGUICountryTransferSelection(oldCountryTransferName);
			CountryRegionSelect(); // refresh selection
		}

		private void SyncGUICountrySelection()
		{
			// recover GUI country index selection
			if (GUICountryName.Length > 0 && _countryNames != null)
			{
				for (var k = 0;
					k < _countryNames.Length;
					k++) // don't use countryNames or the array will be reloaded again if grouped option is enabled causing an infinite loop
					if (_countryNames[k].TrimStart().StartsWith(GUICountryName))
					{
						GUICountryIndex = k;
						countryIndex = map.GetCountryIndex(GUICountryName);
						return;
					}
				SetInfoMsg("Country " + GUICountryName + " not found in this geodata file.");
			}
			GUICountryIndex = -1;
			GUICountryName = "";
			lastMountPointCount = -1;
		}

		private string GetCountryTransferIndexByGUISelection()
		{
			if (GUICountryTransferToCountryIndex < 0 ||
			    _countryNames == null ||
			    GUICountryTransferToCountryIndex >= _countryNames.Length)
				return "";
			var s = _countryNames[GUICountryTransferToCountryIndex].Split(new char[]
			{
				'(',
				')'
			}, System.StringSplitOptions.RemoveEmptyEntries);
			if (s.Length >= 2)
				return s[0].Trim();
			return "";
		}

		private void SyncGUICountryTransferSelection(string oldName)
		{
			// recover GUI country index selection
			if (oldName.Length > 0)
			{
				for (var k = 0;
					k < _countryNames.Length;
					k++) // don't use countryNames or the array will be reloaded again if grouped option is enabled causing an infinite loop
					if (_countryNames[k].TrimStart().StartsWith(oldName))
					{
						GUICountryTransferToCountryIndex = k;
						return;
					}
				SetInfoMsg("Country " + oldName + " not found in this geodata file.");
			}
			GUICountryTransferToCountryIndex = -1;
		}

		/// <summary>
		/// Deletes current region of country but not any of its dependencies
		/// </summary>
		public void CountryRegionDelete()
		{
			if (countryIndex < 0 || countryIndex >= map.countries.Length)
				return;
			map.HideCountryRegionHighlights(true);

			if (map.countries[countryIndex].regions.Count > 1)
			{
				map.countries[countryIndex].regions.RemoveAt(countryRegionIndex);
				map.RefreshCountryDefinition(countryIndex, null);
			}
			ClearSelection();
			map.Redraw();
			countryChanges = true;
		}

		/// <summary>
		/// Deletes current country.
		/// </summary>
		public void CountryDelete()
		{
			if (countryIndex < 0 || countryIndex >= map.countries.Length)
				return;
			map.HideCountryRegionHighlights(true);

			mDeleteCountryProvinces();
			DeleteCountryCities();
			DeleteCountryMountPoints();
			var newAdmins = new List<Country>(map.countries.Length - 1);
			for (var k = 0; k < map.countries.Length; k++)
				if (k != countryIndex)
					newAdmins.Add(map.countries[k]);
			map.countries = newAdmins.ToArray();
			// Updates country index in provinces
			for (var k = 0; k < map.provinces.Length; k++)
				if (map.provinces[k].countryIndex > countryIndex)
					map.provinces[k].countryIndex--;
			// Updates country index in cities
			for (var k = 0; k < map.cities.Length; k++)
				if (map.cities[k].countryIndex > countryIndex)
					map.cities[k].countryIndex--;
			// Updates country index in mount points
			if (map.mountPoints != null)
				for (var k = 0; k < map.mountPoints.Count; k++)
					if (map.mountPoints[k].countryIndex > countryIndex)
						map.mountPoints[k].countryIndex--;

			ClearSelection();
			map.OptimizeFrontiers();
			map.Redraw();
			map.DrawMapLabels();
			countryChanges = true;
		}

		public void CountryDeleteSameContinent()
		{
			if (countryIndex < 0 || countryIndex >= map.countries.Length)
				return;

			var continent = map.countries[countryIndex].continent;
			map.CountriesDeleteFromContinent(continent);

			ClearSelection();
			map.OptimizeFrontiers();
			map.Redraw();
			map.DrawMapLabels();
			countryChanges = true;

			SyncGUICitySelection();
			map.DrawCities();
			cityChanges = true;

			SyncGUIProvinceSelection();
			provinceChanges = true;

			GUIMountPointName = "";
			SyncGUIMountPointSelection();
			map.DrawMountPoints();
			mountPointChanges = true;
		}

		/// <summary>
		/// Makes one country to annex another. Used internally by Map Editor.
		/// </summary>
		public void CountryTransferTo()
		{
			if (countryIndex < 0 ||
			    GUICountryTransferToCountryIndex < 0 ||
			    GUICountryTransferToCountryIndex >= countryNames.Length)
				return;
			// Get target country
			// recover GUI country index selection
			var targetCountryIndex = -1;
			var s = countryNames[GUICountryTransferToCountryIndex].Split(new char[]
			{
				'(',
				')'
			}, System.StringSplitOptions.RemoveEmptyEntries);
			if (s.Length >= 2)
				if (!int.TryParse(s[1], out targetCountryIndex))
					return;
			map.HideCountryRegionHighlights(true);
			map.HideProvinceRegionHighlights(true);
			var sourceCountry = map.countries[countryIndex];
			var mainRegionIndex = sourceCountry.mainRegionIndex;
			map.CountryTransferCountryRegion(targetCountryIndex, sourceCountry.regions[mainRegionIndex],
				true);
			countryChanges = true;
			provinceChanges = true;
			cityChanges = true;
			mountPointChanges = true;
			countryIndex = targetCountryIndex;
			countryRegionIndex = map.countries[countryIndex].mainRegionIndex;
			CountryRegionSelect();
			map.DrawMapLabels();
		}

		public bool CountryChangeContinent()
		{
			if (countryIndex < 0 || countryIndex >= map.countries.Length)
				return false;
			map.countries[countryIndex].continent = GUICountryNewContinent;
			countryChanges = true;
			return true;
		}

		public bool CountryChangeFIPSAndISOCodes()
		{
			if (countryIndex < 0 || countryIndex >= map.countries.Length)
				return false;
			map.countries[countryIndex].fips10_4 = GUICountryNewFIPS10_4;
			map.countries[countryIndex].iso_a2 = GUICountryNewISO_A2;
			map.countries[countryIndex].iso_a3 = GUICountryNewISO_A3;
			map.countries[countryIndex].iso_n3 = GUICountryNewISO_N3;
			countryChanges = true;
			return true;
		}

		public void CountryHideOffScreen()
		{
			var countries = map.GetVisibleCountries(sceneCamera);
			if (countries == null)
				return;
			for (var k = 0; k < map.countries.Length; k++)
				if (!countries.Contains(map.countries[k]))
				{
					map.countries[k].hidden = true;
					countryChanges = true;
				}
		}

		public void CountryDeleteOffScreen()
		{
			var countries = map.GetVisibleCountries(sceneCamera);
			if (countries == null)
				return;
			for (var k = 0; k < map.countries.Length; k++)
				if (!countries.Contains(map.countries[k]))
				{
					map.CountryDelete(k, true, false);
					countryChanges = true;
					provinceChanges = true;
					cityChanges = true;
					mountPointChanges = true;
					k--;
				}
		}

		/// <summary>
		/// Exports the geographic data in packed string format with reduced quality.
		/// </summary>
		public string GetCountryGeoDataLowQuality()
		{
			// step 1: duplicate data
			IAdminEntity[] entities;
			if (editingMode == EDITING_MODE.COUNTRIES)
				entities = map.countries;
			else
				entities = map.provinces;
			var entities1 = new List<IAdminEntity>(entities);

			for (var k = 0; k < entities1.Count; k++)
			{
				entities1[k].regions = new List<Region>(entities1[k].regions);
				for (var r = 0; r < entities[k].regions.Count; r++)
					entities1[k].regions[r].points =
						new List<Vector2>(entities1[k].regions[r].points).ToArray();
			}
			// step 2: ensure near points between neighbours
			var MAX_DIST = 0.00000001f;
			//			int join = 0;
			for (var k = 0; k < entities1.Count; k++)
			{
				for (var r = 0; r < entities1[k].regions.Count; r++)
				{
					var region1 = entities1[k].regions[r];
					for (var p = 0; p < entities1[k].regions[r].points.Length; p++)
					{
						// Search near points
						for (var k2 = 0; k2 < region1.neighbours.Count; k2++)
						{
							for (var r2 = 0; r2 < entities1[k2].regions.Count; r2++)
							{
								var region2 = entities1[k2].regions[r2];
								for (var p2 = 0; p2 < entities1[k2].regions[r2].points.Length; p2++)
								{
									var dist = (region1.points[p].x - region2.points[p2].x) *
									           (region1.points[p].x - region2.points[p2].x) +
									           (region1.points[p].y - region2.points[p2].y) *
									           (region1.points[p].y - region2.points[p2].y);
									if (dist < MAX_DIST)
										region2.points[p2] = region1.points[p];
									//										join++;
								}
							}
						}
					}
				}
			}

			// step 2: simplify
			var frontiersHit = new Dictionary<Vector2, bool>();
			var entities2 = new List<IAdminEntity>(entities1.Count);
			int savings = 0, totalPoints = 0;
			var FACTOR = 1000f;
			for (var k = 0; k < entities1.Count; k++)
			{
				var refEntity = entities1[k];
				IAdminEntity newEntity;
				if (refEntity is Country)
					newEntity = new Country(refEntity.name, ((Country)refEntity).continent,
						map.GetUniqueId(new List<IExtendableAttribute>(map.countries)));
				else
					newEntity = new Province(refEntity.name, ((Province)refEntity).countryIndex,
						map.GetUniqueId(new List<IExtendableAttribute>(map.provinces)));
				for (var r = 0; r < refEntity.regions.Count; r++)
				{
					var region = refEntity.regions[r];
					var numPoints = region.points.Length;
					totalPoints += numPoints;
					var points = new List<Vector2>(numPoints);
					frontiersHit.Clear();

					var blockyPoints = new Vector3[numPoints];
					for (var p = 0; p < numPoints; p++)
						blockyPoints[p] =
							new Vector2(Mathf.RoundToInt(region.points[p].x * FACTOR) / FACTOR,
								Mathf.RoundToInt(region.points[p].y * FACTOR) / FACTOR);

					points.Add(region.points[0] * WMSK.MAP_PRECISION);
					for (var p = 1; p < numPoints - 1; p++)
					{
						if (blockyPoints[p - 1].y == blockyPoints[p].y &&
						    blockyPoints[p].y == blockyPoints[p + 1].y ||
						    blockyPoints[p - 1].x == blockyPoints[p].x &&
						    blockyPoints[p].x == blockyPoints[p + 1].x)
						{
							savings++;
							continue;
						}
						if (!frontiersHit.ContainsKey(blockyPoints[p]))
						{
							// add neighbour references
							frontiersHit.Add(blockyPoints[p], true);
							points.Add(region.points[p] * WMSK.MAP_PRECISION);
						}
						else
							savings++;
					}
					points.Add(region.points[numPoints - 1] * WMSK.MAP_PRECISION);
					if (points.Count >= 5)
					{
						var newRegion = new Region(newEntity, newEntity.regions.Count);
						newRegion.points = points.ToArray();
						newEntity.regions.Add(newRegion);
					}
				}
				if (newEntity.regions.Count > 0)
					entities2.Add(newEntity);
			}

			Debug.Log(savings +
			          " points removed of " +
			          totalPoints +
			          " (" +
			          ((float)savings / totalPoints * 100.0f).ToString("F1") +
			          "%)");

			var sb = new StringBuilder();
			for (var k = 0; k < entities2.Count; k++)
			{
				var entity = entities2[k];
				if (k > 0)
					sb.Append("|");
				sb.Append(entity.name + "$");
				if (entity is Country)
					sb.Append(((Country)entity).continent + "$");
				else
					sb.Append(map.countries[((Province)entity).countryIndex].name + "$");
				for (var r = 0; r < entity.regions.Count; r++)
				{
					if (r > 0)
						sb.Append("*");
					var region = entity.regions[r];
					for (var p = 0; p < region.points.Length; p++)
					{
						if (p > 0)
							sb.Append(";");
						var point = region.points[p];
						sb.Append(point.x.ToString() + ",");
						sb.Append(point.y.ToString());
					}
				}
			}
			return sb.ToString();
		}

		private int GetNearestCountryToShape()
		{
			var countryIndex = -1;
			var minDist = float.MaxValue;
			var p = newShape[0];
			for (var k = 0; k < map.countries.Length; k++)
			{
				var dist = (p - map.countries[k].center).sqrMagnitude;
				if (dist < minDist)
				{
					minDist = dist;
					countryIndex = k;
				}
			}
			return countryIndex;
		}

		/// <summary>
		/// Creates a new country with the current shape.
		/// </summary>
		public void CountryCreate()
		{
			if (newShape.Count < 6)
				return;
			string continent;
			var nearestCountry = GetNearestCountryToShape();
			if (nearestCountry >= 0)
				continent = map.countries[nearestCountry].continent;
			else
				continent = "World";
			countryIndex = map.countries.Length;
			countryRegionIndex = 0;
			var uniqueName = GetCountryUniqueName("New Country");
			var newCountry = new Country(uniqueName, continent,
				map.GetUniqueId(new List<IExtendableAttribute>(map.countries)));
			var region = new Region(newCountry, 0);
			region.points = newShape.ToArray();
			newCountry.regions.Add(region);
			map.CountryAdd(newCountry);
			lastCountryCount = -1;
			GUICountryName = "";
			ReloadCountryNames();
			countryChanges = true;

			map.Redraw();
			CountryRegionSelect();
		}

		/// <summary>
		/// Creates a special background country named Pool
		/// </summary>
		public void CountryCreatePool()
		{
			ClearSelection();
			countryIndex = _map.CountryCreateProvincesPool("Pool", false);
			countryRegionIndex = 0;
			lastCountryCount = -1;
			ReloadCountryNames();
			countryChanges = true;
		}

		/// <summary>
		/// Creates a new country based on a given region. Existing region is removed from its source entity.
		/// </summary>
		/// <param name="region">Region.</param>
		public void CountryCreate(Region region)
		{
			// Remove region from source entity
			var entity = region.entity;
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

			// Create the new country
			var uniqueName = GetCountryUniqueName(country.name);
			var newCountry = new Country(uniqueName, country.continent,
				map.GetUniqueId(new List<IExtendableAttribute>(map.countries)));
			if (entity is Country)
				newCountry.regions.Add(region);
			else
			{
				var newRegion = new Region(newCountry, 0);
				newRegion.UpdatePointsAndRect(region.points);
				newCountry.regions.Add(newRegion);
			}
			countryIndex = map.CountryAdd(newCountry);
			countryRegionIndex = 0;
			lastCountryCount = -1;
			GUICountryName = "";
			ReloadCountryNames();
			countryChanges = true;

			// Update cities
			var cities = _map.GetCities(region);
			var citiesCount = cities.Count;
			if (citiesCount > 0)
				for (var k = 0; k < citiesCount; k++)
					if (cities[k].countryIndex != countryIndex)
					{
						cities[k].countryIndex = countryIndex;
						cityChanges = true;
					}

			// Update mount points
			var mp = _map.GetMountPoints(region);
			if (mp.Count > 0)
				for (var k = 0; k < mp.Count; k++)
					if (mp[k].countryIndex != countryIndex)
					{
						mp[k].countryIndex = countryIndex;
						mountPointChanges = true;
					}

			// Transfer any contained province
			if (entity is Country)
			{
				var provinces = _map.GetProvinces(region);
				for (var k = 0; k < provinces.Count; k++)
				{
					var prov = provinces[k];
					if (prov.regions == null)
						_map.ReadProvincePackedString(prov);
					if (prov.regions == null)
						continue;
					if (_map.CountryTransferProvinceRegion(countryIndex, prov.mainRegion, false))
						provinceChanges = true;
				}
			}

			map.Redraw();
			CountryRegionSelect();
		}

		/// <summary>
		/// Adds a new region to current country
		/// </summary>
		public void CountryRegionCreate()
		{
			if (newShape.Count < 3 || countryIndex < 0)
				return;

			var country = map.countries[countryIndex];
			countryRegionIndex = country.regions.Count;
			var region = new Region(country, countryRegionIndex);
			region.points = newShape.ToArray();
			country.regions.Add(region);
			map.RefreshCountryDefinition(countryIndex, null);
			map.Redraw();
			countryChanges = true;
			CountryRegionSelect();
		}

		/// <summary>
		/// Merges all provinces in each continent so their number doesn't exceed a given number
		/// </summary>
		/// <param name="max">Maximum number of countries per continent.</param>
		/// <param name="z">The current country index.</param>
		public void CountriesEqualize(int max, out bool repeat)
		{
			if (max < 1)
				max = 1;

			if (groupedCountries == null)
				groupedCountries = new Dictionary<string, List<Country>>();
			else
				groupedCountries.Clear();

			repeat = false;

			// Select continents
			for (var k = 0; k < map.countries.Length; k++)
			{
				var country = map.countries[k];
				if (country.continent.Length > 0)
				{
					List<Country> countries;
					if (groupedCountries.TryGetValue(country.continent, out countries))
						countries.Add(country);
					else
					{
						countries = new List<Country>();
						countries.Add(country);
						groupedCountries[country.continent] = countries;
					}
				}
			}

			var continents = new List<string>(groupedCountries.Keys);
			for (var k = 0; k < continents.Count; k++)
			{
				var continent = continents[k];
				var count = groupedCountries[continent].Count;
				if (count > max) // Take first country and merge with first neighbour
					for (var j = 0; j < count; j++)
					{
						var country = groupedCountries[continent][j];
						var countryIndex = map.GetCountryIndex(country);
						var neighbourCount = country.mainRegion.neighbours.Count;
						if (neighbourCount > 0)
						{
							var randomNeighbour = Random.Range(0, neighbourCount - 1);
							var otherRegion = country.mainRegion.neighbours[randomNeighbour];
							var otherCountry = (Country)otherRegion.entity;
							if (otherCountry.continent.Equals(continent))
								if (map.CountryTransferCountryRegion(countryIndex, otherRegion, false))
								{
									countryChanges = true;
									provinceChanges = true;
									cityChanges = true;
									mountPointChanges = true;
									repeat = true;
									return;
								}
						}
					}
			}
		}

		#endregion

		#region IO stuff

		/// <summary>
		/// Returns the file name corresponding to the current country data file (countries10, countries110)
		/// </summary>
		public string GetCountryGeoDataFileName() => map.frontiersDetail == FRONTIERS_DETAIL.Low
			? "countries110.txt"
			: "countries10.txt";

		#endregion
	}
}