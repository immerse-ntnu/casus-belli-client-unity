#if !UNITY_WSA
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace WorldMapStrategyKit
{
	public enum TerritoriesImporterMode
	{
		Countries = 0,
		Provinces = 1
	}

	public class WMSK_TextureImporter
	{
		private const int MAX_THREADS = 16;
		private const int LAST_FILTER = 4;
		private const int REPLACEABLE_COLOR = 37;
		private const string BACKGROUND_COUNTRY = "";

		private enum DIR
		{
			TR,
			R,
			BR,
			B,
			BL,
			L,
			TL,
			T
		}

		private struct ThreadSlot
		{
			public Thread thread;
			public bool busy;
			public int y0, y1;
			public int colorIndex;
			public List<Vector2[]> points;
			public Vector2[] positions;
			public Color32[] colors;
		}

		public float progress;
		public string results;
		public Texture2D texture;
		public TerritoriesImporterMode mode;
		public bool snapToCountryFrontiers;

		private float[] directionXOffset = { -0.15f, 0.00f, 0.15f, 0.25f, -0.15f, 0.00f, -0.15f, -0.25f };

		private float[] directionYOffset = { 0.15f, 0.25f, 0.15f, 0.00f, 0.15f, -0.25f, -0.15f, 0.00f };

		private int[] directionDX = { 1, 1, 1, 0, -1, -1, -1, 0 };
		private int[] directionDY = { 1, 0, -1, -1, -1, 0, 1, 1 };

		public int goodColorCount
		{
			get
			{
				if (goodColors != null)
					return goodColors.Count;
				return 0;
			}
		}

		private int y, tw, th;
		private Color32[] colors;
		private Color32[] colorsAux;
		public List<Color32> goodColors;
		private Dictionary<Color32, int> goodColorsDict;
		private int colorIndex;
		private static Color32 color0 = new(0, 0, 0, 0);
		private Color32 backgroundColor;
		private int filter, totalPasses;
		public string status;
		public List<IAdminEntity> entities;
		private int actualReplaceableColors, totalReplaceableColors;
		private ThreadSlot[] threads;

		public WMSK_TextureImporter(Texture2D texture, Color32 backgroundColor, int detail)
		{
			progress = 0f;
			results = "";
			tw = texture.width;
			th = texture.height;
			colors = texture.GetPixels32();
			colorsAux = new Color32[colors.Length];
			this.texture = texture;
			this.backgroundColor = backgroundColor;
			entities = new List<IAdminEntity>();
			threads = new ThreadSlot[MAX_THREADS];
			for (var k = 0; k < threads.Length; k++)
			{
				threads[k].points = new List<Vector2[]>();
				threads[k].positions = new Vector2[16000];
			}

			GetTerritoriesColors(detail);
			if (goodColors.Count == 0)
				return;
			colorIndex = 0;
			filter = -1;
			NextFilter();
		}

		public bool IsGoodColor(Color32 color) => goodColorsDict.ContainsKey(color);

		public void AddGoodColor(Color32 color)
		{
			if (!goodColorsDict.ContainsKey(color))
			{
				var count = goodColors.Count;
				goodColors.Add(color);
				goodColorsDict[color] = count;
			}
		}

		public void ClearGoodColors()
		{
			goodColors.Clear();
			goodColorsDict.Clear();
		}

		public Country[] GetCountries()
		{
			if (mode != TerritoriesImporterMode.Countries)
				return null;
			var entitiesCount = entities.Count;
			for (var k = 0; k < entitiesCount; k++)
				if (entities[k].regions.Count == 0)
				{
					entities.RemoveAt(k);
					entitiesCount--;
					if (k > 0)
						k--;
				}
			entitiesCount = entities.Count;

			// Check for holes
			for (var c = 0; c < entitiesCount; c++)
			{
				var country = (Country)entities[c];
				if (country.name.Equals(BACKGROUND_COUNTRY))
					continue;
				WMSK.instance.RefreshCountryGeometry(country);
				for (var r0 = 0; r0 < country.regions.Count; r0++)
				{
					var region0 = country.regions[r0];
					for (var r1 = 0; r1 < country.regions.Count; r1++)
					{
						if (r1 == r0)
							continue;
						var region1 = country.regions[r1];
						if (region0.Contains(region1))
						{
							// region1 is a hole
							if (!CountryBordersTooNearRegion(country, region1))
							{
								var bgCountry = GetBackgroundCountry();
								bgCountry.regions.Add(region1);
							}
							country.regions.Remove(region1);
							r0 = -1;
							break;
						}
					}
				}
			}

			entitiesCount = entities.Count;
			var newCountries = new Country[entitiesCount];
			for (var k = 0; k < entitiesCount; k++)
				newCountries[k] = (Country)entities[k];
			return newCountries;
		}

		/// <summary>
		/// Ensures that a region is not too near a country borders
		/// Returns true if they're too near
		/// </summary>
		private bool CountryBordersTooNearRegion(Country country, Region region)
		{
			const float threshold = 0.0001f;
			var crc = country.regions.Count;
			for (var k = 0; k < crc; k++)
			{
				var cRegion = country.regions[k];
				if (cRegion == region)
					return true;
				var points0 = region.points;
				var points1 = cRegion.points;
				for (var p0 = 0; p0 < points0.Length; p0++)
				{
					var p0x = points0[p0].x;
					var p0y = points0[p0].y;
					for (var p1 = 0; p1 < points1.Length; p1++)
					{
						var p1x = points1[p1].x;
						var p1y = points1[p1].y;
						var dx = p0x - p1x;
						var dy = p0y - p1y;
						dx = dx < 0 ? -dx : dx;
						dy = dy < 0 ? -dy : dy;
						if (dx < threshold || dy < threshold)
							return true;
					}
				}
			}
			return false;
		}

		private Country GetBackgroundCountry()
		{
			for (var k = 0; k < entities.Count; k++)
			{
				var country = (Country)entities[k];
				if (country.name.Equals(BACKGROUND_COUNTRY))
					return country;
			}
			var bgCountry = new Country(BACKGROUND_COUNTRY, "World", 0);
			bgCountry.hidden = true;
			entities.Add(bgCountry);
			return bgCountry;
		}

		public Province[] GetProvinces()
		{
			if (mode != TerritoriesImporterMode.Provinces)
				return null;
			var entitiesCount = entities.Count;
			for (var k = 0; k < entitiesCount; k++)
				if (entities[k].regions == null || entities[k].regions.Count == 0)
				{
					entities.RemoveAt(k);
					entitiesCount--;
					k--;
				}
			entitiesCount = entities.Count;
			var newProvinces = new Province[entitiesCount];
			for (var k = 0; k < entitiesCount; k++)
				newProvinces[k] = (Province)entities[k];
			return newProvinces;
		}

		private void GetTerritoriesColors(int detail)
		{
			var prevColor = color0;
			var colorCount = colors.Length;
			if (goodColors == null)
			{
				goodColors = new List<Color32>(256);
				goodColorsDict = new Dictionary<Color32, int>(256);
			}
			else
			{
				goodColors.Clear();
				goodColorsDict.Clear();
			}

			totalReplaceableColors = 0;
			actualReplaceableColors = 0;
			for (var j = 2; j < th - 2; j++)
			{
				var jj = j * tw;
				for (var k = 2; k < tw - 2; k++)
				{
					var index = jj + k;
					var co = colors[index];
					if (isSameRGB(co, prevColor))
						continue;
					if (!isGoodColor(co))
						continue;
					var same = true;
					// Check neighbours
					for (var j0 = j - detail; j0 <= j + detail && same; j0++)
					{
						var j0j0 = j0 * tw;
						for (var k0 = k - detail; k0 <= k + detail; k0++)
						{
							var otherIndex = j0j0 + k0;
							var otherColor = colors[otherIndex];
							if (!isSameRGB(co, otherColor))
							{
								same = false;
								break;
							}
						}
					}
					if (same)
					{
						var count = goodColors.Count;
						goodColorsDict[co] = count;
						goodColors.Add(co);
						prevColor = co;
					}
					else
					{
						// Mark this color as replaceable
						colors[index].a = REPLACEABLE_COLOR;
						totalReplaceableColors++;
					}
				}
			}
			actualReplaceableColors = totalReplaceableColors;
		}

		public void CancelOperation()
		{
			IssueTextureUpdate();
			progress = 1f;
		}

		public void IssueTextureUpdate()
		{
			if (texture != null)
			{
				if (filter == 1)
					texture.SetPixels32(colorsAux);
				else
					texture.SetPixels32(colors);
				texture.Apply();
			}
		}

		private bool isGoodColor(Color32 co) =>
			co.a > 0 && !isSameRGB(co, backgroundColor) && !goodColorsDict.ContainsKey(co);

		public void StartProcess(TerritoriesImporterMode mode, bool snapToCountryFrontiers)
		{
			this.mode = mode;
			this.snapToCountryFrontiers = snapToCountryFrontiers;
			totalPasses = 5;
			if (snapToCountryFrontiers && mode == TerritoriesImporterMode.Provinces)
				totalPasses++;
			entities = new List<IAdminEntity>(goodColors.Count);
			switch (mode)
			{
				case TerritoriesImporterMode.Countries:
					for (var k = 0; k < goodColors.Count; k++)
						entities.Add(new Country("Country " + k, "World", k));
					break;
				case TerritoriesImporterMode.Provinces:
					for (var k = 0; k < goodColors.Count; k++)
						entities.Add(new Province("Province " + k, -1, k));
					break;
			}
			filter = 0;
			progress = 0f;
		}

		public void Process()
		{
			results = "";
			switch (filter)
			{
				case 0:
					FilterRemoveBackground();
					break;
				case 1:
					FilterFindEdges();
					break;
				case 2:
					FilterExtractRegions();
					break;
				case 3:
					FilterSnapRegions();
					break;
				case 4:
					FilterPolishJoints();
					break;
				case 5:
					FilterSnapToCountryFrontiers();
					break;
			}
			if (progress >= 1f)
				NextFilter();
		}

		private void NextFilter()
		{
			filter++;
			switch (filter)
			{
				case 0:
					status = "Removing background and odd colors (pass 1/" + totalPasses + ")...";
					break;
				case 1:
					status = "Finding edges (pass 2/" + totalPasses + ")...";
					break;
				case 2:
					colorIndex = 0;
					status = "Extracting regions (pass 3/" + totalPasses + ")...";
					break;
				case 3:
					snapEntityIndex = 0;
					status = "Snapping regions (pass 4/" + totalPasses + ")...";
					break;
				case 4:
					polishEntityIndex = 0;
					status = "Polishing joints (pass 5/" + totalPasses + ")...";
					break;
				case 5:
					if (mode == TerritoriesImporterMode.Provinces && snapToCountryFrontiers)
					{
						snapToCountryFrontiersIndex = 0;
						status = "Snapping to country frontiers (pass 6/" + totalPasses + ")...";
					}
					else
						filter++;
					break;
			}

			texture.SetPixels32(colors);
			texture.Apply();

			if (filter < totalPasses - 1)
				progress = 0;
		}

		private void FilterRemoveBackground()
		{
			// Check odd colors
			if (progress < 0.9f)
			{
				var changes = false;
				var max = colors.Length - tw;
				for (var k = 0; k < max; k++)
				{
					var co = colors[k];
					if (co.a == REPLACEABLE_COLOR)
					{
						var bottom = colors[k + tw];
						if (bottom.a != REPLACEABLE_COLOR)
						{
							colors[k] = bottom;
							changes = true;
							actualReplaceableColors--;
						}
						else
						{
							var right = colors[k + 1];
							if (right.a != REPLACEABLE_COLOR)
							{
								colors[k] = right;
								changes = true;
								actualReplaceableColors--;
							}
						}
					}
				}
				if (!changes)
					progress = 0.9f;
				else
					progress = 0.9f * (1f - (float)actualReplaceableColors / totalReplaceableColors);
			}
			else
			{
				for (var k = 0; k < colors.Length; k++)
				{
					var co = colors[k];
					if (isSameRGB(co, backgroundColor))
						colors[k] = color0;
				}
				progress = 1f;
			}
		}

		private void FilterFindEdges()
		{
			// Check thread status
			var activeThreads = 0;
			for (var t = 0; t < threads.Length; t++)
				if (threads[t].busy)
				{
					activeThreads++;
					// Manage current threads
					if (!threads[t].thread.IsAlive)
					{
						progress += (threads[t].y1 - threads[t].y0 + 1f) / th;
						threads[t].busy = false;
					}
				}
				else
				{
					// Check if there's pending work
					if (y < th - 1)
					{
						threads[t].busy = true;
						var slice = 24;
						var y1 = y + slice;
						if (y1 > th)
							y1 = th - 1;
						threads[t].y0 = y;
						y = y1;
						threads[t].y1 = y1;
						var thisSlice = t;
						threads[t].thread = new Thread(() =>
						{
							Thread.CurrentThread.IsBackground = true;
							FindEdgesInSlice(thisSlice);
						});
						threads[t].thread.Start();
						activeThreads++;
					}
				}
			// Ensure progress is not 1 until all threads have finished
			if (progress >= 1f)
				progress = 0.99f;
			if (activeThreads == 0)
			{
				progress = 1f;
				Array.Copy(colorsAux, colors, colors.Length);
			}
		}

		private void FindEdgesInSlice(int t)
		{
			for (var y = threads[t].y0; y <= threads[t].y1; y++)
			{
				var pos = y * tw;
				for (var x = 0; x < tw; x++, pos++)
				{
					var c = colors[pos];
					if (y > 0 && x > 0 && y < th - 1 && x < tw - 1)
					{
						var n = colors[pos - tw];
						var w = colors[pos - 1];
						var e = colors[pos + 1];
						var s = colors[pos + tw];
						if (c.r == n.r &&
						    c.r == w.r &&
						    c.r == e.r &&
						    c.r == s.r &&
						    c.g == n.g &&
						    c.g == w.g &&
						    c.g == e.g &&
						    c.g == s.g &&
						    c.b == n.b &&
						    c.b == w.b &&
						    c.b == e.b &&
						    c.b == s.b)
							c = color0;
					}
					colorsAux[pos] = c;
				}
			}
		}

		private void FilterExtractRegions()
		{
			// Check thread status
			var activeThreads = 0;
			for (var t = 0; t < threads.Length; t++)
				if (threads[t].busy)
				{
					activeThreads++;
					// Manage current threads
					if (!threads[t].thread.IsAlive)
					{
						var listCount = threads[t].points.Count;
						for (var k = 0; k < listCount; k++)
						{
							var points = threads[t].points[k];
							switch (mode)
							{
								case TerritoriesImporterMode.Countries:
									AddNewCountryRegion(points, threads[t].colorIndex);
									break;
								case TerritoriesImporterMode.Provinces:
									AddNewProvinceRegion(points, threads[t].colorIndex);
									break;
							}
						}
						progress += 1f / goodColorCount;
						if (progress >= 1f)
							progress = 0.99f;
						threads[t].busy = false;
					}
				}
				else
				{
					// Check if there's pending work
					if (colorIndex < goodColorCount)
					{
						threads[t].busy = true;
						threads[t].colorIndex = colorIndex++;
						threads[t].points.Clear();
						if (threads[t].colors == null)
							threads[t].colors = new Color32[colors.Length];
						Array.Copy(colors, threads[t].colors, colors.Length);
						var thisThread = t;
						threads[t].thread = new Thread(() =>
						{
							Thread.CurrentThread.IsBackground = true;
							FilterExtractRegions(thisThread);
						});
						threads[t].thread.Start();
						activeThreads++;
					}
				}
			if (activeThreads == 0)
				progress = 1f;
		}

		private void FilterExtractRegions(int t)
		{
			var colorIndex = threads[t].colorIndex;
			var currentColor = goodColors[colorIndex];
			var colors = threads[t].colors;
			var colorArrayIndex = colors.Length - 1;

			// Locate one point for current color
			while (colorArrayIndex >= 0)
			{
				if (colors[colorArrayIndex].a > 0 && isSameRGB(colors[colorArrayIndex], currentColor))
				{
					var points = GetRegionForCurrentColor(t, colorArrayIndex, currentColor);
					if (points.Length > 5)
						threads[t].points.Add(points);
				}
				colorArrayIndex--;
			}
		}

		//		void FilterExtractRegions () {
		//			if (colorIndex >= goodColorCount || progress >= 1f) {
		//				progress = 1f;
		//				return;
		//			}
		//			currentColor = goodColors [colorIndex];
		//			progress = (float)colorIndex / goodColorCount;
		//
		//			// Locate one point for current color
		//			while (colorArrayIndex >= 0) {
		//				if (colors [colorArrayIndex].a > 0 && isSameRGB (colors [colorArrayIndex], currentColor))
		//					break;
		//				colorArrayIndex--;
		//			}
		//			if (colorArrayIndex < 0) {
		//				colorArrayIndex = colors.Length - 1;
		//				colorIndex++;
		//				return;
		//			}
		//
		//			Vector2[] points = GetRegionForCurrentColor ();
		//			if (points.Length > 5) {
		//				switch (mode) {
		//				case TerritoriesImporterMode.Countries:
		//					AddNewCountryRegion (points);
		//					break;
		//				case TerritoriesImporterMode.Provinces:
		//					AddNewProvinceRegion (points);
		//					break;
		//				}
		//			}
		//		}

		private void AddNewCountryRegion(Vector2[] points, int colorIndex)
		{
			var country = (Country)entities[colorIndex];
			var region = new Region(country, country.regions.Count);
			region.points = points;
			country.regions.Add(region);
		}

		private void AddNewProvinceRegion(Vector2[] points, int colorIndex)
		{
			// Compute province center
			var minX = float.MaxValue;
			var maxX = float.MinValue;
			var minY = float.MaxValue;
			var maxY = float.MinValue;
			var posCount = points.Length;
			for (var k = 0; k < posCount; k++)
			{
				if (points[k].x < minX)
					minX = points[k].x;
				if (points[k].x > maxX)
					maxX = points[k].x;
				if (points[k].y < minY)
					minY = points[k].y;
				if (points[k].y > maxY)
					maxY = points[k].y;
			}
			var provinceCenter = new Vector2((maxX + minX) * 0.5f, (maxY + minY) * 0.5f);

			var province = (Province)entities[colorIndex];
			if (province.countryIndex < 0)
			{
				// Get country at province center
				var countryIndex = WMSK.instance.GetCountryIndex(provinceCenter);
				if (countryIndex < 0)
					return;
				province.countryIndex = countryIndex;
			}
			if (province.regions == null)
				province.regions = new List<Region>();
			var region = new Region(province, province.regions.Count);
			region.points = points;
			province.regions.Add(region);
		}

		private Vector2[] GetRegionForCurrentColor(int threadIndex, int colorArrayIndex,
			Color32 currentColor)
		{
			var colors = threads[threadIndex].colors;
			var positions = threads[threadIndex].positions;
			var positionsCount = 0;
			var y = colorArrayIndex / tw;
			var x = colorArrayIndex % tw;
			var direction = DIR.TR;
			var hasMoved = true;
			var directionTries = 0;
			var newPos = Misc.Vector2zero;
			for (var i = 0; i < 200000; i++)
			{
				// safety loop
				if (hasMoved)
				{
					// Based on direction add a little offset to allow turning back
					newPos.x = x + directionXOffset[(int)direction];
					newPos.y = y + directionYOffset[(int)direction];
					if (positionsCount >= positions.Length)
					{
						var newPositions = new Vector2[positionsCount * 2];
						Array.Copy(positions, newPositions, positions.Length);
						threads[threadIndex].positions = newPositions;
						positions = newPositions;
					}
					positions[positionsCount++] = newPos;
					// has completed a lap?
					if (positionsCount >= 5)
					{
						var fx = positions[0].x - newPos.x;
						var fy = positions[0].y - newPos.y;
						if (fx * fx + fy * fy < 3f)
							break;
					}
					hasMoved = false;
					directionTries = 0;
					if (i > 0)
					{
						direction -= 3;
						if ((int)direction < 0)
							direction = (DIR)(((int)direction + 8) % 8);
					}
				}
				var dx = directionDX[(int)direction];
				var dy = directionDY[(int)direction];
				var changeDirection = false;
				if (x + dx >= tw - 1 || x + dx <= 0 || y + dy >= th - 1 || y + dy <= 0)
					changeDirection = true;
				else
				{
					var co = colors[(y + dy) * tw + x + dx];
					if (co.a > 0 && isSameRGB(co, currentColor))
					{
						x += dx;
						y += dy;
						hasMoved = true;
					}
					else
						changeDirection = true;
				}
				if (changeDirection)
				{
					directionTries++;
					if (directionTries > 8)
						break;
					direction++;
					if ((int)direction > 7)
						direction = 0;
				}
			}

			var positionsArray = new Vector2[positionsCount];
			for (var k = 0; k < positionsCount; k++)
			{
				var l = Mathf.RoundToInt(positions[k].y);
				var c = Mathf.RoundToInt(positions[k].x);
				// Extract internal part of region
				colors[l * tw + c] = color0;
				// Convert positions from pixel coordinates to map coordinates
				positionsArray[k].x = (positions[k].x - tw * 0.5f + 0.5f) / tw;
				positionsArray[k].y = (positions[k].y - th * 0.5f + 0.5f) / th;
			}

			return positionsArray;
		}

		private bool isSameRGB(Color32 co1, Color32 co2) =>
			co1.r == co2.r && co1.g == co2.g && co1.b == co2.b;

		#region Region snapping

		private int snapEntityIndex;

		private void FilterSnapRegions()
		{
			if (snapEntityIndex >= entities.Count)
			{
				progress = 1f;
				return;
			}
			progress = (float)snapEntityIndex / entities.Count;
			var entity = entities[snapEntityIndex];
			if (entity.regions != null)
			{
				var entityRegionsCount = entity.regions.Count;
				for (var k = 0; k < entityRegionsCount; k++)
				{
					var regionTosnap = entities[snapEntityIndex].regions[k];
					SnapRegion(regionTosnap);
				}
			}
			snapEntityIndex++;
		}

		private void SnapRegion(Region region)
		{
			var regionPointsCount = region.points.Length;
			var entitiesCount = entities.Count;

			var m = Mathf.Max(tw, th);
			var threshold = 2f / m;
			threshold *= threshold;

			for (var k = 0; k < regionPointsCount; k++)
			{
				var p = region.points[k];
				var nearPoint = Misc.Vector2zero;
				var minDist = float.MaxValue;
				for (var e = 0; e < entitiesCount; e++)
				{
					var entity = entities[e];
					if (entity.regions == null)
						continue;
					var entityRegionsCount = entity.regions.Count;
					for (var r = 0; r < entityRegionsCount; r++)
					{
						var entityRegion = entity.regions[r];
						if (entityRegion == region)
							continue;
						var entityRegionsPointsCount = entityRegion.points.Length;
						for (var j = 0; j < entityRegionsPointsCount; j++)
						{
							var op = entityRegion.points[j];

							// Check if both points are near
							var d = (p.x - op.x) * (p.x - op.x) + (p.y - op.y) * (p.y - op.y);
							if (d < threshold && d < minDist)
							{
								nearPoint = op;
								minDist = d;
							}
						}
					}
				}
				// Snap point?
				if (minDist < float.MaxValue)
					region.points[k] = nearPoint;
			}

			region.RemoveDuplicatePoints();
		}

		#endregion

		#region Polish joins

		private int polishEntityIndex;

		private void FilterPolishJoints()
		{
			if (polishEntityIndex >= entities.Count)
			{
				progress = 1f;
				return;
			}
			progress = (float)polishEntityIndex / entities.Count;
			var entity = entities[polishEntityIndex];
			if (entity.regions != null)
			{
				var entityRegionsCount = entity.regions.Count;
				for (var k = 0; k < entityRegionsCount; k++)
				{
					var region = entities[polishEntityIndex].regions[k];
					PolishRegionJoints(region);
				}
			}
			polishEntityIndex++;
		}

		/// <summary>
		/// For each two points, locate a third point belonging to a different region which is not conected to them and below threshold. Add a new point at that position.
		/// </summary>
		private void PolishRegionJoints(Region region)
		{
			var regionPointsCount = region.points.Length;
			var entitiesCount = entities.Count;
			Vector2 midP = Misc.Vector3zero;

			for (var k = 0; k < regionPointsCount; k++)
			{
				var p0 = region.points[k];
				var nextPointIndex = k == regionPointsCount - 1 ? 0 : k + 1;
				var p1 = region.points[nextPointIndex];
				var p01d = (p0.x - p1.x) * (p0.x - p1.x) + (p0.y - p1.y) * (p0.y - p1.y);
				var snapped = false;
				for (var e = 0; e < entitiesCount && !snapped; e++)
				{
					var entity = entities[e];
					if (entity.regions == null)
						continue;
					var entityRegionsCount = entity.regions.Count;
					for (var r = 0; r < entityRegionsCount && !snapped; r++)
					{
						var entityRegion = entity.regions[r];
						if (entityRegion == region)
							continue;
						var entityRegionsPointsCount = entityRegion.points.Length;
						for (var j = 0; j < entityRegionsPointsCount; j++)
						{
							var v = entityRegion.points[j];
							var d = (v.x - p0.x) * (v.x - p0.x) + (v.y - p0.y) * (v.y - p0.y);
							if (d > 0 && d < p01d)
							{
								d = (v.x - p1.x) * (v.x - p1.x) + (v.y - p1.y) * (v.y - p1.y);
								if (d > 0 && d < p01d)
								{
									// make sure center of triangle is not contained by any region (that would mean the change would create overlap)
									midP.x = (p0.x + p1.x + v.x) / 3f;
									midP.y = (p0.y + p1.y + v.y) / 3f;
									if (!AnyRegionContainsPoint(midP))
									{
										// Insert point v in k+1 position
										var rpCount = region.points.Length;
										var newPoints = new Vector2[rpCount + 1];
										var np = -1;
										for (var i = 0; i < rpCount; i++)
										{
											++np;
											if (np == nextPointIndex)
											{
												newPoints[np] = v;
												np++;
											}
											newPoints[np] = region.points[i];
										}
										region.points = newPoints;
										snapped = true;
										regionPointsCount++;
										break;
									}
								}
							}
						}
					}
				}
			}
		}

		private bool AnyRegionContainsPoint(Vector2 p)
		{
			var entitiesCount = entities.Count;
			for (var k = 0; k < entitiesCount; k++)
			{
				var entity = entities[k];
				if (entity.regions == null)
					continue;
				var entityRegionsCount = entity.regions.Count;
				for (var r = 0; r < entityRegionsCount; r++)
				{
					var region = entity.regions[r];
					if (region.Contains(p))
						return true;
				}
			}
			return false;
		}

		#endregion

		#region Snap to Country Frontiers

		private int snapToCountryFrontiersIndex;

		private void FilterSnapToCountryFrontiers()
		{
			if (snapToCountryFrontiersIndex >= entities.Count)
			{
				progress = 1f;
				return;
			}
			progress = (float)snapToCountryFrontiersIndex / entities.Count;
			var entity = entities[snapToCountryFrontiersIndex];
			if (entity.regions != null)
			{
				var entityRegionsCount = entity.regions.Count;
				for (var k = 0; k < entityRegionsCount; k++)
				{
					var regionTosnap = entities[snapEntityIndex].regions[k];
					SnapToCountryFrontiers(regionTosnap);
				}
			}
			snapToCountryFrontiersIndex++;
		}

		// Snap positions to country frontiers?
		private void SnapToCountryFrontiers(Region region)
		{
			var map = WMSK.instance;
			var positionsCount = region.points.Length;
			for (var k = 0; k < positionsCount; k++)
			{
				var p = region.points[k];
				var minDist = float.MaxValue;
				var nearest = Misc.Vector2zero;
				var found = false;
				for (var c = 0; c < map.countries.Length; c++)
				{
					var country = map.countries[c];
					// if country's bounding box does not contain point, skip it
					if (!country.regionsRect2D.Contains(p))
						continue;
					var regionCount = country.regions.Count;
					for (var cr = 0; cr < regionCount; cr++)
					{
						var countryRegion = country.regions[cr];
						// if region's bounding box does not contain point, skip it
						if (!countryRegion.rect2D.Contains(p))
							continue;
						for (var q = 0; q < countryRegion.points.Length; q++)
						{
							var dist = FastVector.SqrDistance(ref countryRegion.points[q],
								ref p); // (countryRegion.points [q] - p).sqrMagnitude;
							if (dist < minDist)
							{
								minDist = dist;
								nearest = region.points[q];
								found = true;
							}
						}
					}
				}
				if (found)
					region.points[k] = nearest;
			}
			region.RemoveDuplicatePoints();
		}

		#endregion
	}
}
#endif