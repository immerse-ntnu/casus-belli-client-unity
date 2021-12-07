using UnityEngine;
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;

namespace WorldMapStrategyKit
{
	public enum OPERATION_MODE
	{
		SELECTION = 0,
		RESHAPE = 1,
		CREATE = 2,
		MAP_GENERATOR = 3,
		MAP_TOOLS = 4,
		REVERT = 5,
		CONFIRM = 6
	}

	public enum RESHAPE_REGION_TOOL
	{
		POINT = 0,
		CIRCLE = 1,
		SPLITV = 2,
		SPLITH = 3,
		MAGNET = 4,
		SMOOTH = 5,
		ERASER = 6,
		DELETE = 7
	}

	public enum RESHAPE_CITY_TOOL
	{
		MOVE = 0,
		DELETE = 1
	}

	public enum RESHAPE_MOUNT_POINT_TOOL
	{
		MOVE = 0,
		DELETE = 1
	}

	public enum CREATE_TOOL
	{
		CITY = 0,
		COUNTRY = 1,
		COUNTRY_REGION = 2,
		PROVINCE = 3,
		PROVINCE_REGION = 4,
		MOUNT_POINT = 5
	}

	public enum EDITING_MODE
	{
		COUNTRIES,
		PROVINCES
	}

	public enum EDITING_COUNTRY_FILE
	{
		COUNTRY_HIGHDEF = 0,
		COUNTRY_LOWDEF = 1
	}

	public static class ReshapeToolExtensions
	{
		public static bool hasCircle(this RESHAPE_REGION_TOOL r) => r == RESHAPE_REGION_TOOL.CIRCLE ||
		                                                            r == RESHAPE_REGION_TOOL.MAGNET ||
		                                                            r == RESHAPE_REGION_TOOL.ERASER;
	}

	[RequireComponent(typeof(WMSK)), ExecuteInEditMode]
	public partial class WMSK_Editor : MonoBehaviour
	{
		public int entityIndex
		{
			get
			{
				if (editingMode == EDITING_MODE.PROVINCES && provinceIndex >= 0)
					return provinceIndex;
				return countryIndex;
			}
		}

		public int regionIndex
		{
			get
			{
				if (editingMode == EDITING_MODE.PROVINCES && provinceIndex >= 0)
					return provinceRegionIndex;
				if (countryIndex >= 0)
					return countryRegionIndex;
				return -1;
			}
		}

		public Region selectedRegion
		{
			get
			{
				if (editingMode == EDITING_MODE.PROVINCES && provinceIndex >= 0)
				{
					if (provinceIndex >= 0 &&
					    provinceRegionIndex >= 0 &&
					    map.provinces != null &&
					    map.provinces[provinceIndex].regions != null &&
					    provinceRegionIndex < map.provinces[provinceIndex].regions.Count)
						return map.provinces[provinceIndex].regions[provinceRegionIndex];
					return null;
				}
				if (countryIndex >= 0 &&
				    countryRegionIndex >= 0 &&
				    map.countries != null &&
				    countryIndex < map.countries.Length &&
				    map.countries[countryIndex].regions != null &&
				    countryRegionIndex < map.countries[countryIndex].regions.Count)
					return map.countries[countryIndex].regions[countryRegionIndex];
				return null;
			}
		}

		public OPERATION_MODE operationMode;
		public RESHAPE_REGION_TOOL reshapeRegionMode;
		public RESHAPE_CITY_TOOL reshapeCityMode;
		public RESHAPE_MOUNT_POINT_TOOL reshapeMountPointMode;
		public CREATE_TOOL createMode;
		public Vector3 cursor;
		public bool circleMoveConstant, circleCurrentRegionOnly;
		public float reshapeCircleWidth = 0.01f;
		public bool shouldHideEditorMesh;
		public bool magnetAgressiveMode = false;
		public bool magnetIncludeCountries = false;
		public int snapPrecisionDigits = 7;

		public string infoMsg = "";
		public DateTime infoMsgStartTime;
		public EDITING_MODE editingMode;
		public EDITING_COUNTRY_FILE editingCountryFile;

		[NonSerialized] public List<Region> highlightedRegions;

		[NonSerialized] public bool territoryImporterActive;

		[NonSerialized] public bool terrainImporterActive;

		[NonSerialized] public bool issueRedraw;

		private List<List<Region>> _undoRegionsList;

		private List<List<Region>> undoRegionsList
		{
			get
			{
				if (_undoRegionsList == null)
					_undoRegionsList = new List<List<Region>>();
				return _undoRegionsList;
			}
		}

		public int undoRegionsDummyFlag;

		private List<City[]> _undoCitiesList;

		private List<City[]> undoCitiesList
		{
			get
			{
				if (_undoCitiesList == null)
					_undoCitiesList = new List<City[]>();
				return _undoCitiesList;
			}
		}

		public int undoCitiesDummyFlag;

		private List<List<MountPoint>> _undoMountPointsList;

		private List<List<MountPoint>> undoMountPointsList
		{
			get
			{
				if (_undoMountPointsList == null)
					_undoMountPointsList = new List<List<MountPoint>>();
				return _undoMountPointsList;
			}
		}

		public int undoMountPointsDummyFlag;

		public IAdminEntity[] allEntities
		{
			get
			{
				if (editingMode == EDITING_MODE.PROVINCES)
					return map.provinces;
				else
					return map.countries;
			}
		}

		public List<Vector2> newShape;
		public Region newShapeSegmentRegion;
		public int newShapeSegmentPointIndex;

		private const float POINT_SQR_PRECISION = 1e-10f;

		private WMSK _map;

		[SerializeField] private int lastMinPopulation;

		[NonSerialized] public Camera sceneCamera;

		private List<Region> affectedRegions;

		private void OnEnable()
		{
			lastMinPopulation = map.minPopulation;
			map.minPopulation = 0;
			ApplySeed();
			GenerateHeightMap(true);
		}

		private void OnDisable()
		{
			if (_map != null)
				if (_map.minPopulation == 0)
					_map.minPopulation = lastMinPopulation;
		}

		#region Editor functionality

		/// <summary>
		/// Accesor to the World Map Strategy Kit API
		/// </summary>
		public WMSK map
		{
			get
			{
				if (_map == null)
					_map = GetComponent<WMSK>();
				return _map;
			}
		}

		// Starts a new map from scratch
		public void NewMap()
		{
			ClearSelection();
			map.mountPoints.Clear();
			map.cities = new City[0];
			map.provinces = new Province[0];
			map.countries = new Country[0];
			countryChanges = true;
			provinceChanges = true;
			cityChanges = true;
			mountPointChanges = true;
			countryAttribChanges = true;
			provinceAttribChanges = true;
			cityAttribChanges = true;
			_map.Redraw(true);
		}

		public void ClearSelection()
		{
			map.HideCountryRegionHighlights(true);
			highlightedRegions = null;
			countryIndex = -1;
			countryRegionIndex = -1;
			GUICountryName = "";
			GUICountryNewName = "";
			GUICountryIndex = -1;
			ClearProvinceSelection();
			ClearCitySelection();
			ClearMountPointSelection();
		}

		public void ChangeEditingMode(EDITING_MODE newMode)
		{
			editingMode = newMode;
			ClearSelection();
			// Ensure file is loaded by the map
			switch (editingMode)
			{
				case EDITING_MODE.COUNTRIES:
					_map.showFrontiers = true;
					_map.showProvinces = false;
					_map.HideProvinces();
					break;
				case EDITING_MODE.PROVINCES:
					_map.showProvinces = true;
					_map.enableProvinceHighlight = true;
					break;
			}
		}

		/// <summary>
		/// Redraws all frontiers and highlights current selected regions.
		/// </summary>
		public void RedrawFrontiers()
		{
			RedrawFrontiers(highlightedRegions, true);
		}

		/// <summary>
		/// Redraws the frontiers and highlights specified regions filtered by provided list of regions. Also highlights current selected country/province.
		/// </summary>
		/// <param name="filterRegions">Regions.</param>
		/// <param name="highlightSelected">Pass false to just redraw borders.</param>
		public void RedrawFrontiers(List<Region> filterRegions, bool highlightSelected)
		{
			map.RefreshCountryDefinition(countryIndex, filterRegions);
			map.Redraw();

			if (highlightSelected)
			{
				map.HideProvinces();
				CountryHighlightSelection(filterRegions);
			}
			if (editingMode == EDITING_MODE.PROVINCES)
			{
				map.RefreshProvinceDefinition(provinceIndex, false);
				if (highlightSelected)
					ProvinceHighlightSelection();
			}
		}

		public void DiscardChanges()
		{
			map.ReloadData();
			map.Redraw(true);
			RedrawFrontiers();
			cityChanges = false;
			countryChanges = false;
			provinceChanges = false;
			mountPointChanges = false;
		}

		/// <summary>
		/// Moves any point inside circle.
		/// </summary>
		/// <returns>Returns a list with changed regions</returns>
		public List<Region> MoveCircle(Vector3 position, Vector2 dragAmount, float circleSize)
		{
			var currentRegion = selectedRegion;
			if (currentRegion == null)
				return null;

			var circleSizeSqr = circleSize * circleSize;
			var regions = new List<Region>(100);
			// Current region
			regions.Add(currentRegion);
			// Current region's neighbours
			if (!circleCurrentRegionOnly)
			{
				var nCount = currentRegion.neighbours.Count;
				for (var r = 0; r < nCount; r++)
				{
					var region = currentRegion.neighbours[r];
					if (!regions.Contains(region))
						regions.Add(region);
				}
				// If we're editing provinces, check if country points can be moved as well
				if (editingMode == EDITING_MODE.PROVINCES)
				{
					// Moves current country
					var crCount = map.countries[countryIndex].regions.Count;
					for (var cr = 0; cr < crCount; cr++)
					{
						var countryRegion = map.countries[countryIndex].regions[cr];
						if (!regions.Contains(countryRegion))
							regions.Add(countryRegion);
						// Moves neighbours
						nCount = countryRegion.neighbours.Count;
						for (var r = 0; r < nCount; r++)
						{
							var region = countryRegion.neighbours[r];
							if (!regions.Contains(region))
								regions.Add(region);
						}
					}
				}
			}
			// Execute move operation on each point
			var rCount = regions.Count;
			if (affectedRegions == null)
				affectedRegions = new List<Region>(regions.Count);
			else
				affectedRegions.Clear();
			for (var r = 0; r < rCount; r++)
			{
				var region = regions[r];
				var regionAffected = false;
				for (var p = 0; p < region.points.Length; p++)
				{
					var rp = region.points[p];
					var dist = (rp.x - position.x) * (rp.x - position.x) * 4.0f +
					           (rp.y - position.y) * (rp.y - position.y);
					if (dist < circleSizeSqr)
					{
						var point = region.points[p];
						if (circleMoveConstant)
							point += dragAmount;
						else
							point += dragAmount - dragAmount * (dist / circleSizeSqr);
						point.x = (float)Math.Round(point.x, snapPrecisionDigits);
						point.y = (float)Math.Round(point.y, snapPrecisionDigits);
						region.points[p] = point;
						regionAffected = true;
					}
				}
				if (regionAffected)
					affectedRegions.Add(region);
			}
			return affectedRegions;
		}

		/// <summary>
		/// Moves a single point.
		/// </summary>
		/// <returns>Returns a list of affected regions</returns>
		public List<Region> MovePoint(Vector3 position, Vector3 dragAmount) =>
			MoveCircle(position, dragAmount, 0.0001f);

		/// <summary>
		/// Moves points of other regions towards current frontier
		/// </summary>
		public bool Magnet(Vector3 position, float circleSize)
		{
			var currentRegion = selectedRegion;
			if (currentRegion == null)
				return false;

			var circleSizeSqr = circleSize * circleSize;

			var attractorsUse = new Dictionary<Vector3, bool>();
			// Attract points of other regions/countries
			var regions = new List<Region>();
			for (var c = 0; c < allEntities.Length; c++)
			{
				var entity = allEntities[c];
				if (entity.regions == null)
					continue;
				for (var r = 0; r < entity.regions.Count; r++)
					if (c != entityIndex || r != regionIndex)
						regions.Add(entity.regions[r]);
			}
			if (editingMode == EDITING_MODE.PROVINCES &&
			    magnetIncludeCountries) // Also add regions of current country and neighbours
				for (var r = 0; r < map.countries[countryIndex].regions.Count; r++)
				{
					var region = map.countries[countryIndex].regions[r];
					regions.Add(region);
					for (var n = 0; n < region.neighbours.Count; n++)
					{
						var nregion = region.neighbours[n];
						if (!regions.Contains(nregion))
							regions.Add(nregion);
					}
				}

			var changes = false;
			var goodAttractor = Misc.Vector3zero;

			var regionCount = regions.Count;
			for (var r = 0; r < regionCount; r++)
			{
				var region = regions[r];
				var changesInThisRegion = false;
				for (var p = 0; p < region.points.Length; p++)
				{
					Vector3 rp = region.points[p];
					var dist = (rp.x - position.x) * (rp.x - position.x) * 4.0f +
					           (rp.y - position.y) * (rp.y - position.y);
					if (dist < circleSizeSqr)
					{
						var minDist = float.MaxValue;
						var nearest = -1;
						for (var a = 0; a < currentRegion.points.Length; a++)
						{
							Vector3 attractor = currentRegion.points[a];
							dist = (rp.x - attractor.x) * (rp.x - attractor.x) * 4.0f +
							       (rp.y - attractor.y) * (rp.y - attractor.y);
							if (dist < circleSizeSqr && dist < minDist)
							{
								minDist = dist;
								nearest = a;
								goodAttractor = attractor;
							}
						}
						if (nearest >= 0)
						{
							changes = true;
							// Check if this attractor is being used by other point
							var used = attractorsUse.ContainsKey(goodAttractor);
							if (!used || magnetAgressiveMode)
							{
								region.points[p] = goodAttractor;
								if (!used)
									attractorsUse[goodAttractor] = true;
								changesInThisRegion = true;
							}
						}
					}
				}
				if (changesInThisRegion)
				{
					// Remove duplicate points in this region
					var repeated = new Dictionary<Vector2, bool>();
					for (var k = 0; k < region.points.Length; k++)
						if (!repeated.ContainsKey(region.points[k]))
							repeated.Add(region.points[k], true);
					region.points = new List<Vector2>(repeated.Keys).ToArray();
				}
			}
			return changes;
		}

		/// <summary>
		/// Erase points inside circle.
		/// </summary>
		public bool Erase(Vector3 position, float circleSize)
		{
			if (circleCurrentRegionOnly)
			{
				if (selectedRegion == null)
					return false;
				return EraseFromRegion(selectedRegion, position, circleSize);
			}
			else
				return EraseFromAnyRegion(position, circleSize);
		}

		private bool EraseFromRegion(Region region, Vector3 position, float circleSize)
		{
			var circleSizeSqr = circleSize * circleSize;

			// Erase points inside the circle
			var changes = false;
			var temp = new List<Vector2>(region.points.Length);
			for (var p = 0; p < region.points.Length; p++)
			{
				Vector3 rp = region.points[p];
				var dist = (rp.x - position.x) * (rp.x - position.x) * 4.0f +
				           (rp.y - position.y) * (rp.y - position.y);
				if (dist > circleSizeSqr)
					temp.Add(rp);
				else
					changes = true;
			}
			if (changes)
			{
				var newPoints = temp.ToArray();
				if (newPoints.Length >= 3)
					region.points = newPoints;
				else
				{
					SetInfoMsg(
						"Minimum of 3 points is required. To remove the region use the DELETE button.");
					return false;
				}
//					// Remove region from entity
//					if (region.entity.regions.Contains(region)) {
//						region.entity.regions.Remove(region);
//					}
//					SetInfoMsg("Region removed from entity!");
//				}
				if (region.entity is Country)
					countryChanges = true;
				else
					provinceChanges = true;
			}

			return changes;
		}

		private bool EraseFromAnyRegion(Vector3 position, float circleSize)
		{
			// Try to delete from any region of any country
			var changes = false;
			for (var c = 0; c < _map.countries.Length; c++)
			{
				var country = _map.countries[c];
				for (var cr = 0; cr < country.regions.Count; cr++)
				{
					var region = country.regions[cr];
					if (EraseFromRegion(region, position, circleSize))
						changes = true;
				}
			}

			// Try to delete from any region of any province
			if (editingMode == EDITING_MODE.PROVINCES && _map.provinces != null)
				for (var p = 0; p < _map.provinces.Length; p++)
				{
					var province = _map.provinces[p];
					for (var pr = 0; pr < province.regions.Count; pr++)
					{
						var region = province.regions[pr];
						if (EraseFromRegion(region, position, circleSize))
							changes = true;
					}
				}
			return changes;
		}

		public void UndoRegionsPush(List<Region> regions)
		{
			UndoRegionsInsertAtCurrentPos(regions);
			undoRegionsDummyFlag++;
			if (editingMode == EDITING_MODE.COUNTRIES)
				countryChanges = true;
			else
				provinceChanges = true;
		}

		public void UndoRegionsInsertAtCurrentPos(List<Region> regions)
		{
			if (regions == null)
				return;
			var clonedRegions = new List<Region>();
			for (var k = 0; k < regions.Count; k++)
				clonedRegions.Add(regions[k].Clone());
			if (undoRegionsDummyFlag > undoRegionsList.Count)
				undoRegionsDummyFlag = undoRegionsList.Count;
			undoRegionsList.Insert(undoRegionsDummyFlag, clonedRegions);
		}

		public void UndoCitiesPush()
		{
			UndoCitiesInsertAtCurrentPos();
			undoCitiesDummyFlag++;
		}

		public void UndoCitiesInsertAtCurrentPos()
		{
			var cities = new List<City>(map.cities.Length);
			for (var k = 0; k < map.cities.Length; k++)
				cities.Add(map.cities[k].Clone());
			if (undoCitiesDummyFlag > undoCitiesList.Count)
				undoCitiesDummyFlag = undoCitiesList.Count;
			undoCitiesList.Insert(undoCitiesDummyFlag, cities.ToArray());
		}

		public void UndoMountPointsPush()
		{
			UndoMountPointsInsertAtCurrentPos();
			undoMountPointsDummyFlag++;
		}

		public void UndoMountPointsInsertAtCurrentPos()
		{
			if (map.mountPoints == null)
				map.mountPoints = new List<MountPoint>();
			var mountPoints = new List<MountPoint>(map.mountPoints.Count);
			for (var k = 0; k < map.mountPoints.Count; k++)
				mountPoints.Add(map.mountPoints[k].Clone());
			if (undoMountPointsDummyFlag > undoMountPointsList.Count)
				undoMountPointsDummyFlag = undoMountPointsList.Count;
			undoMountPointsList.Insert(undoMountPointsDummyFlag, mountPoints);
		}

		public void UndoHandle()
		{
			if (undoRegionsList != null && undoRegionsList.Count >= 2)
			{
				if (undoRegionsDummyFlag >= undoRegionsList.Count)
					undoRegionsDummyFlag = undoRegionsList.Count - 2;
				var savedRegions = undoRegionsList[undoRegionsDummyFlag];
				RestoreRegions(savedRegions);
			}
			if (undoCitiesList != null && undoCitiesList.Count >= 2)
			{
				if (undoCitiesDummyFlag >= undoCitiesList.Count)
					undoCitiesDummyFlag = undoCitiesList.Count - 2;
				var savedCities = undoCitiesList[undoCitiesDummyFlag];
				RestoreCities(savedCities);
			}
			if (undoMountPointsList != null && undoMountPointsList.Count >= 2)
			{
				if (undoMountPointsDummyFlag >= undoMountPointsList.Count)
					undoMountPointsDummyFlag = undoMountPointsList.Count - 2;
				var savedMountPoints = undoMountPointsList[undoMountPointsDummyFlag];
				RestoreMountPoints(savedMountPoints);
			}
		}

		private void RestoreRegions(List<Region> savedRegions)
		{
			for (var k = 0; k < savedRegions.Count; k++)
			{
				var entity = savedRegions[k].entity;
				var regionIndex = savedRegions[k].regionIndex;
				entity.regions[regionIndex] = savedRegions[k];
			}
			RedrawFrontiers();
		}

		private void RestoreCities(City[] savedCities)
		{
			map.cities = savedCities;
			lastCityCount = -1;
			ReloadCityNames();
			map.DrawCities();
		}

		private void RestoreMountPoints(List<MountPoint> savedMountPoints)
		{
			map.mountPoints = savedMountPoints;
			lastMountPointCount = -1;
			ReloadMountPointNames();
			map.DrawMountPoints();
		}

		private int EntityAdd(IAdminEntity newEntity)
		{
			if (newEntity is Country)
				return map.CountryAdd((Country)newEntity);
			else
				return map.ProvinceAdd((Province)newEntity);
		}

		/// <summary>
		/// Adds extra points if distance between 2 consecutive points exceed some threshold 
		/// </summary>
		/// <returns><c>true</c>, if region was smoothed, <c>false</c> otherwise.</returns>
		/// <param name="region">Region.</param>
		private bool SmoothRegion(Region region)
		{
			const float smoothDistance = 0.015f;
			var lastPoint = region.points.Length - 1;
			var changes = false;
			var newPoints = new List<Vector2>(lastPoint + 1);
			for (var k = 0; k <= lastPoint; k++)
			{
				var p0 = region.points[k];
				Vector2 p1;
				if (k == lastPoint)
					p1 = region.points[0];
				else
					p1 = region.points[k + 1];
				newPoints.Add(p0);
				var dist = (p0 - p1).magnitude;
				if (dist > smoothDistance)
				{
					changes = true;
					var steps = Mathf.FloorToInt(dist / smoothDistance);
					var inc = dist / (steps + 1);
					var acum = inc;
					for (var j = 0; j < steps; j++)
					{
						newPoints.Add(Vector2.Lerp(p0, p1, acum / dist));
						acum += inc;
					}
				}
				newPoints.Add(p1);
			}
			if (changes)
				region.UpdatePointsAndRect(newPoints);
			return changes;
		}

		private void SplitCities(int sourceCountryIndex, Region regionOtherCountry)
		{
			var cityCount = map.cities.Length;
			var targetCountryIndex = map.GetCountryIndex((Country)regionOtherCountry.entity);
			for (var k = 0; k < cityCount; k++)
			{
				var city = map.cities[k];
				if (city.countryIndex == sourceCountryIndex &&
				    regionOtherCountry.Contains(city.unity2DLocation))
				{
					city.countryIndex = targetCountryIndex;
					cityChanges = true;
				}
			}
		}

		public void SplitHorizontally()
		{
			var currentRegion = selectedRegion;
			if (currentRegion == null)
				return;
			var currentEntity = currentRegion.entity;
			var center = currentRegion.center;
			var half1 = new List<Vector2>();
			var half2 = new List<Vector2>();
			var prevSide = 0;
			for (var k = 0; k < currentRegion.points.Length; k++)
			{
				Vector3 p = currentRegion.points[k];
				if (p.y > currentRegion.center.y)
				{
					half1.Add(p);
					if (prevSide == -1)
						half2.Add(p);
					prevSide = 1;
				}
				if (p.y <= currentRegion.center.y)
				{
					half2.Add(p);
					if (prevSide == 1)
						half1.Add(p);
					prevSide = -1;
				}
			}
			// Setup new entity
			IAdminEntity newEntity;

			if (currentEntity is Country)
			{
				var uniqueName = GetCountryUniqueName("New " + currentEntity.name);
				newEntity = new Country(uniqueName, ((Country)currentEntity).continent,
					map.GetUniqueId(new List<IExtendableAttribute>(map.countries)));
			}
			else
			{
				var uniqueName = GetProvinceUniqueName("New " + currentEntity.name);
				newEntity = new Province(uniqueName, ((Province)currentEntity).countryIndex,
					map.GetUniqueId(new List<IExtendableAttribute>(map.provinces)));
				newEntity.regions = new List<Region>();
			}

			// Update polygons
			var newRegion = new Region(newEntity, 0);
			if (map.countries[countryIndex].center.y > center.y)
			{
				currentRegion.UpdatePointsAndRect(half1);
				newRegion.UpdatePointsAndRect(half2);
			}
			else
			{
				currentRegion.UpdatePointsAndRect(half2);
				newRegion.UpdatePointsAndRect(half1);
			}

			PostSplit(currentEntity, currentRegion, newEntity, newRegion);
		}

		private void PostSplit(IAdminEntity currentEntity, Region currentRegion, IAdminEntity newEntity,
			Region newRegion)
		{
			_map.RegionSanitize(currentRegion);
			SmoothRegion(currentRegion);
			_map.RegionSanitize(newRegion);
			SmoothRegion(newRegion);
			newEntity.regions.Add(newRegion);

			var idx = EntityAdd(newEntity);

			// Refresh old entity and selects the new
			if (currentEntity is Country)
			{
				_map.RefreshCountryGeometry((Country)newEntity);
				_map.RefreshCountryDefinition(countryIndex, highlightedRegions);
				SplitCities(countryIndex, newRegion);
				countryIndex = idx;
				countryRegionIndex = 0;
				CountryRegionSelect();
				countryChanges = true;
				cityChanges = true;
			}
			else
			{
				_map.RefreshProvinceGeometry(idx);
				_map.RefreshProvinceDefinition(provinceIndex, true);
				provinceIndex = idx;
				provinceRegionIndex = 0;
				countryIndex = map.provinces[provinceIndex].countryIndex;
				countryRegionIndex = map.countries[countryIndex].mainRegionIndex;
				CountryRegionSelect();
				ProvinceRegionSelect();
				provinceChanges = true;
			}

			// Refresh lines
			highlightedRegions.Add(newRegion);

			map.Redraw(true);
		}

		public void SplitVertically()
		{
			var currentRegion = selectedRegion;
			if (currentRegion == null)
				return;

			var currentEntity = currentRegion.entity;
			var center = currentRegion.center;
			var half1 = new List<Vector2>();
			var half2 = new List<Vector2>();
			var prevSide = 0;
			for (var k = 0; k < currentRegion.points.Length; k++)
			{
				Vector3 p = currentRegion.points[k];
				if (p.x > currentRegion.center.x)
				{
					half1.Add(p);
					if (prevSide == -1)
						half2.Add(p);
					prevSide = 1;
				}
				if (p.x <= currentRegion.center.x)
				{
					half2.Add(p);
					if (prevSide == 1)
						half1.Add(p);
					prevSide = -1;
				}
			}

			// Setup new entity
			IAdminEntity newEntity;
			if (currentEntity is Country)
			{
				var uniqueName = GetCountryUniqueName("New " + currentEntity.name);
				newEntity = new Country(uniqueName, ((Country)currentEntity).continent,
					map.GetUniqueId(new List<IExtendableAttribute>(map.countries)));
			}
			else
			{
				var uniqueName = GetProvinceUniqueName("New " + currentEntity.name);
				newEntity = new Province(uniqueName, ((Province)currentEntity).countryIndex,
					map.GetUniqueId(new List<IExtendableAttribute>(map.provinces)));
				newEntity.regions = new List<Region>();
			}

			// Update polygons
			var newRegion = new Region(newEntity, 0);
			if (map.countries[countryIndex].center.x > center.x)
			{
				currentRegion.points = half1.ToArray();
				newRegion.points = half2.ToArray();
			}
			else
			{
				currentRegion.points = half2.ToArray();
				newRegion.points = half1.ToArray();
			}
			newEntity.regions.Add(newRegion);

			PostSplit(currentEntity, currentRegion, newEntity, newRegion);
		}

		/// <summary>
		/// Adds the new point to currently selected region.
		/// </summary>
		public void AddPointToRegion(Vector2 newPoint)
		{
			var region = selectedRegion;
			if (region == null)
				return;
			var minDist = float.MaxValue;
			int nearest = -1, previous = -1;
			var max = region.points.Length;
			for (var p = 0; p < max; p++)
			{
				var q = p == 0 ? max - 1 : p - 1;
				Vector3 rp = (region.points[p] + region.points[q]) * 0.5f;
				var dist = (rp.x - newPoint.x) * (rp.x - newPoint.x) * 4 +
				           (rp.y - newPoint.y) * (rp.y - newPoint.y);
				if (dist < minDist)
				{
					// Get nearest point
					minDist = dist;
					nearest = p;
					previous = q;
				}
			}

			if (nearest >= 0)
			{
				var pointToInsert = (region.points[nearest] + region.points[previous]) * 0.5f;

				// Check if nearest and previous exists in any neighbour
				int nearest2 = -1, previous2 = -1;
				for (var n = 0; n < region.neighbours.Count; n++)
				{
					var nregion = region.neighbours[n];
					for (var p = 0; p < nregion.points.Length; p++)
					{
						if (nregion.points[p] == region.points[nearest])
							nearest2 = p;
						if (nregion.points[p] == region.points[previous])
							previous2 = p;
					}
					if (nearest2 >= 0 && previous2 >= 0)
					{
						nregion.points = InsertPoint(nregion.points, previous2, pointToInsert);
						break;
					}
				}

				// Insert the point in the current region (must be done after inserting in the neighbour so nearest/previous don't unsync)
				region.points = InsertPoint(region.points, nearest, pointToInsert);
			}
		}

		private Vector2[] InsertPoint(Vector2[] pointArray, int index, Vector2 pointToInsert)
		{
			var temp = new List<Vector2>(pointArray.Length + 1);
			for (var k = 0; k < pointArray.Length; k++)
			{
				if (k == index)
					temp.Add(pointToInsert);
				temp.Add(pointArray[k]);
			}
			return temp.ToArray();
		}

		/// <summary>
		/// Adds a new point to current shape. This function ensure points is no repeated.
		/// </summary>
		/// <returns><c>true</c>, if point was added, <c>false</c> otherwise.</returns>
		/// <param name="point">Point.</param>
		private bool AddPointToShape(Vector2 newPoint)
		{
			var count = newShape.Count;
			for (var k = 0; k < count; k++)
			{
				var p = newShape[k];
				var dist = (p - newPoint).sqrMagnitude;
				if (dist < POINT_SQR_PRECISION)
					return false;
			}
			newShape.Add(newPoint);
			return true;
		}

		/// <summary>
		/// Add new points to shape. If one of the new points equals to first point in shape it stops there and return true. Returns false otherwise.
		/// </summary>
		private bool AddPointsToShape(List<Vector2> newPoints)
		{
			var newPointsCount = newPoints.Count;
			var newShapeCount = newShape.Count;
			var canCloseShape = false;
			for (var k = 0; k < newPointsCount; k++)
			{
				var newPoint = newPoints[k];
				if (newShapeCount > 0)
				{
					// Check if the last point closes the polygon
					var dist = (newShape[0] - newPoint).sqrMagnitude;
					if (dist < POINT_SQR_PRECISION)
						canCloseShape = true;
					AddPointToShape(newPoint);
				}
			}
			return canCloseShape;
		}

		public void SetInfoMsg(string msg)
		{
			infoMsg = msg;
			infoMsgStartTime = DateTime.Now;
		}

		/// <summary>
		/// Returns the nearest vertex belonging to another country or province
		/// </summary>
		public bool GetNearestVertex(Vector2 mapPos, out Vector2 nearPoint, out Region existingRegion,
			out int pointIndex)
		{
			existingRegion = null;
			pointIndex = 0;

			// Iterate country regions
			var numCountries = _map.countries.Length;
			var np = mapPos;
			var minDist = float.MaxValue;
			// Countries
			for (var c = 0; c < numCountries; c++)
			{
				var country = _map.countries[c];
				var regCount = country.regions.Count;
				for (var cr = 0; cr < regCount; cr++)
				{
					var region = country.regions[cr];
					var pointCount = region.points.Length;
					for (var p = 0; p < pointCount; p++)
					{
						var dist = (mapPos - region.points[p]).sqrMagnitude;
						if (dist < minDist)
						{
							minDist = dist;
							np = region.points[p];
							existingRegion = region;
							pointIndex = p;
						}
					}
				}
			}
			// Provinces
			if (_map.editor.editingMode == EDITING_MODE.PROVINCES)
			{
				var numProvinces = _map.provinces.Length;
				for (var p = 0; p < numProvinces; p++)
				{
					var province = _map.provinces[p];
					if (province.regions == null)
						_map.ReadProvincePackedString(province);
					if (province.regions == null)
						continue;
					var regCount = province.regions.Count;
					for (var pr = 0; pr < regCount; pr++)
					{
						var region = province.regions[pr];
						var pointCount = region.points.Length;
						for (var po = 0; po < pointCount; po++)
						{
							var dist = (mapPos - region.points[po]).sqrMagnitude;
							if (dist < minDist)
							{
								minDist = dist;
								np = region.points[po];
								existingRegion = region;
								pointIndex = po;
							}
						}
					}
				}
			}

			nearPoint = np;
			return nearPoint != mapPos;
		}

		/// <summary>
		/// Returns the nearest vertex belonging to another country or province
		/// </summary>
		public void GetNearestVertex(Vector2 mapPos, Region region, out Vector2 nearPoint,
			out int pointIndex)
		{
			pointIndex = 0;
			nearPoint = mapPos;
			var minDist = float.MaxValue;

			var pointCount = region.points.Length;
			for (var p = 0; p < pointCount; p++)
			{
				var dist = (mapPos - region.points[p]).sqrMagnitude;
				if (dist < minDist)
				{
					minDist = dist;
					nearPoint = region.points[p];
					pointIndex = p;
				}
			}
		}

		/// <summary>
		/// Snaps mapPos to a border of the map
		/// </summary>
		/// <returns><c>true</c>, if map border near sphere position was gotten, <c>false</c> otherwise.</returns>
		/// <param name="mapPos">Map position.</param>
		/// <param name="borderPoint">Border point.</param>
		public bool GetMapBorderNearPos(Vector2 mapPos, out Vector2 borderPoint)
		{
			borderPoint = mapPos;
			if (borderPoint.x < -0.49f)
			{
				borderPoint.x = -0.5f;
				return true;
			}
			else if (borderPoint.x > 0.49f)
			{
				borderPoint.x = 0.5f;
				return true;
			}
			else if (borderPoint.y > 0.49f)
			{
				borderPoint.y = 0.5f;
				return true;
			}
			else if (borderPoint.y < -0.49f)
			{
				borderPoint.y = -0.5f;
				return true;
			}
			return false;
		}

		/// <summary>
		/// Adds a partial contour to current shape starting at point0 and ending at point1
		/// Returns true if shape is closed
		/// </summary>
		public bool AddNearestContour(Vector2 point0, Vector2 point1)
		{
			var autoContourCW = GetNearestContour(point0, point1, 1);
			var autoContourACW = GetNearestContour(point0, point1, -1);
			if (autoContourCW.Count > autoContourACW.Count)
				autoContourCW = autoContourACW;
			if (autoContourCW.Count == 0)
				return false;
			return AddPointsToShape(autoContourCW);
		}

		private List<Vector2> GetNearestContour(Vector2 point0, Vector2 point1, int direction)
		{
			var contour = new List<Vector2>(100);
			// Iterate country regions
			var numCountries = _map.countries.Length;
			// Try countries
			var contourWithinCurrentCountry = countryIndex >= 0 &&
			                                  (createMode == CREATE_TOOL.PROVINCE ||
			                                   createMode == CREATE_TOOL.PROVINCE_REGION);
			for (var c = 0; c < numCountries; c++)
			{
				if (contourWithinCurrentCountry)
					c = countryIndex;
				var country = _map.countries[c];
				var regCount = country.regions.Count;
				for (var cr = 0; cr < regCount; cr++)
				{
					var region = country.regions[cr];
					var pointCount = region.points.Length;
					for (var p = 0; p < pointCount; p++)
					{
						// Look for point0
						var dist = (point0 - region.points[p]).sqrMagnitude;
						if (dist < POINT_SQR_PRECISION)
						{
							// Look for point1
							var r0 = p + direction;
							for (var q = 0; q < pointCount; q++, r0 += direction)
							{
								var r = (r0 + pointCount) % pointCount;
								if (r == p)
									break;
								contour.Add(region.points[r]);
								dist = (point1 - region.points[r]).sqrMagnitude;
								if (dist < POINT_SQR_PRECISION)
									return contour;
							}
						}
					}
				}
				if (contourWithinCurrentCountry)
					break;
			}
			contour.Clear();
			// Try provinces
			if (_map.editor.editingMode == EDITING_MODE.PROVINCES)
			{
				var numProvinces = _map.provinces.Length;
				for (var p = 0; p < numProvinces; p++)
				{
					var province = _map.provinces[p];
					if (province.regions == null)
						_map.ReadProvincePackedString(province);
					if (province.regions == null)
						continue;
					var regCount = province.regions.Count;
					for (var pr = 0; pr < regCount; pr++)
					{
						var region = province.regions[pr];
						var pointCount = region.points.Length;
						for (var po = 0; po < pointCount; po++)
						{
							// Look for point0
							var dist = (point0 - region.points[po]).sqrMagnitude;
							if (dist < POINT_SQR_PRECISION)
							{
								// Look for point1
								var r0 = po + direction;
								for (var q = 0; q < pointCount; q++, r0 += direction)
								{
									var r = (r0 + pointCount) % pointCount;
									if (r == po)
										break;
									contour.Add(region.points[r]);
									dist = (point1 - region.points[r]).sqrMagnitude;
									if (dist < POINT_SQR_PRECISION)
										return contour;
								}
							}
						}
					}
				}
			}
			return contour;
		}

		#endregion

		#region Country and Province misc functions

		private string GetCountryUniqueName(string proposedName)
		{
			var goodName = proposedName;
			var suffix = 0;

			while (_map.GetCountryIndex(goodName) >= 0)
			{
				suffix++;
				goodName = proposedName + suffix.ToString();
			}
			return goodName;
		}

		private bool isProvinceNameUsed(string name)
		{
			if (_map.provinces == null)
				return false;

			for (var k = 0; k < _map.provinces.Length; k++)
				if (_map.provinces[k].name.Equals(name))
					return true;
			return false;
		}

		private string GetProvinceUniqueName(string proposedName)
		{
			var goodName = proposedName;
			var suffix = 0;

			while (isProvinceNameUsed(goodName))
			{
				suffix++;
				goodName = proposedName + suffix.ToString();
			}
			return goodName;
		}

		#endregion

		#region Region operations

		/// <summary>
		/// Divides the region beneath the new shape in two
		/// </summary>
		public bool RegionDivide()
		{
			if (newShape == null ||
			    newShape.Count < 2 ||
			    newShapeSegmentRegion == null ||
			    newShapeSegmentRegion.points == null)
				return false;

			// Get nearest vertex to latest point
			var numPoints = newShape.Count;
			var lastPoint = newShape[numPoints - 1];
			Vector2 regionLastPoint;
			Region region;
			int regionPointIndex;
			if (!GetNearestVertex(lastPoint, out regionLastPoint, out region, out regionPointIndex))
				return false;
			if (regionLastPoint != lastPoint)
				newShape.Add(regionLastPoint);

			// Locate region under shape (used the middle point)
			var midPoint = newShape[numPoints / 2];
			if (createMode == CREATE_TOOL.PROVINCE || createMode == CREATE_TOOL.PROVINCE_REGION)
				region = _map.GetProvinceRegion(midPoint);
			else
				region = _map.GetCountryRegion(midPoint);
			if (region == null)
				return false;

			// Locate first and end point in region
			Vector2 firstPoint;
			var firstIndex = -1;
			var lastIndex = -1;
			GetNearestVertex(newShape[0], region, out firstPoint, out firstIndex);
			if (newShape[0] != firstPoint)
				newShape.Insert(0, firstPoint);
			GetNearestVertex(lastPoint, region, out lastPoint, out lastIndex);
			if (regionLastPoint != lastPoint)
				newShape[newShape.Count - 1] = lastPoint;

			// Split region in two
			List<Vector2> onePart, secondPart, currentPart;
			onePart = new List<Vector2>();
			secondPart = new List<Vector2>();
			currentPart = onePart;
			var regionPointCount = region.points.Length;
			for (var k = 0; k < regionPointCount; k++)
			{
				currentPart.Add(region.points[firstIndex]);
				firstIndex++;
				if (firstIndex >= regionPointCount)
					firstIndex = 0;
				if (lastIndex == firstIndex)
				{
					onePart.Add(region.points[lastIndex]);
					currentPart = secondPart;
				}
			}
			secondPart.Add(region.points[firstIndex]);

			// Add new shape to first and second part
			newShape.Reverse();
			onePart.AddRange(newShape);
			region.UpdatePointsAndRect(onePart);
			_map.RegionSanitize(region);

//			if (newShape [0] != lastPoint) {
			newShape.Reverse();
//			}
			secondPart.AddRange(newShape);

			var newRegion = new Region(region.entity, region.entity.regions.Count);
			newRegion.UpdatePointsAndRect(secondPart);
			_map.RegionSanitize(newRegion);
			region.entity.regions.Add(newRegion);

			if (region.entity is Province)
				provinceChanges = true;
			else
				countryChanges = true;

			newShape.Clear();

			RedrawFrontiers(null, true);

			return true;
		}

		#endregion
	}
}