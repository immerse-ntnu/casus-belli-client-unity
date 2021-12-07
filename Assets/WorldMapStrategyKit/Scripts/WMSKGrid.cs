// World Strategy Kit for Unity - Main Script
// (C) 2016-2020 by Ramiro Oliva (Kronnect)
// Don't modify this script - changes could be lost if you upgrade to a more recent version of WMSK

using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace WorldMapStrategyKit
{
	/* Event definitions */
	public delegate void OnCellEvent(int cellIndex);

	public delegate void OnCellClickEvent(int cellIndex, int buttonIndex);

	public partial class WMSK : MonoBehaviour
	{
		#region Public properties

		[SerializeField] private bool
			_showGrid;

		/// <summary>
		/// Toggle grid on/off.
		/// </summary>
		public bool showGrid
		{
			get => _showGrid;
			set
			{
				if (value != _showGrid)
				{
					_showGrid = value;
					isDirty = true;

					if (cellLayer != null)
					{
						CheckGridRect();
						cellLayer.SetActive(_showGrid);
					}
					else if (_showGrid)
						DrawGrid();

					if (!_showGrid)
						DestroyGridSurfaces();
				}
			}
		}

		[SerializeField] private bool
			_gridCutOutBorders;

		/// <summary>
		/// Use alpha when coloring parts of cells on water
		/// </summary>
		public bool gridCutOutBorders
		{
			get => _gridCutOutBorders;
			set
			{
				if (value != _gridCutOutBorders)
				{
					_gridCutOutBorders = value;
					isDirty = true;
				}
			}
		}

		/// <summary>
		/// Enable/disable cell highlight when grid is visible and mouse is over.
		/// </summary>
		[SerializeField] private bool
			_enableCellHighlight = true;

		public bool enableCellHighlight
		{
			get => _enableCellHighlight;
			set
			{
				if (_enableCellHighlight != value)
				{
					_enableCellHighlight = value;
					isDirty = true;
					if (_enableCellHighlight)
					{
						showLatitudeLines = false;
						showLongitudeLines = false;
					}
					else
						HideCellHighlight();
				}
			}
		}

		/// <summary>
		/// Enable/disable country or province highlights if a cell is highlighted
		/// </summary>
		[SerializeField] private bool
			_exclusiveHighlight = true;

		public bool exclusiveHighlight
		{
			get => _exclusiveHighlight;
			set
			{
				if (_exclusiveHighlight != value)
				{
					_exclusiveHighlight = value;
					isDirty = true;
				}
			}
		}

		[SerializeField] private int _gridRows = 32;

		/// <summary>
		/// Returns the number of rows for box and hexagonal grid topologies
		/// </summary>
		public int gridRows
		{
			get => _gridRows;
			set
			{
				if (value != _gridRows)
				{
					_gridRows = value;
					isDirty = true;
					GenerateGrid();
				}
			}
		}

		[SerializeField] private int _gridColumns = 64;

		/// <summary>
		/// Returns the number of columns for box and hexagonal grid topologies
		/// </summary>
		public int gridColumns
		{
			get => _gridColumns;
			set
			{
				if (value != _gridColumns)
				{
					_gridColumns = value;
					isDirty = true;
					GenerateGrid();
				}
			}
		}

		/// <summary>
		/// Sets both grid rows and columns
		/// </summary>
		public void SetGridDimensions(int rows, int columns)
		{
			if (rows != _gridRows || columns != _gridColumns)
			{
				_gridRows = rows;
				_gridColumns = columns;
				isDirty = true;
				GenerateGrid();
			}
		}

		/// <summary>
		/// Complete array of cells.
		/// </summary>
		[NonSerialized] public Cell[] cells;

		[SerializeField] private float _highlightFadeAmount = 0.5f;

		/// <summary>
		/// Amount of fading ping-poing effect for highlighted cell
		/// </summary>
		public float highlightFadeAmount
		{
			get => _highlightFadeAmount;
			set
			{
				if (_highlightFadeAmount != value)
				{
					_highlightFadeAmount = value;
					isDirty = true;
				}
			}
		}

		[SerializeField] private Color
			_gridColor = new(0.486f, 0.490f, 0.529f, 1.0f);

		/// <summary>
		/// Cells border color
		/// </summary>
		public Color gridColor
		{
			get => _gridColor;
			set
			{
				if (value != _gridColor)
				{
					_gridColor = value;
					isDirty = true;
					if (gridMat != null && _gridColor != gridMat.color)
						gridMat.color = _gridColor;
				}
			}
		}

		[SerializeField] private Color
			_cellHighlightColor = new(1, 0, 0, 0.7f);

		/// <summary>
		/// Fill color to use when the mouse hovers a cell's region.
		/// </summary>
		public Color cellHighlightColor
		{
			get => _cellHighlightColor;
			set
			{
				if (value != _cellHighlightColor)
				{
					_cellHighlightColor = value;
					isDirty = true;
					if (hudMatCell != null && _cellHighlightColor != hudMatCell.color)
						hudMatCell.color = _cellHighlightColor;
				}
			}
		}

		[SerializeField, Range(0, 1)] private float _gridAphaOnWater = 0.1f;

		/// <summary>
		/// Alpha applied to grid over water
		/// </summary>
		public float gridAphaOnWater
		{
			get => _gridAphaOnWater;
			set
			{
				if (value != _gridAphaOnWater)
				{
					_gridAphaOnWater = value;
					isDirty = true;
					AdjustsGridAlpha();
				}
			}
		}

		[SerializeField] private float _gridMaxDistance = 1000f;

		/// <summary>
		/// Maximum distance from grid where it's visible
		/// </summary>
		public float gridMaxDistance
		{
			get => _gridMaxDistance;
			set
			{
				if (value != _gridMaxDistance)
				{
					_gridMaxDistance = value;
					isDirty = true;
					if (_showGrid)
					{
						if (!Application.isPlaying)
							CheckGridRect(); // if it's playing, CheckGridRect() is called during Update()
						AdjustsGridAlpha();
					}
				}
			}
		}

		[SerializeField] private float _gridMinDistance = 0.01f;

		/// <summary>
		/// Minimum distance from grid where it's visible
		/// </summary>
		public float gridMinDistance
		{
			get => _gridMinDistance;
			set
			{
				if (value != _gridMinDistance)
				{
					_gridMinDistance = value;
					isDirty = true;
					if (_showGrid)
					{
						if (!Application.isPlaying)
							CheckGridRect(); // if it's playing, CheckGridRect() is called during Update()
						AdjustsGridAlpha();
					}
				}
			}
		}

		#endregion

		#region Public API area

		public event OnCellEvent OnCellEnter;
		public event OnCellEvent OnCellExit;
		public event OnCellClickEvent OnCellClick;

		/// <summary>
		/// Returns Cell under mouse position or null if none.
		/// </summary>
		public Cell cellHighlighted => _cellHighlighted;

		/// <summary>
		/// Returns current highlighted cell index.
		/// </summary>
		public int cellHighlightedIndex => _cellHighlightedIndex;

		/// <summary>
		/// Returns Cell index which has been clicked
		/// </summary>
		public int cellLastClickedIndex => _cellLastClickedIndex;

		/// <summary>
		/// Returns the_numCellsrovince in the cells array by its reference.
		/// </summary>
		public int GetCellIndex(Cell cell)
		{
			//			string searchToken = cell.territoryIndex + "|" + cell.name;
			int cellIndex;
			if (cellLookup.TryGetValue(cell, out cellIndex))
				return cellIndex;
			else
				return -1;
		}

		/// <summary>
		/// Gets the cell object in the cells array by its index. Equals to map.cells[cellIndex] since cells array is public.
		/// </summary>
		public Cell GetCell(int cellIndex)
		{
			if (cellIndex < 0 || cells == null || cellIndex >= cells.Length)
				return null;
			return cells[cellIndex];
		}

		/// <summary>
		/// Colorizes specified cell by index.
		/// </summary>
		public Renderer ToggleCellSurface(int cellIndex, Color color) => ToggleCellSurface(cellIndex, true,
			color, false, null, Misc.Vector2one, Misc.Vector2zero, 0);

		/// <summary>
		/// Gets the cost for crossing to this cell computed in the last FindRoute function call
		/// </summary>
		/// <returns>The cell path cost.</returns>
		/// <param name="cellIndex">Cell index.</param>
		public float GetCellPathCost(int cellIndex)
		{
			if (cellIndex < 0 || cellIndex >= _cellsCosts.Length)
				return -1;
			return _cellsCosts[cellIndex].lastPathFindingCost;
		}

		/// <summary>
		/// Colorizes specified cell by index.
		/// </summary>
		public Renderer ToggleCellSurface(int cellIndex, bool visible, Color color) =>
			ToggleCellSurface(cellIndex, visible, color, false, null, Misc.Vector2one, Misc.Vector2zero,
				0);

		/// <summary>
		/// Colorizes specified cell by index.
		/// </summary>
		public Renderer
			ToggleCellSurface(int cellIndex, bool visible, Color color, bool refreshGeometry) =>
			ToggleCellSurface(cellIndex, visible, color, refreshGeometry, null, Misc.Vector2one,
				Misc.Vector2zero, 0);

		/// <summary>
		/// Colorizes specified cell by index.
		/// </summary>
		public Renderer ToggleCellSurface(int cellIndex, bool visible, Color color, bool refreshGeometry,
			bool autoChooseTransparentMaterial) => ToggleCellSurface(cellIndex, visible, color,
			refreshGeometry, null, Misc.Vector2one, Misc.Vector2zero, 0, autoChooseTransparentMaterial);

		/// <summary>
		/// Colorizes or texture specified cell by index.
		/// </summary>
		public Renderer ToggleCellSurface(int cellIndex, bool visible, Color color, bool refreshGeometry,
			Texture2D texture) => ToggleCellSurface(cellIndex, visible, color, refreshGeometry, texture,
			Misc.Vector2one, Misc.Vector2zero, 0, false);

		/// <summary>
		/// Colorizes or texture specified cell by index.
		/// </summary>
		public Renderer ToggleCellSurface(int cellIndex, bool visible, Color color, bool refreshGeometry,
			Texture2D texture, Vector2 textureScale, Vector2 textureOffset, float textureRotation) =>
			ToggleCellSurface(cellIndex, visible, color, refreshGeometry, texture, textureScale,
				textureOffset, textureRotation, false);

		/// <summary>
		/// Colorizes or texture specified cell by index.
		/// </summary>
		/// <param name="cellIndex">Cell index.</param>
		/// <param name="visible">If set to <c>true</c> the cell will be visible.</param>
		/// <param name="color">Color.</param>
		/// <param name="refreshGeometry">If set to <c>true</c> refresh cell geometry.</param>
		/// <param name="texture">Texture.</param>
		/// <param name="textureScale">Texture scale.</param>
		/// <param name="textureOffset">Texture offset.</param>
		/// <param name="textureRotation">Texture rotation.</param>
		/// <param name="autoChooseTransparentMaterial">If set to <c>true</c> a transparent material will be chosen only if alpha is < 1.0. If set to false (default), a transparent material will always be chosen.</param>
		public Renderer ToggleCellSurface(int cellIndex, bool visible, Color color, bool refreshGeometry,
			Texture2D texture, Vector2 textureScale, Vector2 textureOffset, float textureRotation,
			bool autoChooseTransparentMaterial)
		{
			if (cellIndex < 0 || cellIndex >= cells.Length)
				return null;
			if (cells[cellIndex] == null)
				return null;
			if (!visible)
			{
				HideCellSurface(cellIndex);
				return null;
			}
			var renderer = cells[cellIndex].renderer;
			GameObject surf = null;
			var existsInCache = renderer != null;
			if (refreshGeometry && existsInCache)
			{
				var obj = renderer.gameObject;
				cells[cellIndex].renderer = null;
				DestroyImmediate(obj);
				existsInCache = false;
				renderer = null;
			}
			if (existsInCache)
				surf = renderer.gameObject;

			// Should the surface be recreated?
			Material surfMaterial;
			var cell = cells[cellIndex];
			if (surf != null)
			{
				//																renderer = surf.GetComponent<Renderer> ();
				surfMaterial = renderer.sharedMaterial;
				if (texture != null &&
				    (cell.customMaterial == null ||
				     textureScale != cell.customTextureScale ||
				     textureOffset != cell.customTextureOffset ||
				     textureRotation != cell.customTextureRotation ||
				     !cell.customMaterial.name.Equals(texturizedMat.name)))
				{
					cells[cellIndex].renderer = null;
					DestroyImmediate(surf);
					surf = null;
				}
			}
			// If it exists, activate and check proper material, if not create surface
			var isHighlighted = cellHighlightedIndex == cellIndex;
			if (surf != null)
			{
				if (!renderer.enabled)
					renderer.enabled = true;
				// Check if material is ok
				if (renderer == null)
					renderer = surf.GetComponent<Renderer>();
				surfMaterial = renderer.sharedMaterial;
				if (surfMaterial == null ||
				    texture == null && !surfMaterial.name.Equals(coloredMat.name) ||
				    texture != null && !surfMaterial.name.Equals(texturizedMat.name) ||
				    surfMaterial.color != color && !isHighlighted ||
				    texture != null && cell.customMaterial.mainTexture != texture)
				{
					var goodMaterial =
						GetColoredTexturedMaterial(color, texture, autoChooseTransparentMaterial);
					if (_gridCutOutBorders && _gridAphaOnWater < 1f)
						goodMaterial.EnableKeyword(SKW_WATER_MASK);
					cell.customMaterial = goodMaterial;
					ApplyMaterialToSurface(surf, goodMaterial);
				}
			}
			else
			{
				surfMaterial =
					GetColoredTexturedMaterial(color, texture, autoChooseTransparentMaterial, 2);
				if (_gridCutOutBorders && _gridAphaOnWater < 1f)
					surfMaterial.EnableKeyword(SKW_WATER_MASK);
				renderer = GenerateCellSurface(cellIndex, surfMaterial, textureScale, textureOffset,
					textureRotation);
				cell.customMaterial = surfMaterial;
				cell.customTextureOffset = textureOffset;
				cell.customTextureRotation = textureRotation;
				cell.customTextureScale = textureScale;
			}
			// If it was highlighted, highlight it again
			if (cell.customMaterial != null &&
			    isHighlighted &&
			    cell.customMaterial.color != hudMatCell.color)
			{
				var clonedMat = Instantiate(cell.customMaterial);
				if (disposalManager != null)
					disposalManager.MarkForDisposal(clonedMat);
				clonedMat.name = cell.customMaterial.name;
				clonedMat.color = hudMatCell.color;
				if (renderer == null)
					renderer = surf.GetComponent<Renderer>();
				renderer.sharedMaterial = clonedMat;
				cellHighlightedObjRenderer = renderer;
			}
			return renderer;
		}

		/// <summary>
		/// Assigns a temporary color to a cell. This color can be erased calling RestoreCellMaterials().
		/// </summary>
		/// <param name="cellIndex">Cell index.</param>
		/// <param name="color">Color.</param>
		public Renderer SetCellTemporaryColor(int cellIndex, Color color)
		{
			if (cellIndex < 0 || cellIndex >= cells.Length || cellIndex == _cellHighlightedIndex)
				return null;
			var renderer = cells[cellIndex].renderer;
			Material clonedMat = null;
			if (renderer != null)
				clonedMat = renderer.sharedMaterial;
			if (clonedMat != null)
			{
				clonedMat = Instantiate(clonedMat);
				if (disposalManager != null)
					disposalManager
						.MarkForDisposal(clonedMat); // clonedMat.hideFlags = HideFlags.DontSave;
				clonedMat.color = color;
				renderer.sharedMaterial = clonedMat;
				renderer.enabled = true;
			}
			else
			{
				renderer = ToggleCellSurface(cellIndex, color);
				cells[cellIndex].customMaterial = null;
			}
			return renderer;
		}

		/// <summary>
		/// Restores all cells colors to their original materials. Useful when coloring cells temporarily using SetCellTemporaryColor.
		/// </summary>
		public void RestoreCellMaterials()
		{
			for (var k = 0; k < cells.Length; k++)
			{
				if (k == _cellHighlightedIndex || cells[k] == null)
					continue;
				var renderer = cells[k].renderer;
				if (renderer != null && renderer.enabled)
				{
					var customMaterial = cells[k].customMaterial;
					if (customMaterial == null)
						renderer.enabled = false;
					else if (renderer.sharedMaterial != customMaterial)
						renderer.sharedMaterial = customMaterial;
				}
			}
		}

		/// <summary>
		/// Uncolorize/hide all cells.
		/// </summary>
		public void HideCellSurfaces()
		{
			for (var k = 0; k < cells.Length; k++)
				HideCellSurface(k);
		}

		/// <summary>
		/// Uncolorize/hide specified cell by index in the cells collection.
		/// </summary>
		public void HideCellSurface(int cellIndex)
		{
			if (cells[cellIndex] == null)
				return;
			if (_cellHighlightedIndex != cellIndex)
			{
				var renderer = cells[cellIndex].renderer;
				if (renderer != null)
					renderer.enabled = false;
			}
			cells[cellIndex].customMaterial = null;
		}

		/// <summary>
		/// Colors a cell and fades it out during "duration" in seconds.
		/// </summary>
		public void CellFadeOut(int cellIndex, Color color, float duration)
		{
			CellAnimate(FADER_STYLE.FadeOut, cellIndex, color, duration);
		}

		/// <summary>
		/// Colors a group of cells and fades it out during "duration" in seconds.
		/// </summary>
		/// <param name="cellIndices">Cell indices.</param>
		/// <param name="color">Color.</param>
		/// <param name="duration">Duration.</param>
		public void CellFadeOut(List<int> cellIndices, Color color, float duration)
		{
			if (cellIndices == null)
				return;
			var cc = cellIndices.Count;
			for (var k = 0; k < cc; k++)
				CellFadeOut(cellIndices[k], color, duration);
		}

		/// <summary>
		/// Colors a cell and blinks it during "duration" in seconds.
		/// </summary>
		public void CellBlink(int cellIndex, Color color, float duration)
		{
			CellAnimate(FADER_STYLE.Blink, cellIndex, color, duration);
		}

		/// <summary>
		/// Colors a group of cells and blinks them during "duration" in seconds.
		/// </summary>
		/// <param name="cellIndices">Cell indices.</param>
		/// <param name="color">Color.</param>
		/// <param name="duration">Duration.</param>
		public void CellBlink(List<int> cellIndices, Color color, float duration)
		{
			if (cellIndices == null)
				return;
			var cc = cellIndices.Count;
			for (var k = 0; k < cc; k++)
				CellBlink(cellIndices[k], color, duration);
		}

		/// <summary>
		/// Colors a cell and flashes it during "duration" in seconds.
		/// </summary>
		public void CellFlash(int cellIndex, Color color, float duration)
		{
			CellAnimate(FADER_STYLE.Flash, cellIndex, color, duration);
		}

		/// <summary>
		/// Colors a group of cells and flashes them during "duration" in seconds.
		/// </summary>
		/// <param name="cellIndices">Cell indices.</param>
		/// <param name="color">Color.</param>
		/// <param name="duration">Duration.</param>
		public void CellFlash(List<int> cellIndices, Color color, float duration)
		{
			if (cellIndices == null)
				return;
			var cc = cellIndices.Count;
			for (var k = 0; k < cc; k++)
				CellFlash(cellIndices[k], color, duration);
		}

		/// <summary>
		/// Gets the cell's center position in local space.
		/// </summary>
		public Vector2 GetCellPosition(int cellIndex)
		{
			if (cellIndex < 0 || cellIndex >= cells.Length)
				return Misc.Vector2zero;
			return cells[cellIndex].center;
		}

		/// <summary>
		/// Gets the cell's center position in world space.
		/// </summary>
		public Vector3 GetCellWorldPosition(int cellIndex)
		{
			if (cellIndex < 0 || cellIndex >= cells.Length)
				return Misc.Vector3zero;
			var cellGridCenter = cells[cellIndex].center;
			return GetWorldSpacePosition(cellGridCenter);
		}

		/// <summary>
		/// Returns the world space position of the vertex
		/// </summary>
		public Vector3 GetCellVertexWorldPosition(int cellIndex, int vertexIndex)
		{
			var localPosition = cells[cellIndex].points[vertexIndex];
			return GetWorldSpacePosition(localPosition);
		}

		/// <summary>
		/// Returns the cell object under position in local coordinates
		/// </summary>
		public Cell GetCell(Vector2 localPosition)
		{
			var cellIndex = GetCellIndex(localPosition);
			if (cellIndex >= 0)
				return cells[cellIndex];
			return null;
		}

		/// <summary>
		/// Returns the cell at the specified row and column. This is a shortcut to accessing the cells array using row * gridColumns + column as the index.
		/// </summary>
		public Cell GetCell(int row, int column)
		{
			var cellIndex = row * _gridColumns + column;
			if (cellIndex < 0 || cellIndex >= cells.Length)
				return null;
			return cells[cellIndex];
		}

		/// <summary>
		/// Gets the index of the cell.
		/// </summary>
		/// <returns>The cell index.</returns>
		public int GetCellIndex(int row, int column) => row * _gridColumns + column;

		/// <summary>
		/// Returns the cell index under position in local coordinates
		/// </summary>
		public int GetCellIndex(Vector2 localPosition)
		{
			if (cells == null)
				return -1;
			var row = (int)((localPosition.y + 0.5f) * _gridRows);
			var col = (int)((localPosition.x + 0.5f) * _gridColumns);
			for (var r = row - 1; r <= row + 1; r++)
			{
				if (r < 0 || r >= _gridRows)
					continue;
				var rr = r * _gridColumns;
				for (var c = col - 1; c <= col + 1; c++)
				{
					if (c < 0 || c >= _gridColumns)
						continue;
					var cellIndex = rr + c;
					var cell = cells[cellIndex];
					if (cell != null && cell.Contains(localPosition))
						return cellIndex;
				}
			}
			return -1;
		}

		/// <summary>
		/// Returns a list of cells indices whose center belongs to a country regions.
		/// </summary>
		public List<int> GetCellsInCountry(int countryIndex)
		{
			var allCells = new List<int>();
			var country = _countries[countryIndex];
			var regionsCount = country.regions.Count;
			// Clear cells flag
			for (var k = 0; k < cells.Length; k++)
				if (cells[k] != null)
					cells[k].flag = false;
			for (var k = 0; k < regionsCount; k++)
			{
				var region = country.regions[k];
				var candidateCells = GetCellsWithinRect(region.rect2D);
				var candidateCellsCount = candidateCells.Count;
				for (var c = 0; c < candidateCellsCount; c++)
				{
					var cellIndex = candidateCells[c];
					var cell = cells[cellIndex];
					if (cell.flag) // used?
						continue;

					// Check if cell is fully outside the region
					var inside = 0;
					for (var p = 0; p < cell.points.Length; p++)
						if (region.Contains(cell.points[p]))
						{
							inside++;
							break;
						}
					if (inside == 0)
						continue;

					var cellCountry = GetCellCountryIndex(cellIndex);
					if (cellCountry == countryIndex)
					{
						allCells.Add(cellIndex);
						cell.flag = true;
					}
				}
			}
			return allCells;
		}

		/// <summary>
		/// Returns a list of cells whose center belongs to a country region.
		/// </summary>
		public List<int> GetCellsInProvince(int provinceIndex)
		{
			var allCells = new List<int>();
			var province = provinces[provinceIndex];
			for (var k = 0; k < province.regions.Count; k++)
			{
				var candidateCells = GetCellsWithinRect(province.regions[k].rect2D);
				for (var c = 0; c < candidateCells.Count; c++)
				{
					var cellProvince = GetCellProvinceIndex(candidateCells[c]);
					if (cellProvince == provinceIndex)
						allCells.Add(candidateCells[c]);
				}
			}
			return allCells;
		}

		private int[] countryCountTmp;

		/// <summary>
		/// Returns the country index to which the cell belongs.
		/// </summary>
		public int GetCellCountryIndex(int cellIndex)
		{
			int dummy;
			return GetCellCountryIndex(cellIndex, out dummy);
		}

		/// <summary>
		/// Returns the country index to which the cell belongs. It also returns the region index.
		/// </summary>
		public int GetCellCountryIndex(int cellIndex, out int regionIndex)
		{
			regionIndex = -1;
			var cell = cells[cellIndex];
			if (cell == null)
				return -1;
			if (countryCountTmp == null || countryCountTmp.Length < countries.Length)
				countryCountTmp = new int[countries.Length];
			for (var k = 0; k < countryCountTmp.Length; k++)
				countryCountTmp[k] = 0;
			int countryMax = -1, countryMaxCount = 0;
			var pointCount = cell.points.Length;
			// seems complicated but needs to check countries from smallest to biggers to account for enclaves
			for (var k = -1; k < pointCount; k++)
			{
				int countryIndex;
				if (k == -1)
					countryIndex = GetCountryIndex(cell.center);
				else
				{
					Vector2 mp;
					mp.x = cell.points[k].x * 0.9f + cell.center.x * 0.1f;
					mp.y = cell.points[k].y * 0.9f + cell.center.y * 0.1f;
					countryIndex = GetCountryIndex(mp);
				}
				if (countryIndex == -1)
					continue;

				countryCountTmp[countryIndex]++;
				if (countryCountTmp[countryIndex] > countryMaxCount)
				{
					countryMax = countryIndex;
					countryMaxCount = countryCountTmp[countryIndex];
					regionIndex = lastRegionIndex;
					if (countryMaxCount >= 4)
						break;
				}
			}

			if (countryMax < 0)
			{
				// check if there's a small country inside the cell
				var countryCount = countriesOrderedBySize.Count;
				for (var oc = 0; oc < countryCount; oc++)
				{
					var c = _countriesOrderedBySize[oc];
					var country = _countries[c];
					if (country.hidden && Application.isPlaying)
						continue;
					if (!cell.Intersects(country.regionsRect2D))
						continue;
					var crCount = country.regions.Count;
					for (var cr = 0; cr < crCount; cr++)
						if (cell.Contains(country.regions[cr].center))
						{
							countryMax = c;
							regionIndex = cr;
							oc = countryCount;
							break;
						}
				}
			}

			return countryMax;
		}

		/// <summary>
		/// Returns the province index to which the cell belongs.
		/// </summary>
		public int GetCellProvinceIndex(int cellIndex)
		{
			var cell = cells[cellIndex];
			if (cell == null)
				return -1;
			var provinceCount = new Dictionary<int, int>();
			int provinceMax = -1, provinceMaxCount = 0;
			var pointCount = cell.points.Length;
			for (var k = -1; k < pointCount; k++)
			{
				int provinceIndex;
				if (k == -1)
					provinceIndex = GetProvinceIndex(cell.center);
				else
					provinceIndex = GetProvinceIndex(cell.points[k]);
				if (provinceIndex == -1)
					continue;
				int count;
				if (provinceCount.TryGetValue(provinceIndex, out count))
				{
					count++;
					provinceCount[provinceIndex] = count;
				}
				else
				{
					count = 1;
					provinceCount[provinceIndex] = count;
				}
				if (count > provinceMaxCount)
				{
					provinceMaxCount = count;
					provinceMax = provinceIndex;
				}
			}
			return provinceMax;
		}

		/// <summary>
		/// Get a list of cells which are nearer than a given distance in cell count with optional parameters
		/// </summary>
		public List<int> GetCellNeighbours(int cellIndex, int distance, float maxSearchCost = 0,
			TERRAIN_CAPABILITY terrainCapability = TERRAIN_CAPABILITY.Any)
		{
			if (cellIndex < 0 || cellIndex >= cells.Length)
				return null;
			distance++;
			var cell = cells[cellIndex];
			var cc = new List<int>();
			for (var x = cell.column - distance; x <= cell.column + distance; x++)
			{
				if (x < 0 || x >= _gridColumns)
					continue;
				for (var y = cell.row - distance; y <= cell.row + distance; y++)
				{
					if (y < 0 || y >= _gridRows)
						continue;
					if (x == cell.column && y == cell.row)
						continue;
					var otherCell = GetCell(y, x);
					if (otherCell == null)
						continue;
					var steps = FindRoute(cell, otherCell, terrainCapability, maxSearchCost);
					if (steps != null && steps.Count <= distance)
						cc.Add(GetCellIndex(otherCell));
				}
			}
			return cc;
		}

		/// <summary>
		/// Gets a list of cells within the given altitude range (non-inclusive)
		/// </summary>
		/// <returns>The cells by altitude.</returns>
		/// <param name="minAltitude">Minimum altitude.</param>
		/// <param name="maxAltitude">Maximum altitude.</param>
		public List<Cell> GetCellsByAltitude(float minAltitude, float maxAltitude)
		{
			ComputeCellsCostsInfo();
			var highCells = new List<Cell>();
			for (var k = 0; k < cells.Length; k++)
			{
				if (cells[k] == null)
					continue;
				if (_cellsCosts[k].altitude > minAltitude && _cellsCosts[k].altitude < maxAltitude)
					highCells.Add(cells[k]);
			}
			return highCells;
		}

		#endregion
	}
}