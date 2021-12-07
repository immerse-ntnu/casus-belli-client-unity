using UnityEngine;
using System.Collections.Generic;

namespace WorldMapStrategyKit
{
	public abstract class AdminEntity : IAdminEntity, IExtendableAttribute
	{
		/// <summary>
		/// Entity name (country/province or province).
		/// </summary>
		public string name { get; set; }

		/// <summary>
		/// List of all regions for the admin entity.
		/// </summary>
		public List<Region> regions { get; set; }

		/// <summary>
		/// Index of the biggest region
		/// </summary>
		public int mainRegionIndex { get; set; }

		/// <summary>
		/// Returns the region object which is the main region of the country/province
		/// </summary>
		public Region mainRegion
		{
			get
			{
				if (mainRegionIndex < 0 || regions == null || mainRegionIndex >= regions.Count)
					return null;
				else
					return regions[mainRegionIndex];
			}
		}

		/// <summary>
		/// Setting hidden to true will hide completely the country/province (border, label) and it won't be highlighted
		/// </summary>
		public bool hidden { get; set; }

		/// <summary>
		/// Computed Rect area that includes all regions. Used to fast hovering.
		/// </summary>
		public Rect regionsRect2D { get; set; }

		public float regionsRect2DArea => regionsRect2D.width * regionsRect2D.height;

		/// <summary>
		/// Center of the biggest region of the entity in the plane
		/// </summary>
		public Vector2 center { get; set; }

		/// <summary>
		/// Center of the rectangle enclosing all entity regions (equals to regionsRect2D.center)
		/// </summary>
		/// <value>The center rect.</value>
		public Vector2 centerRect => regionsRect2D.center;

		/// <summary>
		/// An unique identifier useful to persist data between sessions. Used by serialization.
		/// </summary>
		public int uniqueId { get; set; }

		/// <summary>
		/// Use this property to add/retrieve custom attributes for this country/province
		/// </summary>
		public JSONObject attrib { get; set; }

		private bool _allowHighlight = true;

		/// <summary>
		/// If this entity can be highlighted.
		/// </summary>
		public bool allowHighlight { get => _allowHighlight; set => _allowHighlight = value; }

		private bool _canCross = true;

		/// <summary>
		/// Used by pathfinding in country/province mode to determine if route can cross a country/province. Defaults to true.
		/// </summary>
		public bool canCross { get => _canCross; set => _canCross = value; }

		private float _crossCost = 1;

		/// <summary>
		/// Used by pathfinding in country/province. Cost for crossing a country/province. Defaults to 1.
		/// </summary>
		/// <value>The cross cost.</value>
		public float crossCost { get => _crossCost; set => _crossCost = value; }

		/// <summary>
		/// Custom array of countries/provinces that could be reached from this country/province. Useful for country/province/Province path-finding.
		/// It defaults to natural neighbours of the country/province/province but you can modify its contents and add your own potential destinations per country/province/province.
		/// </summary>
		public abstract int[] neighbours { get; set; }

		/// <summary>
		/// Used internally by Map Editor.
		/// </summary>
		public bool foldOut { get; set; }

		/// <summary>
		/// Optional custom label. It set, it will be displayed instead of the country/province name.
		/// </summary>
		public string customLabel;

		/// <summary>
		/// Set it to true to specify a custom color for the label.
		/// </summary>
		public bool labelColorOverride;

		/// <summary>
		/// The color of the label.
		/// </summary>
		public Color labelColor = Color.white;

		private Font _labelFont;
		private Material _labelShadowFontMaterial;

		/// <summary>
		/// Internal method used to obtain the shadow material associated to a custom Font provided.
		/// </summary>
		/// <value>The label shadow font material.</value>
		public Material labelFontShadowMaterial => _labelShadowFontMaterial;

		/// <summary>
		/// Optional font for this label. Note that the font material will be instanced so it can change color without affecting other labels.
		/// </summary>
		public Font labelFontOverride
		{
			get => _labelFont;
			set
			{
				if (value != _labelFont)
				{
					_labelFont = value;
					if (_labelFont != null)
					{
						var fontMaterial = Object.Instantiate(_labelFont.material);
						//fontMaterial.hideFlags = HideFlags.DontSave;
						_labelFont.material = fontMaterial;
						_labelShadowFontMaterial = Object.Instantiate(fontMaterial);
						//_labelShadowFontMaterial.hideFlags = HideFlags.DontSave;
						_labelShadowFontMaterial.renderQueue--;
					}
				}
			}
		}

		/// <summary>
		/// Returns true if any of the entity's regions contains the point
		/// </summary>
		/// <param name="point">Point.</param>
		public bool Contains(Vector2 point)
		{
			if (!regionsRect2D.Contains(point))
				return false;

			var regionsCount = regions.Count;
			for (var k = 0; k < regionsCount; k++)
				if (regions[k].Contains(point))
					return true;
			return false;
		}

		/// <summary>
		/// Returns true if any of the entity's regions contains the region
		/// </summary>
		/// <param name="point">Point.</param>
		public bool Contains(Region region)
		{
			if (!regionsRect2D.Overlaps(region.rect2D))
				return false;

			var regionsCount = regions.Count;
			for (var k = 0; k < regionsCount; k++)
				if (regions[k].Contains(region))
					return true;
			return false;
		}

		/// <summary>
		/// Sets whether the country/province name will be shown or not.
		/// </summary>
		public bool labelVisible = true;

		/// <summary>
		/// If set to a value > 0 degrees then label will be rotated according to this value (and not automatically calculated).
		/// </summary>
		public float labelRotation = 0;

		/// <summary>
		/// If set to a value != 0 in both x/y then label will be moved according to this value (and not automatically calculated).
		/// </summary>
		public Vector2 labelOffset = Misc.Vector2zero;

		/// <summary>
		/// If the label has its own font size.
		/// </summary>
		public bool labelFontSizeOverride = false;

		/// <summary>
		/// Manual font size for the label. Must set labelOverridesFontSize = true to have effect.
		/// </summary>
		public float labelFontSize = 0.2f;

		#region internal fields

		// Used internally. Don't change fields below.
		public GameObject labelTextMeshGO;
		public TextMesh labelTextMesh, labelShadowTextMesh;
		public Object labelTextMeshPro;
		public float labelMeshWidth, labelMeshHeight;
		public Vector2 labelMeshCenter;

		#endregion
	}
}