// World Strategy Kit for Unity - Main Script
// (C) 2016-2020 by Ramiro Oliva (Kronnect)
// Don't modify this script - changes could be lost if you upgrade to a more recent version of WMSK

using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using WorldMapStrategyKit.ClipperLib;

namespace WorldMapStrategyKit
{
	public enum PROVINCE_LABELS_VISIBILITY
	{
		Automatic = 0,
		Scripting = 1
	}

	public delegate void OnProvinceEvent(int provinceIndex, int regionIndex);

	public delegate void OnProvinceClickEvent(int provinceIndex, int regionIndex, int buttonIndex);

	public delegate void OnProvinceHighlightEvent(int provinceIndex, int regionIndex,
		ref bool allowHighlight);

	public partial class WMSK : MonoBehaviour
	{
		#region Public properties

		private Province[] _provinces;

		/// <summary>
		/// Complete array of states and provinces and the country name they belong to.
		/// </summary>
		public Province[] provinces
		{
			get
			{
				if (_provinces == null)
					ReadProvincesPackedString();
				return _provinces;
			}
			set
			{
				_provinces = value;
				lastProvinceLookupCount = -1;
			}
		}

		private Province _provinceHighlighted;

		/// <summary>
		/// Returns Province under mouse position or null if none.
		/// </summary>
		public Province provinceHighlighted => _provinceHighlighted;

		private int _provinceHighlightedIndex = -1;

		/// <summary>
		/// Returns current highlighted province index.
		/// </summary>
		public int provinceHighlightedIndex => _provinceHighlightedIndex;

		private Region _provinceRegionHighlighted;

		/// <summary>
		/// Returns currently highlightd province's region.
		/// </summary>
		/// <value>The country region highlighted.</value>
		public Region provinceRegionHighlighted => _provinceRegionHighlighted;

		private int _provinceRegionHighlightedIndex = -1;

		/// <summary>
		/// Returns current highlighted province's region index.
		/// </summary>
		public int provinceRegionHighlightedIndex => _provinceRegionHighlightedIndex;

		private int _provinceLastClicked = -1;

		/// <summary>
		/// Returns the last clicked province index.
		/// </summary>
		public int provinceLastClicked => _provinceLastClicked;

		private int _provinceRegionLastClicked = -1;

		/// <summary>
		/// Returns the last clicked province region index.
		/// </summary>
		public int provinceRegionLastClicked => _provinceRegionLastClicked;

		/// <summary>
		/// Gets the province region's highlighted shape.
		/// </summary>
		public GameObject provinceRegionHighlightedShape => provinceRegionHighlightedObj;

		public event OnProvinceEvent OnProvinceEnter;
		public event OnProvinceEvent OnProvinceExit;
		public event OnProvinceClickEvent OnProvinceClick;
		public event OnProvinceHighlightEvent OnProvinceHighlight;

		[SerializeField] private bool
			_showProvinces = false;

		/// <summary>
		/// Toggle frontiers visibility.
		/// </summary>
		public bool showProvinces
		{
			get => _showProvinces;
			set
			{
				if (value != _showProvinces)
				{
					_showProvinces = value;
					isDirty = true;

					if (_showProvinces)
					{
						if (provinces == null)
							ReadProvincesPackedString();
						if (_drawAllProvinces)
							DrawAllProvinceBorders(true, false);
					}
					else
						HideProvinces();
				}
			}
		}

		[SerializeField] private bool
			_enableProvinceHighlight = true;

		/// <summary>
		/// Enable/disable province highlight when mouse is over and ShowProvinces is true.
		/// </summary>
		public bool enableProvinceHighlight
		{
			get => _enableProvinceHighlight;
			set
			{
				if (_enableProvinceHighlight != value)
				{
					_enableProvinceHighlight = value;
					isDirty = true;
					if (_enableProvinceHighlight)
						if (provinces == null)
							ReadProvincesPackedString();
				}
			}
		}

		/// <summary>
		/// Set whether all regions of active province should be highlighted.
		/// </summary>
		[SerializeField] private bool
			_highlightAllProvinceRegions = false;

		public bool highlightAllProvinceRegions
		{
			get => _highlightAllProvinceRegions;
			set
			{
				if (_highlightAllProvinceRegions != value)
				{
					_highlightAllProvinceRegions = value;
					DestroySurfaces();
					isDirty = true;
				}
			}
		}

		[SerializeField] private bool
			_drawAllProvinces = false;

		/// <summary>
		/// Forces drawing of all provinces and not only thouse of currently selected country.
		/// </summary>
		public bool drawAllProvinces
		{
			get => _drawAllProvinces;
			set
			{
				if (value != _drawAllProvinces)
				{
					_drawAllProvinces = value;
					isDirty = true;
					DrawAllProvinceBorders(true, false);
				}
			}
		}

		/// <summary>
		/// Fill color to use when the mouse hovers a country's region.
		/// </summary>
		[SerializeField] private Color
			_provincesFillColor = new(0, 0, 1, 0.7f);

		public Color provincesFillColor
		{
			get
			{
				if (hudMatProvince != null)
					return hudMatProvince.color;
				else
					return _provincesFillColor;
			}
			set
			{
				if (value != _provincesFillColor)
				{
					_provincesFillColor = value;
					isDirty = true;
					if (hudMatProvince != null && _provincesFillColor != hudMatProvince.color)
						hudMatProvince.color = _provincesFillColor;
				}
			}
		}

		/// <summary>
		/// Global color for provinces.
		/// </summary>
		[SerializeField] private Color
			_provincesColor = Color.white;

		public Color provincesColor
		{
			get
			{
				if (provincesMat != null)
					return provincesMat.color;
				else
					return _provincesColor;
			}
			set
			{
				if (value != _provincesColor)
				{
					_provincesColor = value;
					isDirty = true;

					if (provincesMat != null && _provincesColor != provincesMat.color)
						provincesMat.color = _provincesColor;
				}
			}
		}

		private string _provinceAttributeFile = PROVINCE_ATTRIB_DEFAULT_FILENAME;

		public string provinceAttributeFile
		{
			get => _provinceAttributeFile;
			set
			{
				if (value != _provinceAttributeFile)
				{
					_provinceAttributeFile = value;
					if (_provinceAttributeFile == null)
						_provinceAttributeFile = PROVINCE_ATTRIB_DEFAULT_FILENAME;
					isDirty = true;
					ReloadProvincesAttributes();
				}
			}
		}

		[SerializeField] private bool
			_provincesCoastlines = true;

		public bool provincesCoastlines
		{
			get => _provincesCoastlines;
			set
			{
				if (value != _provincesCoastlines)
				{
					_provincesCoastlines = value;
					isDirty = true;
					DrawAllProvinceBorders(true, false);
				}
			}
		}

		[SerializeField] private bool
			_showProvinceNames = false;

		public bool showProvinceNames
		{
			get => _showProvinceNames;
			set
			{
				if (value != _showProvinceNames)
				{
					_showProvinceNames = value;
					isDirty = true;
					if (textProvinceRoot != null)
						textProvinceRoot.SetActive(_showProvinceNames);
					else if (_showProvinceNames)
						RedrawProvinceLabels(countryProvincesLabelsShown);
				}
			}
		}

		[SerializeField] private bool
			_showAllCountryProvinceNames = true;

		public bool showAllCountryProvinceNames
		{
			get => _showAllCountryProvinceNames;
			set
			{
				if (value != _showAllCountryProvinceNames)
				{
					_showAllCountryProvinceNames = value;
					isDirty = true;
					RedrawProvinceLabels(countryProvincesLabelsShown);
				}
			}
		}

		[SerializeField] private PROVINCE_LABELS_VISIBILITY
			_provinceLabelsVisibility = PROVINCE_LABELS_VISIBILITY.Automatic;

		public PROVINCE_LABELS_VISIBILITY provinceLabelsVisibility
		{
			get => _provinceLabelsVisibility;
			set
			{
				if (value != _provinceLabelsVisibility)
				{
					_provinceLabelsVisibility = value;
					isDirty = true;
				}
			}
		}

		[SerializeField] private float
			_provinceLabelsAbsoluteMinimumSize = 0.05f;

		public float provinceLabelsAbsoluteMinimumSize
		{
			get => _provinceLabelsAbsoluteMinimumSize;
			set
			{
				if (value != _provinceLabelsAbsoluteMinimumSize)
				{
					_provinceLabelsAbsoluteMinimumSize = value;
					isDirty = true;
					if (_showProvinceNames)
						RedrawProvinceLabels(countryProvincesLabelsShown);
				}
			}
		}

		[SerializeField] private float
			_provinceLabelsSize = 0.2f;

		public float provinceLabelsSize
		{
			get => _provinceLabelsSize;
			set
			{
				if (value != _provinceLabelsSize)
				{
					_provinceLabelsSize = value;
					isDirty = true;
					if (_showProvinceNames)
						RedrawProvinceLabels(countryProvincesLabelsShown);
				}
			}
		}

		[SerializeField] private bool
			_showProvinceLabelsShadow = true;

		/// <summary>
		/// Draws a shadow under province labels. Specify the color using labelsShadowColor.
		/// </summary>
		/// <value><c>true</c> if show labels shadow; otherwise, <c>false</c>.</value>
		public bool showProvinceLabelsShadow
		{
			get => _showProvinceLabelsShadow;
			set
			{
				if (value != _showProvinceLabelsShadow)
				{
					_showProvinceLabelsShadow = value;
					isDirty = true;
					if (gameObject.activeInHierarchy)
						RedrawProvinceLabels(countryProvincesLabelsShown);
				}
			}
		}

		[SerializeField] private Color
			_provinceLabelsColor = Color.cyan;

		/// <summary>
		/// Color for province labels.
		/// </summary>
		public Color provinceLabelsColor
		{
			get => _provinceLabelsColor;
			set
			{
				if (value != _provinceLabelsColor)
				{
					_provinceLabelsColor = value;
					isDirty = true;
					if (gameObject.activeInHierarchy && provinceLabelsFont != null)
						_provinceLabelsFont.material.color = _provinceLabelsColor;
				}
			}
		}

		[SerializeField] private Color
			_provinceLabelsShadowColor = new(0, 0, 0, 0.5f);

		/// <summary>
		/// Color for province labels.
		/// </summary>
		public Color provinceLabelsShadowColor
		{
			get => _provinceLabelsShadowColor;
			set
			{
				if (value != _provinceLabelsShadowColor)
				{
					_provinceLabelsShadowColor = value;
					isDirty = true;
					if (gameObject.activeInHierarchy)
						provLabelsShadowMaterial.color = _provinceLabelsShadowColor;
				}
			}
		}

		[SerializeField] private Font _provinceLabelsFont;

		/// <summary>
		/// Gets or sets the default font for province labels
		/// </summary>
		public Font provinceLabelsFont
		{
			get => _provinceLabelsFont;
			set
			{
				if (value != _provinceLabelsFont)
				{
					_provinceLabelsFont = value;
					isDirty = true;
					ReloadProvinceFont();
					RedrawProvinceLabels(countryProvincesLabelsShown);
				}
			}
		}

		#endregion

		#region Public API area

		/// <summary>
		/// Draws the borders of the provinces/states a country by its id. Returns true is country is found, false otherwise.
		/// </summary>
		public bool DrawProvinces(int countryIndex, bool includeNeighbours, bool forceRefresh,
			bool justComputeBorders)
		{
			if (countryIndex >= 0)
				return mDrawProvinces(countryIndex, includeNeighbours, forceRefresh, justComputeBorders);
			return false;
		}

		/// <summary>
		/// Hides the borders of all provinces/states.
		/// </summary>
		public void HideProvinces()
		{
			if (provincesObj != null)
				DestroyImmediate(provincesObj);
			countryProvincesDrawnIndex = -1;
			HideProvinceRegionHighlight();
		}

		/// <summary>
		/// Returns the index of a province in the provinces array by its reference.
		/// </summary>
		public int GetProvinceIndex(Province province)
		{
			int provinceIndex;
			if (provinceLookup.TryGetValue(province, out provinceIndex))
				return provinceIndex;
			else
				return -1;
		}

		/// <summary>
		/// Returns the index of a province in the global provinces array.
		/// </summary>
		public int GetProvinceIndex(string countryName, string provinceName)
		{
			var countryIndex = GetCountryIndex(countryName);
			return GetProvinceIndex(countryIndex, provinceName);
		}

		/// <summary>
		/// Returns the index of a province in the global provinces array.
		/// </summary>
		public int GetProvinceIndex(int countryIndex, string provinceName)
		{
			if (countryIndex < 0 || countryIndex >= _countries.Length)
				return -1;
			var country = _countries[countryIndex];
			if (_provinces == null)
				ReadProvincesPackedString();
			if (country.provinces == null)
				return -1;
			for (var k = 0; k < country.provinces.Length; k++)
				if (country.provinces[k].name.Equals(provinceName))
					return GetProvinceIndex(country.provinces[k]);
			return -1;
		}

		/// <summary>
		/// Gets the index of the province that contains the provided map coordinates. This will ignore hidden countries.
		/// </summary>
		/// <returns>The province index.</returns>
		/// <param name="localPosition">Map coordinates in the range of (-0.5 .. 0.5)</param>
		public int GetProvinceIndex(Vector2 localPosition)
		{
			// verify if hitPos is inside any country polygon
			int provinceIndex, provinceRegionIndex;
			if (GetProvinceRegionIndex(localPosition, -1, out provinceIndex, out provinceRegionIndex))
				return provinceIndex;
			return -1;
		}

		/// <summary>
		/// Gets the index of the province that contains the provided map coordinates and belongs to given country index. This will ignore hidden countries, and it's faster since countryIndex is passed to reduce candidate provinces.
		/// </summary>
		/// <returns>The province index.</returns>
		/// <param name="localPosition">Map coordinates in the range of (-0.5 .. 0.5)</param>
		public int GetProvinceIndex(Vector2 localPosition, int countryIndex)
		{
			// verify if hitPos is inside any country polygon
			int provinceIndex, provinceRegionIndex;
			if (GetProvinceRegionIndex(localPosition, countryIndex, out provinceIndex,
				out provinceRegionIndex))
				return provinceIndex;
			return -1;
		}

		/// <summary>
		/// Gets the region of the province that contains the provided map coordinates.
		/// </summary>
		/// <returns>The province region.</returns>
		/// <param name="localPosition">Map coordinates in the range of (-0.5 .. 0.5)</param>
		public Region GetProvinceRegion(Vector2 localPosition)
		{
			// verify if hitPos is inside any country polygon
			int provinceIndex, provinceRegionIndex;
			if (GetProvinceRegionIndex(localPosition, -1, out provinceIndex, out provinceRegionIndex))
				return provinces[provinceIndex].regions[provinceRegionIndex];
			return null;
		}

		/// <summary>
		/// Gets the region index of the province that contains the provided map coordinates.
		/// </summary>
		/// <returns>The Region index or -1 if no region found.</returns>
		/// <param name="localPosition">Map coordinates in the range of (-0.5 .. 0.5)</param>
		public int GetProvinceRegionIndex(Vector2 localPosition)
		{
			var region = GetProvinceRegion(localPosition);
			if (region == null)
				return -1;
			return region.regionIndex;
		}

		/// <summary>
		/// Gets the index of the province region.
		/// </summary>
		/// <returns>The province region index.</returns>
		/// <param name="provinceIndex">Province index.</param>
		/// <param name="region">Region.</param>
		public int GetProvinceRegionIndex(int provinceIndex, Region region)
		{
			if (provinceIndex < 0 || provinceIndex >= provinces.Length)
				return -1;
			var province = _provinces[provinceIndex];
			if (province.regions == null)
				ReadProvincePackedString(province);
			if (province.regions == null)
				return -1;
			var rc = province.regions.Count;
			for (var k = 0; k < rc; k++)
				if (province.regions[k] == region)
					return k;
			return -1;
		}

		/// <summary>
		/// Gets the index of the province and region that contains the provided map coordinates. This will ignore hidden countries.
		/// </summary>
		public bool GetProvinceRegionIndex(Vector2 localPosition, int countryIndex, out int provinceIndex,
			out int provinceRegionIndex)
		{
			provinceIndex = -1;
			provinceRegionIndex = -1;

			if (_enableEnclaves)
			{
				var candidateProvinceIndex = -1;
				var candidateProvinceRegionIndex = -1;
				var candidateRegionSize = float.MaxValue;
				int cc0, cc1;
				if (countryIndex < 0)
				{
					cc0 = 0;
					cc1 = countriesOrderedBySize.Count;
				}
				else
				{
					cc0 = countryIndex;
					cc1 = countryIndex + 1;
				}
				for (var c = cc0; c < cc1; c++)
				{
					var country = _countries[_countriesOrderedBySize[c]];
					if (country.hidden)
						continue;
					if (country.regionsRect2D.Contains(localPosition))
					{
						if (country.provinces == null)
							continue;
						var pp = country.provinces.Length;
						for (var p = 0; p < pp; p++)
						{
							var prov = country.provinces[p];
							if (prov.regions == null)
								ReadProvincePackedString(prov);
							var rr = prov.regions.Count;
							for (var r = 0; r < rr; r++)
							{
								var reg = prov.regions[r];
								if (reg.rect2DArea < candidateRegionSize && reg.Contains(localPosition))
								{
									candidateRegionSize = reg.rect2DArea;
									candidateProvinceIndex = GetProvinceIndex(prov);
									candidateProvinceRegionIndex = r;
								}
							}
						}
					}
				}
				provinceIndex = candidateProvinceIndex;
				provinceRegionIndex = candidateProvinceRegionIndex;
			}
			else
			{
				if (countryIndex < 0)
					countryIndex = GetCountryIndex(localPosition);
				if (countryIndex >= 0)
				{
					var country = _countries[countryIndex];
					if (country.provinces == null)
						return false;
					for (var p = 0; p < country.provinces.Length; p++)
					{
						var province = country.provinces[p];
						if (province.regions == null)
							ReadProvincePackedString(province);
						if (!province.regionsRect2D.Contains(localPosition))
							continue;
						for (var pr = 0; pr < province.regions.Count; pr++)
							if (province.regions[pr].Contains(localPosition))
							{
								provinceIndex = GetProvinceIndex(province);
								provinceRegionIndex = pr;
								return true;
							}
					}
					// Look for rogue provinces (province that is surrounded by another country)
//				for (int p=0; p<provinces.Length; p++) {
//					Province province = provinces[p];
//					if (province.regions == null) ReadProvincePackedString(province);
//					if (!province.regionsRect2D.Contains(localPosition)) continue;
//					for (int pr=0;pr<province.regions.Count;pr++) {
//						if (province.regions [pr].Contains (localPosition)) {
//							provinceIndex = p;
//							provinceRegionIndex = pr;
//							return true;
//						}
//					}
//				}
				}
			}
			return provinceRegionIndex >= 0;
		}

		/// <summary>
		/// Returns the province index by screen position.
		/// </summary>
		public bool GetProvinceIndex(Ray ray, out int provinceIndex, out int regionIndex)
		{
			// obtain country
			var hitCount = Physics.RaycastNonAlloc(ray, tempHits, 500, layerMask);
			if (hitCount > 0)
				for (var k = 0; k < hitCount; k++)
					if (tempHits[k].collider.gameObject == gameObject)
					{
						Vector2 localHit = transform.InverseTransformPoint(tempHits[k].point);
						return GetProvinceRegionIndex(localHit, -1, out provinceIndex, out regionIndex);
					}
			provinceIndex = -1;
			regionIndex = -1;
			return false;
		}

		/// <summary>
		/// Returns the province object by its name.
		/// </summary>
		public Province GetProvince(string provinceName, string countryName)
		{
			var countryIndex = GetCountryIndex(countryName);
			if (countryIndex < 0)
				return null;
			var provinceIndex = GetProvinceIndex(countryIndex, provinceName);
			if (provinceIndex >= 0)
			{
				var prov = provinces[provinceIndex];
				if (prov.regions == null)
					ReadProvincePackedString(prov);
				return prov;
			}
			return null;
		}

		/// <summary>
		/// Gets the province object in the provinces array by its index. Equals to map.provinces[provinceIndex] as provinces array is public.
		/// </summary>
		public Province GetProvince(int provinceIndex)
		{
			if (provinces == null || provinceIndex < 0 || provinceIndex >= provinces.Length)
				return null;
			return provinces[provinceIndex];
		}

		/// <summary>
		/// Gets the province object in the provinces array by its position.
		/// </summary>
		public Province GetProvince(Vector2 mapPosition)
		{
			var provinceIndex = GetProvinceIndex(mapPosition);
			if (provinceIndex >= 0)
				return provinces[provinceIndex];
			return null;
		}

		/// <summary>
		/// Gets the province index with that unique Id.
		/// </summary>
		public int GetProvinceIndex(int uniqueId)
		{
			var provinceCount = provinces.Length;
			for (var k = 0; k < provinceCount; k++)
				if (_provinces[k].uniqueId == uniqueId)
					return k;
			return -1;
		}

		/// <summary>
		/// Returns all neighbour provinces
		/// </summary>
		public List<Province> ProvinceNeighbours(int provinceIndex)
		{
			var provinceNeighbours = new List<Province>();
			if (provinceIndex < 0 || provinceIndex >= provinces.Length)
				return provinceNeighbours;

			// Get country object
			var province = provinces[provinceIndex];
			if (province.regions == null)
				ReadProvincePackedString(province);
			if (province.regions !=
			    null) // Iterate for all regions (a country can have several separated regions)
				for (var provinceRegionIndex = 0;
					provinceRegionIndex < province.regions.Count;
					provinceRegionIndex++)
				{
					var provinceRegion = province.regions[provinceRegionIndex];
					var neighbours = ProvinceNeighboursOfRegion(provinceRegion);
					neighbours.ForEach(p =>
					{
						if (!provinceNeighbours.Contains(p))
							provinceNeighbours.Add(p);
					});
				}
			return provinceNeighbours;
		}

		/// <summary>
		/// Get neighbours of the main region of a province
		/// </summary>
		public List<Province> ProvinceNeighboursOfMainRegion(int provinceIndex)
		{
			// Get main region
			var province = provinces[provinceIndex];
			var provinceRegion = province.regions[province.mainRegionIndex];
			return ProvinceNeighboursOfRegion(provinceRegion);
		}

		/// <summary>
		/// Get neighbours of the currently selected region
		/// </summary>
		public List<Province> ProvinceNeighboursOfCurrentRegion()
		{
			var selectedRegion = provinceRegionHighlighted;
			return ProvinceNeighboursOfRegion(selectedRegion);
		}

		/// <summary>
		/// Get neighbours of a given province region
		/// </summary>
		public List<Province> ProvinceNeighboursOfRegion(Region provinceRegion)
		{
			var provinceNeighbours = new List<Province>();

			// Get main region
			if (provinceRegion == null)
				return provinceNeighbours;

			// Check if neighbours have been computed
			if (!provinceNeighboursComputed && !_drawAllProvinces)
			{
				var prevShowProvinces = _showProvinces;
				var prevDrawAllProvinces = _drawAllProvinces;
				_showProvinces = true;
				_drawAllProvinces = true;
				DrawAllProvinceBorders(true, true);
				_drawAllProvinces = prevDrawAllProvinces;
				_showProvinces = prevShowProvinces;
				HideProvinces();
			}

			// Get the neighbours for this region
			for (var neighbourIndex = 0;
				neighbourIndex < provinceRegion.neighbours.Count;
				neighbourIndex++)
			{
				var neighbour = provinceRegion.neighbours[neighbourIndex];
				var neighbourProvince = (Province)neighbour.entity;
				if (!provinceNeighbours.Contains(neighbourProvince))
					provinceNeighbours.Add(neighbourProvince);
			}

			// Find neighbours due to enclaves
			if (_enableEnclaves)
			{
				var province = (Province)provinceRegion.entity;
				for (var p = 0; p < provinces.Length; p++)
				{
					var p2 = provinces[p];
					if (p2 == province || p2.regions == null)
						continue;
					if (provinceNeighbours.Contains(p2))
						continue;
					var prc = p2.regions.Count;
					for (var pr = 0; pr < prc; pr++)
					{
						var pregion = p2.regions[pr];
						if (provinceRegion.Contains(pregion) || pregion.Contains(provinceRegion))
						{
							provinceNeighbours.Add(p2);
							break;
						}
					}
				}
			}
			return provinceNeighbours;
		}

		/// <summary>
		/// Renames the province. Name must be unique, different from current and one letter minimum.
		/// </summary>
		/// <returns><c>true</c> if country was renamed, <c>false</c> otherwise.</returns>
		public bool ProvinceRename(int countryIndex, string oldName, string newName)
		{
			if (newName == null || newName.Length == 0)
				return false;
			var provinceIndex = GetProvinceIndex(countryIndex, oldName);
			var newProvinceIndex = GetProvinceIndex(countryIndex, newName);
			if (provinceIndex < 0 || newProvinceIndex >= 0)
				return false;
			provinces[provinceIndex].name = newName;

			// Update cities
			var cities = GetCities(provinces[provinceIndex]);
			var cityCount = cities.Count;
			if (cityCount > 0)
				for (var k = 0; k < cityCount; k++)
					if (cities[k].province != newName)
						cities[k].province = newName;

			lastProvinceLookupCount = -1;
			return true;
		}

		/// <summary>
		/// Creates a new province.
		/// </summary>
		/// <returns>The create.</returns>
		/// <param name="name">Name must be unique!</param>
		/// <param name="countryIndex">Country index.</param>
		public int ProvinceCreate(string name, int countryIndex)
		{
			var newProvince = new Province(name, countryIndex,
				GetUniqueId(new List<IExtendableAttribute>(provinces)));
			newProvince.regions = new List<Region>();
			return ProvinceAdd(newProvince);
		}

		/// <summary>
		/// Adds a new province which has been properly initialized. Used by the Map Editor. Name must be unique.
		/// </summary>
		/// <returns>Index of new province.</returns>
		public int ProvinceAdd(Province province)
		{
			if (province.countryIndex < 0 || province.countryIndex >= _countries.Length)
				return -1;
			var newProvinces = new Province[provinces.Length + 1];
			for (var k = 0; k < provinces.Length; k++)
				newProvinces[k] = provinces[k];
			var provinceIndex = newProvinces.Length - 1;
			newProvinces[provinceIndex] = province;
			provinces = newProvinces;
			lastProvinceLookupCount = -1;
			// add the new province to the country internal list
			if (province.countryIndex >= 0 && province.countryIndex < countries.Length)
			{
				var country = _countries[province.countryIndex];
				if (country.provinces == null)
					country.provinces = new Province[0];
				var newCountryProvinces = new List<Province>(country.provinces);
				newCountryProvinces.Add(province);
				newCountryProvinces.Sort(ProvinceSizeComparer);
				country.provinces = newCountryProvinces.ToArray();
			}

			RefreshProvinceGeometry(provinceIndex);

			return provinceIndex;
		}

		/// <summary>
		/// Creates a new country from an existing province. Existing province will be extracted from previous sovereign. Returns the index of the new country.
		/// </summary>
		/// <returns>The new country index or -1 if failed.</returns>
		public int ProvinceToCountry(Province province, string newCountryName, bool redraw = true)
		{
			if (province == null ||
			    province.countryIndex < 0 ||
			    province.regions == null ||
			    string.IsNullOrEmpty(newCountryName))
				return -1;

			// Checks if newCountryName already exists
			var countryIndex = GetCountryIndex(newCountryName);
			if (countryIndex >= 0)
				return -1;

			// Add new country
			var continent = GetCountry(province.countryIndex).continent;
			var newCountry = new Country(newCountryName, continent,
				GetUniqueId(new List<IExtendableAttribute>(_countries)));

			// Create dummy region
			newCountry.regions.Add(new Region(newCountry, 0));
			newCountry.mainRegionIndex = 0;
			newCountry.provinces = new Province[0];
			var newCountryIndex = CountryAdd(newCountry);

			// Transfer province
			if (province.regions == null)
				ReadProvincePackedString(province);
			if (!CountryTransferProvinceRegion(newCountryIndex, province.regions[province.mainRegionIndex],
				false))
			{
				CountryDelete(newCountryIndex, false, false);
				return -1;
			}

			// Remove dummy region
			newCountry.regions.RemoveAt(0);

			// Update geometries and refresh
			RefreshCountryDefinition(newCountryIndex, null);
			if (redraw)
				Redraw();

			return newCountryIndex;
		}

		/// <summary>
		/// Creates a new country from a list of existing provinces. Existing provinces will be extracted from previous sovereign. Returns the index of the new country.
		/// </summary>
		/// <returns>The new country index or -1 if failed.</returns>
		public int ProvincesToCountry(List<Province> provinces, string newCountryName, bool redraw = true)
		{
			if (provinces == null || provinces.Count == 0)
				return -1;

			// Checks if newCountryName already exists
			var countryIndex = GetCountryIndex(newCountryName);
			if (countryIndex >= 0)
				return -1;

			// Add new country
			var continent = GetCountry(provinces[0].countryIndex).continent;
			var newCountry = new Country(newCountryName, continent,
				GetUniqueId(new List<IExtendableAttribute>(_countries)));

			// Create dummy region
			newCountry.regions.Add(new Region(newCountry, 0));
			newCountry.mainRegionIndex = 0;
			newCountry.provinces = new Province[0];
			var newCountryIndex = CountryAdd(newCountry);

			// Transfer provinces
			foreach (var province in provinces)
			{
				if (province.regions == null)
					ReadProvincePackedString(province);
				if (CountryTransferProvinceRegion(newCountryIndex,
					province.regions[province.mainRegionIndex], false))
					continue;
				CountryDelete(newCountryIndex, false, false);
				return -1;
			}

			// Remove dummy region
			newCountry.regions.RemoveAt(0);

			// Update geometries and refresh
			RefreshCountryDefinition(newCountryIndex, null);
			if (redraw)
				Redraw();

			return newCountryIndex;
		}

		/// <summary>
		/// Flashes specified province by index in the global province array.
		/// </summary>
		public void BlinkProvince(int provinceIndex, Color color1, Color color2, float duration,
			float blinkingSpeed, bool smoothBlink = true)
		{
			if (provinceIndex < 0 || provinces == null || provinceIndex >= provinces.Length)
				return;
			var mainRegionIndex = provinces[provinceIndex].mainRegionIndex;
			BlinkProvince(provinceIndex, mainRegionIndex, color1, color2, duration, blinkingSpeed,
				smoothBlink);
		}

		/// <summary>
		/// Flashes specified province's region.
		/// </summary>
		public void BlinkProvince(int provinceIndex, int regionIndex, Color color1, Color color2,
			float duration, float blinkingSpeed, bool smoothBlink = true)
		{
			if (provinceIndex < 0 || provinces == null || provinceIndex >= provinces.Length)
				return;
			var cacheIndex = GetCacheIndexForProvinceRegion(provinceIndex, regionIndex);
			GameObject surf = null;
			surfaces.TryGetValue(cacheIndex, out surf);
			if (surf == null)
				surf = GenerateProvinceRegionSurface(provinceIndex, regionIndex, hudMatProvince);
			if (surf == null)
				return;
			var sb = surf.AddComponent<SurfaceBlinker>();
			sb.blinkMaterial = hudMatCountry;
			sb.color1 = color1;
			sb.color2 = color2;
			sb.duration = duration;
			sb.speed = blinkingSpeed;
			sb.smoothBlink = smoothBlink;
			sb.customizableSurface = provinces[provinceIndex].regions[regionIndex];
			surf.SetActive(true);
		}

		/// <summary>
		/// Starts navigation to target province/state. Returns false if not found.
		/// </summary>
		public bool FlyToProvince(string countryName, string provinceName)
		{
			var provinceIndex = GetProvinceIndex(countryName, provinceName);
			return FlyToProvince(provinceIndex);
		}

		/// <summary>
		/// Starts navigation to target province/state by index in the provinces collection. Returns false if not found.
		/// </summary>
		public bool FlyToProvince(int provinceIndex) =>
			FlyToProvince(provinceIndex, _navigationTime, GetZoomLevel());

		/// <summary>
		/// Starts navigation to target province/state by name in the provinces collection with duration and zoom level options. Returns false if not found.
		/// </summary>
		public bool FlyToProvince(string countryName, string provinceName, float duration, float zoomLevel)
		{
			var provinceIndex = GetProvinceIndex(countryName, provinceName);
			return FlyToProvince(provinceIndex, duration, zoomLevel);
		}

		/// <summary>
		/// Starts navigation to target province/state by index in the provinces collection with duration and zoom level options. Returns false if not found.
		/// </summary>
		public bool FlyToProvince(int provinceIndex, float duration, float zoomLevel)
		{
			if (provinceIndex < 0 || provinceIndex >= provinces.Length)
				return false;
			var province = provinces[provinceIndex];
			if (province.regions == null)
				ReadProvincePackedString(province); // needed to calculate province center
			SetDestination(provinces[provinceIndex].center, duration, zoomLevel);
			return true;
		}

		/// <summary>
		/// Colorize all regions of specified province/state by index in the global provinces collection.
		/// </summary>
		public void ToggleProvinceSurface(int provinceIndex, bool visible, Color color)
		{
			ToggleProvinceSurface(provinceIndex, visible, color, null, Misc.Vector2one, Misc.Vector2zero,
				0, false);
		}

		/// <summary>
		/// Colorize all regions of specified province/state by index in the global provinces collection.
		/// </summary>
		public void ToggleProvinceSurface(Province province, bool visible, Color color)
		{
			var provinceIndex = GetProvinceIndex(province);
			if (provinceIndex < 0)
				return;
			ToggleProvinceSurface(provinceIndex, visible, color);
		}

		/// <summary>
		/// Colorize all regions of specified province and assings a texture to main region with options.
		/// </summary>
		public void ToggleProvinceSurface(Province province, bool visible, Color color, Texture2D texture,
			bool applyTextureToAllRegions = false)
		{
			var provinceIndex = GetProvinceIndex(province);
			if (provinceIndex < 0)
				return;
			ToggleProvinceSurface(provinceIndex, visible, color, texture, applyTextureToAllRegions);
		}

		/// <summary>
		/// Colorize all regions of specified province and assings a texture to main region with options.
		/// </summary>
		public void ToggleProvinceSurface(int provinceIndex, bool visible, Color color, Texture2D texture,
			bool applyTextureToAllRegions = false)
		{
			ToggleProvinceSurface(provinceIndex, visible, color, texture, Misc.Vector2one,
				Misc.Vector2zero, 0, applyTextureToAllRegions);
		}

		/// <summary>
		/// Colorize all regions of specified province and assings a texture to main region with options.
		/// </summary>
		public void ToggleProvinceSurface(int provinceIndex, bool visible, Color color, Texture2D texture,
			Vector2 textureScale, Vector2 textureOffset, float textureRotation,
			bool applyTextureToAllRegions)
		{
			if (provinceIndex < 0 || provinceIndex >= provinces.Length)
				return;
			if (!visible)
			{
				HideProvinceSurfaces(provinceIndex);
				return;
			}
			var province = _provinces[provinceIndex];
			if (province.regions == null)
				ReadProvincePackedString(province);
			var rCount = province.regions.Count;
			for (var r = 0; r < rCount; r++)
				if (applyTextureToAllRegions || r == province.mainRegionIndex)
					ToggleProvinceRegionSurface(provinceIndex, r, visible, color, texture, textureScale,
						textureOffset, textureRotation);
				else
					ToggleProvinceRegionSurface(provinceIndex, r, visible, color);
		}

		/// <summary>
		/// Colorize main region of specified province and assings a texture to main region with options.
		/// </summary>
		public void ToggleProvinceMainRegionSurface(int provinceIndex, bool visible, Color color)
		{
			if (provinceIndex < 0 || provinceIndex >= provinces.Length)
				return;
			var province = _provinces[provinceIndex];
			if (province.regions == null)
				ReadProvincePackedString(province);
			ToggleProvinceRegionSurface(provinceIndex, province.mainRegionIndex, visible, color);
		}

		/// <summary>
		/// Highlights the province region specified.
		/// Internally used by the Editor component, but you can use it as well to temporarily mark a province region.
		/// </summary>
		/// <param name="refreshGeometry">Pass true only if you're sure you want to force refresh the geometry of the highlight (for instance, if the frontiers data has changed). If you're unsure, pass false.</param>
		public GameObject ToggleProvinceRegionSurfaceHighlight(int provinceIndex, int regionIndex,
			Color color)
		{
			GameObject surf;
			var mat = Instantiate(hudMatProvince);
			if (disposalManager != null)
				disposalManager.MarkForDisposal(mat); //.hideFlags = HideFlags.DontSave;
			mat.color = color;
			mat.renderQueue--;
			var cacheIndex = GetCacheIndexForProvinceRegion(provinceIndex, regionIndex);
			var existsInCache = surfaces.TryGetValue(cacheIndex, out surf);
			if (existsInCache)
			{
				if (surf == null)
					surfaces.Remove(cacheIndex);
				else
				{
					surf.SetActive(true);
					surf.GetComponent<Renderer>().sharedMaterial = mat;
				}
			}
			else
				surf = GenerateProvinceRegionSurface(provinceIndex, regionIndex, mat);
			return surf;
		}

		/// <summary>
		/// Disables all province regions highlights. This doesn't destroy custom materials.
		/// </summary>
		public void HideProvinceRegionHighlights(bool destroyCachedSurfaces)
		{
			HideProvinceRegionHighlight();
			if (provinces == null)
				return;
			for (var c = 0; c < provinces.Length; c++)
			{
				var province = provinces[c];
				if (province == null || province.regions == null)
					continue;
				for (var cr = 0; cr < province.regions.Count; cr++)
				{
					var region = province.regions[cr];
					var cacheIndex = GetCacheIndexForProvinceRegion(c, cr);
					GameObject surf;
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
		/// Colorize a region of specified province/state by index in the provinces collection.
		/// </summary>
		public GameObject
			ToggleProvinceRegionSurface(int provinceIndex, int regionIndex, bool visible, Color color) =>
			ToggleProvinceRegionSurface(provinceIndex, regionIndex, visible, color, null, Misc.Vector2one,
				Misc.Vector2zero, 0);

		/// <summary>
		/// Colorize specified region of a country by indexes.
		/// </summary>
		public GameObject ToggleProvinceRegionSurface(int provinceIndex, int regionIndex, bool visible,
			Color color, Texture2D texture, Vector2 textureScale, Vector2 textureOffset,
			float textureRotation)
		{
			if (!visible)
			{
				HideProvinceRegionSurface(provinceIndex, regionIndex);
				return null;
			}
			GameObject surf = null;
			var province = provinces[provinceIndex];
			if (province.regions == null)
				ReadProvincePackedString(province);
			if (province.regions == null)
				return null;
			var region = provinces[provinceIndex].regions[regionIndex];
			var cacheIndex = GetCacheIndexForProvinceRegion(provinceIndex, regionIndex);
			// Checks if current cached surface contains a material with a texture, if it exists but it has not texture, destroy it to recreate with uv mappings
			surfaces.TryGetValue(cacheIndex, out surf);

			// Should the surface be recreated?
			Material surfMaterial;
			if (surf != null)
			{
				surfMaterial = surf.GetComponent<Renderer>().sharedMaterial;
				if (texture != null &&
				    (textureScale != region.customTextureScale ||
				     textureOffset != region.customTextureOffset ||
				     textureRotation != region.customTextureRotation ||
				     surfMaterial.name.Equals(coloredMat.name)))
				{
					surfaces.Remove(cacheIndex);
					DestroyImmediate(surf);
					surf = null;
				}
			}
			// If it exists, activate and check proper material, if not create surface
			var isHighlighted = provinceHighlightedIndex == provinceIndex &&
			                    (provinceRegionHighlightedIndex == regionIndex ||
			                     _highlightAllProvinceRegions) &&
			                    _enableProvinceHighlight &&
			                    province.allowHighlight &&
			                    _countries[province.countryIndex].allowProvincesHighlight;
			surfMaterial = GetColoredTexturedMaterial(color, texture, true, 1);
			if (surfMaterial.renderQueue < 2006) // ensure it renders on top of country highlight
				surfMaterial.renderQueue = 2006;
			if (surf != null)
			{
				if (!surf.activeSelf)
					surf.SetActive(true);
				// Check if material is ok
				region.customMaterial = surfMaterial;
				surfMaterial = surf.GetComponent<Renderer>().sharedMaterial;
				if (texture == null && !surfMaterial.name.Equals(coloredMat.name) ||
				    texture != null && !surfMaterial.name.Equals(texturizedMat.name) ||
				    surfMaterial.color != color && !isHighlighted ||
				    texture != null && region.customMaterial.mainTexture != texture)
					ApplyMaterialToSurface(surf, surfMaterial);
			}
			else
			{
				surf = GenerateProvinceRegionSurface(provinceIndex, regionIndex, surfMaterial,
					textureScale, textureOffset, textureRotation);
				region.customMaterial = surfMaterial;
				region.customTextureOffset = textureOffset;
				region.customTextureRotation = textureRotation;
				region.customTextureScale = textureScale;
			}
			// If it was highlighted, highlight it again
			if (region.customMaterial != null &&
			    isHighlighted &&
			    region.customMaterial.color != hudMatProvince.color)
			{
				var clonedMat = Instantiate(region.customMaterial);
				if (disposalManager != null)
					disposalManager.MarkForDisposal(clonedMat); //.hideFlags = HideFlags.DontSave;
				clonedMat.name = region.customMaterial.name;
				clonedMat.color = hudMatProvince.color;
				ApplyMaterialToSurface(surf, clonedMat);
				provinceRegionHighlightedObj = surf;
			}
			return surf;
		}

		/// <summary>
		/// Hides all colorized regions of all provinces/states.
		/// </summary>
		public void HideProvinceSurfaces()
		{
			if (provinces == null)
				return;
			for (var p = 0; p < provinces.Length; p++)
				HideProvinceSurfaces(p);
		}

		/// <summary>
		/// Hides all colorized regions of one province/state.
		/// </summary>
		public void HideProvinceSurfaces(int provinceIndex)
		{
			if (provinces[provinceIndex].regions == null)
				return;
			var rCount = provinces[provinceIndex].regions.Count;
			for (var r = 0; r < rCount; r++)
				HideProvinceRegionSurface(provinceIndex, r);
		}

		/// <summary>
		/// Hides all regions of one province.
		/// </summary>
		public void HideProvinceRegionSurface(int provinceIndex, int regionIndex)
		{
			var cacheIndex = GetCacheIndexForProvinceRegion(provinceIndex, regionIndex);
			GameObject obj;
			if (surfaces.TryGetValue(cacheIndex, out obj))
				if (obj != null)
					obj.SetActive(false);
			provinces[provinceIndex].regions[regionIndex].customMaterial = null;
		}

		/// <summary>
		/// Returns an array of province names. The returning list can be grouped by country.
		/// </summary>
		public string[] GetProvinceNames(bool groupByCountry, bool addProvinceIndex = true)
		{
			var c = new List<string>(provinces.Length + _countries.Length);
			if (provinces == null)
				return c.ToArray();
			var countriesAdded = new bool[_countries.Length];
			for (var k = 0; k < provinces.Length; k++)
			{
				var province = provinces[k];
				if (province != null)
				{
					// could be null if country doesn't exist in this level of quality
					if (groupByCountry)
					{
						if (!countriesAdded[province.countryIndex])
						{
							countriesAdded[province.countryIndex] = true;
							c.Add(_countries[province.countryIndex].name);
						}
						if (addProvinceIndex)
							c.Add(_countries[province.countryIndex].name +
							      "|" +
							      province.name +
							      " (" +
							      k +
							      ")");
						else
							c.Add(_countries[province.countryIndex].name + "|" + province.name);
					}
					else
					{
						if (addProvinceIndex)
							c.Add(province.name + " (" + k + ")");
						else
							c.Add(province.name);
					}
				}
			}
			c.Sort();

			if (groupByCountry)
			{
				var k = -1;
				while (++k < c.Count)
				{
					var i = c[k].IndexOf('|');
					if (i > 0)
						c[k] = "  " + c[k].Substring(i + 1);
				}
			}
			return c.ToArray();
		}

		/// <summary>
		/// Returns an array of province names for the specified country.
		/// </summary>
		public string[] GetProvinceNames(int countryIndex, bool addProvinceIndex = true)
		{
			var c = new List<string>(100);
			if (provinces == null || countryIndex < 0 || countryIndex >= _countries.Length)
				return c.ToArray();
			for (var k = 0; k < _provinces.Length; k++)
			{
				var province = _provinces[k];
				if (province.countryIndex == countryIndex)
				{
					if (addProvinceIndex)
						c.Add(province.name + " (" + k + ")");
					else
						c.Add(province.name);
				}
			}
			c.Sort();
			return c.ToArray();
		}

		/// <summary>
		/// Returns an array of province objects for the specified country.
		/// </summary>
		public Province[] GetProvinces(Country country)
		{
			if (country == null || provinces == null)
				return null;
			return country.provinces;
		}

		/// <summary>
		/// Returns an array of province objects for the specified country.
		/// </summary>
		public Province[] GetProvinces(int countryIndex)
		{
			if (countryIndex < 0 || countryIndex >= countries.Length)
				return null;
			return GetProvinces(_countries[countryIndex]);
		}

		/// <summary>
		/// Returns a list of provinces whose center is contained in a given region
		/// </summary>
		public List<Province> GetProvinces(Region region)
		{
			var provCount = provinces.Length;
			var cc = new List<Province>();
			for (var k = 0; k < provCount; k++)
				if (region.Contains(_provinces[k].center))
					cc.Add(_provinces[k]);
			return cc;
		}

		/// <summary>
		/// Gets a list of provinces that overlap with a given region
		/// </summary>
		public List<Province> GetProvincesOverlap(Region region)
		{
			var rr = new List<Province>();
			var provinceCount = provinces.Length;
			for (var k = 0; k < provinceCount; k++)
			{
				var province = _provinces[k];
				if (province.regions == null)
					ReadProvincePackedString(province);
				if (province.regions == null || rr.Contains(province))
					continue;
				var rCount = province.regions.Count;
				for (var r = 0; r < rCount; r++)
				{
					var otherRegion = province.regions[r];
					if (region.Intersects(otherRegion))
					{
						rr.Add(province);
						break;
					}
				}
			}
			return rr;
		}

		/// <summary>
		/// Gets a list of provinces regions that overlap with a given region
		/// </summary>
		public List<Region> GetProvinceRegionsOverlap(Region region)
		{
			var rr = new List<Region>();
			var count = provinces.Length;
			for (var k = 0; k < count; k++)
			{
				var province = _provinces[k];
				if (!province.regionsRect2D.Overlaps(region.rect2D))
					continue;
				if (province.regions == null)
					continue;
				var rCount = province.regions.Count;
				for (var r = 0; r < rCount; r++)
				{
					var otherRegion = province.regions[r];
					if (otherRegion.points.Length > 0 && region.Intersects(otherRegion))
						rr.Add(otherRegion);
				}
			}
			return rr;
		}

		/// <summary>
		/// Delete all provinces from specified continent. This operation does not include dependencies.
		/// </summary>
		public void ProvincesDeleteOfSameContinent(string continentName)
		{
			HideProvinceRegionHighlights(true);
			if (provinces == null)
				return;
			var numProvinces = _provinces.Length;
			var newProvinces = new List<Province>(numProvinces);
			for (var k = 0; k < numProvinces; k++)
				if (_provinces[k] != null)
				{
					var c = _provinces[k].countryIndex;
					if (!_countries[c].continent.Equals(continentName))
						newProvinces.Add(_provinces[k]);
				}
			provinces = newProvinces.ToArray();
		}

		/// <summary>
		/// Returns a list of provinces whose attributes matches predicate
		/// </summary>
		public List<Province> GetProvinces(AttribPredicate predicate)
		{
			var selectedProvinces = new List<Province>();
			var provinceCount = provinces.Length;
			for (var k = 0; k < provinceCount; k++)
			{
				var province = _provinces[k];
				if (predicate(province.attrib))
					selectedProvinces.Add(province);
			}
			return selectedProvinces;
		}

		/// <summary>
		/// Returns a list of provinces that are visible in the Game View
		/// </summary>
		public List<Province> GetVisibleProvinces()
		{
			var cam = Application.isPlaying ? currentCamera : Camera.current;
			return GetVisibleProvinces(cam);
		}

		/// <summary>
		/// Returns a list of provinces that are visible (front facing camera)
		/// </summary>
		public List<Province> GetVisibleProvinces(Camera camera)
		{
			if (camera == null)
				return null;
			if (provinces == null)
				return null;
			var vc = GetVisibleCountries();
			var vp = new List<Province>(30);
			for (var k = 0; k < vc.Count; k++)
			{
				var country = vc[k];
				if (country.provinces == null)
					continue;
				for (var p = 0; p < country.provinces.Length; p++)
				{
					var prov = country.provinces[p];
					var center = transform.TransformPoint(prov.center);
					var vpos = camera.WorldToViewportPoint(center);
					if (vpos.x >= 0 && vpos.x <= 1 && vpos.y >= 0 && vpos.y <= 1)
						vp.Add(prov);
				}
			}
			return vp;
		}

		/// <summary>
		/// Returns a list of provinces that are visible inside the window rectangle (constraint rect)
		/// </summary>
		public List<Province> GetVisibleProvincesInWindowRect()
		{
			if (provinces == null)
				return null;
			var vc = GetVisibleCountriesInWindowRect();
			var vp = new List<Province>(30);
			for (var k = 0; k < vc.Count; k++)
			{
				var country = vc[k];
				if (country.provinces == null)
					continue;
				for (var p = 0; p < country.provinces.Length; p++)
				{
					var prov = country.provinces[p];
					if (_windowRect.Contains(prov.center))
						vp.Add(prov);
				}
			}
			return vp;
		}

		/// <summary>
		/// Returns the list of costal positions of a given province
		/// </summary>
		public List<Vector2> GetProvinceCoastalPoints(int provinceIndex, float minDistance = 0.005f)
		{
			var coastalPoints = new List<Vector2>();
			minDistance *= minDistance;
			if (provinces[provinceIndex].regions == null)
				ReadProvincePackedString(provinces[provinceIndex]);
			for (var r = 0; r < provinces[provinceIndex].regions.Count; r++)
			{
				var region = provinces[provinceIndex].regions[r];
				for (var p = 0; p < region.points.Length; p++)
				{
					var position = region.points[p];
					if (ContainsWater(position, 4))
					{
						var valid = true;
						for (var s = coastalPoints.Count - 1; s >= 0; s--)
						{
							var sqrDist =
								FastVector.SqrDistanceByValue(coastalPoints[s],
									position); // (coastalPoints [s] - position).sqrMagnitude;
							if (sqrDist < minDistance)
							{
								valid = false;
								break;
							}
						}
						if (valid)
							coastalPoints.Add(position);
					}
				}
			}
			return coastalPoints;
		}

		/// <summary>
		/// Returns a list of common border points between two provinces.
		/// Use extraWidth to widen the points, useful when using the result to block pass in pathfinding
		/// </summary>
		public List<Vector2> GetProvinceBorderPoints(int provinceIndex1, int provinceIndex2,
			int extraWidth = 0)
		{
			if (provinceIndex1 < 0 ||
			    provinceIndex1 >= provinces.Length ||
			    provinceIndex2 < 0 ||
			    provinceIndex2 >= provinces.Length)
				return null;

			var province1 = provinces[provinceIndex1];
			var province2 = provinces[provinceIndex2];
			var samePoints = new List<Vector2>();

			for (var cr = 0; cr < province1.regions.Count; cr++)
			{
				var region1 = province1.regions[cr];
				for (var n = 0; n < region1.neighbours.Count; n++)
				{
					var otherRegion = region1.neighbours[n];
					if (province2.regions.Contains(otherRegion))
						for (var p = 0; p < region1.points.Length; p++)
						{
							for (var o = 0; o < otherRegion.points.Length; o++)
								if (region1.points[p] == otherRegion.points[o])
									samePoints.Add(region1.points[p]);
						}
				}
			}

			// Adds optional width to the line
			var count = samePoints.Count;
			var dx = new Vector2(1.0f / EARTH_ROUTE_SPACE_WIDTH, 0);
			var dy = new Vector2(0, 1.0f / EARTH_ROUTE_SPACE_HEIGHT);
			for (var p = 0; p < count; p++)
			{
				var point = MatrixCostPositionToMap2D(Map2DToMatrixCostPosition(samePoints[p]));
				for (var k = -extraWidth; k <= extraWidth; k++)
				{
					for (var j = -extraWidth; j <= extraWidth; j++)
					{
						if (k == 0 && j == 0)
							continue;
						var pw = point + k * dx + j * dy;
						if (!samePoints.Contains(pw))
							samePoints.Add(pw);
					}
				}
			}

			return samePoints;
		}

		/// <summary>
		/// Returns the points for the given province region. Optionally in world space coordinates (normal map, not viewport).
		/// </summary>
		public Vector3[] GetProvinceBorderPoints(int provinceIndex, bool worldSpace)
		{
			if (provinceIndex < 0 || provinceIndex >= provinces.Length)
				return null;
			return GetProvinceBorderPoints(provinceIndex, provinces[provinceIndex].mainRegionIndex,
				worldSpace);
		}

		/// <summary>
		/// Returns the points for the given province region. Optionally in world space coordinates (normal map, not viewport).
		/// </summary>
		public Vector3[] GetProvinceBorderPoints(int provinceIndex, int regionIndex, bool worldSpace)
		{
			if (provinceIndex < 0 || provinceIndex >= countries.Length)
				return null;
			if (regionIndex < 0 || regionIndex >= provinces[provinceIndex].regions.Count)
				return null;

			var region = provinces[provinceIndex].regions[regionIndex];
			var pointsCount = region.points.Length;
			var points = new Vector3[pointsCount];
			if (worldSpace)
				for (var k = 0; k < pointsCount; k++)
					points[k] = transform.TransformPoint(region.points[k]);
			else
				for (var k = 0; k < pointsCount; k++)
					points[k] = region.points[k];
			return points;
		}

		/// <summary>
		/// Returns the zoom level required to show the entire province region on screen
		/// </summary>
		/// <returns>The province zoom level of -1 if error.</returns>
		/// <param name="provinceIndex">Province index.</param>
		public float GetProvinceRegionZoomExtents(int provinceIndex)
		{
			if (provinceIndex < 0 || provinces == null || provinceIndex >= provinces.Length)
				return -1;
			return GetProvinceRegionZoomExtents(provinceIndex, provinces[provinceIndex].mainRegionIndex);
		}

		/// <summary>
		/// Returns the zoom level required to show the entire province region on screen
		/// </summary>
		/// <returns>The province zoom level of -1 if error.</returns>
		/// <param name="provinceIndex">Country index.</param>
		/// <param name="regionIndex">Region index of the country.</param>
		public float GetProvinceRegionZoomExtents(int provinceIndex, int regionIndex)
		{
			if (provinceIndex < 0 || provinces == null || provinceIndex >= provinces.Length)
				return -1;
			var province = provinces[provinceIndex];
			if (province.regions == null)
				ReadProvincePackedString(province);
			if (regionIndex < 0 || regionIndex >= province.regions.Count)
				return -1;
			var region = province.regions[regionIndex];
			return GetFrustumZoomLevel(region.rect2D.width * mapWidth, region.rect2D.height * mapHeight);
		}

		/// <summary>
		/// Returns the zoom level required to show the entire province (including all regions) on screen
		/// </summary>
		/// <returns>The province zoom level of -1 if error.</returns>
		/// <param name="provinceIndex">Province index.</param>
		public float GetProvinceZoomExtents(int provinceIndex)
		{
			if (provinceIndex < 0 || provinces == null || provinceIndex >= provinces.Length)
				return -1;
			var province = provinces[provinceIndex];
			if (province.regions == null)
				ReadProvincePackedString(province);
			return GetFrustumZoomLevel(province.regionsRect2D.width * mapWidth,
				province.regionsRect2D.height * mapHeight);
		}

		/// <summary>
		/// Checks quality of province's polygon points. Useful before using polygon clipping operations.
		/// </summary>
		/// <returns><c>true</c>, if province was changed, <c>false</c> otherwise.</returns>
		public bool ProvinceSanitize(int provinceIndex, int minimumPoints = 3, bool refresh = true)
		{
			if (provinceIndex < 0 || provinceIndex >= provinces.Length)
				return false;

			var province = provinces[provinceIndex];
			var changes = false;
			if (province.regions != null)
				for (var k = 0; k < province.regions.Count; k++)
				{
					var region = province.regions[k];
					if (RegionSanitize(region))
						changes = true;
					if (region.points.Length < minimumPoints)
					{
						province.regions.Remove(region);
						if (province.regions == null)
							return true;
						k--;
						changes = true;
					}
				}
			if (changes)
			{
				province.mainRegionIndex = 0;
				if (refresh)
					RefreshProvinceGeometry(provinceIndex);
			}
			return changes;
		}

		/// <summary>
		/// Makes provinceIndex absorb another province providing any of its regions. All regions are transfered to target province.
		/// This function is quite slow with high definition frontiers.
		/// </summary>
		/// <param name="provinceIndex">Province index of the conquering province.</param>
		/// <param name="sourceRegion">Source region of the loosing province.</param>
		/// <param name="redraw">If set to true, map will be redrawn after operation finishes.</param>
		public bool ProvinceTransferProvinceRegion(int provinceIndex, Region sourceProvinceRegion,
			bool redraw)
		{
			var sourceProvinceIndex = GetProvinceIndex((Province)sourceProvinceRegion.entity);
			if (provinceIndex < 0 || sourceProvinceIndex < 0 || provinceIndex == sourceProvinceIndex)
				return false;

			// Transfer cities
			var sourceProvince = provinces[sourceProvinceIndex];
			var targetProvince = provinces[provinceIndex];
			if (sourceProvince.countryIndex !=
			    targetProvince.countryIndex) // Transfer source province to target country province
				if (!CountryTransferProvinceRegion(targetProvince.countryIndex, sourceProvinceRegion,
					false))
					return false;

			var cityCount = cities.Length;
			for (var k = 0; k < cityCount; k++)
				if (_cities[k].countryIndex == sourceProvince.countryIndex &&
				    _cities[k].province.Equals(sourceProvince.name))
					_cities[k].province = targetProvince.name;

			// Transfer mount points
			var mountPointCount = mountPoints.Count;
			for (var k = 0; k < mountPointCount; k++)
				if (mountPoints[k].provinceIndex == sourceProvinceIndex)
					mountPoints[k].provinceIndex = provinceIndex;

			// Transfer regions
			var sprCount = sourceProvince.regions.Count;
			if (sprCount > 0)
			{
				var targetRegions = new List<Region>(targetProvince.regions);
				for (var k = 0; k < sprCount; k++)
					targetRegions.Add(sourceProvince.regions[k]);
				targetProvince.regions = targetRegions;
			}

			// Fusion any adjacent regions that results from merge operation
			MergeAdjacentRegions(targetProvince);
			RegionSanitize(targetProvince.regions);
			targetProvince.mainRegionIndex = 0; // will be updated on RefreshProvinceDefinition

			// Finish operation
			ProvinceDelete(sourceProvinceIndex);
			if (provinceIndex > sourceProvinceIndex)
				provinceIndex--;
			RefreshProvinceDefinition(provinceIndex, true);
			ResortCountryProvinces(provinces[provinceIndex].countryIndex);
			if (redraw)
				Redraw(true);
			return true;
		}

		/// <summary>
		/// Makes provinceIndex absorb an hexagonal portion of the map. If that portion belong to another province, it will be substracted from that province as well.
		/// This function is quite slow with high definition frontiers.
		/// </summary>
		/// <param name="provinceIndex">Province index of the conquering province.</param>
		/// <param name="cellIndex">Index of the cell to add to the province.</param>
		public bool ProvinceTransferCell(int provinceIndex, int cellIndex, bool redraw = true)
		{
			if (provinceIndex < 0 || cellIndex < 0 || cells == null || cellIndex >= cells.Length)
				return false;

			// Start process
			var province = provinces[provinceIndex];
			var cell = cells[cellIndex];
			var countryIndex = province.countryIndex;

			// Create a region for the cell
			var sourceRegion = new Region(province, province.regions.Count);
			sourceRegion.UpdatePointsAndRect(cell.points, true);

			// Transfer cities
			var citiesInCell = GetCities(sourceRegion);
			var cityCount = citiesInCell.Count;
			for (var k = 0; k < cityCount; k++)
			{
				var city = citiesInCell[k];
				if (city.countryIndex != countryIndex)
				{
					city.countryIndex = countryIndex;
					city.province = province.name;
				}
			}

			// Transfer mount points
			var mountPointsInCell = GetMountPoints(sourceRegion);
			var mountPointCount = mountPointsInCell.Count;
			for (var k = 0; k < mountPointCount; k++)
			{
				var mp = mountPointsInCell[k];
				if (mp.countryIndex != countryIndex)
				{
					mp.countryIndex = countryIndex;
					mp.provinceIndex = provinceIndex;
				}
			}

			// Add region to target country's polygon - only if the country is touching or crossing target country frontier
			var targetRegion = province.mainRegion;
			if (targetRegion != null && sourceRegion.Intersects(targetRegion))
			{
				RegionMagnet(sourceRegion, targetRegion);
				var clipper = new Clipper();
				clipper.AddPath(targetRegion, PolyType.ptSubject);
				clipper.AddPath(sourceRegion, PolyType.ptClip);
				clipper.Execute(ClipType.ctUnion);
			}
			else
			{
				// Add new region to country
				sourceRegion.entity = province;
				sourceRegion.regionIndex = province.regions.Count;
				province.regions.Add(sourceRegion);
			}

			// Fusion any adjacent regions that results from merge operation
			MergeAdjacentRegions(province);
			RegionSanitize(province.regions);

			// Finish operation with the country
			RefreshProvinceGeometry(provinceIndex);

			// Substract cell region from any other country
			var otherProvinces = GetProvincesOverlap(sourceRegion);
			var orCount = otherProvinces.Count;
			for (var k = 0; k < orCount; k++)
			{
				var otherProvince = otherProvinces[k];
				if (otherProvince == province)
					continue;
				var clipper = new Clipper();
				clipper.AddPaths(otherProvince.regions, PolyType.ptSubject);
				clipper.AddPath(sourceRegion, PolyType.ptClip);
				clipper.Execute(ClipType.ctDifference, otherProvince);
				var otherProvinceIndex = GetProvinceIndex(otherProvince);
				if (otherProvince.regions.Count == 0)
					ProvinceDelete(otherProvinceIndex);
				else
				{
					RegionSanitize(otherProvince.regions);
					RefreshProvinceDefinition(otherProvinceIndex, true);
				}
			}

			if (redraw)
				Redraw();
			return true;
		}

		/// <summary>
		/// Removes a cell from a province.
		/// </summary>
		/// <param name="provinceIndex">Province index.</param>
		/// <param name="cellIndex">Index of the cell to remove from the province.</param>
		public bool ProvinceRemoveCell(int provinceIndex, int cellIndex, bool redraw = true)
		{
			if (provinceIndex < 0 ||
			    provinceIndex >= provinces.Length ||
			    cellIndex < 0 ||
			    cells == null ||
			    cellIndex >= cells.Length)
				return false;

			var province = provinces[provinceIndex];
			var cell = cells[cellIndex];
			var sourceRegion = new Region(province, province.regions.Count);
			sourceRegion.UpdatePointsAndRect(cell.points, true);

			var clipper = new Clipper();
			clipper.AddPaths(province.regions, PolyType.ptSubject);
			clipper.AddPath(sourceRegion, PolyType.ptClip);
			clipper.Execute(ClipType.ctDifference, province);
			if (province.regions.Count == 0)
				ProvinceDelete(provinceIndex);
			else
			{
				RegionSanitize(province.regions);
				RefreshProvinceGeometry(provinceIndex);
			}

			OptimizeFrontiers();

			if (redraw)
				Redraw();
			return true;
		}

		/// <summary>
		/// Returns the colored surface (game object) of a province. If it has not been colored yet, it will return null.
		/// </summary>
		public GameObject GetProvinceRegionSurfaceGameObject(int provinceIndex, int regionIndex)
		{
			var cacheIndex = GetCacheIndexForProvinceRegion(provinceIndex, regionIndex);
			GameObject obj = null;
			if (surfaces.TryGetValue(cacheIndex, out obj))
				return obj;
			return null;
		}

		/// <summary>
		/// Draws the labels for the provinces of a given country
		/// </summary>
		/// <param name="country">Country.</param>
		public void DrawProvinceLabels(int countryIndex)
		{
			if (countryIndex < 0 || countryIndex >= countries.Length)
				return;
			DrawProvinceLabels(_countries[countryIndex]);
		}

		/// <summary>
		/// Draws the labels for the provinces of a given country
		/// </summary>
		/// <param name="country">Country.</param>
		public void DrawProvinceLabels(Country country)
		{
			DrawProvinceLabelsInt(country);
		}

		/// <summary>
		/// Hides all province labels
		/// </summary>
		public void HideProvinceLabels()
		{
			DestroyProvinceLabels();
		}

		#endregion

		#region IO functions area

		/// <summary>
		/// Exports the geographic data in packed string format.
		/// </summary>
		public string GetProvinceGeoData()
		{
			var sb = new StringBuilder();
			for (var k = 0; k < provinces.Length; k++)
			{
				var province = provinces[k];
				var countryIndex = province.countryIndex;
				if (countryIndex < 0 || countryIndex >= countries.Length)
					continue;
				var countryName = countries[countryIndex].name;
				if (k > 0)
					sb.Append("|");
				sb.Append(province.name);
				sb.Append("$");
				sb.Append(countryName);
				sb.Append("$");
				if (province.regions == null)
					ReadProvincePackedString(province);
				if (province.regions != null)
				{
					var provinceRegionsCount = province.regions.Count;
					for (var r = 0; r < provinceRegionsCount; r++)
					{
						if (r > 0)
							sb.Append("*");
						var region = province.regions[r];
						for (var p = 0; p < region.points.Length; p++)
						{
							if (p > 0)
								sb.Append(";");
							var x = (int)(region.points[p].x * MAP_PRECISION);
							var y = (int)(region.points[p].y * MAP_PRECISION);
							sb.Append(x.ToString(Misc.InvariantCulture));
							sb.Append(",");
							sb.Append(y.ToString(Misc.InvariantCulture));
						}
					}
				}
				sb.Append("$");
				sb.Append(province.uniqueId);
			}
			return sb.ToString();
		}

		public void SetProvincesGeoData(string s)
		{
			var provincesPackedStringData =
				s.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
			var provinceCount = provincesPackedStringData.Length;
			var newProvinces = new List<Province>(provinceCount);
			var countryProvinces = new List<Province>[_countries.Length];
			var separatorProvinces = new char[] { '$' };
			for (var k = 0; k < provinceCount; k++)
			{
				var provinceInfo = provincesPackedStringData[k].Split(separatorProvinces);
				if (provinceInfo.Length <= 2)
					continue;
				var name = provinceInfo[0];
				var countryName = provinceInfo[1];
				var countryIndex = GetCountryIndex(countryName);
				if (countryIndex >= 0)
				{
					int uniqueId;
					if (provinceInfo.Length >= 4)
						uniqueId = int.Parse(provinceInfo[3]);
					else
						uniqueId = GetUniqueId(new List<IExtendableAttribute>(newProvinces.ToArray()));
					var province = new Province(name, countryIndex, uniqueId);
					province.packedRegions = provinceInfo[2];
					newProvinces.Add(province);
					if (countryProvinces[countryIndex] == null)
						countryProvinces[countryIndex] = new List<Province>(50);
					countryProvinces[countryIndex].Add(province);
				}
			}
			provinces = newProvinces.ToArray();
			for (var k = 0; k < _countries.Length; k++)
				if (countryProvinces[k] != null)
				{
					countryProvinces[k].Sort(ProvinceSizeComparer);
					_countries[k].provinces = countryProvinces[k].ToArray();
				}

			ReloadProvincesAttributes();
		}

		/// <summary>
		/// Gets XML attributes of all provinces in jSON format.
		/// </summary>
		public string GetProvincesAttributes(bool prettyPrint = true) =>
			GetProvincesAttributes(new List<Province>(provinces), prettyPrint);

		/// <summary>
		/// Gets XML attributes of provided provinces in jSON format.
		/// </summary>
		public string GetProvincesAttributes(List<Province> provinces, bool prettyPrint = true)
		{
			var composed = new JSONObject();
			var provinceCount = provinces.Count;
			for (var k = 0; k < provinceCount; k++)
			{
				var province = _provinces[k];
				if (province.attrib.keys != null)
					composed.AddField(province.uniqueId.ToString(), province.attrib);
			}
			return composed.Print(prettyPrint);
		}

		/// <summary>
		/// Sets provinces attributes from a jSON formatted string.
		/// </summary>
		public void SetProvincesAttributes(string jSON)
		{
			var composed = new JSONObject(jSON);
			if (composed.keys == null)
				return;
			var keyCount = composed.keys.Count;
			for (var k = 0; k < keyCount; k++)
			{
				var uniqueId = int.Parse(composed.keys[k]);
				var provinceIndex = GetProvinceIndex(uniqueId);
				if (provinceIndex >= 0)
					provinces[provinceIndex].attrib = composed[k];
			}
		}

		#endregion

		#region Province Color Map Methods

		/// <summary>
		/// Analizes and generate new countries based on a color map
		/// </summary>
		/// <returns>The provinces color map.</returns>
		/// <param name="tex">Tex.</param>
		public int ImportProvincesColorMap(string filename)
		{
			if (!File.Exists(filename))
				return 0;

			// Load texture data
			var fileData = File.ReadAllBytes(filename);
			var tex = new Texture2D(2, 2, TextureFormat.ARGB32, false);
			tex.LoadImage(fileData);

			var texColors = tex.GetPixels();
			var width = tex.width;
			var height = tex.height;
			var provCount = provinces.Length;
			var countryCount = 0;
			// Map provinces to new countries
			var colorsAndProvinces = new Dictionary<Color, List<Province>>();
			for (var prov = 0; prov < provCount; prov++)
			{
				// gets the color of this province in the texture
				var province = _provinces[prov];
				if (province.regions == null)
					ReadProvincePackedString(province);
				var localPos = province.center;
				var pointInside = false;
				var countryColor = Misc.ColorBlack;
				for (var t = 0; t < 100; t++)
				{
					if (province.mainRegion.Contains(localPos))
					{
						var texCoords = Conversion.ConvertToTextureCoordinates(localPos, width, height);
						var colorIndex = (int)(texCoords.y * width + texCoords.x);
						countryColor = texColors[colorIndex];
						if (countryColor.a > 0.5f)
						{
							pointInside = true;
							break;
						}
					}
					localPos.x = UnityEngine.Random.Range(province.mainRegion.rect2D.xMin,
						province.mainRegion.rect2D.xMax);
					localPos.y = UnityEngine.Random.Range(province.mainRegion.rect2D.yMin,
						province.mainRegion.rect2D.yMax);
				}
				if (!pointInside)
					continue;

				List<Province> countryProvinces;
				if (!colorsAndProvinces.TryGetValue(countryColor, out countryProvinces))
				{
					countryProvinces = new List<Province>();
					colorsAndProvinces[countryColor] = countryProvinces;
					countryCount++;
				}
				countryProvinces.Add(province);
			}

			// Generate new countries
			var newCountries = new Country[countryCount];
			var countryIndex = 0;
			foreach (var kvp in colorsAndProvinces)
			{
				var color = kvp.Key;
				var provinces = kvp.Value;

				// Try to get the continent from first province
				var province = provinces[0];
				var continent = "Continent";
				var country = GetCountry(province.center);
				if (country != null)
					continent = country.continent;
				var newCountry = new Country("Country " + countryIndex, continent,
					GetUniqueId(new List<IExtendableAttribute>(newCountries)));
				newCountries[countryIndex] = newCountry;
				var pCount = provinces.Count;
				for (var p = 0; p < pCount; p++)
				{
					province = provinces[p];
					province.countryIndex = countryIndex;
					newCountry.provinces = provinces.ToArray();
					var regCount = province.regions.Count;
					for (var pr = 0; pr < regCount; pr++)
					{
						var reg = province.regions[pr].Clone();
						newCountry.regions.Add(reg);
					}
				}
				countryIndex++;
			}

			// Assign new countries
			countries = newCountries;

			// Merge adjacent regions
			for (var k = 0; k < _countries.Length; k++)
			{
				MergeAdjacentRegions(_countries[k]);
				CountrySanitize(k);
				RefreshCountryGeometry(_countries[k]);
			}

			// Remap cities & mount points
			var citiesCount = cities.Length;
			var newCities = new List<City>(citiesCount);
			for (var c = 0; c < citiesCount; c++)
			{
				var pi = GetProvinceIndex(_cities[c].unity2DLocation);
				if (pi >= 0)
				{
					_cities[c].province = _provinces[pi].name;
					_cities[c].countryIndex = _provinces[pi].countryIndex;
					newCities.Add(_cities[c]);
				}
				else
				{
					var ci = GetCountryIndex(_cities[c].unity2DLocation);
					if (ci >= 0)
					{
						_cities[c].countryIndex = ci;
						_cities[c].province = "";
						newCities.Add(_cities[c]);
					}
				}
			}
			cities = newCities.ToArray();

			var mountpointsCount = mountPoints.Count;
			var newMountpoints = new List<MountPoint>(mountpointsCount);
			for (var c = 0; c < mountpointsCount; c++)
			{
				var ci = GetCountryIndex(_cities[c].unity2DLocation);
				if (ci >= 0)
				{
					mountPoints[c].countryIndex = ci;
					newMountpoints.Add(mountPoints[c]);
				}
			}
			mountPoints = newMountpoints;

			Redraw(true);

			return countryIndex;
		}

		#endregion
	}
}