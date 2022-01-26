using UnityEditor;
using UnityEngine;

namespace WorldMapStrategyKit
{
	[ExecuteInEditMode]
	public class WMSKProvincesEqualizer : EditorWindow
	{
		private int provincesMin = 8;
		private int provincesMax = 12;
		public static int countryIndex = -1;

		private int currentCountryIndex;
		private bool started;
		private int countryCount;

		private void OnEnable()
		{
			started = false;
			currentCountryIndex = -1;
			countryCount = WMSK.instance.countries.Length;
		}

		private void OnDestroy() { }

		public static void ShowWindow(int countryIndex = -1)
		{
			var w = 375;
			var h = 160;
			WMSKProvincesEqualizer.countryIndex = countryIndex;
			var rect = new Rect(Screen.currentResolution.width / 2 - w / 2,
				Screen.currentResolution.height / 2 - h / 2, w, h);
			var window =
				GetWindowWithRect<WMSKProvincesEqualizer>(rect, true, "Provinces Equalizer", true);
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

			if (countryIndex >= 0)
				EditorGUILayout.HelpBox(
					"This option will merge provinces of " +
					WMSK.instance.countries[countryIndex].name +
					" producing a number of provinces in the range below. The final number of provinces will be randomly chosen according to this range.",
					MessageType.Info);
			else
				EditorGUILayout.HelpBox(
					"This option will merge provinces of each country producing a number of provinces in the range below. The final number of provinces of each country will be randomly chosen according to this range.",
					MessageType.Info);
			EditorGUILayout.Separator();
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Minimum", GUILayout.Width(80));
			provincesMin = EditorGUILayout.IntSlider(provincesMin, 1, 40);
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Maximum", GUILayout.Width(80));
			provincesMax = EditorGUILayout.IntSlider(provincesMax, 1, 40);
			EditorGUILayout.EndHorizontal();
			if (provincesMax < provincesMin)
				provincesMax = provincesMin;
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
					WMSK.instance.showProvinces = true;
					WMSK.instance.drawAllProvinces = true;
					WMSK.instance.editor.ClearSelection();
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
				if (countryIndex >= 0)
				{
					WMSK.instance.editor.ProvincesEqualize(provincesMin, provincesMax, countryIndex);
					started = false;
					WMSK.instance.Redraw(true);
					Close();
				}
				else
				{
					currentCountryIndex++;
					if (currentCountryIndex < countryCount)
					{
						WMSK.instance.editor.ProvincesEqualize(provincesMin, provincesMax,
							currentCountryIndex);
						Repaint();
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
}