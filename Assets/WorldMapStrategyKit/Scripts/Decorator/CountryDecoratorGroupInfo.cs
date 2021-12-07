using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace WorldMapStrategyKit
{
	[ExecuteInEditMode, Serializable]
	public class CountryDecoratorGroupInfo : MonoBehaviour
	{
		[SerializeField] private bool
			_active;

		public bool active
		{
			get => _active;
			set
			{
				if (value != _active)
				{
					_active = value;
					UpdateDecorators(true);
				}
			}
		}

		[HideInInspector] public int
			groupIndex;

		public List<CountryDecorator> decorators;
		private WMSK _map;

		private WMSK map
		{
			get
			{
				if (_map == null)
					_map = transform.parent.parent.GetComponent<WMSK>();
				return _map;
			}
		}

		private int lastCheck;

		#region Lifecycle events

		private void OnEnable()
		{
			if (decorators == null)
				decorators = new List<CountryDecorator>();
			UpdateDecorators(true, true);
		}

		private void Start()
		{
			UpdateDecorators(true, true);
		}

		// Update is called once per frame
		private void LateUpdate()
		{
			if (!active)
				return;
			if (++lastCheck % 10 == 0)
			{
				UpdateDecorators(false);
				lastCheck = 0;
			}
		}

		#endregion

		private int GetDecoratorIndex(string countryName)
		{
			for (var k = 0; k < decorators.Count; k++)
				if (decorators[k].countryName.Equals(countryName))
					return k;
			return -1;
		}

		public CountryDecorator GetDecorator(string countryName)
		{
			var k = GetDecoratorIndex(countryName);
			if (k >= 0)
				return decorators[k];
			else
				return null;
		}

		public void SetDecorator(CountryDecorator decorator)
		{
			var k = GetDecoratorIndex(decorator.countryName);
			if (k >= 0)
				decorators[k] = decorator;
			else
				decorators.Add(decorator);
			decorator.isNew = false;
			UpdateDecorators(true);
		}

		public void RemoveDecorator(string countryName)
		{
			var k = GetDecoratorIndex(countryName);
			if (k >= 0)
			{
				decorators[k].Reset();
				UpdateDecorators(true);
				decorators.RemoveAt(k);
			}
		}

		public void RemoveAllDecorators()
		{
			for (var k = 0; k < decorators.Count; k++)
				decorators[k].Reset();
			UpdateDecorators(true);
			decorators.Clear();
		}

		public void UpdateDecorators(bool ignoreActive, bool firstTime = false)
		{
			if (!active && !ignoreActive)
				return;
			if (decorators == null || map == null)
				return;

			var needsLabelRedraw = false;
			var needsFrontiersRedraw = false;

			for (var k = 0; k < decorators.Count; k++)
			{
				var decorator = decorators[k];

				if (!decorator.isPersistent && !firstTime)
					continue;

				// Check if something needs to be changed
				var countryIndex = map.GetCountryIndex(decorator.countryName);
				if (countryIndex >= 0)
				{
					var country = map.countries[countryIndex];
					var mainRegion = country.regions[country.mainRegionIndex];
					if (active)
					{
						// label Font override
						if (country.labelFontOverride != decorator.labelFontOverride)
						{
							country.labelFontOverride = decorator.labelFontOverride;
							needsLabelRedraw = true;
						}
						// label color override
						if (country.labelColorOverride != decorator.labelOverridesColor)
						{
							country.labelColorOverride = decorator.labelOverridesColor;
							needsLabelRedraw = true;
						}
						if (country.labelColorOverride && country.labelColor != decorator.labelColor)
						{
							country.labelColor = decorator.labelColor;
							needsLabelRedraw = true;
						}
						// label visible
						if (country.labelVisible != decorator.labelVisible)
						{
							country.labelVisible = decorator.labelVisible;
							needsLabelRedraw = true;
						}
						// label rotation
						if (country.labelRotation != decorator.labelRotation)
						{
							country.labelRotation = decorator.labelRotation;
							needsLabelRedraw = true;
						}
						// label offset
						if (country.labelOffset != decorator.labelOffset)
						{
							country.labelOffset = decorator.labelOffset;
							needsLabelRedraw = true;
						}
						// custom label
						if (decorator.labelOverride &&
						    (country.customLabel == null && decorator.customLabel.Length > 0 ||
						     country.customLabel != null && country.customLabel != decorator.customLabel))
						{
							if (decorator.customLabel.Length > 0)
								country.customLabel = decorator.customLabel;
							else
								country.customLabel = null;
							if (country.labelTextMeshGO != null)
							{
								DestroyImmediate(country.labelTextMeshGO);
								country.labelTextMeshGO = null;
								country.labelTextMesh = null;
							}
							needsLabelRedraw = true;
						}
						// label font size override
						if (country.labelFontSizeOverride != decorator.labelOverridesFontSize)
						{
							country.labelFontSizeOverride = decorator.labelOverridesFontSize;
							needsLabelRedraw = true;
						}
						if (country.labelFontSizeOverride &&
						    country.labelFontSize != decorator.labelFontSize)
						{
							country.labelFontSize = decorator.labelFontSize;
							needsLabelRedraw = true;
						}
						// colorize
						if (decorator.isColorized)
						{
							if (decorator.includeAllRegions)
								map.ToggleCountrySurface(countryIndex, true, decorator.fillColor,
									decorator.texture, decorator.textureScale, decorator.textureOffset,
									decorator.textureRotation, decorator.applyTextureToAllRegions);
							else
								map.ToggleCountryMainRegionSurface(countryIndex, true, decorator.fillColor,
									decorator.texture, decorator.textureScale, decorator.textureOffset,
									decorator.textureRotation);
						}
						else if (!decorator.isColorized &&
						         mainRegion.customMaterial != null &&
						         mainRegion.customMaterial.color == decorator.fillColor)
							map.HideCountrySurface(countryIndex);
						// hidden
						if (country.hidden != decorator.hidden)
						{
							country.hidden = decorator.hidden;
							needsFrontiersRedraw = true;
						}
					}
					else
					{
						if (country.labelFontOverride != null)
						{
							country.labelFontOverride = null;
							needsLabelRedraw = true;
						}
						if (country.labelColorOverride)
						{
							country.labelColorOverride = false;
							needsLabelRedraw = true;
						}
						if (!country.labelVisible)
						{
							country.labelVisible = true;
							needsLabelRedraw = true;
						}
						if (decorator.labelOverride && country.customLabel != null)
						{
							country.customLabel = null;
							needsLabelRedraw = true;
						}
						if (country.labelFontSizeOverride)
						{
							country.labelFontSizeOverride = false;
							needsLabelRedraw = true;
						}
						if (country.labelRotation > 0)
						{
							country.labelRotation = 0;
							needsLabelRedraw = true;
						}
						if (country.labelOffset != Misc.Vector2zero)
						{
							country.labelOffset = Misc.Vector2zero;
							needsLabelRedraw = true;
						}
						if (country.hidden)
						{
							country.hidden = false;
							needsFrontiersRedraw = true;
						}
						if (mainRegion.customMaterial != null)
						{
							mainRegion.customMaterial = null;
							map.HideCountrySurface(countryIndex);
						}
					}
				}
			}

			if (needsFrontiersRedraw)
			{
				map.OptimizeFrontiers();
				map.Redraw();
			}
			else if (needsLabelRedraw)
				map.DrawMapLabels();
		}

		public List<string> GetDecoratedCountries(bool addCountryIndexSuffix)
		{
			var decoratedCountries = new List<string>();
			if (decorators == null || map == null)
				return decoratedCountries;
			for (var k = 0; k < decorators.Count; k++)
			{
				var s = decorators[k].countryName;
				if (addCountryIndexSuffix)
					s += " (" + map.GetCountryIndex(s) + ")";
				decoratedCountries.Add(s);
			}
			return decoratedCountries;
		}
	}
}