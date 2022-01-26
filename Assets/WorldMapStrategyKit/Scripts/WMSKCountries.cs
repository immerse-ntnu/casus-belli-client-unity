// World Strategy Kit for Unity - Main Script
// (C) 2016-2020 by Ramiro Oliva (Kronnect)
// Don't modify this script - changes could be lost if you upgrade to a more recent version of WMSK

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using WorldMapStrategyKit.ClipperLib;
using Object = UnityEngine.Object;

namespace WorldMapStrategyKit
{
	public enum FRONTIERS_DETAIL
	{
		Low = 0,
		High = 1
	}

	public enum OUTLINE_DETAIL
	{
		Simple = 0,
		Textured = 1
	}

	public enum TEXT_ENGINE
	{
		TextMeshStandard = 0,
		TextMeshPro = 1
	}

	public delegate void OnCountryEvent(int countryIndex, int regionIndex);

	public delegate void OnCountryClickEvent(int countryIndex, int regionIndex, int buttonIndex);

	public delegate void OnCountryHighlightEvent(int countryIndex, int regionIndex,
		ref bool allowHighlight);

	public partial class WMSK : MonoBehaviour
	{
		#region Public properties

		private Country[] _countries;

		/// <summary>
		/// Complete array of countries and the continent name they belong to.
		/// </summary>
		public Country[] countries
		{
			get => _countries;
			set
			{
				_countries = value;
				lastCountryLookupCount = -1;
			}
		}

		private Country _countryHighlighted;

		/// <summary>
		/// Returns Country under mouse position or null if none.
		/// </summary>
		public Country countryHighlighted => _countryHighlighted;

		private int _countryHighlightedIndex = -1;

		/// <summary>
		/// Returns currently highlighted country index in the countries list.
		/// </summary>
		public int countryHighlightedIndex => _countryHighlightedIndex;

		private Region _countryRegionHighlighted;

		/// <summary>
		/// Returns currently highlightd country's region.
		/// </summary>
		/// <value>The country region highlighted.</value>
		public Region countryRegionHighlighted => _countryRegionHighlighted;

		private int _countryRegionHighlightedIndex = -1;

		/// <summary>
		/// Returns currently highlighted region of the country.
		/// </summary>
		public int countryRegionHighlightedIndex => _countryRegionHighlightedIndex;

		private int _countryLastClicked = -1;

		/// <summary>
		/// Returns the last clicked country.
		/// </summary>
		public int countryLastClicked => _countryLastClicked;

		private int _countryRegionLastClicked = -1;

		/// <summary>
		/// Returns the last clicked country region index.
		/// </summary>
		public int countryRegionLastClicked => _countryRegionLastClicked;

		/// <summary>
		/// Gets the country region's highlighted shape.
		/// </summary>
		public GameObject countryRegionHighlightedShape => countryRegionHighlightedObj;

		public event OnCountryEvent OnCountryEnter;
		public event OnCountryEvent OnCountryExit;
		public event OnCountryClickEvent OnCountryClick;
		public event OnCountryHighlightEvent OnCountryHighlight;

		/// <summary>
		/// Enable/disable country highlight when mouse is over.
		/// </summary>
		[SerializeField] private bool
			_enableCountryHighlight = true;

		public bool enableCountryHighlight
		{
			get => _enableCountryHighlight;
			set
			{
				if (_enableCountryHighlight != value)
				{
					_enableCountryHighlight = value;
					isDirty = true;
				}
			}
		}

		/// <summary>
		/// Set whether all regions of active country should be highlighted.
		/// </summary>
		[SerializeField] private bool
			_highlightAllCountryRegions;

		public bool highlightAllCountryRegions
		{
			get => _highlightAllCountryRegions;
			set
			{
				if (_highlightAllCountryRegions != value)
				{
					_highlightAllCountryRegions = value;
					DestroySurfaces();
					isDirty = true;
				}
			}
		}

		[SerializeField] private float
			_highlightMaxScreenAreaSize = 1f;

		/// <summary>
		/// Defines the maximum area of a highlighted country or province. To prevent filling the whole screen with the highlight color, you can reduce this value and if the highlighted screen area size is greater than this factor (1=whole screen) the country won't be filled (it will behave as selected though)
		/// </summary>
		public float highlightMaxScreenAreaSize
		{
			get => _highlightMaxScreenAreaSize;
			set
			{
				if (_highlightMaxScreenAreaSize != value)
				{
					_highlightMaxScreenAreaSize = value;
					isDirty = true;
				}
			}
		}

		[SerializeField] private bool
			_showFrontiers = true;

		/// <summary>
		/// Toggle frontiers visibility.
		/// </summary>
		public bool showFrontiers
		{
			get => _showFrontiers;
			set
			{
				if (value != _showFrontiers)
				{
					_showFrontiers = value;
					isDirty = true;

					if (frontiersLayer != null)
						frontiersLayer.SetActive(_showFrontiers);
					else if (_showFrontiers)
						DrawFrontiers();
				}
			}
		}

		[SerializeField] private bool
			_frontiersCoastlines = true;

		/// <summary>
		/// Include coasts in frontier lines
		/// </summary>
		public bool frontiersCoastlines
		{
			get => _frontiersCoastlines;
			set
			{
				if (value != _frontiersCoastlines)
				{
					_frontiersCoastlines = value;
					isDirty = true;
					needOptimizeFrontiers = true;
					DrawFrontiers();
				}
			}
		}

		/// <summary>
		/// Fill color to use when the mouse hovers a country's region.
		/// </summary>
		[SerializeField] private Color
			_fillColor = new(1, 0, 0, 0.7f);

		public Color fillColor
		{
			get
			{
				if (hudMatCountry != null)
					return hudMatCountry.color;
				return _fillColor;
			}
			set
			{
				if (value != _fillColor)
				{
					_fillColor = value;
					isDirty = true;
					if (hudMatCountry != null && _fillColor != hudMatCountry.color)
						hudMatCountry.color = _fillColor;
				}
			}
		}

		/// <summary>
		/// Inner Color for country frontiers.
		/// </summary>
		[SerializeField] private Color
			_frontiersColor = Color.green;

		public Color frontiersColor
		{
			get
			{
				if (frontiersMat != null)
					return frontiersMat.color;
				return _frontiersColor;
			}
			set
			{
				if (value != _frontiersColor)
				{
					_frontiersColor = value;
					isDirty = true;

					if (frontiersMat != null && _frontiersColor != frontiersMat.color)
						frontiersMat.color = _frontiersColor;
				}
			}
		}

		/// <summary>
		/// Outer color for country frontiers.
		/// </summary>
		[SerializeField] private Color
			_frontiersColorOuter = new(0, 1, 0, 0.5f);

		public Color frontiersColorOuter
		{
			get => _frontiersColorOuter;
			set
			{
				if (value != _frontiersColorOuter)
				{
					_frontiersColorOuter = value;
					isDirty = true;

					if (frontiersMat != null)
						frontiersMat.SetColor("_OuterColor", _frontiersColorOuter);
				}
			}
		}

		[SerializeField] private bool
			_thickerFrontiers;

		/// <summary>
		/// Enable alternate frontiers shader.
		/// </summary>
		public bool thickerFrontiers
		{
			get => _thickerFrontiers;
			set
			{
				if (value != _thickerFrontiers)
				{
					_thickerFrontiers = value;
					isDirty = true;
					UpdateFrontiersMaterial();
					Redraw();
				}
			}
		}

		[SerializeField] private bool
			_frontiersDynamicWidth = true;

		/// <summary>
		/// Enable dynamic width of country frontiers when zooming in/out
		/// </summary>
		public bool frontiersDynamicWidth
		{
			get => _frontiersDynamicWidth;
			set
			{
				if (value != _frontiersDynamicWidth)
				{
					_frontiersDynamicWidth = value;
					isDirty = true;
					UpdateFrontiersMaterial();
					Redraw();
				}
			}
		}

		[SerializeField] private float _frontiersWidth = 0.05f;

		public float frontiersWidth
		{
			get => _frontiersWidth;
			set
			{
				if (value != _frontiersWidth)
				{
					_frontiersWidth = value;
					isDirty = true;

					if (frontiersMat != null)
						frontiersMat.SetFloat("_Thickness", _frontiersWidth);
				}
			}
		}

		[SerializeField] private bool
			_showOutline = true;

		/// <summary>
		/// Toggle frontiers thicker outline visibility.
		/// </summary>
		public bool showOutline
		{
			get => _showOutline;
			set
			{
				if (value != _showOutline)
				{
					_showOutline = value;
					Redraw(); // recreate surfaces layer
					isDirty = true;
				}
			}
		}

		/// <summary>
		/// Color for country frontiers outline.
		/// </summary>
		[SerializeField] private Color
			_outlineColor = Color.black;

		public Color outlineColor
		{
			get
			{
				if (outlineMat != null)
					return outlineMat.color;
				return _outlineColor;
			}
			set
			{
				if (value != _outlineColor)
				{
					_outlineColor = value;
					isDirty = true;

					if (outlineMat != null && _outlineColor != outlineMat.color)
						outlineMat.color = _outlineColor;
				}
			}
		}

		[SerializeField] private OUTLINE_DETAIL
			_outlineDetail = OUTLINE_DETAIL.Simple;

		/// <summary>
		/// Quality level for outline.
		/// </summary>
		public OUTLINE_DETAIL outlineDetail
		{
			get => _outlineDetail;
			set
			{
				if (value != _outlineDetail)
				{
					_outlineDetail = value;
					Redraw(); // recreate surfaces layer
					isDirty = true;
				}
			}
		}

		[SerializeField] private Texture2D
			_outlineTexture;

		/// <summary>
		/// Texture for the outline when outlineDetail is set to Textured.
		/// </summary>
		public Texture2D outlineTexture
		{
			get => _outlineTexture;
			set
			{
				if (value != _outlineTexture)
				{
					_outlineTexture = value;
					if (outlineMatTextured != null)
						outlineMatTextured.mainTexture = _outlineTexture;
					isDirty = true;
				}
			}
		}

		[SerializeField] private float _outlineWidth = 0.1f;

		public float outlineWidth
		{
			get => _outlineWidth;
			set
			{
				if (value != _outlineWidth)
				{
					_outlineWidth = value;
					Redraw(); // recreate surfaces layer
					isDirty = true;
				}
			}
		}

		[SerializeField] private float _outlineTilingScale = 1f;

		public float outlineTilingScale
		{
			get => _outlineTilingScale;
			set
			{
				if (value != _outlineWidth)
				{
					_outlineTilingScale = value;
					if (outlineMatTextured != null)
						outlineMatTextured.mainTextureScale = new Vector2(_outlineTilingScale, 1f);
					isDirty = true;
				}
			}
		}

		[SerializeField, Range(-5f, 5f)] private float _outlineAnimationSpeed = -1f;

		public float outlineAnimationSpeed
		{
			get => _outlineAnimationSpeed;
			set
			{
				if (value != _outlineAnimationSpeed)
				{
					_outlineAnimationSpeed = value;
					isDirty = true;
				}
			}
		}

		[SerializeField] private FRONTIERS_DETAIL
			_frontiersDetail = FRONTIERS_DETAIL.Low;

		public FRONTIERS_DETAIL frontiersDetail
		{
			get => _frontiersDetail;
			set
			{
				if (_frontiersDetail != value)
				{
					_frontiersDetail = value;
					isDirty = true;
					ReloadData();
					Redraw();
				}
			}
		}

		[SerializeField] private bool
			_showCountryNames;

		public bool showCountryNames
		{
			get => _showCountryNames;
			set
			{
				if (value != _showCountryNames)
				{
					_showCountryNames = value;
					isDirty = true;
					if (textRoot != null)
						textRoot.SetActive(_showCountryNames);
					else
						DrawMapLabels();
				}
			}
		}

		[SerializeField] private TEXT_ENGINE _countryLabelsTextEngine = TEXT_ENGINE.TextMeshStandard;

		public TEXT_ENGINE countryLabelsTextEngine
		{
			get => _countryLabelsTextEngine;
			set
			{
				if (_countryLabelsTextEngine != value)
				{
					_countryLabelsTextEngine = value;
					isDirty = true;
					DrawMapLabels();
				}
			}
		}

		[SerializeField] private float
			_countryLabelsLength = 0.8f;

		public float countryLabelsLength
		{
			get => _countryLabelsLength;
			set
			{
				if (value != _countryLabelsLength)
				{
					_countryLabelsLength = value;
					isDirty = true;
					DrawMapLabels();
				}
			}
		}

		[SerializeField] private float
			_countryLabelsHorizontality = 2f;

		public float countryLabelsHorizontality
		{
			get => _countryLabelsHorizontality;
			set
			{
				if (value != _countryLabelsHorizontality)
				{
					_countryLabelsHorizontality = value;
					isDirty = true;
					DrawMapLabels();
				}
			}
		}

		[SerializeField] private float
			_countryLabelsCurvature = 1f;

		public float countryLabelsCurvature
		{
			get => _countryLabelsCurvature;
			set
			{
				if (value != _countryLabelsCurvature)
				{
					_countryLabelsCurvature = value;
					isDirty = true;
					DrawMapLabels();
				}
			}
		}

		[SerializeField] private float
			_countryLabelsAbsoluteMinimumSize = 0.5f;

		public float countryLabelsAbsoluteMinimumSize
		{
			get => _countryLabelsAbsoluteMinimumSize;
			set
			{
				if (value != _countryLabelsAbsoluteMinimumSize)
				{
					_countryLabelsAbsoluteMinimumSize = value;
					isDirty = true;
					DrawMapLabels();
				}
			}
		}

		[SerializeField] private float
			_countryLabelsSize = 0.25f;

		public float countryLabelsSize
		{
			get => _countryLabelsSize;
			set
			{
				if (value != _countryLabelsSize)
				{
					_countryLabelsSize = value;
					isDirty = true;
					DrawMapLabels();
				}
			}
		}

		[SerializeField] private bool _countryLabelsEnableAutomaticFade = true;

		/// <summary>
		/// Automatic fading of country labels depending on camera distance and label screen size
		/// </summary>
		public bool countryLabelsEnableAutomaticFade
		{
			get => _countryLabelsEnableAutomaticFade;
			set
			{
				if (_countryLabelsEnableAutomaticFade != value)
				{
					_countryLabelsEnableAutomaticFade = value;
					isDirty = true;
					DrawMapLabels();
				}
			}
		}

		[SerializeField] private float
			_countryLabelsAutoFadeMaxHeight = 0.2f;

		/// <summary>
		/// Max height of a label relative to screen height (0..1) at which fade out starts
		/// </summary>
		public float countryLabelsAutoFadeMaxHeight
		{
			get => _countryLabelsAutoFadeMaxHeight;
			set
			{
				if (value != _countryLabelsAutoFadeMaxHeight)
				{
					_countryLabelsAutoFadeMaxHeight = value;
					_countryLabelsAutoFadeMinHeight = Mathf.Min(_countryLabelsAutoFadeMaxHeight,
						_countryLabelsAutoFadeMinHeight);
					isDirty = true;
					FadeCountryLabels();
				}
			}
		}

		[SerializeField] private float
			_countryLabelsAutoFadeMaxHeightFallOff = 0.5f;

		/// <summary>
		/// Fall off for fade labels when height is greater than min height
		/// </summary>
		public float countryLabelsAutoFadeMaxHeightFallOff
		{
			get => _countryLabelsAutoFadeMaxHeightFallOff;
			set
			{
				if (value != _countryLabelsAutoFadeMaxHeightFallOff)
				{
					_countryLabelsAutoFadeMaxHeightFallOff = value;
					isDirty = true;
					FadeCountryLabels();
				}
			}
		}

		[SerializeField] private float
			_countryLabelsAutoFadeMinHeight = 0.018f;

		/// <summary>
		/// Min height of a label relative to screen height (0..1) at which fade out starts
		/// </summary>
		public float countryLabelsAutoFadeMinHeight
		{
			get => _countryLabelsAutoFadeMinHeight;
			set
			{
				if (value != _countryLabelsAutoFadeMinHeight)
				{
					_countryLabelsAutoFadeMinHeight = value;
					_countryLabelsAutoFadeMaxHeight = Mathf.Max(_countryLabelsAutoFadeMaxHeight,
						_countryLabelsAutoFadeMinHeight);
					isDirty = true;
					FadeCountryLabels();
				}
			}
		}

		[SerializeField] private float
			_countryLabelsAutoFadeMinHeightFallOff = 0.005f;

		/// <summary>
		/// Fall off for fade labels when height is less than min height
		/// </summary>
		public float countryLabelsAutoFadeMinHeightFallOff
		{
			get => _countryLabelsAutoFadeMinHeightFallOff;
			set
			{
				if (value != _countryLabelsAutoFadeMinHeightFallOff)
				{
					_countryLabelsAutoFadeMinHeightFallOff = value;
					isDirty = true;
					FadeCountryLabels();
				}
			}
		}

		[SerializeField] private bool
			_showLabelsShadow = true;

		/// <summary>
		/// Draws a shadow under map labels. Specify the color using labelsShadowColor.
		/// </summary>
		/// <value><c>true</c> if show labels shadow; otherwise, <c>false</c>.</value>
		public bool showLabelsShadow
		{
			get => _showLabelsShadow;
			set
			{
				if (value != _showLabelsShadow)
				{
					_showLabelsShadow = value;
					isDirty = true;
					DrawMapLabels();
				}
			}
		}

		[SerializeField] private Color
			_countryLabelsColor = Color.white;

		/// <summary>
		/// Color for map labels.
		/// </summary>
		public Color countryLabelsColor
		{
			get => _countryLabelsColor;
			set
			{
				if (value != _countryLabelsColor)
				{
					_countryLabelsColor = value;
					isDirty = true;
					if (gameObject.activeInHierarchy)
					{
						if (!Application.isPlaying ||
						    _countryLabelsTextEngine != TEXT_ENGINE.TextMeshStandard)
							DrawMapLabels();
						else
						{
							if (labelsFont != null && labelsFont.material != null)
								labelsFont.material.color = _countryLabelsColor;
							else
								DrawMapLabels();
						}
					}
				}
			}
		}

		[SerializeField] private Color
			_countryLabelsShadowColor = new(0, 0, 0, 0.5f);

		/// <summary>
		/// Color for map labels.
		/// </summary>
		public Color countryLabelsShadowColor
		{
			get => _countryLabelsShadowColor;
			set
			{
				if (value != _countryLabelsShadowColor)
				{
					_countryLabelsShadowColor = value;
					isDirty = true;
					if (gameObject.activeInHierarchy)
						labelsShadowMaterial.color = _countryLabelsShadowColor;
				}
			}
		}

		[SerializeField] private Font _countryLabelsFont;

		/// <summary>
		/// Gets or sets the default font for country labels
		/// </summary>
		public Font countryLabelsFont
		{
			get => _countryLabelsFont;
			set
			{
				if (value != _countryLabelsFont)
				{
					_countryLabelsFont = value;
					isDirty = true;
					ReloadFont();
					DrawMapLabels();
				}
			}
		}

		[SerializeField] private Object _countryLabelsFontTMPro;

		public Object countryLabelsFontTMPro
		{
			get => _countryLabelsFontTMPro;
			set
			{
				if (_countryLabelsFontTMPro != value)
				{
					_countryLabelsFontTMPro = value;
					ReloadFont();
					DrawMapLabels();
				}
			}
		}

		[SerializeField] private Color
			_countryLabelsOutlineColor = Color.black;

		/// <summary>
		/// Color for the label outline. Only used with TextMesh Pro text engine.
		/// </summary>
		public Color countryLabelsOutlineColor
		{
			get => _countryLabelsOutlineColor;
			set
			{
				if (value != _countryLabelsOutlineColor)
				{
					_countryLabelsOutlineColor = value;
					isDirty = true;
					if (gameObject.activeInHierarchy)
						if (_countryLabelsTextEngine == TEXT_ENGINE.TextMeshPro)
							DrawMapLabels();
				}
			}
		}

		[SerializeField] private float
			_countryLabelsOutlineWidth = 0.1f;

		/// <summary>
		/// Width for the label outline. Only used with TextMesh Pro text engine.
		/// </summary>
		public float countryLabelsOutlineWidth
		{
			get => _countryLabelsOutlineWidth;
			set
			{
				if (value != _countryLabelsOutlineWidth)
				{
					_countryLabelsOutlineWidth = value;
					isDirty = true;
					if (gameObject.activeInHierarchy)
						if (_countryLabelsTextEngine == TEXT_ENGINE.TextMeshPro)
							DrawMapLabels();
				}
			}
		}

		private string _countryAttributeFile = COUNTRY_ATTRIB_DEFAULT_FILENAME;

		public string countryAttributeFile
		{
			get => _countryAttributeFile;
			set
			{
				if (value != _countryAttributeFile)
				{
					_countryAttributeFile = value;
					if (_countryAttributeFile == null)
						_countryAttributeFile = COUNTRY_ATTRIB_DEFAULT_FILENAME;
					isDirty = true;
					ReloadCountryAttributes();
				}
			}
		}

		[SerializeField] private float
			_labelsElevation = 0.001f;

		/// <summary>
		/// Labels elevation for normal 2D flat mode
		/// </summary>
		public float labelsElevation
		{
			get => _labelsElevation;
			set
			{
				if (value != _labelsElevation)
				{
					if (value < 0.001f)
						value = 0.001f;
					_labelsElevation = value;
					isDirty = true;
					DrawMapLabels();
				}
			}
		}

		#endregion

		#region Public API area

		/// <summary>
		/// Returns the index of a country in the countries array by its name.
		/// </summary>
		public int GetCountryIndex(string countryName)
		{
			int countryIndex;
			if (countryLookup != null && countryLookup.TryGetValue(countryName, out countryIndex))
				return countryIndex;
			return -1;
		}

		/// <summary>
		/// Returns the index of a country in the countries collection by its reference.
		/// </summary>
		public int GetCountryIndex(Country country)
		{
			int countryIndex;
			if (countryLookup != null &&
			    country != null &&
			    countryLookup.TryGetValue(country.name, out countryIndex))
				return countryIndex;
			return -1;
		}

		/// <summary>
		/// Returns the index of a country in the countries by its FIPS 10 4 code.
		/// </summary>
		public int GetCountryIndexByFIPS10_4(string fips)
		{
			for (var k = 0; k < _countries.Length; k++)
				if (_countries[k].fips10_4.Equals(fips))
					return k;
			return -1;
		}

		/// <summary>
		/// Returns the index of a country in the countries by its ISO A-2 code.
		/// </summary>
		public int GetCountryIndexByISO_A2(string iso_a2)
		{
			for (var k = 0; k < _countries.Length; k++)
				if (_countries[k].iso_a2.Equals(iso_a2))
					return k;
			return -1;
		}

		/// <summary>
		/// Returns the index of a country in the countries by its ISO A-3 code.
		/// </summary>
		public int GetCountryIndexByISO_A3(string iso_a3)
		{
			for (var k = 0; k < _countries.Length; k++)
				if (_countries[k].iso_a3.Equals(iso_a3))
					return k;
			return -1;
		}

		/// <summary>
		/// Returns the index of a country in the countries by its ISO N-3 code.
		/// </summary>
		public int GetCountryIndexByISO_N3(string iso_n3)
		{
			for (var k = 0; k < _countries.Length; k++)
				if (_countries[k].iso_n3.Equals(iso_n3))
					return k;
			return -1;
		}

		/// <summary>
		/// Gets the index of the country region.
		/// </summary>
		/// <returns>The country region index.</returns>
		/// <param name="countryIndex">Country index.</param>
		/// <param name="region">Region.</param>
		public int GetCountryRegionIndex(int countryIndex, Region region)
		{
			if (countryIndex < 0 || countryIndex >= countries.Length)
				return -1;
			var country = _countries[countryIndex];
			var rc = country.regions.Count;
			for (var k = 0; k < rc; k++)
				if (country.regions[k] == region)
					return k;
			return -1;
		}

		/// <summary>
		/// Returns the country object by its name.
		/// </summary>
		public Country GetCountry(string countryName)
		{
			var countryIndex = GetCountryIndex(countryName);
			return GetCountry(countryIndex);
		}

		/// <summary>
		/// Returns the country object by its position.
		/// </summary>
		public Country GetCountry(Vector2 mapPosition)
		{
			var countryIndex = GetCountryIndex(mapPosition);
			if (countryIndex >= 0)
				return GetCountry(countryIndex);
			return null;
		}

		/// <summary>
		/// Returns the country object by its index. This is same than doing countries[countryIndex] but does a safety check.
		/// </summary>
		public Country GetCountry(int countryIndex)
		{
			if (countryIndex < 0 || countryIndex >= _countries.Length)
				return null;
			return _countries[countryIndex];
		}

		/// <summary>
		/// Gets the country index with that unique Id.
		/// </summary>
		public int GetCountryIndex(int uniqueId)
		{
			if (countries == null)
				return -1;
			for (var k = 0; k < _countries.Length; k++)
				if (_countries[k].uniqueId == uniqueId)
					return k;
			return -1;
		}

		/// <summary>
		/// Gets the index of the country that contains the provided map coordinates. This will ignore hidden countries.
		/// </summary>
		/// <returns>The country index.</returns>
		/// <param name="localPosition">Map coordinates in the range of (-0.5 .. 0.5)</param>
		public int GetCountryIndex(Vector2 localPosition)
		{
			// verify if hitPos is inside any country polygon
			var countryCount = countriesOrderedBySize.Count;
			for (var oc = 0; oc < countryCount; oc++)
			{
				var c = _countriesOrderedBySize[oc];
				var country = _countries[c];
				if (country.hidden && Application.isPlaying)
					continue;
				if (!country.regionsRect2D.Contains(localPosition))
					continue;
				var crCount = country.regions.Count;
				for (var cr = 0; cr < crCount; cr++)
					if (country.regions[cr].Contains(localPosition))
					{
						lastRegionIndex = cr;
						return c;
					}
			}
			return -1;
		}

		/// <summary>
		/// Gets the index of the country that contains the provided map coordinates. This will ignore hidden countries.
		/// </summary>
		/// <returns>The country index and regionIndex by reference.</returns>
		/// <param name="localPosition">Map coordinates in the range of (-0.5 .. 0.5)</param>
		public int GetCountryIndex(Vector2 localPosition, out int regionIndex)
		{
			// verify if hitPos is inside any country polygon
			var countryCount = countriesOrderedBySize.Count;
			for (var oc = 0; oc < countryCount; oc++)
			{
				var c = _countriesOrderedBySize[oc];
				var country = _countries[c];
				if (country.hidden)
					continue;
				if (!country.regionsRect2D.Contains(localPosition))
					continue;
				var crCount = country.regions.Count;
				for (var cr = 0; cr < crCount; cr++)
					if (country.regions[cr].Contains(localPosition))
					{
						regionIndex = cr;
						return c;
					}
			}
			regionIndex = -1;
			return -1;
		}

		/// <summary>
		/// Gets the region of the country that contains the provided map coordinates. This will ignore hidden countries.
		/// </summary>
		/// <returns>The Region object.</returns>
		/// <param name="localPosition">Map coordinates in the range of (-0.5 .. 0.5)</param>
		public Region GetCountryRegion(Vector2 localPosition)
		{
			// verify if hitPos is inside any country polygon
			var countryCount = countriesOrderedBySize.Count;
			for (var oc = 0; oc < countryCount; oc++)
			{
				var c = _countriesOrderedBySize[oc];
				var country = _countries[c];
				if (country.hidden)
					continue;
				if (!country.regionsRect2D.Contains(localPosition))
					continue;
				for (var cr = 0; cr < country.regions.Count; cr++)
				{
					var region = country.regions[cr];
					if (region.Contains(localPosition))
						return region;
				}
			}
			return null;
		}

		/// <summary>
		/// Gets the region index of the country that contains the provided map coordinates. This will ignore hidden countries.
		/// </summary>
		/// <returns>The Region index or -1 if no region found.</returns>
		/// <param name="localPosition">Map coordinates in the range of (-0.5 .. 0.5)</param>
		public int GetCountryRegionIndex(Vector2 localPosition)
		{
			var region = GetCountryRegion(localPosition);
			if (region == null)
				return -1;
			return region.regionIndex;
		}

		/// <summary>
		/// Returns all neighbour countries
		/// </summary>
		public List<Country> CountryNeighbours(int countryIndex)
		{
			var countryNeighbours = new List<Country>();
			// Iterate for all regions (a country can have several separated regions)
			foreach (var countryRegion in _countries[countryIndex].regions)
				foreach (var c in CountryNeighboursOfRegion(countryRegion))
					if (!countryNeighbours.Contains(c))
						countryNeighbours.Add(c);

			return countryNeighbours;
		}

		/// <summary>
		/// Get neighbours of the main region of a country
		/// </summary>
		public List<Country> CountryNeighboursOfMainRegion(int countryIndex)
		{
			// Get main region
			var country = _countries[countryIndex];
			var countryRegion = country.regions[country.mainRegionIndex];
			return CountryNeighboursOfRegion(countryRegion);
		}

		/// <summary>
		/// Get neighbours of the currently selected region
		/// </summary>
		public List<Country> CountryNeighboursOfCurrentRegion() =>
			CountryNeighboursOfRegion(countryRegionHighlighted);

		/// <summary>
		/// Get neighbours of a given country region
		/// </summary>
		public List<Country> CountryNeighboursOfRegion(Region countryRegion)
		{
			var countryNeighbours = new List<Country>();
			if (countryRegion == null)
				return countryNeighbours;

			// Get the neighbours for this region
			for (var neighbourIndex = 0; neighbourIndex < countryRegion.neighbours.Count; neighbourIndex++)
			{
				var neighbour = countryRegion.neighbours[neighbourIndex];
				var neighbourCountry = (Country)neighbour.entity;
				if (!countryNeighbours.Contains(neighbourCountry))
					countryNeighbours.Add(neighbourCountry);
			}

			// Find neighbours due to enclaves
			if (_enableEnclaves)
			{
				var country = (Country)countryRegion.entity;
				for (var c = 0; c < _countries.Length; c++)
				{
					var c2 = _countries[c];
					if (!country.regionsRect2D.Contains(c2.center) &&
					    !c2.regionsRect2D.Contains(country.center))
						continue;
					if (c2 == country)
						continue;
					if (countryNeighbours.Contains(c2))
						continue;
					var crc = c2.regions.Count;
					for (var cr = 0; cr < crc; cr++)
					{
						var cregion = c2.regions[cr];
						if (countryRegion.Contains(cregion) || cregion.Contains(countryRegion))
						{
							countryNeighbours.Add(c2);
							break;
						}
					}
				}
			}
			return countryNeighbours;
		}

		/// <summary>
		/// Returns a list of countries that are visible in the game view
		/// </summary>
		public List<Country> GetVisibleCountries()
		{
			var cam = Application.isPlaying ? currentCamera : Camera.current;
			return GetVisibleCountries(cam);
		}

		/// <summary>
		/// Returns a list of countries that are visible by provided camera
		/// </summary>
		public List<Country> GetVisibleCountries(Camera camera)
		{
			if (camera == null)
				return null;
			var vc = new List<Country>(30);
			for (var k = 0; k < _countries.Length; k++)
			{
				var country = _countries[k];
				if (country.hidden)
					continue;

				// Check if country is facing camera
				var center = transform.TransformPoint(country.center);
				// Check if center of country is inside viewport
				var vpos = camera.WorldToViewportPoint(center);
				if (vpos.x >= 0 && vpos.x <= 1 && vpos.y >= 0 && vpos.y <= 1)
					vc.Add(country);
				else
				{
					// Check if some frontier point is inside viewport
					var frontier = country.regions[country.mainRegionIndex].points;
					var step = 1 + frontier.Length / 25;
					for (var p = 0; p < frontier.Length; p += step)
					{
						var pos = transform.TransformPoint(frontier[p]);
						vpos = camera.WorldToViewportPoint(pos);
						if (vpos.x >= 0 && vpos.x <= 1 && vpos.y >= 0 && vpos.y <= 1)
						{
							vc.Add(country);
							break;
						}
					}
				}
			}
			return vc;
		}

		/// <summary>
		/// Returns a list of countries that are visible inside the window rectangle (rect constraints)
		/// </summary>
		public List<Country> GetVisibleCountriesInWindowRect()
		{
			var vc = new List<Country>(30);
			for (var k = 0; k < _countries.Length; k++)
			{
				var country = _countries[k];
				if (country.hidden)
					continue;
				// Check if center of country is inside window rect
				if (_windowRect.Contains(country.center))
					vc.Add(country);
				else
				{
					// Check if some frontier point is inside viewport
					var frontier = country.regions[country.mainRegionIndex].points;
					var step = 1 + frontier.Length / 25;
					for (var p = 0; p < frontier.Length; p += step)
						if (_windowRect.Contains(frontier[p]))
						{
							vc.Add(country);
							break;
						}
				}
			}
			return vc;
		}

		/// <summary>
		/// Returns the zoom level required to show the entire country region on screen
		/// </summary>
		/// <returns>The country zoom level of -1 if error.</returns>
		/// <param name="countryIndex">Country index.</param>
		public float GetCountryRegionZoomExtents(int countryIndex)
		{
			if (countryIndex < 0 || countries == null || countryIndex >= countries.Length)
				return -1;
			return GetCountryRegionZoomExtents(countryIndex, countries[countryIndex].mainRegionIndex);
		}

		/// <summary>
		/// Returns the zoom level required to show the entire country region on screen
		/// </summary>
		/// <returns>The country zoom level of -1 if error.</returns>
		/// <param name="countryIndex">Country index.</param>
		/// <param name="regionIndex">Region index of the country.</param>
		public float GetCountryRegionZoomExtents(int countryIndex, int regionIndex)
		{
			if (countryIndex < 0 || countries == null || countryIndex >= countries.Length)
				return -1;
			var country = countries[countryIndex];
			if (regionIndex < 0 || regionIndex >= country.regions.Count)
				return -1;
			var region = country.regions[regionIndex];
			return GetFrustumZoomLevel(region.rect2D.width * mapWidth, region.rect2D.height * mapHeight);
		}

		/// <summary>
		/// Returns the zoom level required to show the entire country (including all regions or only the main region) on screen
		/// </summary>
		/// <returns>The country zoom level of -1 if error.</returns>
		/// <param name="countryIndex">Country index.</param>
		/// <param name="onlyMainRegion">If set to true, only the main region will be considered. A value of false (default) ensures entire country including islands fits into the screen.</param>> 
		public float GetCountryZoomExtents(int countryIndex, bool onlyMainRegion = false)
		{
			if (countryIndex < 0 || countries == null || countryIndex >= countries.Length)
				return -1;
			var country = _countries[countryIndex];
			var rect = onlyMainRegion ? country.mainRegion.rect2D : country.regionsRect2D;
			return GetFrustumZoomLevel(rect.width * mapWidth, rect.height * mapHeight);
		}

		/// <summary>
		/// Renames the country. Name must be unique, different from current and one letter minimum.
		/// </summary>
		/// <returns><c>true</c> if country was renamed, <c>false</c> otherwise.</returns>
		public bool CountryRename(string oldName, string newName)
		{
			if (string.IsNullOrEmpty(newName))
				return false;
			var countryIndex = GetCountryIndex(oldName);
			var newCountryIndex = GetCountryIndex(newName);
			if (countryIndex < 0 || newCountryIndex >= 0)
				return false;
			_countries[countryIndex].name = newName;
			// Look for any decorator
			decorator.UpdateDecoratorsCountryName(oldName, newName);
			lastCountryLookupCount = -1;
			return true;
		}

		/// <summary>
		/// Deletes the country. Optionally also delete its dependencies (provinces, cities, mountpoints).
		/// </summary>
		/// <returns><c>true</c> if country was deleted, <c>false</c> otherwise.</returns>
		public bool CountryDelete(int countryIndex, bool deleteDependencies, bool redraw = true)
		{
			if (internal_CountryDelete(countryIndex, deleteDependencies))
			{
				// Update lookup dictionaries
				lastCountryLookupCount = -1;
				return true;
			}
			if (redraw)
				Redraw();
			return false;
		}

		/// <summary>
		/// Deletes all provinces from a country.
		/// </summary>
		/// <returns><c>true</c>, if provinces where deleted, <c>false</c> otherwise.</returns>
		public bool CountryDeleteProvinces(int countryIndex)
		{
			var numProvinces = provinces.Length;
			var newProvinces = new List<Province>(numProvinces);
			for (var k = 0; k < numProvinces; k++)
				if (provinces[k] != null && provinces[k].countryIndex != countryIndex)
					newProvinces.Add(provinces[k]);
			provinces = newProvinces.ToArray();
			return true;
		}

		public void CountriesDeleteFromContinent(string continentName)
		{
			HideCountryRegionHighlights(true);

			ProvincesDeleteOfSameContinent(continentName);
			CitiesDeleteFromContinent(continentName);
			MountPointsDeleteFromSameContinent(continentName);

			var newAdmins = new List<Country>(_countries.Length - 1);
			for (var k = 0; k < _countries.Length; k++)
				if (!_countries[k].continent.Equals(continentName))
					newAdmins.Add(_countries[k]);
				else
				{
					var lastIndex = newAdmins.Count - 1;
					// Updates country index in provinces
					if (provinces != null)
						for (var p = 0; p < _provinces.Length; p++)
							if (_provinces[p].countryIndex > lastIndex)
								_provinces[p].countryIndex--;
					// Updates country index in cities
					var cityCount = cities.Length;
					if (cities != null)
						for (var c = 0; c < cityCount; c++)
							if (_cities[c].countryIndex > lastIndex)
								_cities[c].countryIndex--;
					// Updates country index in mount points
					if (mountPoints != null)
						for (var c = 0; c < mountPoints.Count; c++)
							if (mountPoints[c].countryIndex > lastIndex)
								mountPoints[c].countryIndex--;
				}

			countries = newAdmins.ToArray();
		}

		/// <summary>
		/// Creates a country and adds it to the country list.
		/// </summary>
		/// <param name="name">Name must be unique!</param>
		/// <param name="continent">Continent.</param>
		public int CountryCreate(string name, string continent)
		{
			var newCountry = new Country(name, continent,
				GetUniqueId(new List<IExtendableAttribute>(countries)));
			return CountryAdd(newCountry);
		}

		/// <summary>
		/// Adds a new country which has been properly initialized. Used by the Map Editor. Name must be unique.
		/// </summary>
		/// <returns><c>country index</c> if country was added, <c>-1</c> otherwise.</returns>
		public int CountryAdd(Country country)
		{
			var countryIndex = GetCountryIndex(country.name);
			if (countryIndex >= 0)
				return -1;
			var newCountries = new Country[_countries.Length + 1];
			for (var k = 0; k < _countries.Length; k++)
				newCountries[k] = _countries[k];
			countryIndex = newCountries.Length - 1;
			newCountries[countryIndex] = country;
			countries = newCountries;
			RefreshCountryDefinition(countryIndex, null);
			return countryIndex;
		}

		/// <summary>
		/// Creates a special hidden country that acts as a pool for all provinces in the map.
		/// You can then create new countries from a single province from this pool using ProvinceToCountry function
		/// Or attach a province from the pool to a new country using CountryTransferProvince function
		/// </summary>
		/// <returns>The country index of the new country that acts as the province pool.</returns>
		/// <param name="countryName">A name for this special country (eg: "Pool").</param>
		public int CountryCreateProvincesPool(string countryName, bool removeAllOtherCountries)
		{
			if (_dontLoadGeodataAtStart) // needs country and province data!
				ReloadData();
			var bgCountryIndex = GetCountryIndex("Pool");
			if (bgCountryIndex >= 0)
				return bgCountryIndex;

			var bgCountry = new Country(countryName, "<Background>", 1)
			{
				hidden = true,
				isPool = true
			};
			var dummyRegion = new Region(bgCountry, 0);
			dummyRegion.UpdatePointsAndRect(new Vector2[]
			{
				new(0.5f, 0.5f),
				new(0.5f, -0.5f),
				new(-0.5f, -0.5f),
				new(-0.5f, 0.5f)
			});
			bgCountry.regions.Add(dummyRegion);
			CountryAdd(bgCountry);
			bgCountryIndex = GetCountryIndex(bgCountry);
			if (removeAllOtherCountries)
			{
				_countries[bgCountryIndex].provinces = provinces;
				for (var k = 0; k < _provinces.Length; k++)
					_provinces[k].countryIndex = bgCountryIndex;
				// Delete all countries except the background country
				while (_countries.Length > 1)
					CountryDelete(0, false, false);
			}
			return bgCountryIndex;
		}

		/// <summary>
		/// Returns the country index by screen position.
		/// </summary>
		public bool GetCountryIndex(Ray ray, out int countryIndex, out int regionIndex)
		{
			var hitCount = Physics.RaycastNonAlloc(ray, tempHits, 500, layerMask);
			if (hitCount > 0)
			{
				var countryCount = countriesOrderedBySize.Count;
				for (var k = 0; k < hitCount; k++)
					if (tempHits[k].collider.gameObject == gameObject)
					{
						Vector2 localHit = transform.InverseTransformPoint(tempHits[k].point);
						for (var oc = 0; oc < countryCount; oc++)
						{
							var c = _countriesOrderedBySize[oc];
							var country = _countries[c];
							var crCount = country.regions.Count;
							for (var cr = 0; cr < crCount; cr++)
							{
								var region = country.regions[cr];
								if (region.Contains(localHit))
								{
									countryIndex = c;
									regionIndex = cr;
									return true;
								}
							}
						}
					}
			}
			countryIndex = -1;
			regionIndex = -1;
			return false;
		}

		/// <summary>
		/// Starts navigation to target country. Returns false if country is not found.
		/// </summary>
		public bool FlyToCountry(string name)
		{
			var countryIndex = GetCountryIndex(name);
			if (countryIndex >= 0)
			{
				FlyToCountry(countryIndex);
				return true;
			}
			return false;
		}

		/// <summary>
		/// Starts navigation to target country. with specified duration, ignoring NavigationTime property.
		/// Set duration to zero to go instantly.
		/// Returns false if country is not found. 
		/// </summary>
		public bool FlyToCountry(string name, float duration) =>
			FlyToCountry(name, duration, GetZoomLevel());

		/// <summary>
		/// Starts navigation to target country. with specified duration and zoom level, ignoring NavigationTime property.
		/// Set duration to zero to go instantly.
		/// Returns false if country is not found. 
		/// </summary>
		public bool FlyToCountry(string name, float duration, float zoomLevel)
		{
			var countryIndex = GetCountryIndex(name);
			if (countryIndex >= 0)
			{
				FlyToCountry(countryIndex, duration, zoomLevel);
				return true;
			}
			return false;
		}

		/// <summary>
		/// Starts navigation to target country by index in the countries collection. Returns false if country is not found.
		/// </summary>
		public void FlyToCountry(int countryIndex)
		{
			FlyToCountry(countryIndex, _navigationTime);
		}

		/// <summary>
		/// Starts navigating to target country by index in the countries collection with specified duration, ignoring NavigationTime property.
		/// Set duration to zero to go instantly.
		/// </summary>
		public void FlyToCountry(int countryIndex, float duration)
		{
			FlyToCountry(countryIndex, duration, GetZoomLevel());
		}

		/// <summary>
		/// Starts navigating to target country by index in the countries collection with specified duration, ignoring NavigationTime property.
		/// Set duration to zero to go instantly.
		/// </summary>
		public void FlyToCountry(int countryIndex, float duration, float zoomLevel)
		{
			if (countryIndex < 0 || countryIndex >= countries.Length)
				return;
			SetDestination(_countries[countryIndex].center, duration, zoomLevel);
		}

		/// <summary>
		/// Colorize all regions of specified country by name. Returns false if not found.
		/// </summary>
		public bool ToggleCountrySurface(string name, bool visible, Color color)
		{
			var countryIndex = GetCountryIndex(name);
			if (countryIndex >= 0)
			{
				ToggleCountrySurface(countryIndex, visible, color);
				return true;
			}
			return false;
		}

		/// <summary>
		/// Iterates for the countries list and colorizes those belonging to specified continent name.
		/// </summary>
		public void ToggleContinentSurface(string continentName, bool visible, Color color)
		{
			for (var colorizeIndex = 0; colorizeIndex < _countries.Length; colorizeIndex++)
				if (_countries[colorizeIndex].continent.Equals(continentName))
					ToggleCountrySurface(_countries[colorizeIndex].name, visible, color);
		}

		/// <summary>
		/// Uncolorize/hide specified countries beloning to a continent.
		/// </summary>
		public void HideContinentSurface(string continentName)
		{
			for (var colorizeIndex = 0; colorizeIndex < _countries.Length; colorizeIndex++)
				if (_countries[colorizeIndex].continent.Equals(continentName))
					HideCountrySurface(colorizeIndex);
		}

		/// <summary>
		/// Colorize all regions of specified country by index in the countries collection.
		/// </summary>
		public void ToggleCountrySurface(int countryIndex, bool visible, Color color)
		{
			ToggleCountrySurface(countryIndex, visible, color, null, Misc.Vector2one, Misc.Vector2zero, 0,
				false);
		}

		/// <summary>
		/// Colorize all regions of specified country and assings a texture.
		/// </summary>
		public void ToggleCountrySurface(int countryIndex, bool visible, Color color, Texture2D texture,
			bool applyTextureToAllRegions = false)
		{
			ToggleCountrySurface(countryIndex, visible, color, texture, Misc.Vector2one, Misc.Vector2zero,
				0, applyTextureToAllRegions);
		}

		/// <summary>
		/// Colorize all regions of specified country and assings a texture.
		/// </summary>
		/// <param name="countryIndex">Country index.</param>
		/// <param name="visible">If set to <c>true</c> visible.</param>
		/// <param name="color">Color.</param>
		/// <param name="texture">Texture.</param>
		/// <param name="textureScale">Texture scale.</param>
		/// <param name="textureOffset">Texture offset.</param>
		/// <param name="textureRotation">Texture rotation.</param>
		/// <param name="applyTextureToAllRegions">If set to <c>true</c> the texture will be applied to all regions, otherwise only the main region will get the texture and the remaining regions will get the color.</param>
		public void ToggleCountrySurface(int countryIndex, bool visible, Color color, Texture2D texture,
			Vector2 textureScale, Vector2 textureOffset, float textureRotation,
			bool applyTextureToAllRegions)
		{
			if (countryIndex < 0 || countryIndex >= _countries.Length)
				return;
			if (!visible)
			{
				HideCountrySurface(countryIndex);
				return;
			}
			var country = _countries[countryIndex];
			var rCount = country.regions.Count;
			for (var r = 0; r < rCount; r++)
				if (applyTextureToAllRegions || r == country.mainRegionIndex)
					ToggleCountryRegionSurface(countryIndex, r, visible, color, texture, textureScale,
						textureOffset, textureRotation);
				else
					ToggleCountryRegionSurface(countryIndex, r, visible, color);
		}

		/// <summary>
		/// Uncolorize/hide specified country by index in the countries collection.
		/// </summary>
		public void HideCountrySurface(int countryIndex)
		{
			var rCount = _countries[countryIndex].regions.Count;
			for (var r = 0; r < rCount; r++)
				HideCountryRegionSurface(countryIndex, r);
		}

		/// <summary>
		/// Highlights the country region specified.
		/// Internally used by the Editor component, but you can use it as well to temporarily mark a country region.
		/// </summary>
		/// <param name="refreshGeometry">Pass true only if you're sure you want to force refresh the geometry of the highlight (for instance, if the frontiers data has changed). If you're unsure, pass false.</param>
		public GameObject ToggleCountryRegionSurfaceHighlight(int countryIndex, int regionIndex,
			Color color, bool drawOutline)
		{
			var mat = Instantiate(hudMatCountry);
			disposalManager?.MarkForDisposal(mat); // mat.hideFlags = HideFlags.DontSave;
			mat.color = color;
			mat.renderQueue--;
			var cacheIndex = GetCacheIndexForCountryRegion(countryIndex, regionIndex);

			var existsInCache = surfaces.TryGetValue(cacheIndex, out var surf);
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
				surf = GenerateCountryRegionSurface(countryIndex, regionIndex, mat, Misc.Vector2one,
					Misc.Vector2zero, 0);
			return surf;
		}

		/// <summary>
		/// Colorize main region of a country by index in the countries collection.
		/// </summary>
		public GameObject ToggleCountryMainRegionSurface(int countryIndex, bool visible, Color color) =>
			ToggleCountryMainRegionSurface(countryIndex, visible, color, null, Misc.Vector2zero,
				Misc.Vector2zero, 0);

		/// <summary>
		/// Add texture to main region of a country by index in the countries collection.
		/// </summary>
		/// <param name="texture">Optional texture or null to colorize with single color</param>
		public GameObject
			ToggleCountryMainRegionSurface(int countryIndex, bool visible, Texture2D texture) =>
			ToggleCountryRegionSurface(countryIndex, _countries[countryIndex].mainRegionIndex, visible,
				Color.white, texture, Misc.Vector2one, Misc.Vector2zero, 0);

		/// <summary>
		/// Colorize main region of a country by index in the countries collection.
		/// </summary>
		/// <param name="texture">Optional texture or null to colorize with single color</param>
		public GameObject ToggleCountryMainRegionSurface(int countryIndex, bool visible, Color color,
			Texture2D texture, Vector2 textureScale, Vector2 textureOffset, float textureRotation) =>
			ToggleCountryRegionSurface(countryIndex, _countries[countryIndex].mainRegionIndex, visible,
				color, texture, textureScale, textureOffset, textureRotation);

		public GameObject
			ToggleCountryRegionSurface(int countryIndex, int regionIndex, bool visible, Color color) =>
			ToggleCountryRegionSurface(countryIndex, regionIndex, visible, color, null, Misc.Vector2one,
				Misc.Vector2zero, 0);

		/// <summary>
		/// Colorize specified region of a country by indexes.
		/// </summary>
		public GameObject ToggleCountryRegionSurface(int countryIndex, int regionIndex, bool visible,
			Color color, Texture2D texture, Vector2 textureScale, Vector2 textureOffset,
			float textureRotation)
		{
			if (!visible)
			{
				HideCountryRegionSurface(countryIndex, regionIndex);
				return null;
			}

			var cacheIndex = GetCacheIndexForCountryRegion(countryIndex, regionIndex);

			GameObject surf;
			// Checks if current cached surface contains a material with a texture, if it exists but it has not texture, destroy it to recreate with uv mappings
			surfaces.TryGetValue(cacheIndex, out surf);

			// Should the surface be recreated?
			Material surfMaterial;

			var region = _countries[countryIndex].regions[regionIndex];
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
			var isHighlighted = countryHighlightedIndex == countryIndex &&
			                    (countryRegionHighlightedIndex == regionIndex ||
			                     _highlightAllCountryRegions) &&
			                    _enableCountryHighlight &&
			                    _countries[countryIndex].allowHighlight;
			if (surf != null)
			{
				if (!surf.activeSelf)
					surf.SetActive(true);
				// Check if material is ok
				var goodMaterial = GetColoredTexturedMaterial(color, texture);
				region.customMaterial = goodMaterial;
				var renderer = surf.GetComponent<Renderer>();
				surfMaterial = renderer.sharedMaterial;
				if (texture == null && !surfMaterial.name.Equals(coloredMat.name) ||
				    texture != null && !surfMaterial.name.Equals(texturizedMat.name) ||
				    surfMaterial.color != color && !isHighlighted ||
				    texture != null && region.customMaterial.mainTexture != texture)
					ApplyMaterialToSurface(surf, goodMaterial);
			}
			else
			{
				surfMaterial = GetColoredTexturedMaterial(color, texture);
				surf = GenerateCountryRegionSurface(countryIndex, regionIndex, surfMaterial, textureScale,
					textureOffset, textureRotation);
				region.customMaterial = surfMaterial;
				region.customTextureOffset = textureOffset;
				region.customTextureRotation = textureRotation;
				region.customTextureScale = textureScale;
			}
			// If it was highlighted, highlight it again
			if (region.customMaterial != null &&
			    isHighlighted &&
			    region.customMaterial.color != hudMatCountry.color)
			{
				var clonedMat = Instantiate(region.customMaterial);
				if (disposalManager != null)
					disposalManager
						.MarkForDisposal(clonedMat); // clonedMat.hideFlags = HideFlags.DontSave;
				clonedMat.name = region.customMaterial.name;
				clonedMat.color = hudMatCountry.color;
				ApplyMaterialToSurface(surf, clonedMat);
				countryRegionHighlightedObj = surf;
			}
			return surf;
		}

		/// <summary>
		/// Draws an outline around a region
		/// </summary>
		/// <returns>The country region outline.</returns>
		/// <param name="countryIndex">Country index.</param>
		public GameObject ToggleCountryOutline(int countryIndex, bool visible)
		{
			if (countryIndex < 0 || countryIndex >= _countries.Length)
				return null;
			var region = _countries[countryIndex].mainRegion;
			// try get surface for this country region
			var regionIndex = _countries[countryIndex].mainRegionIndex;
			var cacheIndex = GetCacheIndexForCountryRegion(countryIndex, regionIndex);
			if (!visible)
			{
				HideRegionObject(cacheIndex.ToString(), null, COUNTRY_OUTLINE_GAMEOBJECT_NAME);
				return null;
			}
			return DrawCountryRegionOutline(cacheIndex.ToString(), region);
		}

		/// <summary>
		/// Draws an outline around a region
		/// </summary>
		/// <returns>The country region outline.</returns>
		/// <param name="countryIndex">Country index.</param>
		public GameObject ToggleCountryOutline(int countryIndex, bool visible, Texture2D borderTexure,
			float borderWidth = 0.1f, Color tintColor = default, float textureTiling = 1f,
			float animationSpeed = 0f)
		{
			if (countryIndex < 0 || countryIndex >= _countries.Length)
				return null;
			var region = _countries[countryIndex].mainRegion;
			// try get surface for this country region
			var regionIndex = _countries[countryIndex].mainRegionIndex;
			var cacheIndex = GetCacheIndexForCountryRegion(countryIndex, regionIndex);
			if (!visible)
			{
				HideRegionObject(cacheIndex.ToString(), null, COUNTRY_OUTLINE_GAMEOBJECT_NAME);
				return null;
			}
			region.customBorderTexture = borderTexure;
			region.customBorderWidth = borderWidth;
			region.customBorderTextureTiling = textureTiling;
			region.customBorderAnimationSpeed = animationSpeed;
			if (tintColor != default)
				region.customBorderTintColor = tintColor;
			return DrawCountryRegionOutline(cacheIndex.ToString(), region);
		}

		/// <summary>
		/// Uncolorize/hide specified country by index in the countries collection.
		/// </summary>
		public void HideCountryRegionSurface(int countryIndex, int regionIndex)
		{
			if (_countryHighlightedIndex != countryIndex || _countryRegionHighlightedIndex != regionIndex)
			{
				var cacheIndex = GetCacheIndexForCountryRegion(countryIndex, regionIndex);
				GameObject obj;
				if (surfaces.TryGetValue(cacheIndex, out obj))
				{
					if (obj == null)
						surfaces.Remove(cacheIndex);
					else
						obj.SetActive(false);
				}
			}
			_countries[countryIndex].regions[regionIndex].customMaterial = null;
		}

		/// <summary>
		/// Hides all colorized regions of all countries.
		/// </summary>
		public void HideCountrySurfaces()
		{
			for (var c = 0; c < _countries.Length; c++)
				HideCountrySurface(c);
		}

		/// <summary>
		/// Flashes specified country by index in the countries collection.
		/// </summary>
		public void BlinkCountry(string countryName, Color color1, Color color2, float duration,
			float blinkingSpeed)
		{
			var countryIndex = GetCountryIndex(countryName);
			BlinkCountry(countryIndex, color1, color2, duration, blinkingSpeed);
		}

		/// <summary>
		/// Flashes specified country by index in the countries collection.
		/// </summary>
		public void BlinkCountry(int countryIndex, Color color1, Color color2, float duration,
			float blinkingSpeed, bool smoothBlink = true)
		{
			if (countryIndex < 0 || countryIndex >= _countries.Length)
				return;
			var mainRegionIndex = _countries[countryIndex].mainRegionIndex;
			BlinkCountry(countryIndex, mainRegionIndex, color1, color2, duration, blinkingSpeed,
				smoothBlink);
		}

		/// <summary>
		/// Flashes specified country's region.
		/// </summary>
		public void BlinkCountry(int countryIndex, int regionIndex, Color color1, Color color2,
			float duration, float blinkingSpeed, bool smoothBlink = true)
		{
			var cacheIndex = GetCacheIndexForCountryRegion(countryIndex, regionIndex);
			var surf = surfaces.ContainsKey(cacheIndex)
				? surfaces[cacheIndex]
				: GenerateCountryRegionSurface(countryIndex, regionIndex, hudMatCountry);
			var sb = surf.AddComponent<SurfaceBlinker>();
			sb.blinkMaterial = hudMatCountry;
			sb.color1 = color1;
			sb.color2 = color2;
			sb.duration = duration;
			sb.speed = blinkingSpeed;
			sb.smoothBlink = smoothBlink;
			sb.customizableSurface = _countries[countryIndex].regions[regionIndex];
			surf.SetActive(true);
		}

		/// <summary>
		/// Returns an array of country names. The returning list can be grouped by continent.
		/// </summary>
		public string[] GetCountryNames(bool groupByContinent, bool addCountryIndex = true)
		{
			var c = new List<string>();
			if (_countries == null)
				return Array.Empty<string>();
			for (var i = 0; i < _countries.Length; i++)
			{
				var country = _countries[i];
				if (groupByContinent && !c.Contains(country.continent))
					c.Add(country.continent);
				c.Add(addCountryIndex ? country.name + " (" + i + ")" : country.name);
			}
			c.Sort();

			if (groupByContinent)
				for (var i = 0; i < c.Count; i++)
				{
					var j = c[i].IndexOf('|');
					if (j > 0)
						c[i] = "  " + c[i][(j + 1)..];
				}
			return c.ToArray();
		}

		/// <summary>
		/// Returns a list of countries whose attributes matches predicate
		/// </summary>
		public List<Country> GetCountries(AttribPredicate predicate)
		{
			var selectedCountries = new List<Country>();
			for (var k = 0; k < _countries.Length; k++)
			{
				var country = _countries[k];
				if (predicate(country.attrib))
					selectedCountries.Add(country);
			}
			return selectedCountries;
		}

		/// <summary>
		/// Gets a list of countries that overlap with a given region
		/// </summary>
		public List<Country> GetCountriesOverlap(Region region)
		{
			var rr = new List<Country>();
			for (var k = 0; k < _countries.Length; k++)
			{
				var country = _countries[k];
				if (!country.regionsRect2D.Overlaps(region.rect2D))
					continue;
				if (country.regions == null)
					continue;
				var rCount = country.regions.Count;
				for (var r = 0; r < rCount; r++)
				{
					var otherRegion = country.regions[r];
					if (region.Intersects(otherRegion))
					{
						rr.Add(country);
						break;
					}
				}
			}
			return rr;
		}

		/// <summary>
		/// Gets a list of country regions that overlap with a given region
		/// </summary>
		public List<Region> GetCountryRegionsOverlap(Region region)
		{
			var rr = new List<Region>();
			for (var k = 0; k < _countries.Length; k++)
			{
				var country = _countries[k];
				if (!country.regionsRect2D.Overlaps(region.rect2D))
					continue;
				if (country.regions == null)
					continue;
				var rCount = country.regions.Count;
				for (var r = 0; r < rCount; r++)
				{
					var otherRegion = country.regions[r];
					if (otherRegion.points.Length > 0 && region.Intersects(otherRegion))
						rr.Add(otherRegion);
				}
			}
			return rr;
		}

		/// <summary>
		/// Returns the list of costal positions of a given country
		/// </summary>
		public List<Vector2> GetCountryCoastalPoints(int countryIndex, float minDistance = 0.005f,
			bool includeAllRegions = false)
		{
			if (countryIndex < 0 ||
			    countryIndex >= _countries.Length ||
			    _countries[countryIndex].regions == null)
				return null;

			var coastalPoints = new List<Vector2>();
			minDistance *= minDistance;
			var onlyMainRegion = !includeAllRegions;
			for (var r = 0; r < _countries[countryIndex].regions.Count; r++)
			{
				if (onlyMainRegion)
					r = _countries[countryIndex].mainRegionIndex;
				var region = _countries[countryIndex].regions[r];
				for (var i = 0; i < region.points.Length; i++)
				{
					var position = region.points[i];
					if (TryGetPosition(minDistance, coastalPoints, position))
						coastalPoints.Add(position);
				}
				if (onlyMainRegion)
					break;
			}
			return coastalPoints;
		}

		/// <summary>
		/// Returns the list of costal positions of a given country
		/// </summary>
		public List<Vector2> GetCountryCoastalPoints(int countryIndex, int regionIndex,
			float minDistance = 0.005f)
		{
			if (countryIndex < 0 ||
			    countryIndex >= _countries.Length ||
			    regionIndex < 0 ||
			    _countries[countryIndex].regions == null ||
			    regionIndex >= _countries[countryIndex].regions.Count)
				return null;
			var coastalPoints = new List<Vector2>();
			minDistance *= minDistance;
			var region = _countries[countryIndex].regions[regionIndex];
			for (var i = 0; i < region.points.Length; i++)
			{
				var position = region.points[i];
				if (TryGetPosition(minDistance, coastalPoints, position))
					coastalPoints.Add(position);
			}
			return coastalPoints;
		}

		private bool TryGetPosition(float minDistance, List<Vector2> coastalPoints, Vector2 position)
		{
			if (!ContainsWater(position, 4))
				return false;
			for (var s = coastalPoints.Count - 1; s >= 0; s--)
			{
				var sqrDist = (coastalPoints[s] - position).sqrMagnitude;
				if (sqrDist < minDistance)
					return false;
			}
			return true;
		}

		/// <summary>
		/// Returns a list of common frontier points between two countries.
		/// Use extraWidth to widen the points, useful when using the result to block pass in pathfinding
		/// </summary>
		public List<Vector2> GetCountryFrontierPoints(int countryIndex1, int countryIndex2,
			int extraWidth = 0)
		{
			if (countryIndex1 < 0 ||
			    countryIndex1 >= _countries.Length ||
			    countryIndex2 < 0 ||
			    countryIndex2 >= _countries.Length)
				return null;

			var country1 = _countries[countryIndex1];
			var country2 = _countries[countryIndex2];
			var samePoints = new List<Vector2>();

			var country1RegionsCount = country1.regions.Count;
			for (var cr = 0; cr < country1RegionsCount; cr++)
			{
				var region1 = country1.regions[cr];
				var region1neighboursCount = region1.neighbours.Count;
				for (var n = 0; n < region1neighboursCount; n++)
				{
					var otherRegion = region1.neighbours[n];
					if (!country2.regions.Contains(otherRegion))
						continue;
					for (var p = 0; p < region1.points.Length; p++)
						for (var o = 0; o < otherRegion.points.Length; o++)
							if (region1.points[p] == otherRegion.points[o])
								samePoints.Add(region1.points[p]);
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
		/// Returns the points for the given country region. Optionally in world space coordinates (normal map, not viewport).
		/// </summary>
		public Vector3[] GetCountryFrontierPoints(int countryIndex, bool worldSpace)
		{
			if (countryIndex < 0 || countryIndex >= countries.Length)
				return null;
			return GetCountryFrontierPoints(countryIndex, countries[countryIndex].mainRegionIndex,
				worldSpace);
		}

		/// <summary>
		/// Returns the points for the given country region. Optionally in world space coordinates (normal map, not viewport).
		/// </summary>
		public Vector3[] GetCountryFrontierPoints(int countryIndex, int regionIndex, bool worldSpace)
		{
			if (countryIndex < 0 || countryIndex >= countries.Length)
				return null;
			if (regionIndex < 0 || regionIndex >= countries[countryIndex].regions.Count)
				return null;

			var region = countries[countryIndex].regions[regionIndex];
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
		/// Checks quality of country's polygon points. Useful before using polygon clipping operations.
		/// </summary>
		/// <returns><c>true</c>, if country was sanitized (there was any change), <c>false</c> if country data has not changed.</returns>
		public bool CountrySanitize(int countryIndex, int minimumPoints = 3, bool refresh = true)
		{
			if (countryIndex < 0 || countryIndex >= _countries.Length)
				return false;

			var changes = false;
			var country = _countries[countryIndex];
			for (var k = 0; k < country.regions.Count; k++)
			{
				var region = country.regions[k];
				if (RegionSanitize(region))
					changes = true;
				if (region.points.Length < minimumPoints)
				{
					country.regions.Remove(region);
					if (country.regions == null)
						return true;
					k--;
					changes = true;
				}
			}
			if (changes && refresh)
				RefreshCountryDefinition(countryIndex, null);
			return changes;
		}

		/// <summary>
		/// Returns the colored surface (game object) of a country. If it has not been colored yet, it will return null.
		/// </summary>
		public GameObject GetCountryRegionSurfaceGameObject(int countryIndex, int regionIndex)
		{
			var cacheIndex = GetCacheIndexForCountryRegion(countryIndex, regionIndex);
			GameObject obj = null;
			if (surfaces.TryGetValue(cacheIndex, out obj))
				return obj;
			return null;
		}

		/// <summary>
		/// Makes countryIndex absorb another country. All regions are transfered to target country.
		/// This function is quite slow with high definition frontiers.
		/// </summary>
		/// <param name="countryIndex">Country index of the conquering country.</param>
		/// <param name="sourceCountryIndex">Source country of the loosing country.</param>
		public bool CountryTransferCountry(int countryIndex, int sourceCountryIndex, bool redraw)
		{
			if (sourceCountryIndex < 0 || sourceCountryIndex >= countries.Length)
				return false;
			var sourceCountryRegion = countries[sourceCountryIndex].mainRegion;
			return CountryTransferCountryRegion(countryIndex, sourceCountryRegion, redraw);
		}

		/// <summary>
		/// Makes countryIndex absorb another country providing any of its regions. All regions are transfered to target country.
		/// This function is quite slow with high definition frontiers.
		/// </summary>
		/// <param name="countryIndex">Country index of the conquering country.</param>
		/// <param name="sourceRegion">Source region of the loosing country.</param>
		public bool CountryTransferCountryRegion(int countryIndex, Region sourceCountryRegion, bool redraw)
		{
			var sourceCountryIndex = GetCountryIndex((Country)sourceCountryRegion.entity);
			if (countryIndex < 0 || sourceCountryIndex < 0 || countryIndex == sourceCountryIndex)
				return false;

			if (_provinces == null && !_showProvinces)
				ReadProvincesPackedString(); // Forces loading of provinces

			// Transfer all provinces records to target country
			var sourceCountry = _countries[sourceCountryIndex];
			var targetCountry = _countries[countryIndex];
			if (targetCountry.provinces != null && sourceCountry.provinces != null)
			{
				var destProvinces = new List<Province>(targetCountry.provinces);
				for (var k = 0; k < sourceCountry.provinces.Length; k++)
				{
					var province = sourceCountry.provinces[k];
					province.countryIndex = countryIndex;
					destProvinces.Add(province);
				}
				destProvinces.Sort(ProvinceSizeComparer);
				targetCountry.provinces = destProvinces.ToArray();
			}

			// Transfer cities
			var cityCount = cities.Length;
			for (var k = 0; k < cityCount; k++)
				if (_cities[k].countryIndex == sourceCountryIndex)
					_cities[k].countryIndex = countryIndex;

			// Transfer mount points
			var mountPointCount = mountPoints.Count;
			for (var k = 0; k < mountPointCount; k++)
				if (mountPoints[k].countryIndex == sourceCountryIndex)
					mountPoints[k].countryIndex = countryIndex;

			// Add main region of the source country to target if they are joint
			if (targetCountry.regions == null)
			{
				targetCountry.regions = new List<Region>();
				targetCountry.regions.Add(sourceCountryRegion);
				sourceCountryRegion.entity = targetCountry;
				sourceCountryRegion.regionIndex = 0;
			}
			else if (targetCountry.mainRegionIndex >= 0 &&
			         targetCountry.mainRegionIndex < targetCountry.regions.Count)
			{
				var targetRegion = targetCountry.regions[targetCountry.mainRegionIndex];

				// Add region to target country's polygon - only if the country is touching or crossing target country frontier
				if (sourceCountryRegion.Intersects(targetRegion))
				{
					RegionMagnet(sourceCountryRegion, targetRegion);
					var clipper = new Clipper();
					clipper.AddPath(targetRegion, PolyType.ptSubject);
					clipper.AddPath(sourceCountryRegion, PolyType.ptClip);
					clipper.Execute(ClipType.ctUnion);
				}
				else
				{
					// Add new region to country
					sourceCountryRegion.entity = targetRegion.entity;
					sourceCountryRegion.regionIndex = targetCountry.regions.Count;
					targetCountry.regions.Add(sourceCountryRegion);
				}
			}

			// Transfer additional regions
			if (sourceCountry.regions.Count > 1)
			{
				var targetRegions = new List<Region>(targetCountry.regions);
				for (var k = 0; k < sourceCountry.regions.Count; k++)
				{
					var otherRegion = sourceCountry.regions[k];
					if (otherRegion != sourceCountryRegion)
						targetRegions.Add(sourceCountry.regions[k]);
				}
				targetCountry.regions = targetRegions;
			}

			// Fusion any adjacent regions that results from merge operation
			MergeAdjacentRegions(targetCountry);
			RegionSanitize(targetCountry.regions);

			// Finish operation
			internal_CountryDelete(sourceCountryIndex, false);
			if (countryIndex > sourceCountryIndex)
				countryIndex--;
			RefreshCountryDefinition(countryIndex, null);
			if (redraw)
				Redraw();
			return true;
		}

		/// <summary>
		/// Changes province's owner to specified country and modifies frontiers/borders.
		/// Note: provinceRegion parameter usually is the province main region - although it does not matter since all regions will transfer as well. 
		/// </summary>
		public bool CountryTransferProvinceRegion(int targetCountryIndex, Region provinceRegion,
			bool redraw)
		{
			if (provinceRegion == null)
				return false;
			var provinceIndex = GetProvinceIndex((Province)provinceRegion.entity);
			if (provinceIndex < 0 || targetCountryIndex < 0 || targetCountryIndex >= _countries.Length)
				return false;

			// Province must belong to another country
			var province = provinces[provinceIndex];
			var sourceCountryIndex = province.countryIndex;
			if (sourceCountryIndex == targetCountryIndex)
				return false;

			// Remove province form source country
			var sourceCountry = _countries[sourceCountryIndex];

			if (sourceCountry.provinces != null)
			{
				var sourceProvinces = new List<Province>(sourceCountry.provinces);
				if (sourceProvinces.Contains(province))
				{
					sourceProvinces.Remove(province);
					sourceCountry.provinces = sourceProvinces.ToArray();
				}
			}

			// Adds province to target country
			var targetCountry = _countries[targetCountryIndex];
			List<Province> destProvinces;
			destProvinces = targetCountry.provinces == null ? new List<Province>() : new List<Province>(targetCountry.provinces);
			destProvinces.Add(province);
			destProvinces.Sort(ProvinceSizeComparer);
			targetCountry.provinces = destProvinces.ToArray();

			// Apply boolean operations on country polygons
			var targetRegion = targetCountry.regions[targetCountry.mainRegionIndex];

			if (!sourceCountry.isPool &&
			    sourceCountry.regions != null &&
			    sourceCountry.mainRegionIndex >= 0 &&
			    sourceCountry.mainRegionIndex < sourceCountry.regions.Count)
			{
				var sourceRegion = sourceCountry.regions[sourceCountry.mainRegionIndex];

				// Extract from source country - only if province is in the frontier or is crossing the country
				var enclave = sourceCountry.Contains(provinceRegion);
				if (!enclave)
				{
					var clipper = new Clipper();
					clipper.AddPaths(sourceCountry.regions, PolyType.ptSubject);
					clipper.AddPath(provinceRegion, PolyType.ptClip);
					clipper.Execute(ClipType.ctDifference, sourceCountry);
				}

				// Remove invalid regions from source country
				for (var k = 0; k < sourceCountry.regions.Count; k++)
				{
					var otherSourceRegion = sourceCountry.regions[k];
					if (!otherSourceRegion.sanitized && otherSourceRegion.points.Length < 5)
					{
						sourceCountry.regions.RemoveAt(k);
						k--;
					}
				}
			}

			// Add all province regions to target country's polygon - only if the province is touching or crossing target country frontier
			for (var k = 0; k < province.regions.Count; k++)
			{
				provinceRegion = province.regions[k];
				var regionAddedToCountry = false;
				if (provinceRegion.Intersects(targetRegion))
				{
					RegionMagnet(provinceRegion, targetRegion);
					regionAddedToCountry = true;
					var clipper = new Clipper();
					clipper.AddPath(targetRegion, PolyType.ptSubject);
					clipper.AddPath(provinceRegion, PolyType.ptClip);
					clipper.Execute(ClipType.ctUnion);
				}
				if (!regionAddedToCountry)
				{
					// Add new region to country: this is a new physical region at the country frontier level - the province will maintain its region at the province level
					var newCountryRegion = new Region(targetCountry, targetCountry.regions.Count);
					newCountryRegion.UpdatePointsAndRect(provinceRegion);
					targetCountry.regions.Add(newCountryRegion);
				}
			}

			// Fusion any adjacent regions that results from merge operation
			MergeAdjacentRegions(targetCountry);

			if (!sourceCountry.isPool)
			{
				// Finds the source country region that could overlap with target country, then substract
				// This handles province extraction but also extraction of two or more provinces than get merged with CountryMergeAdjacentRegions - the result of this merge, needs to be substracted from source country
				var clipper = new Clipper();
				clipper.AddPaths(sourceCountry.regions, PolyType.ptSubject);
				for (var k = 0; k < targetCountry.regions.Count; k++)
					if (!sourceCountry.Contains(targetCountry.regions[k]))
						clipper.AddPath(targetCountry.regions[k], PolyType.ptClip);
				clipper.Execute(ClipType.ctDifference, sourceCountry);

				// Remove invalid regions from source country
				for (var k = 0; k < sourceCountry.regions.Count; k++)
				{
					var otherSourceRegion = sourceCountry.regions[k];
					if (!otherSourceRegion.sanitized && otherSourceRegion.points.Length < 5)
					{
						sourceCountry.regions.RemoveAt(k);
						k--;
					}
				}

				if (!Application.isPlaying) // Ensure all provinces have a land region at country level
					// Auto-fix helper for Map Editor
					if (sourceCountry.provinces != null)
					{
						var crc = sourceCountry.regions.Count;
						for (var p = 0; p < sourceCountry.provinces.Length; p++)
						{
							var sourceProv = sourceCountry.provinces[p];
							if (sourceProv.regions == null)
								ReadProvincePackedString(sourceProv);
							if (sourceProv.regions == null)
								continue;
							var sprc = sourceProv.regions.Count;
							for (var pr = 0; pr < sprc; pr++)
							{
								var sourceProvRegion = sourceProv.regions[pr];
								var covered = false;
								for (var cr = 0; cr < crc; cr++)
								{
									var sourceCountryRegion = sourceCountry.regions[cr];
									if (sourceProvRegion.Intersects(sourceCountryRegion))
									{
										covered = true;
										break;
									}
								}
								if (!covered)
								{
									var newCountryRegion = new Region(sourceCountry,
										sourceCountry.regions.Count);
									newCountryRegion.UpdatePointsAndRect(sourceProvRegion);
									sourceCountry.regions.Add(newCountryRegion);
								}
							}
						}
					}
			}

			// Update cities
			var cityCount = cities.Length;
			for (var k = 0; k < cityCount; k++)
			{
				var city = _cities[k];
				if (city.countryIndex == sourceCountryIndex && city.province.Equals(province.name))
					city.countryIndex = targetCountryIndex;
			}

			// Update mount points
			var mountPointsCount = mountPoints.Count;
			for (var k = 0; k < mountPointsCount; k++)
			{
				var mp = mountPoints[k];
				if (mp.countryIndex == sourceCountryIndex && mp.provinceIndex == provinceIndex)
					mp.countryIndex = targetCountryIndex;
			}

			// Update source country definition
			if (!sourceCountry.isPool)
			{
				if (sourceCountry.regions is { Count: 0 })
				{
					internal_CountryDelete(sourceCountryIndex, false);
					if (targetCountryIndex > sourceCountryIndex)
						targetCountryIndex--;
				}
				else
				{
					RegionSanitize(sourceCountry.regions);
					RefreshCountryDefinition(sourceCountryIndex, null);
				}
			}

			// Update target country definition
			RegionSanitize(targetCountry.regions);
			RefreshCountryDefinition(targetCountryIndex, null);
			province.countryIndex = targetCountryIndex;

			if (redraw)
				Redraw();

			return true;
		}

		/// <summary>
		/// Makes countryIndex absorb an hexagonal portion of the map. If that portion belong to another country, it will be substracted from that country as well.
		/// This function is quite slow with high definition frontiers.
		/// </summary>
		/// <param name="countryIndex">Country index of the conquering country.</param>
		/// <param name="cellIndex">Index of the cell to add to the country.</param>
		public bool CountryTransferCell(int countryIndex, int cellIndex, bool redraw = true)
		{
			if (countryIndex < 0 || cellIndex < 0 || cells == null || cellIndex >= cells.Length)
				return false;

			// Start process
			var country = countries[countryIndex];
			var cell = cells[cellIndex];

			// Create a region for the cell
			var sourceRegion = new Region(country, country.regions.Count);
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
					city.province = ""; // clear province since it does not apply anymore
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
					mp.provinceIndex =
						-1; // same as cities - province cleared in case it's informed since it does not apply anymore
				}
			}

			// Add region to target country's polygon - only if the country is touching or crossing target country frontier
			var targetRegion = country.mainRegion;
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
				sourceRegion.entity = country;
				sourceRegion.regionIndex = country.regions.Count;
				country.regions.Add(sourceRegion);
			}

			// Fusion any adjacent regions that results from merge operation
			MergeAdjacentRegions(country);
			RegionSanitize(country.regions);

			// Finish operation with the country
			RefreshCountryGeometry(country);

			// Substract cell region from any other country
			var otherCountries = GetCountriesOverlap(sourceRegion);
			var orCount = otherCountries.Count;
			for (var k = 0; k < orCount; k++)
			{
				var otherCountry = otherCountries[k];
				if (otherCountry == country)
					continue;
				var clipper = new Clipper();
				clipper.AddPaths(otherCountry.regions, PolyType.ptSubject);
				clipper.AddPath(sourceRegion, PolyType.ptClip);
				clipper.Execute(ClipType.ctDifference, otherCountry);
				if (otherCountry.regions.Count == 0)
				{
					var otherCountryIndex = GetCountryIndex(otherCountry);
					CountryDelete(otherCountryIndex, true, false);
				}
				else
				{
					RegionSanitize(otherCountry.regions);
					RefreshCountryGeometry(otherCountry);
				}
			}

			OptimizeFrontiers();

			if (redraw)
				Redraw();
			return true;
		}

		/// <summary>
		/// Removes a cell from a country.
		/// </summary>
		/// <param name="countryIndex">Country index.</param>
		/// <param name="cellIndex">Index of the cell to remove from the country.</param>
		public bool CountryRemoveCell(int countryIndex, int cellIndex, bool redraw = true)
		{
			if (countryIndex < 0 ||
			    countryIndex >= countries.Length ||
			    cellIndex < 0 ||
			    cells == null ||
			    cellIndex >= cells.Length)
				return false;

			var country = countries[countryIndex];
			var cell = cells[cellIndex];
			var sourceRegion = new Region(country, country.regions.Count);
			sourceRegion.UpdatePointsAndRect(cell.points, true);

			var clipper = new Clipper();
			clipper.AddPaths(country.regions, PolyType.ptSubject);
			clipper.AddPath(sourceRegion, PolyType.ptClip);
			clipper.Execute(ClipType.ctDifference, country);
			if (country.regions.Count == 0)
				CountryDelete(countryIndex, true, false);
			else
			{
				RegionSanitize(country.regions);
				RefreshCountryGeometry(country);
			}

			OptimizeFrontiers();

			if (redraw)
				Redraw();
			return true;
		}

		#endregion

		#region IO functions area

		public void SetCountryGeoData(string s)
		{
			var countryList = s.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
			var countryCount = countryList.Length;
			countries = new Country[countryCount];
			var min = Misc.Vector2one * 10;
			var max = -min;

			var separatorCountries = new[] { '$' };
			for (var k = 0; k < countryCount; k++)
			{
				var countryInfo = countryList[k].Split(separatorCountries, StringSplitOptions.None);
				var name = countryInfo[0];
				var continent = countryInfo[1];
				int uniqueId;
				if (countryInfo.Length >= 5)
					uniqueId = int.Parse(countryInfo[4]);
				else
					uniqueId = GetUniqueId(new List<IExtendableAttribute>(_countries));
				var country = new Country(name, continent, uniqueId);
				var regions = countryInfo[2]
					.Split(SPLIT_SEP_ASTERISK, StringSplitOptions.RemoveEmptyEntries);
				var regionCount = regions.Length;
				country.regions = new List<Region>();
				float maxVol = 0;
				var minCountry = new Vector2(10, 10);
				var maxCountry = -minCountry;
				for (var r = 0; r < regionCount; r++)
				{
					var coordinates = regions[r]
						.Split(SPLIT_SEP_SEMICOLON, StringSplitOptions.RemoveEmptyEntries);
					var coorCount = coordinates.Length;
					if (coorCount < 3)
						continue;
					min.x = min.y = 10;
					max.x = max.y = -10;
					var countryRegion = new Region(country, country.regions.Count);
					var newPoints = new Vector2[coorCount];
					for (var c = 0; c < coorCount; c++)
					{
						float x, y;
						GetPointFromPackedString(ref coordinates[c], out x, out y);
						if (x < min.x)
							min.x = x;
						if (x > max.x)
							max.x = x;
						if (y < min.y)
							min.y = y;
						if (y > max.y)
							max.y = y;
						newPoints[c].x = x;
						newPoints[c].y = y;
					}
					countryRegion.UpdatePointsAndRect(newPoints);
					countryRegion.sanitized = true;

					// Calculate country bounding rect
					if (min.x < minCountry.x)
						minCountry.x = min.x;
					if (min.y < minCountry.y)
						minCountry.y = min.y;
					if (max.x > maxCountry.x)
						maxCountry.x = max.x;
					if (max.y > maxCountry.y)
						maxCountry.y = max.y;
					var vol = FastVector.SqrDistance(ref min, ref max);
					if (vol > maxVol)
					{
						maxVol = vol;
						country.mainRegionIndex = country.regions.Count;
						country.center = countryRegion.center;
					}
					country.regions.Add(countryRegion);
				}
				// hidden
				if (countryInfo.Length >= 4)
				{
					var hidden = 0;
					if (int.TryParse(countryInfo[3], out hidden))
						country.hidden = hidden > 0;
				}
				// fip 10 4
				if (countryInfo.Length >= 6)
					country.fips10_4 = countryInfo[5];
				// iso A2
				if (countryInfo.Length >= 7)
					country.iso_a2 = countryInfo[6];
				// iso A3
				if (countryInfo.Length >= 8)
					country.iso_a3 = countryInfo[7];
				// iso N3
				if (countryInfo.Length >= 9)
					country.iso_n3 = countryInfo[8];
				country.regionsRect2D = new Rect(minCountry.x, minCountry.y,
					Math.Abs(maxCountry.x - minCountry.x), Mathf.Abs(maxCountry.y - minCountry.y));
				_countries[k] = country;
			}
			lastCountryLookupCount = -1;
			needOptimizeFrontiers = true;
		}

		/// <summary>
		/// Exports the geographic data in packed string format.
		/// </summary>
		public string GetCountryGeoData()
		{
			var sb = new StringBuilder();
			for (var k = 0; k < countries.Length; k++)
			{
				var country = countries[k];
				if (country.regions.Count < 1)
					continue;
				if (k > 0)
					sb.Append("|");
				sb.Append(country.name);
				sb.Append("$");
				sb.Append(country.continent);
				sb.Append("$");
				for (var r = 0; r < country.regions.Count; r++)
				{
					if (r > 0)
						sb.Append("*");
					var region = country.regions[r];
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
				sb.Append("$");
				sb.Append(country.hidden ? "1" : "0");
				sb.Append("$");
				sb.Append(country.uniqueId.ToString());
				sb.Append("$");
				sb.Append(country.fips10_4);
				sb.Append("$");
				sb.Append(country.iso_a2);
				sb.Append("$");
				sb.Append(country.iso_a3);
				sb.Append("$");
				sb.Append(country.iso_n3);
			}
			return sb.ToString();
		}

		/// <summary>
		/// Gets XML attributes of all countries in jSON format.
		/// </summary>
		public string GetCountriesAttributes(bool prettyPrint = true) =>
			GetCountriesAttributes(new List<Country>(_countries), prettyPrint);

		/// <summary>
		/// Gets XML attributes of provided countries in jSON format.
		/// </summary>
		public string GetCountriesAttributes(List<Country> countries, bool prettyPrint = true)
		{
			var composed = new JSONObject();
			for (var k = 0; k < countries.Count; k++)
			{
				var country = countries[k];
				if (country.attrib.keys != null)
					composed.AddField(country.uniqueId.ToString(), country.attrib);
			}
			return composed.Print(prettyPrint);
		}

		/// <summary>
		/// Sets countries attributes from a jSON formatted string.
		/// </summary>
		public void SetCountriesAttributes(string jSON)
		{
			var composed = new JSONObject(jSON);
			if (composed.keys == null)
				return;
			var keyCount = composed.keys.Count;
			for (var k = 0; k < keyCount; k++)
			{
				var uniqueId = int.Parse(composed.keys[k]);
				var countryIndex = GetCountryIndex(uniqueId);
				if (countryIndex >= 0)
					_countries[countryIndex].attrib = composed[k];
			}
		}

		#endregion
	}
}