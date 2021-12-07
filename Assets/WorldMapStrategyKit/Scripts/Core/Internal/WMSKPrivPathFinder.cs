// World Strategy Kit for Unity - Main Script
// (C) 2016-2020 by Ramiro Oliva (Kronnect)
// Don't modify this script - changes could be lost if you upgrade to a more recent version of WMSK

using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using WorldMapStrategyKit.PathFinding;

namespace WorldMapStrategyKit
{
	public enum TERRAIN_CAPABILITY
	{
		Any = 1,
		OnlyGround = 2,
		OnlyWater = 4
	}

	public partial class WMSK : MonoBehaviour
	{
		private byte[] earthRouteMatrix;

		// bit 1 for custom elevation, bit 2 for ground without elevation restrictions, bit 3 for water without elevation restrictions
		private float[] _customRouteMatrix;

		// optional values for custom validation
		private float earthRouteMatrixWithElevationMinAltitude, earthRouteMatrixWithElevationMaxAltitude;
		private byte computedMatrixBits;

		private FastBitArray earthWaterMask;

		private const byte EARTH_WATER_MASK_OCEAN_LEVEL_MAX_ALPHA = 16;
		// A lower alpha value in texture means water

		private int earthWaterMaskWidth, earthWaterMaskHeight;

		private int EARTH_ROUTE_SPACE_WIDTH = 2048;

		// both must be power of 2
		private int EARTH_ROUTE_SPACE_HEIGHT = 1024;
		private PathFinderFast finder;
		private PathFinderAdminEntity finderCountries;
		private PathFinderAdminEntity finderProvinces;
		private PathFinderCells finderCells;
		private int lastMatrix;
		private Texture2D pathFindingCustomMatrixCostTexture;
		private bool cellsCostsComputed;
		private CellCosts[] _cellsCosts;

		private float[] customRouteMatrix
		{
			get
			{
				if (_customRouteMatrix == null || _customRouteMatrix.Length == 0)
					PathFindingCustomRouteMatrixReset();
				return _customRouteMatrix;
			}
		}

		private void PathFindingPrewarm()
		{
			CheckRouteWaterMask();
			ComputeRouteMatrix(TERRAIN_CAPABILITY.OnlyGround, 0, 1.0f);
			ComputeRouteMatrix(TERRAIN_CAPABILITY.OnlyWater, 0, 1.0f);
			if (_showGrid)
				ComputeCellsCostsInfo();
		}

		// Returns true if water mask buffer has been created; false if it was already created
		private bool CheckRouteWaterMask()
		{
			if (earthWaterMask != null)
				return false;

			// Get water mask info
			Texture2D waterMap = null;
			if (waterMask == null)
			{
				if (_earthStyle.isScenicPlus())
					waterMap = (Texture2D)earthMat.GetTexture("_TerrestrialMap");
			}
			else
				waterMap = waterMask;
			if (waterMap == null)
			{
				Debug.LogError("Water mask texture could not be loaded");
				return false;
			}
			earthWaterMaskHeight = waterMap.height;
			earthWaterMaskWidth = waterMap.width;
			var colors = waterMap.GetPixels32();
			earthWaterMask = new FastBitArray(colors, _waterMaskLevel);

			// Remind to compute cell costs again
			cellsCostsComputed = false;

			return true;
		}

		private void ComputeRouteMatrix(TERRAIN_CAPABILITY terrainCapability, float minAltitude,
			float maxAltitude)
		{
			var computeMatrix = false;
			byte thisMatrix = 1;

			// prepare matrix
			if (earthRouteMatrix == null)
			{
				earthRouteMatrix = new byte[EARTH_ROUTE_SPACE_WIDTH * EARTH_ROUTE_SPACE_HEIGHT];
				computedMatrixBits = 0;
			}

			// prepare water mask data
			var checkWater = terrainCapability != TERRAIN_CAPABILITY.Any;
			if (checkWater)
				computeMatrix = CheckRouteWaterMask();

			// check elevation data if needed
			var checkElevation = minAltitude > 0f || maxAltitude < 1.0f;
			if (checkElevation)
			{
				if (viewportElevationPoints == null)
				{
					Debug.LogError(
						"Viewport needs to be initialized before calling using Path Finding functions.");
					return;
				}
				if (minAltitude != earthRouteMatrixWithElevationMinAltitude ||
				    maxAltitude != earthRouteMatrixWithElevationMaxAltitude)
				{
					computeMatrix = true;
					earthRouteMatrixWithElevationMinAltitude = minAltitude;
					earthRouteMatrixWithElevationMaxAltitude = maxAltitude;
				}
			}
			else
			{
				if (terrainCapability == TERRAIN_CAPABILITY.OnlyGround)
					thisMatrix = 2;
				else
					thisMatrix = 4; // water
				if ((computedMatrixBits & thisMatrix) == 0)
				{
					computeMatrix = true;
					computedMatrixBits |= thisMatrix; // mark computedMatrixBits
				}
			}

			// Compute route
			if (computeMatrix)
			{
				int jj_waterMask = 0, kk_waterMask;
				int jj_terrainElevation = 0, kk_terrainElevation;
				var dry = false;
				float elev = 0;
				for (var j = 0; j < EARTH_ROUTE_SPACE_HEIGHT; j++)
				{
					var jj = j * EARTH_ROUTE_SPACE_WIDTH;
					if (checkWater)
						jj_waterMask = (int)(j * (float)earthWaterMaskHeight / EARTH_ROUTE_SPACE_HEIGHT) *
						               earthWaterMaskWidth;
					if (checkElevation)
						jj_terrainElevation =
							(int)(j * (float)heightmapTextureHeight / EARTH_ROUTE_SPACE_HEIGHT) *
							heightmapTextureWidth;
					for (var k = 0; k < EARTH_ROUTE_SPACE_WIDTH; k++)
					{
						var setBit = false;
						// Check altitude
						if (checkElevation)
						{
							kk_terrainElevation =
								(int)(k * (float)heightmapTextureWidth / EARTH_ROUTE_SPACE_WIDTH);
							elev = viewportElevationPoints[jj_terrainElevation + kk_terrainElevation];
						}
						if (elev >= minAltitude && elev <= maxAltitude)
						{
							if (checkWater)
							{
								kk_waterMask =
									(int)(k * (float)earthWaterMaskWidth / EARTH_ROUTE_SPACE_WIDTH);
								dry = !earthWaterMask.GetBit(jj_waterMask + kk_waterMask);
							}
							if (terrainCapability == TERRAIN_CAPABILITY.Any ||
							    terrainCapability == TERRAIN_CAPABILITY.OnlyGround && dry ||
							    terrainCapability == TERRAIN_CAPABILITY.OnlyWater && !dry)
								setBit = true;
						}
						if (setBit) // set navigation bit
							earthRouteMatrix[jj + k] |= thisMatrix;
						else // clear navigation bit
							earthRouteMatrix[jj + k] &= (byte)(byte.MaxValue ^ thisMatrix);
					}
				}
			}

			if (finder == null)
			{
				if (_customRouteMatrix == null || !_pathFindingEnableCustomRouteMatrix)
					PathFindingCustomRouteMatrixReset();
				finder = new PathFinderFast(earthRouteMatrix, thisMatrix, EARTH_ROUTE_SPACE_WIDTH,
					EARTH_ROUTE_SPACE_HEIGHT, _customRouteMatrix);
			}
			else
			{
				if (computeMatrix || thisMatrix != lastMatrix)
				{
					lastMatrix = thisMatrix;
					finder.SetCalcMatrix(earthRouteMatrix, thisMatrix);
				}
			}
		}

		private void ComputeCellsCostsInfo()
		{
			if (cellsCostsComputed || _cellsCosts == null)
				return;

			CheckRouteWaterMask();

			var cellsCount = cells.Length;
			var usesViewport = renderViewportIsEnabled && viewportElevationPoints != null;
			for (var k = 0; k < cellsCount; k++)
			{
				if (cells[k] == null)
					continue;
				var x = cells[k].center.x + 0.5f;
				var y = cells[k].center.y + 0.5f;
				var px = (int)(x * earthWaterMaskWidth);
				var py = (int)(y * earthWaterMaskHeight);
				var water = earthWaterMask.GetBit(py * earthWaterMaskWidth + px);
				_cellsCosts[k].isWater = water;

				if (usesViewport)
				{
					px = (int)(x * heightmapTextureWidth);
					py = (int)(y * heightmapTextureHeight);
					var elev = viewportElevationPoints[py * heightmapTextureWidth + px];
					_cellsCosts[k].altitude = elev;
				}
			}

			cellsCostsComputed = true;

			if (finderCells == null)
				finderCells = new PathFinderCells(_cellsCosts, _gridColumns, _gridRows);
			else
				finderCells.SetCustomCellsCosts(_cellsCosts);
		}

		/// <summary>
		/// Used by FindRoute method to satisfy custom positions check
		/// </summary>
		private float FindRoutePositionValidator(int location)
		{
			if (_customRouteMatrix == null)
				PathFindingCustomRouteMatrixReset();
			float cost = 1;
			if (OnPathFindingCrossPosition != null)
			{
				var y = location / EARTH_ROUTE_SPACE_WIDTH;
				var x = location - y * EARTH_ROUTE_SPACE_WIDTH;
				var position = MatrixCostPositionToMap2D(x, y);
				cost = OnPathFindingCrossPosition(position);
			}
			_customRouteMatrix[location] = cost;
			return cost;
		}

		/// <summary>
		/// Used by FindRoute method in country-country mode
		/// </summary>
		/// <returns>The extra cross cost.</returns>
		private float FindRouteCountryValidator(int countryIndex)
		{
			if (OnPathFindingCrossCountry != null)
				return OnPathFindingCrossCountry(countryIndex);
			return 0;
		}

		/// <summary>
		/// Used by FindRoute method in province-province mode
		/// </summary>
		/// <returns>The extra cross cost.</returns>
		private float FindRouteProvinceValidator(int provinceIndex)
		{
			if (OnPathFindingCrossProvince != null)
				return OnPathFindingCrossProvince(provinceIndex);
			return 0;
		}

		/// <summary>
		/// Used by FindRoute method in cell mode
		/// </summary>
		/// <returns>The extra cross cost.</returns>
		private float FindRouteCellValidator(int cellIndex)
		{
			if (OnPathFindingCrossCell != null)
				return OnPathFindingCrossCell(cellIndex);
			return 0;
		}

		private Point Map2DToMatrixCostPosition(Vector2 position)
		{
			var x = (int)((position.x + 0.5f) * EARTH_ROUTE_SPACE_WIDTH);
			var y = (int)((position.y + 0.5f) * EARTH_ROUTE_SPACE_HEIGHT);
			return new Point(x, y);
		}

		private Vector2 MatrixCostPositionToMap2D(Point position) =>
			MatrixCostPositionToMap2D(position.X, position.Y);

		private Vector2 MatrixCostPositionToMap2D(int k, int j)
		{
			var x = (k + 0.5f) / EARTH_ROUTE_SPACE_WIDTH - 0.5f;
			var y = (j + 0.5f) / EARTH_ROUTE_SPACE_HEIGHT - 0.5f;
			return new Vector2(x, y);
		}

		private int PointToRouteMatrixIndex(Point p) => p.Y * EARTH_ROUTE_SPACE_WIDTH + p.X;

		private void UpdatePathfindingMatrixCostTexture()
		{
			if (pathFindingCustomMatrixCostTexture == null)
			{
				pathFindingCustomMatrixCostTexture =
					new Texture2D(EARTH_ROUTE_SPACE_WIDTH, EARTH_ROUTE_SPACE_HEIGHT);
				if (disposalManager != null)
					disposalManager.MarkForDisposal(
						pathFindingCustomMatrixCostTexture); // pathFindingCustomMatrixCostTexture.hideFlags = HideFlags.DontSave;
				pathFindingCustomMatrixCostTexture.filterMode = FilterMode.Point;
			}
			var len = customRouteMatrix.Length;
			float maxValue = 0;
			for (var k = 0; k < len; k++)
				if (_customRouteMatrix[k] > maxValue)
					maxValue = _customRouteMatrix[k];
			if (maxValue <= 0)
				maxValue = 1;
			var colors = new Color[_customRouteMatrix.Length];
			var white = Color.white;
			var black = Color.black;
			var c = Color.white;
			for (var k = 0; k < colors.Length; k++)
				if (_customRouteMatrix[k] < 0)
					colors[k] = white;
				else if (_customRouteMatrix[k] == 0)
					colors[k] = black;
				else
				{
					var t = _customRouteMatrix[k] / maxValue;
					if (t > 1f)
						t = 1f;
					c.g = c.b = t;
					colors[k] = c;
				}
			pathFindingCustomMatrixCostTexture.SetPixels(colors);
			pathFindingCustomMatrixCostTexture.Apply();
		}
	}
}