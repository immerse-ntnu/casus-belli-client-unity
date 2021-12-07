using UnityEngine;
using UnityEditor;

namespace WorldMapStrategyKit
{
	[ExecuteInEditMode]
	public class WMSKCountriesEqualizer : EditorWindow
	{
		private int countriesMax = 8;
		private int seed = 0;

		private int currentCountryIndex;
		private bool started;
		private int countryCount;

		private void OnEnable()
		{
			started = false;
			currentCountryIndex = -1;
			countryCount = WMSK.instance.countries.Length;
		}

		public static void ShowWindow()
		{
			var w = 400;
			var h = 160;
			var rect = new Rect(Screen.currentResolution.width / 2 - w / 2,
				Screen.currentResolution.height / 2 - h / 2, w, h);
			var window =
				GetWindowWithRect<WMSKCountriesEqualizer>(rect, true, "Countries Equalizer", true);
			window.ShowUtility();
		}

		private void OnGUI()
		{
			if (WMSK.instance == null)
			{
				DestroyImmediate(this);
				GUIUtility.ExitGUI();
				return;
			}

			EditorGUILayout.HelpBox(
				"This option will merge existing countries with their neighbours until the total number of countries per continent falls below the given figure:",
				MessageType.Info);
			EditorGUILayout.Separator();
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Max per continent", GUILayout.Width(150));
			countriesMax = EditorGUILayout.IntSlider(countriesMax, 1, 200);
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Random Seed", GUILayout.Width(150));
			seed = EditorGUILayout.IntSlider(seed, 1, 100);
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.Separator();
			EditorGUILayout.Separator();
			EditorGUILayout.BeginHorizontal();
			if (started)
				EditorGUILayout.HelpBox(
					"This operation can take some time. Please wait until it finishes. Processing .... " +
					currentCountryIndex +
					"/" +
					countryCount, MessageType.Warning);
			else
				EditorGUILayout.HelpBox(
					"This operation can take some time. Please wait until it finishes.",
					MessageType.Warning);
			if (started)
			{
				GUI.enabled = false;
				GUILayout.Button("Busy...");
				GUI.enabled = true;
			}
			else
			{
				if (GUILayout.Button("Start"))
				{
					WMSK.instance.editor.ClearSelection();
					Random.InitState(seed);
					started = true;
				}
			}
			if (GUILayout.Button("Cancel"))
			{
				started = false;
				Close();
			}
		}

		private void Update()
		{
			if (started)
			{
				currentCountryIndex++;
				if (currentCountryIndex < countryCount)
				{
					bool repeat;
					WMSK.instance.editor.CountriesEqualize(countriesMax, out repeat);
					Repaint();
					if (!repeat)
						currentCountryIndex = countryCount;
				}
				else
				{
					started = false;
					WMSK.instance.Redraw(true);
					Close();
				}
			}
		}
	}
}