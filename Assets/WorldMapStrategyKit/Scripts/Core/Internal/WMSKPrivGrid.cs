// World Map Strategy Kit for Unity - Main Script
// (C) 2016-2020 by Ramiro Oliva (Kronnect)
// Don't modify this script - changes could be lost if you upgrade to a more recent version of WMSK

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace WorldMapStrategyKit
{
	public partial class WMSK : MonoBehaviour
	{
		// Materials and resources
		private Material gridMat, hudMatCell;

		// Cell mesh data
		private const string SKW_WATER_MASK = "USE_MASK";
		private const string CELLS_LAYER_NAME = "Grid";

		private const float GRID_ENCLOSING_THRESHOLD = 0.3f;

		// size of the enclosing viewport rect
		private Vector3[][] cellMeshBorders;
		private Vector2[][] cellUVs;
		private int[][] cellMeshIndices;
		private bool recreateCells;

		// Common territory & cell structures
		private Vector2[] gridPoints;
		private int[] hexIndices = { 0, 1, 5, 1, 2, 5, 5, 2, 4, 2, 3, 4 };

		private int[] hexIndicesWrapped = {
			0,
			1,
			5,
			1,
			2,
			5,
			5,
			2,
			4,
			6,
			7,
			8
		};

		private Vector3[] hexPoints = new Vector3[9];

		// Placeholders and layers
		private GameObject _gridSurfacesLayer;

		private GameObject gridSurfacesLayer
		{
			get
			{
				if (_gridSurfacesLayer == null)
					CreateGridSurfacesLayer();
				return _gridSurfacesLayer;
			}
		}

		private GameObject cellLayer;
		private Rect gridRect;

		// Caches
		private Dictionary<Cell, int> _cellLookup;
		private int lastCellLookupCount = -1;
		private bool refreshMesh;
		private CellSegment[] sides;

		// Cell highlighting
		private Renderer cellHighlightedObjRenderer;
		private Cell _cellHighlighted;
		private int _cellHighlightedIndex = -1;
		private float highlightFadeStart;
		private int _cellLastClickedIndex = -1;
		private int _cellLastPointerOn = -1;

		private Dictionary<Cell, int> cellLookup
		{
			get
			{
				if (_cellLookup != null && cells.Length == lastCellLookupCount)
					return _cellLookup;
				if (_cellLookup == null)
					_cellLookup = new Dictionary<Cell, int>();
				else
					_cellLookup.Clear();
				for (var k = 0; k < cells.Length; k++)
					if (cells[k] != null)
						_cellLookup[cells[k]] = k;
				lastCellLookupCount = cells.Length;
				return _cellLookup;
			}
		}

		#region Initialization

		private void CreateGridSurfacesLayer()
		{
			var t = transform.Find("GridSurfaces");
			if (t != null)
				DestroyImmediate(t.gameObject);
			_gridSurfacesLayer = new GameObject("GridSurfaces");
			_gridSurfacesLayer.transform.SetParent(transform, false);
			_gridSurfacesLayer.transform.localPosition = Misc.Vector3back * 0.001f;
			_gridSurfacesLayer.layer = gameObject.layer;
		}

		private void DestroyGridSurfaces()
		{
			HideCellHighlight();
			if (cells != null)
				for (var k = 0; k < cells.Length; k++)
					if (cells[k] != null && cells[k].renderer != null)
						DestroyImmediate(cells[k].renderer.gameObject);
			if (_gridSurfacesLayer != null)
				DestroyImmediate(_gridSurfacesLayer);
		}

		#endregion

		#region Map generation

		private void CreateCells()
		{
			var newLength = _gridRows * _gridColumns;
			if (cells == null || cells.Length != newLength)
				cells = new Cell[newLength];
			if (_cellsCosts == null || _cellsCosts.Length != cells.Length)
				_cellsCosts = new CellCosts[newLength];
			lastCellLookupCount = -1;
			_cellLastPointerOn = -1;
			var qxOffset = _wrapHorizontally ? 0 : 0.25f;

			var qx = _gridColumns * 3f / 4f + qxOffset;

			var stepX = 1f / qx;
			var stepY = 1f / _gridRows;

			var halfStepX = stepX * 0.5f;
			var centerOffset = halfStepX;
			var halfStepY = stepY * 0.5f;
			var halfStepX2 = stepX * 0.25f;

			var sidesCount = _gridRows * _gridColumns * 6;
			if (sides == null || sides.Length < sidesCount)
				sides = new CellSegment[_gridRows *
				                        _gridColumns *
				                        6]; // 0 = left-up, 1 = top, 2 = right-up, 3 = right-down, 4 = down, 5 = left-down
			Vector2 center, start, end;

			var sideIndex = 0;
			var cellIndex = 0;
			for (var j = 0; j < _gridRows; j++)
			{
				center.y = (float)j / _gridRows - 0.5f + halfStepY;
				for (var k = 0; k < _gridColumns; k++, cellIndex++)
				{
					center.x = k / qx - 0.5f + centerOffset;
					center.x -= k * halfStepX2;
					var cell = new Cell(j, k, center);

					var offsetY = k % 2 == 0 ? 0 : -halfStepY;

					start.x = center.x - halfStepX;
					start.y = center.y + offsetY;
					end.x = center.x - halfStepX2;
					end.y = center.y + halfStepY + offsetY;
					CellSegment leftUp;
					if (k > 0 && offsetY < 0) // leftUp is right-down edge of (k-1, j) but swapped
						leftUp = sides[sideIndex - 3]
							.swapped; // was sides[k - 1, j, 3].swapped;; sideIndex - 3 at this point equals to (k-1, j, 3)
					else
						leftUp = new CellSegment(start, end);
					sides[sideIndex++] = leftUp; // 0

					start.x = center.x - halfStepX2;
					start.y = center.y + halfStepY + offsetY;
					end.x = center.x + halfStepX2;
					end.y = center.y + halfStepY + offsetY;
					var top = new CellSegment(start, end);
					sides[sideIndex++] = top; // 1

					start.x = center.x + halfStepX2;
					start.y = center.y + halfStepY + offsetY;
					end.x = center.x + halfStepX;
					end.y = center.y + offsetY;
					var rightUp = new CellSegment(start, end);
					if (_wrapHorizontally && k == _gridColumns - 1)
						rightUp.isRepeated = true;
					sides[sideIndex++] = rightUp; // 2

					CellSegment rightDown;
					if (j > 0 &&
					    k < _gridColumns - 1 &&
					    offsetY < 0) // rightDown is left-up edge of (k+1, j-1) but swapped
						rightDown =
							sides[sideIndex - _gridColumns * 6 + 3]
								.swapped; // was sides[k + 1, j - 1, 0].swapped
					else
					{
						start.x = center.x + halfStepX;
						start.y = center.y + offsetY;
						end.x = center.x + halfStepX2;
						end.y = center.y - halfStepY + offsetY;
						rightDown = new CellSegment(start, end);
						if (_wrapHorizontally && k == _gridColumns - 1)
							rightDown.isRepeated = true;
					}
					sides[sideIndex++] = rightDown; // 3

					CellSegment bottom;
					if (j > 0) // bottom is top edge from (k, j-1) but swapped
						bottom = sides[sideIndex - _gridColumns * 6 - 3]
							.swapped; // was sides[k, j - 1, 1].swapped
					else
					{
						start.x = center.x + halfStepX2;
						start.y = center.y - halfStepY + offsetY;
						end.x = center.x - halfStepX2;
						end.y = center.y - halfStepY + offsetY;
						bottom = new CellSegment(start, end);
					}
					sides[sideIndex++] = bottom; // 4

					CellSegment leftDown;
					if (offsetY < 0 && j > 0) // leftDown is right up from (k-1, j-1) but swapped
						leftDown = sides[sideIndex - _gridColumns * 6 - 9]
							.swapped; // was  sides [k - 1, j - 1, 2].swapped
					else if (offsetY == 0 && k > 0) // leftDOwn is right up from (k-1, j) but swapped
						leftDown = sides[sideIndex - 9].swapped; // was sides [k - 1, j, 2].swapped
					else
					{
						start.x = center.x - halfStepX2;
						start.y = center.y - halfStepY + offsetY;
						end.x = center.x - halfStepX;
						end.y = center.y + offsetY;
						leftDown = new CellSegment(start, end);
					}
					sides[sideIndex++] = leftDown; // 5

					if (j > 0 || offsetY == 0)
					{
						cell.center.y += offsetY;

						if (j == 1)
							bottom.isRepeated = false;
						else if (j == 0)
							leftDown.isRepeated = false;

						cell.segments[0] = leftUp;
						cell.segments[1] = top;
						cell.segments[2] = rightUp;
						cell.segments[3] = rightDown;
						cell.segments[4] = bottom;
						cell.segments[5] = leftDown;
						if (_wrapHorizontally && k == _gridColumns - 1)
							cell.isWrapped = true;
						cell.rect2D = new Rect(leftUp.start.x, bottom.start.y,
							rightUp.end.x - leftUp.start.x, top.start.y - bottom.start.y);
						cells[cellIndex] = cell;
					}
				}
			}
		}

		private void GenerateCellsMesh()
		{
			if (gridPoints == null || gridPoints.Length == 0)
				gridPoints = new Vector2[200000];

			var gridPointsCount = 0;
			var y0 = (int)((gridRect.yMin + 0.5f) * _gridRows);
			var y1 = (int)((gridRect.yMax + 0.5f) * _gridRows);
			y0 = Mathf.Clamp(y0, 0, _gridRows - 1);
			y1 = Mathf.Clamp(y1, 0, _gridRows - 1);
			for (var y = y0; y <= y1; y++)
			{
				var yy = y * _gridColumns;
				var x0 = (int)((gridRect.xMin + 0.5f) * _gridColumns);
				var x1 = (int)((gridRect.xMax + 0.5f) * _gridColumns);
				for (var x = x0; x <= x1; x++)
				{
					var wrapX = x;
					if (_wrapHorizontally)
					{
						if (x < 0)
							wrapX += _gridColumns;
						else if (x >= gridColumns)
							wrapX -= gridColumns;
					}
					if (wrapX < 0 || wrapX >= _gridColumns)
						continue;
					var cell = cells[yy + wrapX];
					if (cell != null)
					{
						if (gridPoints.Length <= gridPointsCount + 12)
						{
							// Resize and copy elements; similar to C# standard list but we avoid excesive calls when accessing elements
							var newSize = gridPoints.Length * 2;
							var tmp = new Vector2[newSize];
							Array.Copy(gridPoints, tmp, gridPointsCount);
							gridPoints = tmp;
						}
						for (var i = 0; i < 6; i++)
						{
							var s = cell.segments[i];
							if (!s.isRepeated)
							{
								gridPoints[gridPointsCount++] = s.start;
								gridPoints[gridPointsCount++] = s.end;
							}
						}
					}
				}
			}

			var meshGroups = gridPointsCount / 65000 + 1;
			var meshIndex = -1;
			if (cellMeshIndices == null || cellMeshIndices.GetUpperBound(0) != meshGroups - 1)
			{
				cellMeshIndices = new int[meshGroups][];
				cellMeshBorders = new Vector3[meshGroups][];
				cellUVs = new Vector2[meshGroups][];
			}
			if (gridPointsCount == 0)
			{
				cellMeshBorders[0] = new Vector3[0];
				cellMeshIndices[0] = new int[0];
				cellUVs[0] = new Vector2[0];
			}
			else
				for (var k = 0; k < gridPointsCount; k += 65000)
				{
					var max = Mathf.Min(gridPointsCount - k, 65000);
					++meshIndex;
					if (cellMeshBorders[meshIndex] == null ||
					    cellMeshBorders[0].GetUpperBound(0) != max - 1)
					{
						cellMeshBorders[meshIndex] = new Vector3[max];
						cellMeshIndices[meshIndex] = new int[max];
						cellUVs[meshIndex] = new Vector2[max];
					}
					for (var j = 0; j < max; j++)
					{
						cellMeshBorders[meshIndex][j].x = gridPoints[j + k].x;
						cellMeshBorders[meshIndex][j].y = gridPoints[j + k].y;
						cellMeshIndices[meshIndex][j] = j;
						cellUVs[meshIndex][j].x = gridPoints[j + k].x + 0.5f;
						cellUVs[meshIndex][j].y = gridPoints[j + k].y + 0.5f;
					}
				}
			refreshMesh = false; // mesh creation finished at this point
		}

		#endregion

		#region Drawing stuff

		public void GenerateGrid()
		{
			recreateCells = true;
			if (_wrapHorizontally && _gridColumns % 2 != 0)
				_gridColumns++; // in wrapped mode, only even columns are allowed
			DrawGrid();
		}

		/// <summary>
		/// Determines if grid needs to be generated again, based on current viewport position
		/// </summary>
		public void CheckGridRect()
		{
			ComputeViewportRect();

			// Check rect size thresholds
			var validGrid = true;
			var dx = renderViewportRect.width;
			var dy = renderViewportRect.height;
			if (dx > gridRect.width || dy > gridRect.height)
				validGrid = false;
			else if (dx < gridRect.width * GRID_ENCLOSING_THRESHOLD ||
			         dy < gridRect.height * GRID_ENCLOSING_THRESHOLD)
				validGrid = false;
			else
			{
				// if current viewport rect is inside grid rect and viewport size is between 0.8 and 1 from grid size then we're ok and exit.
				var p0 = new Vector2(_renderViewportRect.xMin, _renderViewportRect.yMax);
				var p1 = new Vector2(_renderViewportRect.xMax, _renderViewportRect.yMin);
				if (!gridRect.Contains(p0) || !gridRect.Contains(p1))
					validGrid = false;
			}
			if (validGrid)
			{
				AdjustsGridAlpha();
				return;
			}

			refreshMesh = true;
			CheckCells();
			DrawCellBorders();
		}

		public void DrawGrid()
		{
			if (!gameObject.activeInHierarchy)
				return;

			// Initialize surface cache
			DestroyGridSurfaces();
			if (!_showGrid)
				return;

			refreshMesh = true;
			gridRect = new Rect(-1000, -1000, 1, 1);

			CheckCells();
			if (_showGrid)
			{
				DrawCellBorders();
				DrawColorizedCells();
			}
			recreateCells = false;
		}

		private void CheckCells()
		{
			if (!_showGrid && !_enableCellHighlight)
				return;
			if (cells == null || recreateCells)
			{
				CreateCells();
				refreshMesh = true;
			}
			if (refreshMesh)
			{
				var f = GRID_ENCLOSING_THRESHOLD + (1f - GRID_ENCLOSING_THRESHOLD) * 0.5f;
				var gridWidth = renderViewportRect.width / f;
				var gridHeight = renderViewportRect.height / f;
				gridRect = new Rect(_renderViewportRect.center.x - gridWidth * 0.5f,
					_renderViewportRect.center.y - gridHeight * 0.5f, gridWidth, gridHeight);
				GenerateCellsMesh();
			}
		}

		private void DrawCellBorders()
		{
			if (cellLayer != null)
				DestroyImmediate(cellLayer);
			else
			{
				var t = transform.Find(CELLS_LAYER_NAME);
				if (t != null)
					DestroyImmediate(t.gameObject);
			}
			if (cells.Length == 0)
				return;

			cellLayer = new GameObject(CELLS_LAYER_NAME);
			if (disposalManager != null)
				disposalManager.MarkForDisposal(cellLayer);
			cellLayer.transform.SetParent(transform, false);
			cellLayer.transform.localPosition = Vector3.back * 0.001f;
			var layer = transform.gameObject.layer;
			cellLayer.layer = layer;

			for (var k = 0; k < cellMeshBorders.Length; k++)
			{
				var flayer = new GameObject("flayer");
				if (disposalManager != null)
					disposalManager.MarkForDisposal(flayer);
				flayer.layer = layer;
				flayer.transform.SetParent(cellLayer.transform, false);
				flayer.transform.localPosition = Vector3.zero;
				flayer.transform.localRotation = Quaternion.Euler(Vector3.zero);

				var mesh = new Mesh();
				mesh.vertices = cellMeshBorders[k];
				mesh.SetIndices(cellMeshIndices[k], MeshTopology.Lines, 0);
				mesh.uv = cellUVs[k];

				mesh.RecalculateBounds();
				if (disposalManager != null)
					disposalManager.MarkForDisposal(mesh);

				var mf = flayer.AddComponent<MeshFilter>();
				mf.sharedMesh = mesh;

				var mr = flayer.AddComponent<MeshRenderer>();
				mr.receiveShadows = false;
				mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
				mr.shadowCastingMode = ShadowCastingMode.Off;
				mr.sharedMaterial = gridMat;
			}
			AdjustsGridAlpha();
		}

		// Adjusts alpha according to minimum and maximum distance
		private void AdjustsGridAlpha()
		{
			float gridAlpha;
			if (!_showGrid)
				return;
			if (lastDistanceFromCamera < _gridMinDistance)
				gridAlpha = 1f - (_gridMinDistance - lastDistanceFromCamera) / (_gridMinDistance * 0.2f);
			else if (lastDistanceFromCamera > _gridMaxDistance)
				gridAlpha = 1f - (lastDistanceFromCamera - _gridMaxDistance) / (_gridMaxDistance * 0.5f);
			else
				gridAlpha = 1f;
			gridAlpha = Mathf.Clamp01(_gridColor.a * gridAlpha);
			if (gridAlpha != gridMat.color.a)
				gridMat.color = new Color(_gridColor.r, _gridColor.g, _gridColor.b, gridAlpha);
			gridMat.SetFloat("_WaterLevel", _waterLevel);
			gridMat.SetFloat("_AlphaOnWater", _gridAphaOnWater);
			cellLayer.SetActive(_showGrid && gridAlpha > 0);
			hudMatCell.SetFloat("_WaterLevel", _waterLevel);
			hudMatCell.SetFloat("_AlphaOnWater", _gridAphaOnWater);
			if (_gridCutOutBorders && _gridAphaOnWater < 1f)
				hudMatCell.EnableKeyword(SKW_WATER_MASK);
		}

		private void DrawColorizedCells()
		{
			var cellsCount = cells.Length;
			for (var k = 0; k < cellsCount; k++)
			{
				var cell = cells[k];
				if (cell == null)
					continue;
				if (cell.customMaterial != null) // && cell.visible) {
					ToggleCellSurface(k, true, cell.customMaterial.color, false,
						(Texture2D)cell.customMaterial.mainTexture, cell.customTextureScale,
						cell.customTextureOffset, cell.customTextureRotation);
			}
		}

		private Renderer GenerateCellSurface(int cellIndex, Material material, Vector2 textureScale,
			Vector2 textureOffset, float textureRotation)
		{
			if (cellIndex < 0 || cellIndex >= cells.Length)
				return null;
			return GenerateCellSurface(cells[cellIndex], material, textureScale, textureOffset,
				textureRotation);
		}

		private Renderer GenerateCellSurface(Cell cell, Material material, Vector2 textureScale,
			Vector2 textureOffset, float textureRotation)
		{
			var rect = cell.rect2D;
			var thePoints = cell.points; // this method is expensive
			var pointCount = thePoints.Length;
			for (var k = 0; k < pointCount; k++)
				hexPoints[k] = thePoints[k];
			if (cell.isWrapped)
			{
				hexPoints[6] = hexPoints[2] + Misc.Vector3left;
				hexPoints[7] = hexPoints[3] + Misc.Vector3left;
				hexPoints[8] = hexPoints[4] + Misc.Vector3left;
			}
			var renderer = Drawing.CreateSurface("Cell", hexPoints,
				cell.isWrapped ? hexIndicesWrapped : hexIndices, material, rect, textureScale,
				textureOffset, textureRotation, disposalManager,
				_gridCutOutBorders && _gridAphaOnWater < 1f);
			var surf = renderer.gameObject;
			surf.transform.SetParent(gridSurfacesLayer.transform, false);
			surf.transform.localPosition = Misc.Vector3zero;
			surf.layer = gameObject.layer;
			cell.renderer = renderer;
			return renderer;
		}

		#endregion

		#region Highlighting

		private void GridCheckMousePos()
		{
			if (!Application.isPlaying || !_showGrid)
				return;

			if (!lastMouseMapHitPosGood)
			{
				HideCellHighlight();
				return;
			}

			if (_exclusiveHighlight &&
			    (_enableCountryHighlight &&
			     _countryHighlightedIndex >= 0 &&
			     _countries[_countryHighlightedIndex].allowHighlight ||
			     _enableProvinceHighlight &&
			     _provinceHighlightedIndex >= 0 &&
			     _provinces[_provinceHighlightedIndex].allowHighlight &&
			     _countries[_provinces[_provinceHighlightedIndex].countryIndex]
				     .allowProvincesHighlight))
			{
				HideCellHighlight();
				return;
			}

			// verify if last highlited cell remains active
			if (_cellHighlightedIndex >= 0)
				if (_cellHighlighted.Contains(lastMouseMapHitPos))
					return;
			var newCellHighlightedIndex = GetCellIndex(lastMouseMapHitPos);
			if (OnCellExit != null &&
			    _cellLastPointerOn >= 0 &&
			    _cellLastPointerOn != newCellHighlightedIndex)
				OnCellExit(_cellLastPointerOn);
			if (newCellHighlightedIndex >= 0)
			{
				if (_cellHighlightedIndex != newCellHighlightedIndex)
					HighlightCell(newCellHighlightedIndex, false);
				if (OnCellEnter != null && _cellLastPointerOn != newCellHighlightedIndex)
					OnCellEnter(newCellHighlightedIndex);
				_cellLastPointerOn = newCellHighlightedIndex;
			}
			else
			{
				_cellLastPointerOn = -1;
				HideCellHighlight();
			}
		}

		private void GridUpdateHighlightFade()
		{
			if (_highlightFadeAmount == 0)
				return;

			if (cellHighlightedObjRenderer != null)
			{
				var newAlpha = 1.0f - Mathf.PingPong(time - highlightFadeStart, _highlightFadeAmount);
				var mat = cellHighlightedObjRenderer.sharedMaterial;
				if (mat != hudMatCell)
					cellHighlightedObjRenderer.sharedMaterial = hudMatCell;
				var color = hudMatCell.color;
				var newColor = new Color(color.r, color.g, color.b, newAlpha);
				hudMatCell.color = newColor;
			}
		}

		/// <summary>
		/// Highlights the cell region specified. Returns the generated highlight surface gameObject.
		/// Internally used by the Map UI and the Editor component, but you can use it as well to temporarily mark a territory region.
		/// </summary>
		/// <param name="refreshGeometry">Pass true only if you're sure you want to force refresh the geometry of the highlight (for instance, if the frontiers data has changed). If you're unsure, pass false.</param>
		private void HighlightCell(int cellIndex, bool refreshGeometry)
		{
			if (cellHighlightedObjRenderer != null)
				HideCellHighlight();
			if (cellIndex < 0 || cellIndex >= cells.Length)
				return;

			if (!cells[cellIndex].isFading && _enableCellHighlight)
			{
				var existsInCache = cells[cellIndex].renderer != null;
				if (refreshGeometry && existsInCache)
				{
					var obj = cells[cellIndex].renderer.gameObject;
					cells[cellIndex].renderer = null;
					DestroyImmediate(obj);
					existsInCache = false;
				}
				if (existsInCache)
				{
					cellHighlightedObjRenderer = cells[cellIndex].renderer;
					if (cellHighlightedObjRenderer != null)
					{
						cellHighlightedObjRenderer.enabled = true;
						cellHighlightedObjRenderer.sharedMaterial = hudMatCell;
					}
				}
				else
					cellHighlightedObjRenderer = GenerateCellSurface(cellIndex, hudMatCell,
						Misc.Vector2one, Misc.Vector2zero, 0);
				highlightFadeStart = time;
			}

			_cellHighlighted = cells[cellIndex];
			_cellHighlightedIndex = cellIndex;
		}

		private void HideCellHighlight()
		{
			if (cellHighlighted == null)
				return;
			if (cellHighlightedObjRenderer != null)
			{
				if (!cellHighlighted.isFading)
				{
					if (cellHighlighted.customMaterial != null)
						cellHighlightedObjRenderer.sharedMaterial = cellHighlighted.customMaterial;
					else
						cellHighlightedObjRenderer.enabled = false;
				}
				cellHighlightedObjRenderer = null;
			}
			_cellHighlighted = null;
			_cellHighlightedIndex = -1;
		}

		#endregion

		#region Geometric functions

		private Vector3 GetWorldSpacePosition(Vector2 localPosition) =>
			transform.TransformPoint(localPosition);

		#endregion

		#region Cell stuff

		private List<int> GetCellsWithinRect(Rect rect2D)
		{
			var r0 = (int)((rect2D.yMin + 0.5f) * _gridRows);
			var r1 = (int)((rect2D.yMax + 0.5f) * _gridRows);
			r1 = Mathf.Clamp(r1 + 1, 0, _gridRows - 1);
			var c0 = (int)((rect2D.xMin + 0.5f) * _gridColumns);
			var c1 = (int)((rect2D.xMax + 0.5f) * _gridColumns);
			var indices = new List<int>();
			for (var r = r0; r <= r1; r++)
			{
				var rr = r * _gridColumns;
				for (var c = c0; c <= c1; c++)
				{
					var cellIndex = rr + c;
					var cell = cells[cellIndex];
					if (cell != null && cell.rect2D.yMin <= rect2D.yMax && cell.rect2D.yMax >= rect2D.yMin)
						indices.Add(cellIndex);
				}
			}
			return indices;
		}

		private void CellAnimate(FADER_STYLE style, int cellIndex, Color color, float duration)
		{
			if (cellIndex < 0 || cellIndex >= cells.Length)
				return;
			if (cellIndex == _cellHighlightedIndex)
			{
				cells[cellIndex].isFading = true;
				HideCellHighlight();
			}
			var initialColor = Misc.ColorClear;
			var renderer = cells[cellIndex].renderer;
			if (renderer == null || renderer.sharedMaterial == null)
				renderer = SetCellTemporaryColor(cellIndex, initialColor);
			else
			{
				if (renderer.enabled)
					initialColor = renderer.sharedMaterial.color;
				else
					renderer.enabled = true;
			}
			SurfaceFader.Animate(style, cells[cellIndex], renderer, initialColor, color, duration);
		}

		#endregion
	}
}