using UnityEngine;

namespace WorldMapStrategyKit
{
	public class DemoHexGridViewport : MonoBehaviour
	{
		private enum ACTION_MODE
		{
			Idle = 0,
			FadeOut = 1,
			Flash = 2,
			Blink = 3,
			Paint = 10,
			FadeCountry = 15
		}

		private WMSK map;
		private GUIStyle labelStyle, labelStyleShadow, buttonStyle, sliderStyle, sliderThumbStyle;
		private ColorPicker colorPicker;
		private float zoomLevel = 1.0f;
		private float cellsCount;
		private ACTION_MODE mode;

		private void Start()
		{
			// Get a reference to the World Map API:
			map = WMSK.instance;

			// UI Setup - non-important, only for this demo
			labelStyle = new GUIStyle();
			labelStyle.alignment = TextAnchor.MiddleCenter;
			labelStyle.normal.textColor = Color.white;
			labelStyleShadow = new GUIStyle(labelStyle);
			labelStyleShadow.normal.textColor = Color.black;
			buttonStyle = new GUIStyle(labelStyle);
			buttonStyle.alignment = TextAnchor.MiddleLeft;
			buttonStyle.normal.background = Texture2D.whiteTexture;
			buttonStyle.normal.textColor = Color.white;
			colorPicker = gameObject.GetComponent<ColorPicker>();
			sliderStyle = new GUIStyle();
			sliderStyle.normal.background = Texture2D.whiteTexture;
			sliderStyle.fixedHeight = 4.0f;
			sliderThumbStyle = new GUIStyle();
			sliderThumbStyle.normal.background = Resources.Load<Texture2D>("GUI/thumb");
			sliderThumbStyle.overflow = new RectOffset(0, 0, 8, 0);
			sliderThumbStyle.fixedWidth = 20.0f;
			sliderThumbStyle.fixedHeight = 12.0f;

			// setup GUI resizer - only for the demo
			GUIResizer.Init(800, 500);

			/* Register events: this is optionally but allows your scripts to be informed instantly as the mouse enters or exits a country, province or city */
			map.OnCellEnter += (int cellIndex) => Debug.Log("Entered cell #" +
			                                                cellIndex +
			                                                " at row " +
			                                                map.cells[cellIndex].row +
			                                                ", column " +
			                                                map.cells[cellIndex].column);
			map.OnCellExit += (int cellIndex) => Debug.Log("Exited cell #" +
			                                               cellIndex +
			                                               " at row " +
			                                               map.cells[cellIndex].row +
			                                               ", column " +
			                                               map.cells[cellIndex].column);
			map.OnCellClick += (int cellIndex, int buttonIndex) =>
			{
				var row = map.cells[cellIndex].row;
				var col = map.cells[cellIndex].column;
				Debug.Log("Clicked cell #" +
				          cellIndex +
				          " at row " +
				          row +
				          ", column " +
				          col +
				          ", center = " +
				          map.cells[cellIndex].center);
				switch (mode)
				{
					case ACTION_MODE.Blink:
					case ACTION_MODE.FadeOut:
					case ACTION_MODE.Flash:
						AnimateCells(row, col);
						break;
					case ACTION_MODE.Paint:
						PaintCurrentCell();
						break;
					case ACTION_MODE.FadeCountry:
						PaintCurrentCountry();
						break;
				}
			};

			map.SetZoomLevel(0.3f);
			map.showGrid = true;
			cellsCount = map.gridColumns;

			map.FlyToCountry("Spain", 0, 0.17f);
		}

		// Update is called once per frame
		private void OnGUI()
		{
			// Do autoresizing of GUI layer
			GUIResizer.AutoResize();

			// Check whether a country or city is selected, then show a label with the entity name and its neighbours (new in V4.1!)
			if (map.cellHighlighted != null)
			{
				var cellIndex = map.cellHighlightedIndex;
				var text = "Cell index = " +
				           cellIndex +
				           ", row = " +
				           map.cells[cellIndex].row +
				           ", col = " +
				           map.cells[cellIndex].column +
				           ", center = " +
				           map.cells[cellIndex].center;
				// shadow
				float x, y;
				x = Screen.width / 2.0f;
				y = Screen.height - 40;
				GUI.Label(new Rect(x - 1, y - 1, 0, 10), text, labelStyleShadow);
				GUI.Label(new Rect(x + 1, y + 2, 0, 10), text, labelStyleShadow);
				GUI.Label(new Rect(x + 2, y + 3, 0, 10), text, labelStyleShadow);
				GUI.Label(new Rect(x + 3, y + 4, 0, 10), text, labelStyleShadow);
				// texst face
				GUI.Label(new Rect(x, y, 0, 10), text, labelStyle);
			}

			// Assorted options to show/hide frontiers, cities, Earth and enable country highlighting
			GUI.Box(new Rect(5, 0, 175, 180), "");
			map.showGrid = GUI.Toggle(new Rect(10, 20, 170, 30), map.showGrid, "Toggle Grid");
			map.enableCellHighlight = GUI.Toggle(new Rect(10, 50, 170, 30), map.enableCellHighlight,
				"Enable Cell Highlighting");
			if (GUI.Toggle(new Rect(10, 80, 170, 30), mode == ACTION_MODE.FadeOut, "Toggle Fade Circle"))
				mode = ACTION_MODE.FadeOut;
			if (GUI.Toggle(new Rect(10, 110, 170, 30), mode == ACTION_MODE.Flash, "Toggle Flash Circle"))
				mode = ACTION_MODE.Flash;
			if (GUI.Toggle(new Rect(10, 140, 170, 30), mode == ACTION_MODE.Blink, "Toggle Blink Circle"))
				mode = ACTION_MODE.Blink;
			if (GUI.Toggle(new Rect(10, 170, 170, 30), mode == ACTION_MODE.Paint, "Toggle Colorize Cell"))
				mode = ACTION_MODE.Paint;
			if (GUI.Toggle(new Rect(10, 200, 170, 30), mode == ACTION_MODE.FadeCountry,
				"Toggle Fade Country"))
				mode = ACTION_MODE.FadeCountry;

			// buttons background color
			GUI.backgroundColor = new Color(0.1f, 0.1f, 0.3f, 0.95f);

			// Clear painted cells
			if (GUI.Button(new Rect(10, 230, 160, 30), "  Clear Cells", buttonStyle))
				map.HideCellSurfaces();

			// Add buttons to show the color picker and change colors for the cells
			if (GUI.Button(new Rect(10, 265, 160, 30), "  Change Grid Color", buttonStyle))
				colorPicker.showPicker = true;
			if (colorPicker.showPicker)
				map.gridColor = colorPicker.setColor;

			// Slider to show the new set zoom level API in V4.1
			GUI.Button(new Rect(10, 300, 85, 30), "  Cells", buttonStyle);
			GUI.backgroundColor = Color.white;
			var prevCellsCount = (int)cellsCount;
			cellsCount = (int)GUI.HorizontalSlider(new Rect(100, 315, 80, 30), cellsCount, 32, 512,
				sliderStyle, sliderThumbStyle);
			if ((int)cellsCount != prevCellsCount)
			{
				map.gridColumns = (int)cellsCount;
				map.gridRows = map.gridColumns / 2;
				cellsCount = map.gridColumns;
			}
			GUI.backgroundColor = new Color(0.1f, 0.1f, 0.3f, 0.95f);

			// Slider to show the new set zoom level API in V4.1
			GUI.Button(new Rect(10, 335, 85, 30), "  Zoom Level", buttonStyle);
			var prevZoomLevel = zoomLevel;
			GUI.backgroundColor = Color.white;
			zoomLevel = GUI.HorizontalSlider(new Rect(100, 350, 80, 30), zoomLevel, 0, 1, sliderStyle,
				sliderThumbStyle);
			GUI.backgroundColor = new Color(0.1f, 0.1f, 0.3f, 0.95f);
			if (zoomLevel != prevZoomLevel)
			{
				prevZoomLevel = zoomLevel;
				map.SetZoomLevel(zoomLevel);
			}
		}

		/// <summary>
		/// Paints current cell in green
		/// </summary>
		private void PaintCurrentCell()
		{
			if (map.cellHighlightedIndex < 0)
				return;
			map.ToggleCellSurface(map.cellHighlightedIndex, true, Color.green, false);
		}

		/// <summary>
		/// Fade out all cells in current country in blue
		/// </summary>
		private void PaintCurrentCountry()
		{
			if (map.countryHighlightedIndex < 0)
				return;
			var cellsInCountry = map.GetCellsInCountry(map.countryHighlightedIndex);
			for (var k = 0; k < cellsInCountry.Count; k++)
				map.CellFadeOut(cellsInCountry[k], Color.blue, 2.0f);
		}

		/// <summary>
		/// Adds a fade out effect around row, col positions in a circle
		/// </summary>
		private void AnimateCells(int row, int col)
		{
			var radius = 6;
			for (var r = row - radius; r <= row + radius; r++)
			{
				if (r < 0 || r >= map.gridRows)
					continue;
				for (var c = col - radius; c <= col + radius; c++)
				{
					if (c < 0 || c >= map.gridColumns)
						continue;
					var distance = (int)Mathf.Sqrt((row - r) * (row - r) + (col - c) * (col - c));
					if (distance < radius)
					{
						var cellIndex = r * map.gridColumns + c;
						switch (mode)
						{
							default:
								map.CellFadeOut(cellIndex, Color.red, distance * 0.25f);
								break;
							case ACTION_MODE.Flash:
								map.CellFlash(cellIndex, Color.red, distance * 0.25f);
								break;
							case ACTION_MODE.Blink:
								map.CellBlink(cellIndex, Color.red, distance * 0.25f);
								break;
						}
					}
				}
			}
		}
	}
}