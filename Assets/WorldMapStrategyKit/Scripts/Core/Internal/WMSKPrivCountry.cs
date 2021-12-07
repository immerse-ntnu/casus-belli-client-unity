// World Map Strategy Kit for Unity - Main Script
// (C) 2016-2020 by Ramiro Oliva (Kronnect)
// Don't modify this script - changes could be lost if you upgrade to a more recent version of WMSK

using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using WorldMapStrategyKit.Poly2Tri;
using WorldMapStrategyKit.ClipperLib;

namespace WorldMapStrategyKit
{
	public partial class WMSK : MonoBehaviour
	{
		private delegate void TestNeighbourSegment(Region region, int i, int j);

		private const string COUNTRY_OUTLINE_GAMEOBJECT_NAME = "countryOutline";
		private const string COUNTRY_SURFACES_ROOT_NAME = "surfaces";
		private const string COUNTRY_ATTRIB_DEFAULT_FILENAME = "countriesAttrib";

		/// <summary>
		/// Country look up dictionary. Used internally for fast searching of country names.
		/// </summary>
		private Dictionary<string, int> _countryLookup;

		private List<int> _countriesOrderedBySize;

		private int _lastCountryLookupCount = -1;

		private int lastCountryLookupCount
		{
			get => _lastCountryLookupCount;
			set
			{
				if (value == -1)
					RefreshCountryLookUp();
				else
					_lastCountryLookupCount = value;
			}
		}

		private Dictionary<string, int> countryLookup
		{
			get
			{
				if (_countries == null)
					return _countryLookup;
				if (_countryLookup != null && _countries.Length == _lastCountryLookupCount)
					return _countryLookup;
				RefreshCountryLookUp();
				return _countryLookup;
			}
		}

		private List<int> countriesOrderedBySize
		{
			get
			{
				if (_lastCountryLookupCount == -1)
					RefreshCountryLookUp();
				return _countriesOrderedBySize;
			}
		}

		// resources
		private Material frontiersMat, hudMatCountry;

		// gameObjects
		private GameObject countryRegionHighlightedObj;
		private GameObject frontiersLayer;

		// maintains a reference to the country outline to hide it when zooming too much
		private GameObject lastCountryOutlineRef;

		// caché and gameObject lifetime control
		private Vector3[][] frontiers;
		private bool needOptimizeFrontiers = true;
		private int[][] frontiersIndices;

		/// <summary>
		/// Must be called internally when country list is changed (ie: a country has been deleted or added)
		/// </summary>
		private void RefreshCountryLookUp()
		{
			if (_countries != null && _countries.Length > 0 && _countries[0] != null)
			{
				// Build dictionary for fast country object look up
				// Also build ordered index list of countries for allowing to highlight countries surrounded by other greater countries (smaller countries checked first).
				var countryCount = _countries.Length;
				if (_countryLookup == null)
					_countryLookup = new Dictionary<string, int>(countryCount);
				else
					_countryLookup.Clear();
				if (_countriesOrderedBySize == null)
					_countriesOrderedBySize = new List<int>(countryCount);
				else
					_countriesOrderedBySize.Clear();
				for (var k = 0; k < countryCount; k++)
				{
					var c = _countries[k];
					_countryLookup.Add(c.name, k);
					if (c.regions != null && c.mainRegionIndex >= 0 && c.mainRegionIndex < c.regions.Count)
						_countriesOrderedBySize.Add(k);
				}

				// Sort countries based on size
				_countriesOrderedBySize.Sort((int cIndex1, int cIndex2) =>
				{
					var c1 = _countries[cIndex1];
					var r1 = c1.regions[c1.mainRegionIndex];
					var c2 = _countries[cIndex2];
					var r2 = c2.regions[c2.mainRegionIndex];
					if (r1.rect2DArea < r2.rect2DArea)
						return -1;
					else if (r1.rect2DArea > r2.rect2DArea)
						return 1;
					else
						return 0;
				});
			}
			else
			{
				_countryLookup = new Dictionary<string, int>(250);
				_countriesOrderedBySize = new List<int>(250);
			}
			_lastCountryLookupCount = _countryLookup.Count;
		}

		private void ReadCountriesPackedString()
		{
			var frontiersFileName = _geodataResourcesPath +
			                        (_frontiersDetail == FRONTIERS_DETAIL.Low
				                        ? "/countries110"
				                        : "/countries10");
			var ta = Resources.Load<TextAsset>(frontiersFileName);
			if (ta != null)
			{
				SetCountryGeoData(ta.text);
				ReloadCountryAttributes();
			}
		}

		private void ReloadCountryAttributes()
		{
			var ta = Resources.Load<TextAsset>(_geodataResourcesPath + "/" + _countryAttributeFile);
			if (ta == null)
				return;
			SetCountriesAttributes(ta.text);
		}

		/// <summary>
		/// Computes surfaces for big countries
		/// </summary>
		private void CountriesPrewarmBigSurfaces()
		{
			for (var k = 0; k < _countries.Length; k++)
			{
				var points = _countries[k].regions[_countries[k].mainRegionIndex].points.Length;
				if (points > 6000)
				{
					ToggleCountrySurface(k, true, Misc.ColorClear);
					ToggleCountrySurface(k, false, Misc.ColorClear);
				}
			}
		}

		/// <summary>
		/// Used internally by the Map Editor. It will recalculate de boundaries and optimize frontiers based on new data of countries array
		/// Note: this not redraws the map. Redraw must be called afterwards.
		/// </summary>
		public void RefreshCountryDefinition(int countryIndex, List<Region> filterRegions)
		{
			if (countryIndex < 0 || countryIndex >= _countries.Length)
				return;
			var country = _countries[countryIndex];
			RefreshCountryGeometry(country);
			OptimizeFrontiers(filterRegions);
		}

		/// <summary>
		/// Used internally by the Map Editor. It will recalculate de boundaries and optimize frontiers based on new data of countries array
		/// </summary>
		public void RefreshCountryGeometry(IAdminEntity country)
		{
			float maxVol = 0;
			if (country.regions == null)
				return;
			var regionCount = country.regions.Count;
			var min = Misc.Vector2one * 10;
			var max = -min;
			var minCountry = Misc.Vector2one * 10;
			var maxCountry = -minCountry;
			for (var r = 0; r < regionCount; r++)
			{
				var countryRegion = country.regions[r];
				if (countryRegion.points == null)
					continue;
				countryRegion.entity = country; // just in case one country has been deleted
				countryRegion.regionIndex = r; // just in case a region has been deleted
				var coorCount = countryRegion.points.Length;
				min.x = min.y = 10f;
				max.x = max.y = -10;
				for (var c = 0; c < coorCount; c++)
				{
					var x = countryRegion.points[c].x;
					var y = countryRegion.points[c].y;
					if (x < min.x)
						min.x = x;
					if (x > max.x)
						max.x = x;
					if (y < min.y)
						min.y = y;
					if (y > max.y)
						max.y = y;
				}
				FastVector.Average(ref min, ref max,
					ref countryRegion.center); // countryRegion.center = (min + max) * 0.5f;

				// Calculate country bounding rect
				if (min.x < minCountry.x)
					minCountry.x = min.x;
				if (min.y < minCountry.y)
					minCountry.y = min.y;
				if (max.x > maxCountry.x)
					maxCountry.x = max.x;
				if (max.y > maxCountry.y)
					maxCountry.y = max.y;
				// Calculate bounding rect
				countryRegion.rect2D = new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
				countryRegion.rect2DArea = countryRegion.rect2D.width * countryRegion.rect2D.height;
				var vol = FastVector.SqrDistance(ref max, ref min); // (max - min).sqrMagnitude;
				if (vol > maxVol)
				{
					maxVol = vol;
					country.mainRegionIndex = r;
					country.center = countryRegion.center;
				}
			}
			country.regionsRect2D = new Rect(minCountry.x, minCountry.y,
				Math.Abs(maxCountry.x - minCountry.x), Mathf.Abs(maxCountry.y - minCountry.y));
			lastCountryLookupCount = -1;
		}

		private void TestNeighbourSegmentAny(Region region, int i, int j)
		{
			double v = region.points[i].x +
			           region.points[j].x +
			           MAP_PRECISION * (region.points[i].y + region.points[j].y);
			Region neighbour;
			if (frontiersCacheHit.TryGetValue(v, out neighbour))
			{
				// add neighbour references
				if (neighbour != region)
					if (!region.neighbours.Contains(neighbour))
					{
						region.neighbours.Add(neighbour);
						neighbour.neighbours.Add(region);
					}
			}
			else
			{
				frontiersCacheHit[v] = region;
				frontiersPoints.Add(region.points[i]);
				frontiersPoints.Add(region.points[j]);
			}
		}

		private void TestNeighbourSegmentInland(Region region, int i, int j)
		{
			double v = region.points[i].x +
			           region.points[j].x +
			           MAP_PRECISION * (region.points[i].y + region.points[j].y);
			Region neighbour;
			if (frontiersCacheHit.TryGetValue(v, out neighbour))
			{
				// add neighbour references
				if (neighbour != region)
				{
					if (!region.neighbours.Contains(neighbour))
					{
						region.neighbours.Add(neighbour);
						neighbour.neighbours.Add(region);
					}
					frontiersPoints.Add(region.points[i]);
					frontiersPoints.Add(region.points[j]);
				}
			}
			else
				frontiersCacheHit[v] = region;
		}

		/// <summary>
		/// Prepare and cache meshes for frontiers. Used internally by extra components (decorator). This is called just after loading data or when hidding a country.
		/// </summary>
		public void OptimizeFrontiers()
		{
			OptimizeFrontiers(null);
		}

		private void OptimizeFrontiers(List<Region> filterRegions)
		{
			if (frontiersPoints == null)
				frontiersPoints = new List<Vector2>(1000000); // needed for high-def resolution map
			else
				frontiersPoints.Clear();
			if (frontiersCacheHit == null)
				frontiersCacheHit =
					new Dictionary<double, Region>(500000); // needed for high-resolution map
			else
				frontiersCacheHit.Clear();

			for (var k = 0; k < _countries.Length; k++)
			{
				var country = _countries[k];
				var crCount = country.regions.Count;
				for (var r = 0; r < crCount; r++)
				{
					var region = country.regions[r];
					if (filterRegions == null || filterRegions.Contains(region))
					{
						region.entity = country;
						region.regionIndex = r;
						region.neighbours.Clear();
					}
				}
			}

			// Find neighbours by common frontiers
			TestNeighbourSegment testFunction;
			if (_frontiersCoastlines)
				testFunction = TestNeighbourSegmentAny;
			else
				testFunction = TestNeighbourSegmentInland;

			for (var k = 0; k < _countries.Length; k++)
			{
				var country = _countries[k];
				if (country.hidden)
					continue;
				var crCount = country.regions.Count;
				for (var r = 0; r < crCount; r++)
				{
					var region = country.regions[r];
					if (region.points == null || region.points.Length == 0)
						continue;
					if (filterRegions == null || filterRegions.Contains(region))
					{
						var numPoints = region.points.Length - 1;
						for (var i = 0; i < numPoints; i++)
							testFunction(region, i, i + 1);
						// Close the polygon
						testFunction(region, numPoints, 0);
					}
				}
			}

			// If frontier coastlines is disabled, make sure enclaves are also visible (countries without neighbours found)
			if (!_frontiersCoastlines)
				for (var k = 0; k < _countries.Length; k++)
				{
					var country = _countries[k];
					if (country.hidden || country.neighbours.Length != 0)
						continue;
					var crCount = country.regions.Count;
					for (var r = 0; r < crCount; r++)
					{
						var region = country.regions[r];
						if (region.points == null || region.points.Length == 0)
							continue;
						if (filterRegions == null || filterRegions.Contains(region))
						{
							var numPoints = region.points.Length - 1;
							for (var i = 0; i < numPoints; i++)
							{
								frontiersPoints.Add(region.points[i]);
								frontiersPoints.Add(region.points[i + 1]);
							}
							// Close the polygon
							frontiersPoints.Add(region.points[numPoints]);
							frontiersPoints.Add(region.points[0]);
						}
					}
				}

			var meshGroups = frontiersPoints.Count / 65000 + 1;
			var meshIndex = -1;
			frontiersIndices = new int[meshGroups][];
			frontiers = new Vector3[meshGroups][];
			for (var k = 0; k < frontiersPoints.Count; k += 65000)
			{
				var max = Mathf.Min(frontiersPoints.Count - k, 65000);
				frontiers[++meshIndex] = new Vector3[max];
				frontiersIndices[meshIndex] = new int[max];
				for (var j = k; j < k + max; j++)
				{
					frontiers[meshIndex][j - k].x = frontiersPoints[j].x;
					frontiers[meshIndex][j - k].y = frontiersPoints[j].y;
					frontiersIndices[meshIndex][j - k] = j - k;
				}
			}
		}

		private void ResortCountryProvinces(int countryIndex)
		{
			var provinces = new List<Province>(_countries[countryIndex].provinces);
			provinces.Sort(ProvinceSizeComparer);
			_countries[countryIndex].provinces = provinces.ToArray();
		}

		#region Drawing stuff

		private const string FRONTIERS_MULTIPASS_SHADER = "WMSK/Unlit Country Frontiers Order 3";
		private const string FRONTIERS_GEOMETRIC_SHADER = "WMSK/Unlit Country Frontiers Geom";

		private void UpdateFrontiersMaterial()
		{
			if (frontiersMat == null)
				return;
			var actualShader = frontiersMat.shader;
			if (actualShader == null)
				return;
			var shaderIsGeometry = actualShader.name.Equals(FRONTIERS_GEOMETRIC_SHADER);
			if (_thickerFrontiers && !shaderIsGeometry)
				frontiersMat.shader = Shader.Find(FRONTIERS_GEOMETRIC_SHADER);
			else if (!thickerFrontiers && shaderIsGeometry)
				frontiersMat.shader = Shader.Find(FRONTIERS_MULTIPASS_SHADER);
			frontiersMat.color = _frontiersColor;
			frontiersMat.SetColor("_OuterColor", frontiersColorOuter);
			frontiersMat.SetFloat("_Thickness", _frontiersWidth);
			UpdateShadersLOD();
			shouldCheckBoundaries = true;
		}

		private int GetCacheIndexForCountryRegion(int countryIndex, int regionIndex) =>
			countryIndex * 1000 + regionIndex;

		private void DrawFrontiers()
		{
			if (!gameObject.activeInHierarchy)
				return;
			if (!_showFrontiers)
				return;

			// Create frontiers layer
			var t = transform.Find("Frontiers");
			if (t != null)
				DestroyRecursive(t.gameObject);

			if (needOptimizeFrontiers)
				OptimizeFrontiers(); // lazy optimization

			frontiersLayer = new GameObject("Frontiers");
			if (disposalManager != null)
				disposalManager
					.MarkForDisposal(frontiersLayer); // frontiersLayer.hideFlags = HideFlags.DontSave;
			frontiersLayer.transform.SetParent(transform, false);
			frontiersLayer.transform.localPosition = Misc.Vector3zero;
			frontiersLayer.transform.localRotation = Quaternion.Euler(Misc.Vector3zero);
			frontiersLayer.layer = gameObject.layer;

			if (frontiers != null)
				for (var k = 0; k < frontiers.Length; k++)
				{
					var flayer = new GameObject("flayer");
					if (disposalManager != null)
						disposalManager.MarkForDisposal(flayer); // flayer.hideFlags = HideFlags.DontSave;
					flayer.hideFlags |= HideFlags.HideInHierarchy;
					flayer.transform.SetParent(frontiersLayer.transform, false);
					flayer.transform.localPosition = Misc.Vector3zero;
					flayer.transform.localRotation = Quaternion.Euler(Misc.Vector3zero);
					flayer.layer = frontiersLayer.layer;

					var mesh = new Mesh();
					mesh.vertices = frontiers[k];
					mesh.SetIndices(frontiersIndices[k], MeshTopology.Lines, 0);
					mesh.RecalculateBounds();
					if (disposalManager != null)
						disposalManager.MarkForDisposal(mesh); //mesh.hideFlags = HideFlags.DontSave;

					var mf = flayer.AddComponent<MeshFilter>();
					mf.sharedMesh = mesh;

					var mr = flayer.AddComponent<MeshRenderer>();
					mr.receiveShadows = false;
					mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
					mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
					mr.sharedMaterial = frontiersMat;
				}

			// Toggle frontiers visibility layer according to settings
			frontiersLayer.SetActive(_showFrontiers);
		}

		#endregion

		#region Country highlighting

		private void HideCountryRegionHighlight()
		{
			HideProvinceRegionHighlight();
			HideCityHighlight();

			if (_provinceLabelsVisibility == PROVINCE_LABELS_VISIBILITY.Automatic)
				DestroyProvinceLabels();

			if (_countryHighlightedIndex >= 0 && _countryRegionHighlightedIndex >= 0)
			{
				if (_countryHighlighted != null)
				{
					var rCount = _countryHighlighted.regions.Count;
					for (var k = 0; k < rCount; k++)
						HideCountryRegionHighlightSingle(k);
				}
				countryRegionHighlightedObj = null;

				// Raise exit event
				if (OnCountryExit != null)
					OnCountryExit(_countryHighlightedIndex, _countryRegionHighlightedIndex);
				if (OnRegionExit != null)
					OnRegionExit(_countries[_countryHighlightedIndex]
						.regions[_countryRegionHighlightedIndex]);
			}

			_countryHighlighted = null;
			_countryHighlightedIndex = -1;
			_countryRegionHighlighted = null;
			_countryRegionHighlightedIndex = -1;
		}

		private void HideCountryRegionHighlightSingle(int regionIndex)
		{
			var cacheIndex = GetCacheIndexForCountryRegion(_countryHighlightedIndex, regionIndex);
			var region = _countryHighlighted.regions[regionIndex];
			GameObject surf = null;
			surfaces.TryGetValue(cacheIndex, out surf);
			if (surf == null)
				surfaces.Remove(cacheIndex);
			if (surf != null)
			{
				var mat = region.customMaterial;
				if (mat != null)
					ApplyMaterialToSurface(surf, mat);
				else
					surf.SetActive(false);
			}
			HideCountryRegionOutline(cacheIndex.ToString(), region);
			lastCountryOutlineRef = null;
		}

		private void HideCountryRegionOutline(string entityId, Region region)
		{
			// Hides country outline
			if (region.customBorderTexture == null)
				HideRegionObject(entityId, null, COUNTRY_OUTLINE_GAMEOBJECT_NAME);
			else
			{
				// Stop animation?
				if (region.customBorderAnimationSpeed == 0)
				{
					var t = surfacesLayer.transform.Find(entityId + "/" + COUNTRY_OUTLINE_GAMEOBJECT_NAME);
					if (t != null)
					{
						var mat = t.GetComponent<LineRenderer>().sharedMaterial;
						mat.SetFloat("_AnimationSpeed", 0);
						region.customBorderAnimationAcumOffset +=
							(time - region.customBorderAnimationStartTime) * _outlineAnimationSpeed;
						mat.SetFloat("_AnimationAcumOffset", region.customBorderAnimationAcumOffset);
					}
				}
			}
		}

		/// <summary>
		/// Disables all country regions highlights. This doesn't remove custom materials.
		/// </summary>
		public void HideCountryRegionHighlights(bool destroyCachedSurfaces)
		{
			HideCountryRegionHighlight();
			if (_countries == null)
				return;
			for (var c = 0; c < _countries.Length; c++)
			{
				var country = _countries[c];
				for (var cr = 0; cr < country.regions.Count; cr++)
				{
					var region = country.regions[cr];
					var cacheIndex = GetCacheIndexForCountryRegion(c, cr);
					GameObject surf = null;
					if (surfaces.TryGetValue(cacheIndex, out surf))
					{
						if (surf == null)
							surfaces.Remove(cacheIndex);
						else
						{
							if (destroyCachedSurfaces)
							{
								surfaces.Remove(cacheIndex);
								DestroyImmediate(surf);
							}
							else
							{
								if (region.customMaterial == null)
									surf.SetActive(false);
								else
									ApplyMaterialToSurface(surf, region.customMaterial);
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// Highlights the country region specified. Returns the generated highlight surface gameObject.
		/// Internally used by the Map UI and the Editor component, but you can use it as well to temporarily mark a country region.
		/// </summary>
		/// <param name="refreshGeometry">Pass true only if you're sure you want to force refresh the geometry of the highlight (for instance, if the frontiers data has changed). If you're unsure, pass false.</param>
		public GameObject HighlightCountryRegion(int countryIndex, int regionIndex, bool refreshGeometry,
			bool drawOutline)
		{
			if (_countryHighlightedIndex == countryIndex &&
			    _countryRegionHighlightedIndex == regionIndex &&
			    !refreshGeometry)
				return countryRegionHighlightedObj;
			if (_countryHighlightedIndex >= 0) // hides both surface and outline
				HideCountryRegionHighlight();
			if (countryIndex < 0 ||
			    countryIndex >= _countries.Length ||
			    regionIndex < 0 ||
			    regionIndex >= _countries[countryIndex].regions.Count ||
			    _countries[countryIndex].isPool)
				return null;

			if (OnCountryHighlight != null)
			{
				var allowHighlight = true;
				OnCountryHighlight(countryIndex, regionIndex, ref allowHighlight);
				if (!allowHighlight)
					return null;
			}

			if (_enableCountryHighlight && _countries[countryIndex].allowHighlight)
			{
				countryRegionHighlightedObj = HighlightCountryRegionSingle(countryIndex, regionIndex,
					refreshGeometry, drawOutline);
				if (_highlightAllCountryRegions)
				{
					var country = _countries[countryIndex];
					var rCount = country.regions.Count;
					for (var r = 0; r < rCount; r++)
						if (r != regionIndex)
							HighlightCountryRegionSingle(countryIndex, r, refreshGeometry, drawOutline);
				}
			}

			_countryHighlightedIndex = countryIndex;
			_countryRegionHighlighted = _countries[countryIndex].regions[regionIndex];
			_countryRegionHighlightedIndex = regionIndex;
			_countryHighlighted = _countries[countryIndex];
			return countryRegionHighlightedObj;
		}

		private GameObject HighlightCountryRegionSingle(int countryIndex, int regionIndex,
			bool refreshGeometry, bool drawOutline)
		{
			var cacheIndex = GetCacheIndexForCountryRegion(countryIndex, regionIndex);

			// Draw outline?
			if (drawOutline)
				DrawCountryRegionOutline(cacheIndex.ToString(),
					_countries[countryIndex].regions[regionIndex], true, _outlineAnimationSpeed);

			GameObject obj;
			var existsInCache = surfaces.TryGetValue(cacheIndex, out obj);
			if (refreshGeometry && existsInCache)
			{
				surfaces.Remove(cacheIndex);
				DestroyImmediate(obj);
				existsInCache = false;
			}
			var doHighlight = true;
			if (_highlightMaxScreenAreaSize < 1f)
			{
				// Check screen area size
				var region = countries[countryIndex].regions[regionIndex];
				doHighlight = CheckScreenAreaSizeOfRegion(region);
			}
			if (doHighlight)
			{
				if (existsInCache)
				{
					obj = surfaces[cacheIndex];
					if (obj == null)
						surfaces.Remove(cacheIndex);
					else
					{
						if (!obj.activeSelf)
							obj.SetActive(true);
						var rr = obj.GetComponent<Renderer>();
						if (rr.sharedMaterial != hudMatCountry)
							rr.sharedMaterial = hudMatCountry;
						return obj;
					}
				}
				obj = GenerateCountryRegionSurface(countryIndex, regionIndex, hudMatCountry,
					Misc.Vector2one, Misc.Vector2zero, 0);
			}
			return obj;
		}

		private GameObject
			GenerateCountryRegionSurface(int countryIndex, int regionIndex, Material material) =>
			GenerateCountryRegionSurface(countryIndex, regionIndex, material, Misc.Vector2one,
				Misc.Vector2zero, 0);

		private void CountrySubstractProvinceEnclaves(int countryIndex, Region region, Polygon poly)
		{
			var negativeRegions = new List<Region>();
			for (var op = 0; op < _countries.Length; op++)
			{
				if (op == countryIndex)
					continue;
				var opCountry = _countries[op];
				if (opCountry.provinces == null)
					continue;
				if (opCountry.regionsRect2D.Overlaps(region.rect2D, true))
				{
					var provCount = opCountry.provinces.Length;
					for (var p = 0; p < provCount; p++)
					{
						var oProv = opCountry.provinces[p];
						if (oProv.regions == null)
							ReadProvincePackedString(oProv);
						if (oProv.regions == null)
							continue;
						if (oProv.mainRegionIndex < 0 || oProv.mainRegionIndex >= oProv.regions.Count)
							continue;
						var oProvRegion = oProv.regions[oProv.mainRegionIndex];
						if (region.Contains(
							oProvRegion)) // just check main region of province for speed purposes
							negativeRegions.Add(oProvRegion.Clone());
					}
				}
			}
			// Collapse negative regions in big holes
			for (var nr = 0; nr < negativeRegions.Count - 1; nr++)
			{
				for (var nr2 = nr + 1; nr2 < negativeRegions.Count; nr2++)
					if (negativeRegions[nr].Intersects(negativeRegions[nr2]))
					{
						var clipper = new Clipper();
						var control = negativeRegions[nr].points.Length;
						clipper.AddPath(negativeRegions[nr], PolyType.ptSubject);
						clipper.AddPath(negativeRegions[nr2], PolyType.ptClip);
						clipper.Execute(ClipType.ctUnion);
						negativeRegions.RemoveAt(nr2);
						nr = -1;
						break;
					}
			}

			// Substract holes
			for (var r = 0; r < negativeRegions.Count; r++)
			{
				var pointCount = negativeRegions[r].points.Length;
				var pp = new Vector2[pointCount];
				for (var p = 0; p < pointCount; p++)
				{
					var point = negativeRegions[r].points[p];
					var midPoint = negativeRegions[r].center;
					pp[p] = point +
					        (midPoint - point) *
					        0.0001f; // prevents Poly2Tri issues when enclave boarders are to near from region borders
				}
				var polyHole = new Polygon(pp);
				poly.AddHole(polyHole);
			}
		}

		private void CountrySubstractCountryEnclaves(int countryIndex, Region region, Polygon poly)
		{
			var negativeRegions = new List<Region>();
			var countryCount = countriesOrderedBySize.Count;
			for (var ops = 0; ops < countryCount; ops++)
			{
				var op = _countriesOrderedBySize[ops];
				if (op == countryIndex)
					continue;
				var opCountry = _countries[op];
				var opCountryRegion = opCountry.regions[opCountry.mainRegionIndex];
				if (opCountryRegion.points.Length >= 5 &&
				    opCountry.mainRegion.rect2DArea < region.rect2DArea &&
				    opCountryRegion.rect2D.Overlaps(region.rect2D, true))
					if (region.Contains(
						opCountryRegion)) // just check main region of province for speed purposes
						negativeRegions.Add(opCountryRegion.Clone());
			}
			// Collapse negative regions in big holes
			for (var nr = 0; nr < negativeRegions.Count - 1; nr++)
			{
				for (var nr2 = nr + 1; nr2 < negativeRegions.Count; nr2++)
					if (negativeRegions[nr].Intersects(negativeRegions[nr2]))
					{
						var clipper = new Clipper();
						var control = negativeRegions[nr].points.Length;
						clipper.AddPath(negativeRegions[nr], PolyType.ptSubject);
						clipper.AddPath(negativeRegions[nr2], PolyType.ptClip);
						clipper.Execute(ClipType.ctUnion);
						negativeRegions.RemoveAt(nr2);
						nr = -1;
						break;
					}
			}

			// Substract holes
			for (var r = 0; r < negativeRegions.Count; r++)
			{
				var polyHole = new Polygon(negativeRegions[r].points);
				poly.AddHole(polyHole);
			}
		}

		private GameObject DrawCountryRegionOutline(string entityId, Region region,
			bool overridesAnimationSpeed = false, float animationSpeed = 0f)
		{
			if (_showTiles && _currentZoomLevel > _tileLinesMaxZoomLevel)
				return null;

			var boldFrontiers = DrawRegionOutlineMesh(COUNTRY_OUTLINE_GAMEOBJECT_NAME, region,
				overridesAnimationSpeed, animationSpeed);
			ParentObjectToRegion(entityId, null, boldFrontiers);
			lastCountryOutlineRef = boldFrontiers;
			return boldFrontiers;
		}

		private GameObject GenerateCountryRegionSurface(int countryIndex, int regionIndex,
			Material material, Vector2 textureScale, Vector2 textureOffset, float textureRotation)
		{
			if (countryIndex < 0 || countryIndex >= _countries.Length)
				return null;
			var country = _countries[countryIndex];
			var region = country.regions[regionIndex];
			if (region.points.Length < 3)
				return null;

			var poly = new Polygon(region.points);
			// Extracts enclaves from main region
			if (_enableEnclaves && regionIndex == country.mainRegionIndex)
			{
				// Remove negative provinces
				if (_showProvinces)
					CountrySubstractProvinceEnclaves(countryIndex, region, poly);
				else
					CountrySubstractCountryEnclaves(countryIndex, region, poly);
			}
			P2T.Triangulate(poly);

			// Prepare surface cache entry and deletes older surface if exists
			var cacheIndex = GetCacheIndexForCountryRegion(countryIndex, regionIndex);
			var id = cacheIndex.ToString();

			// Creates surface mesh
			var surf = Drawing.CreateSurface(id, poly, material, region.rect2D, textureScale,
				textureOffset, textureRotation, disposalManager);
			ParentObjectToRegion(id, COUNTRY_SURFACES_ROOT_NAME, surf);
			surfaces[cacheIndex] = surf;

			return surf;
		}

		#endregion

		#region Country manipulation

		/// <summary>
		/// Deletes the country. Optionally also delete its dependencies (provinces, cities, mountpoints).
		/// This internal method does not refresh cachés.
		/// </summary>
		private bool internal_CountryDelete(int countryIndex, bool deleteDependencies)
		{
			if (countryIndex < 0 || countryIndex >= _countries.Length)
				return false;

			// Update dependencies
			if (deleteDependencies)
			{
				var newProvinces = new List<Province>(provinces.Length);
				int k;
				for (k = 0; k < provinces.Length; k++)
					if (provinces[k].countryIndex != countryIndex)
						newProvinces.Add(provinces[k]);
				provinces = newProvinces.ToArray();
				lastProvinceLookupCount = -1;

				k = -1;
				var cities = new List<City>(this.cities);
				while (++k < cities.Count)
					if (cities[k].countryIndex == countryIndex)
					{
						cities.RemoveAt(k);
						k--;
					}
				this.cities = cities.ToArray();
				lastCityLookupCount = -1;

				k = -1;
				while (++k < mountPoints.Count)
					if (mountPoints[k].countryIndex == countryIndex)
					{
						mountPoints.RemoveAt(k);
						k--;
					}
			}

			// Updates provinces reference to country
			for (var k = 0; k < provinces.Length; k++)
				if (provinces[k].countryIndex > countryIndex)
					provinces[k].countryIndex--;

			// Updates country index in cities
			for (var k = 0; k < cities.Length; k++)
				if (_cities[k].countryIndex > countryIndex)
					_cities[k].countryIndex--;
			// Updates country index in mount points
			if (mountPoints != null)
				for (var k = 0; k < mountPoints.Count; k++)
					if (mountPoints[k].countryIndex > countryIndex)
						mountPoints[k].countryIndex--;

			// Excludes country from new array
			var newCountries = new List<Country>(_countries.Length);
			for (var k = 0; k < _countries.Length; k++)
				if (k != countryIndex)
					newCountries.Add(_countries[k]);
			countries = newCountries.ToArray();
			return true;
		}

		#endregion
	}
}