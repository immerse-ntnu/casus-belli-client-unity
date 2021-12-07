//#define SHOW_DEBUG_GIZMOS

using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using WorldMapStrategyKit.MapGenerator.Geom;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WorldMapStrategyKit
{
	public partial class WMSK_Editor : MonoBehaviour
	{
		private float[] heights;
		private Color[] heightColors, backgroundColors;
		private int currentHeightmapWidth, currentHeightmapHeight;

		public void GenerateHeightMap(bool preview = false)
		{
			try
			{
				if (userHeightMapTexture != null)
				{
					var hh = userHeightMapTexture.GetPixels();
					heights = new float[hh.Length];
					for (var k = 0; k < hh.Length; k++)
						heights[k] = hh[k].r;
					return;
				}

				var lowQuality = mapGenerationQuality == MapGenerationQuality.Draft || preview;
				currentHeightmapWidth = lowQuality ? 256 : heightMapWidth;
				currentHeightmapHeight = lowQuality ? 128 : heightMapHeight;

				var heightsSize = currentHeightmapWidth * currentHeightmapHeight;
				if (heights == null || heights.Length != heightsSize)
					heights = new float[heightsSize];
				else
					for (var k = 0; k < heightsSize; k++)
						heights[k] = 0;

				if (noiseOctaves == null)
					return;

				// Fetch some noise
				var ratio = (float)currentHeightmapWidth / currentHeightmapHeight;
				for (var n = 0; n < noiseOctaves.Length; n++)
				{
					if (noiseOctaves[n].disabled)
						continue;
					var c = 0;
					var frecuency = noiseOctaves[n].frecuency;
					var amplitude = noiseOctaves[n].amplitude;
					var xmult = frecuency * ratio / currentHeightmapWidth;
					var ymult = frecuency / currentHeightmapHeight;
					if (noiseOctaves[n].ridgeNoise)
						for (var y = 0; y < currentHeightmapHeight; y++)
						{
							var yy = y * ymult;
							for (var x = 0; x < currentHeightmapWidth; x++, c++)
							{
								var noise = Mathf.PerlinNoise(x * xmult, yy);
								noise = 2f * (0.5f - Mathf.Abs(0.5f - noise));
								noise *= amplitude;
								if (n > 0)
									noise *= heights[c];
								heights[c] += noise;
							}
						}
					else
						for (var y = 0; y < currentHeightmapHeight; y++)
						{
							var yy = y * ymult;
							for (var x = 0; x < currentHeightmapWidth; x++, c++)
							{
								var noise = Mathf.PerlinNoise(x * xmult, yy) * amplitude;
								heights[c] += noise;
							}
						}
				}

				// Apply exponent & island factor
				var maxHeight = float.MinValue;
				for (var k = 0; k < heights.Length; k++)
				{
					var h = heights[k];

					// island factor
					float x = k % currentHeightmapWidth;
					float y = k / currentHeightmapWidth;
					var nx = 2f * x / currentHeightmapWidth - 1f;
					var ny = 2f * y / currentHeightmapHeight - 1f;
					var dx = nx * nx;
					var dy = ny * ny;
					var d = dx + dy;

					var mask = 1f - d * islandFactor;
					if (mask < 0)
						mask = 0;
					h *= mask;

					// pow
					h = Mathf.Pow(h, noisePower);

					h = h + elevationShift;

					if (h > maxHeight)
						maxHeight = h;

					heights[k] = h;
				}

				// Normalize values
				if (maxHeight > 0)
					for (var k = 0; k < heights.Length; k++)
						if (heights[k] < 0)
							heights[k] = 0;
						else
							heights[k] /= maxHeight;

				if (heightMapTexture == null ||
				    heightMapTexture.width != currentHeightmapWidth ||
				    heightMapTexture.height != currentHeightmapHeight)
					heightMapTexture = new Texture2D(currentHeightmapWidth, currentHeightmapHeight,
						TextureFormat.ARGB32, false);
				if (heightColors == null || heightColors.Length != heights.Length)
					heightColors = new Color[heights.Length];

				if (preview)
				{
					for (var k = 0; k < heights.Length; k++)
						heightColors[k].r = heightColors[k].g =
							heightColors[k].b = heightColors[k].a = heights[k];
					heightMapTexture.SetPixels(heightColors);
					heightMapTexture.Apply();
					return;
				}

				// Create water mask
				if (waterMaskTexture == null ||
				    waterMaskTexture.width != currentHeightmapWidth ||
				    heightMapTexture.height != currentHeightmapHeight)
					waterMaskTexture = new Texture2D(currentHeightmapWidth, currentHeightmapHeight,
						TextureFormat.ARGB32, false);
				var wc = new Color();
				for (var k = 0; k < heights.Length; k++)
				{
					wc.a = heights[k];
					heightColors[k] = wc;
				}
				waterMaskTexture.SetPixels(heightColors);
				waterMaskTexture.Apply();
			}
			catch (Exception ex)
			{
				Debug.LogError("Error generating heightmap: " + ex.ToString());
			}
		}

		private void AssignHeightMapToProvinces(bool colorGroundCells)
		{
			if (heightMapTexture == null)
				return;

			if (currentHeightmapWidth == 0)
				return;
			if (heightGradient == null)
				heightGradientPreset = HeightMapGradientPreset.Colored;

			GradientColorKey[] colorKeys;
			switch (heightGradientPreset)
			{
				case HeightMapGradientPreset.Colored:
					heightGradient = new Gradient();
					colorKeys = new GradientColorKey[4];
					colorKeys[0] = new GradientColorKey(Color.gray, 0f);
					colorKeys[1] = new GradientColorKey(new Color(0.133f, 0.545f, 0.133f), seaLevel);
					colorKeys[2] = new GradientColorKey(new Color(0.898f, 0.898f, 0.298f),
						(seaLevel + 1f) * 0.5f);
					colorKeys[3] = new GradientColorKey(Color.white, 1f);
					heightGradient.colorKeys = colorKeys;
					break;
				case HeightMapGradientPreset.ColoredLight:
					heightGradient = new Gradient();
					colorKeys = new GradientColorKey[4];
					colorKeys[0] = new GradientColorKey(Color.gray, 0f);
					colorKeys[1] = new GradientColorKey(new Color(0.333f, 0.745f, 0.333f), seaLevel);
					colorKeys[2] = new GradientColorKey(new Color(0.998f, 0.998f, 0.498f),
						(seaLevel + 1f) * 0.5f);
					colorKeys[3] = new GradientColorKey(Color.white, 1f);
					heightGradient.colorKeys = colorKeys;
					break;
				case HeightMapGradientPreset.Grayscale:
					heightGradient = new Gradient();
					colorKeys = new GradientColorKey[3];
					colorKeys[0] = new GradientColorKey(Color.black, 0f);
					colorKeys[1] = new GradientColorKey(Color.gray, seaLevel);
					colorKeys[2] = new GradientColorKey(Color.white, 1f);
					heightGradient.colorKeys = colorKeys;
					break;
				case HeightMapGradientPreset.BlackAndWhite:
					heightGradient = new Gradient();
					colorKeys = new GradientColorKey[3];
					colorKeys[0] = new GradientColorKey(Color.black, 0f);
					colorKeys[1] = new GradientColorKey(Color.white, seaLevel);
					colorKeys[2] = new GradientColorKey(Color.white, 1f);
					heightGradient.colorKeys = colorKeys;
					break;
			}

			var provCount = mapProvinces.Count;
			for (var k = 0; k < provCount; k++)
			{
				var prov = mapProvinces[k];
				var x = (int)Mathf.Clamp((prov.center.x + 0.5f) * currentHeightmapWidth, 0,
					currentHeightmapWidth - 1);
				var y = (int)Mathf.Clamp((prov.center.y + 0.5f) * currentHeightmapHeight, 0,
					currentHeightmapHeight - 1);
				var j = y * currentHeightmapWidth + x;
				var h = heights[j];
				if (h < seaLevel)
					prov.visible = false;
				else
				{
					prov.visible = true;
					prov.color = colorGroundCells ? heightGradient.Evaluate(h) : Color.white;
				}
			}
		}

		private void GenerateWorldTexture()
		{
			// Create background texture
			var backgroundTextureWidth = mapGenerationQuality == MapGenerationQuality.Draft
				? 256
				: this.backgroundTextureWidth;
			var backgroundTextureHeight = mapGenerationQuality == MapGenerationQuality.Draft
				? 128
				: this.backgroundTextureHeight;

			if (backgroundTexture == null ||
			    backgroundTexture.width != backgroundTextureWidth ||
			    backgroundTexture.height != backgroundTextureHeight)
				backgroundTexture = new Texture2D(backgroundTextureWidth, backgroundTextureHeight,
					TextureFormat.RGBA32, true);
			var bufferLen = backgroundTextureWidth * backgroundTextureHeight;
			if (backgroundColors == null || backgroundColors.Length != bufferLen)
				backgroundColors = new Color[bufferLen];
			var backColor = seaColor;
			backColor.a = 0;
			backgroundColors.Fill<Color>(backColor);

			var provincesCount = _map.provinces.Length;
			for (var k = 0; k < provincesCount; k++)
			{
				var prov = _map.provinces[k];
				if (prov.regions == null)
				{
					_map.ReadProvincePackedString(prov);
					if (prov.regions == null)
						continue;
				}
				var region = prov.regions[0];
				if (gradientPerPixel)
					_map.RegionPaintHeights(backgroundColors, backgroundTextureWidth,
						backgroundTextureHeight, region, heights, seaLevel, currentHeightmapWidth,
						currentHeightmapHeight, heightGradient);
				else
				{
					Color provColor = prov.attrib["mapColor"];
					provColor.a = 1;
					_map.RegionPaint(backgroundColors, backgroundTextureWidth, backgroundTextureHeight,
						region, provColor);
				}
			}

			backgroundTexture.SetPixels(backgroundColors);
			backgroundTexture.Apply();

			// Set height to 0 out of land areas
			if (heightMapTexture != null)
			{
				if (currentHeightmapWidth == backgroundTextureWidth &&
				    currentHeightmapHeight == backgroundTextureHeight)
					for (var k = 0; k < backgroundColors.Length; k++)
						if (backgroundColors[k].a == 0)
						{
							heights[k] = 0;
							heightColors[k] = Misc.ColorClear;
						}
						else
							heightColors[k].r = heightColors[k].g =
								heightColors[k].b = heightColors[k].a = heights[k];
				else
					for (int k = 0, y = 0; y < currentHeightmapHeight; y++)
					{
						var backy = y * backgroundTextureHeight / currentHeightmapHeight;
						var backyy = backy * backgroundTextureWidth;
						for (var x = 0; x < currentHeightmapWidth; x++, k++)
						{
							var backx = x * backgroundTextureWidth / currentHeightmapWidth;
							if (backgroundColors[backyy + backx].a == 0)
							{
								heights[k] = 0;
								heightColors[k] = Misc.ColorClear;
							}
							else
								heightColors[k].r = heightColors[k].g =
									heightColors[k].b = heightColors[k].a = heights[k];
						}
					}
				heightMapTexture.SetPixels(heightColors);
				heightMapTexture.Apply();
			}
		}
	}
}