using UnityEditor;

namespace WorldMapStrategyKit
{
	public partial class WMSKEditorInspector
	{
		// Add a menu item called "Hexify Frontiers".
		[MenuItem("CONTEXT/WMSK_Editor/Hexify Frontiers", false, 134)]
		private static void HexifyFrontiersMenuOption(MenuCommand command)
		{
			var editor = (WMSK_Editor)command.context;

			if (!editor.map.showGrid)
			{
				EditorUtility.DisplayDialog("Hexify Frontiers", "Grid must be enabled in WMSK inspector.",
					"Ok");
				return;
			}

			editor.ClearSelection();
			if (editor.editingMode == EDITING_MODE.COUNTRIES)
			{
				if (!EditorUtility.DisplayDialog("Hexify Frontiers",
					"This command will adjust COUNTRY frontiers (NOT PROVINCES) to match grid shape. If you want to include province borders, switch 'Show Layers' setting to Country + Provinces.\n\nBefore continuing, make sure the grid dimensions are fine.",
					"Ok", "Cancel"))
					return;
				var cc = new HexifyOpContext
				{
					title = "Hexifying Countries...",
					progress = hexifyProgress,
					finish = hexifyFinished
				};
				EditorCoroutines.Start(editor.HexifyCountries(cc));
			}
			else
			{
				if (!EditorUtility.DisplayDialog("Hexify Frontiers",
					"This command will adjust COUNTRY and PROVINCE borders to match grid shape. Before continuing, make sure the grid dimensions are fine.",
					"Ok", "Cancel"))
					return;
				EditorCoroutines.Start(editor.HexifyAll(hexifyProgress, hexifyFinished));
			}
		}

		private static bool hexifyProgress(float progress, string title, string text)
		{
			if (progress < 1.0f)
				return EditorUtility.DisplayCancelableProgressBar("Operation in progress",
					title + (text.Length > 0 ? " (" + text + ")" : ""), progress);
			EditorUtility.ClearProgressBar();
			return false;
		}

		private static void hexifyFinished(bool cancelled)
		{
			if (cancelled)
				EditorUtility.DisplayDialog("Operation Cancelled",
					"Some frontiers may have changed, others not. Use 'Revert' button to reload frontiers.",
					"Ok");
			else
				EditorUtility.DisplayDialog("Operation Complete",
					"Frontiers now match current grid. Use 'Save' button to make changes permanent.",
					"Ok");
		}
	}
}