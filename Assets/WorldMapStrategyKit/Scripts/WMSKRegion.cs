// World Strategy Kit for Unity - Main Script
// (C) 2016-2020 by Ramiro Oliva (Kronnect)
// Don't modify this script - changes could be lost if you upgrade to a more recent version of WMSK

using System.Collections.Generic;
using UnityEngine;
using WorldMapStrategyKit.ClipperLib;

namespace WorldMapStrategyKit
{
	public delegate void OnRegionClickEvent(Region region, int buttonIndex);

	public delegate void OnRegionEvent(Region region);

	public partial class WMSK : MonoBehaviour
	{
		#region Public API area

		public event OnRegionClickEvent OnRegionClick;
		public event OnRegionEvent OnRegionEnter;
		public event OnRegionEvent OnRegionExit;

		/// <summary>
		/// Returns the colored surface (game object) of a region. If it has not been colored yet, it will return null.
		/// </summary>
		public GameObject GetRegionSurfaceGameObject(Region region)
		{
			int cacheIndex;
			if (region.entity is Country)
			{
				var countryIndex = GetCountryIndex((Country)region.entity);
				cacheIndex = GetCacheIndexForCountryRegion(countryIndex, region.regionIndex);
			}
			else
			{
				var provinceIndex = GetProvinceIndex((Province)region.entity);
				cacheIndex = GetCacheIndexForProvinceRegion(provinceIndex, region.regionIndex);
			}
			GameObject obj;
			if (surfaces.TryGetValue(cacheIndex, out obj))
				return obj;
			return null;
		}

		/// <summary>
		/// Returns the color of the surface of a region. If it has not been colored yet, it will return Transparent (0,0,0,0).
		/// </summary>
		public Color GetRegionColor(Region region)
		{
			var go = GetRegionSurfaceGameObject(region);
			if (go != null)
			{
				var renderer = go.GetComponent<Renderer>();
				if (renderer != null)
					return renderer.sharedMaterial.color;
			}
			return new Color(0, 0, 0, 0);
		}

		/// <summary>
		/// Gets a list of regions that overlap with a given region
		/// </summary>
		public List<Region> GetRegionsOverlap(Region region, bool includeProvinces = false)
		{
			var rr = new List<Region>();
			for (var k = 0; k < _countries.Length; k++)
			{
				var country = _countries[k];
				if (country.regions == null)
					continue;
				var rCount = country.regions.Count;
				for (var r = 0; r < rCount; r++)
				{
					var otherRegion = country.regions[r];
					if (region.Intersects(otherRegion))
						rr.Add(otherRegion);
				}
			}

			if (includeProvinces)
			{
				var provinceCount = provinces.Length; // triggers lazy load
				for (var k = 0; k < provinceCount; k++)
				{
					var province = _provinces[k];
					if (province.regions == null)
						continue;
					var rCount = province.regions.Count;
					for (var r = 0; r < rCount; r++)
					{
						var otherRegion = province.regions[r];
						if (region.Intersects(otherRegion))
							rr.Add(otherRegion);
					}
				}
			}
			return rr;
		}

		/// <summary>
		/// Paints the region pixels with a given eraseColor.
		/// </summary>
		/// <param name="region">Region.</param>
		/// <param name="eraseColor">Erase color.</param>
		/// <param name="redraw">If set to true, redraws the map. if you're calling RegionErase several times, pass false and call Redraw(true) manually at the end.</param>
		public void RegionErase(Region region, Color eraseColor)
		{
			if (region == null)
				return;
			var regions = new List<Region>();
			regions.Add(region);
			RegionErase(regions, eraseColor);
		}

		/// <summary>
		/// Paints the region pixels with a given eraseColor.
		/// </summary>
		/// <param name="region">Region.</param>
		/// <param name="eraseColor">Erase color.</param>
		/// <param name="redraw">If set to true, redraws the map. if you're calling RegionErase several times, pass false and call Redraw(true) manually at the end.</param>
		public void RegionErase(List<Region> regions, Color eraseColor)
		{
			RegionErase(regions, eraseColor, false);
		}

		/// <summary>
		/// Paints the region pixels with a given eraseColor.
		/// </summary>
		/// <param name="region">Region.</param>
		/// <param name="eraseColor">Erase color.</param>
		/// <param name="invertMode">If set to true, clears everything except the regions</param>
		/// <param name="redraw">If set to true, redraws the map. if you're calling RegionErase several times, pass false and call Redraw(true) manually at the end.</param>
		public void RegionErase(List<Region> regions, Color eraseColor, bool invertMode)
		{
			if (regions == null)
				return;

			// Get all triangles and its colors
			Texture2D texture;
			Color[] colors;
			var tex = (Texture2D)transform.GetComponent<Renderer>().sharedMaterial.mainTexture;
			if (_earthStyle == EARTH_STYLE.SolidColor || tex == null)
			{
				var tw = 2048;
				var th = 1024;
				texture = new Texture2D(tw, th, TextureFormat.RGB24, false);
				colors = new Color[tw * th];
				var solidColor = _earthColor;
				for (var k = 0; k < colors.Length; k++)
					colors[k] = solidColor;
			}
			else
			{
				var tw = tex.width;
				var th = tex.height;
				texture = new Texture2D(tw, th, TextureFormat.RGB24, false);
				colors = tex.GetPixels();
			}
			if (disposalManager != null)
				disposalManager.MarkForDisposal(texture); //.hideFlags = HideFlags.DontSave;
			var width = texture.width;
			var height = texture.height;
			var rCount = regions.Count;
			if (invertMode)
			{
				var maskColors = new Color[width * height];
				var maskColor = Color.white;
				var entities = new List<IAdminEntity>();
				for (var k = 0; k < rCount; k++)
				{
					if (regions[k] == null)
						continue;
					RegionPaint(maskColors, width, height, regions[k], maskColor);
					entities.Add(regions[k].entity);
				}
				// Clears all colors from original texture except the masked colors
				for (var k = 0; k < colors.Length; k++)
					if (maskColors[k].a < 1f)
						colors[k] = eraseColor;
				// Mark all other entities as hidden
				for (var k = 0; k < _countries.Length; k++)
					if (!entities.Contains(_countries[k]))
						_countries[k].hidden = true;
			}
			else
				for (var k = 0; k < rCount; k++)
				{
					if (regions[k] == null)
						continue;
					RegionPaint(colors, width, height, regions[k], eraseColor);
					regions[k].entity.hidden = true;
				}
			texture.SetPixels(colors);
			texture.Apply();
			transform.GetComponent<Renderer>().sharedMaterial.mainTexture = texture;
		}

		/// <summary>
		/// Marks a list of regions to extrude by a given amount (0..1)
		/// </summary>
		/// <param name="amount">Amount in the 0..1 range.</param>
		public void RegionSetCustomElevation(List<Region> regions, float amount)
		{
			if (regions == null)
				return;
			var count = regions.Count;
			var changes = false;
			for (var k = 0; k < count; k++)
			{
				var region = regions[k];
				if (region != null)
				{
					if (region.extrusionAmount != amount)
					{
						region.extrusionAmount = amount;
						changes = true;
					}
					if (!extrudedRegions.Contains(region))
					{
						extrudedRegions.Add(region);
						changes = true;
					}
				}
			}
			if (changes)
			{
				earthLastElevation = -1;
				if (renderViewportIsEnabled)
					UpdateViewport();
			}
		}

		/// <summary>
		/// Cancels extrusion effect of a set of regions
		/// </summary>
		/// <param name="regions">Regions.</param>
		public void RegionRemoveCustomElevation(List<Region> regions)
		{
			if (regions == null)
				return;
			var count = regions.Count;
			var changes = false;
			for (var k = 0; k < count; k++)
			{
				regions[k].extrusionAmount = 0;
				if (extrudedRegions.Contains(regions[k]))
				{
					changes = true;
					extrudedRegions.Remove(regions[k]);
				}
			}
			if (changes)
			{
				earthLastElevation = -1;
				if (renderViewportIsEnabled)
					UpdateViewport();
			}
		}

		/// <summary>
		/// Removes any extrusion from all regions
		/// </summary>
		public void RegionRemoveAllCustomElevations()
		{
			if (extrudedRegions == null)
				return;
			var changes = false;
			var count = extrudedRegions.Count;
			for (var k = 0; k < count; k++)
			{
				var region = extrudedRegions[k];
				if (region.extrusionAmount > 0)
				{
					extrudedRegions[k].extrusionAmount = 0;
					changes = true;
				}
			}
			extrudedRegions.Clear();
			if (changes)
			{
				earthLastElevation = -1;
				if (renderViewportIsEnabled)
					UpdateViewport();
			}
		}

		/// <summary>
		/// Returns a region which is the result of mergin region1 and region2. Original regions are not modified.
		/// </summary>
		/// <returns>The merge.</returns>
		/// <param name="region1">Region1.</param>
		/// <param name="region2">Region2.</param>
		public Region RegionMerge(Region region1, Region region2)
		{
			var newRegion = region1.Clone();

			RegionMagnet(newRegion, region2);
			var clipper = new Clipper();
			clipper.AddPath(newRegion, PolyType.ptSubject);
			clipper.AddPath(region2, PolyType.ptClip);
			clipper.Execute(ClipType.ctUnion, newRegion);
			return newRegion;
		}

		/// <summary>
		/// Draws an independent outline for a given region and returns the borders gameobject
		/// </summary>
		/// <returns>The region outline.</returns>
		/// <param name="name">Name for the outline gameobject.</param>
		/// <param name="region">Region.</param>
		/// <param name="overridesAnimationSpeed">If set to <c>true</c> overrides animation speed.</param>
		/// <param name="animationSpeed">Animation speed.</param>
		public GameObject DrawRegionOutline(string name, Region region, Texture2D borderTexure = null,
			float borderWidth = 0.1f, Color tintColor = default, float textureTiling = 1f,
			float animationSpeed = 0f)
		{
			region.customBorderTexture = borderTexure;
			region.customBorderWidth = borderWidth;
			region.customBorderTextureTiling = textureTiling;
			region.customBorderAnimationSpeed = animationSpeed;
			if (tintColor != default)
				region.customBorderTintColor = tintColor;
			var boldFrontiers = DrawRegionOutlineMesh(name, region, true, animationSpeed);
			boldFrontiers.layer = gameObject.layer;
			boldFrontiers.transform.SetParent(transform, false);
			return boldFrontiers;
		}

		#endregion
	}
}