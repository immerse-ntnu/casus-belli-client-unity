using System.IO;
using UnityEditor;
using UnityEngine;

namespace WorldMapStrategyKit
{
	public class WMSKAbout : EditorWindow
	{
		private Texture2D _headerTexture;
		private GUIStyle richLabelStyle;
		private Vector2 readmeScroll = Misc.Vector2zero;

		private string readmeText;
//								GUIStyle blackStyle;

		public static void ShowAboutWindow()
		{
			var height = 550.0f;
			var width = 600.0f;

			var rect = new Rect(Screen.currentResolution.width * 0.5f - width * 0.5f,
				Screen.currentResolution.height * 0.5f - height * 0.5f, width, height);
			GetWindowWithRect<WMSKAbout>(rect, true, "About WMSK", true);
		}

		private void OnEnable()
		{
			_headerTexture = Resources.Load<Texture2D>("WMSK/EditorHeader");

			// load readme.txt
			readmeText = File.ReadAllText(GetAssetPath() + "/README.txt");
		}

		private void OnGUI()
		{
			if (richLabelStyle == null)
			{
				richLabelStyle = new GUIStyle(GUI.skin.label);
				richLabelStyle.richText = true;
				richLabelStyle.wordWrap = true;
			}

			EditorGUILayout.Separator();
			GUI.skin.label.alignment = TextAnchor.MiddleCenter;
			GUILayout.Label(_headerTexture, GUILayout.ExpandWidth(true));
			GUI.skin.label.alignment = TextAnchor.MiddleLeft;
			EditorGUILayout.Separator();

			EditorGUILayout.Separator();
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("<b>World Map Strategy Kit</b>\nCopyright (C) by Kronnect", richLabelStyle);
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.Separator();
			GUILayout.Label("Thanks for purchasing!");
			EditorGUILayout.Separator();

			EditorGUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			readmeScroll = GUILayout.BeginScrollView(readmeScroll, GUILayout.Width(Screen.width * 0.95f));
			GUILayout.Label(readmeText, richLabelStyle);
			GUILayout.EndScrollView();
			GUILayout.FlexibleSpace();
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.Separator();
			EditorGUILayout.Separator();

			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("Support Forum and more assets!", GUILayout.Height(40)))
				Application.OpenURL("http://kronnect.me");
			if (GUILayout.Button("Rate this Asset", GUILayout.Height(40)))
				Application.OpenURL("com.unity3d.kharma:content/55121");
			if (GUILayout.Button("Close Window", GUILayout.Height(40)))
				Close();
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.Separator();
		}

		private string GetAssetPath()
		{
			// Proceed and restore
			var paths = AssetDatabase.GetAllAssetPaths();
			for (var k = 0; k < paths.Length; k++)
				if (paths[k].EndsWith("WorldMapStrategyKit"))
					return paths[k];
			return "";
		}
	}
}