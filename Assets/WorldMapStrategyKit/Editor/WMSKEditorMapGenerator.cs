using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace WorldMapStrategyKit
{
	public partial class WMSKEditorInspector : Editor
	{
		private GUIStyle titleLabelStyle, infoLabelStyle, generateButtonStyle;

		private SerializedProperty mapGenerationQuality,
			outputFolder,
			seed,
			seedNames,
			numProvinces,
			numCountries;

		private SerializedProperty userHeightMapTexture,
			heightmapWidth,
			heightmapHeight,
			octavesBySeed,
			noiseOctaves,
			seaColor,
			seaLevel,
			noisePower;

		private SerializedProperty islandFactor, elevationShift;
		private SerializedProperty backgroundTextureWidth, backgroundTextureHeight;
		private SerializedProperty gridRelaxation, edgeMaxLength, edgeNoise;
		private SerializedProperty numCitiesPerCountryMin, numCitiesPerCountryMax;

		private SerializedProperty heightGradient,
			heightGradientPreset,
			gradientPerPixel,
			changeStyle,
			generateNormalMap,
			normalMapBumpiness;

		private Material previewMat;

		private void InitMapGenerator()
		{
			outputFolder = serializedObject.FindProperty("outputFolder");
			mapGenerationQuality = serializedObject.FindProperty("mapGenerationQuality");

			seed = serializedObject.FindProperty("seed");
			seedNames = serializedObject.FindProperty("seedNames");
			numProvinces = serializedObject.FindProperty("numProvinces");
			numCountries = serializedObject.FindProperty("numCountries");

			backgroundTextureWidth = serializedObject.FindProperty("backgroundTextureWidth");
			backgroundTextureHeight = serializedObject.FindProperty("backgroundTextureHeight");

			userHeightMapTexture = serializedObject.FindProperty("userHeightMapTexture");
			heightmapWidth = serializedObject.FindProperty("heightMapWidth");
			heightmapHeight = serializedObject.FindProperty("heightMapHeight");

			octavesBySeed = serializedObject.FindProperty("octavesBySeed");
			noiseOctaves = serializedObject.FindProperty("noiseOctaves");
			seaColor = serializedObject.FindProperty("seaColor");
			seaLevel = serializedObject.FindProperty("seaLevel");
			noisePower = serializedObject.FindProperty("noisePower");

			heightGradient = serializedObject.FindProperty("heightGradient");
			heightGradientPreset = serializedObject.FindProperty("heightGradientPreset");
			gradientPerPixel = serializedObject.FindProperty("gradientPerPixel");
			changeStyle = serializedObject.FindProperty("changeStyle");

			islandFactor = serializedObject.FindProperty("islandFactor");
			elevationShift = serializedObject.FindProperty("elevationShift");

			gridRelaxation = serializedObject.FindProperty("gridRelaxation");
			edgeMaxLength = serializedObject.FindProperty("edgeMaxLength");
			edgeNoise = serializedObject.FindProperty("edgeNoise");

			numCitiesPerCountryMin = serializedObject.FindProperty("numCitiesPerCountryMin");
			numCitiesPerCountryMax = serializedObject.FindProperty("numCitiesPerCountryMax");

			generateNormalMap = serializedObject.FindProperty("generateNormalMap");
			normalMapBumpiness = serializedObject.FindProperty("normalMapBumpiness");
		}

		public bool ShowMapGeneratorOptions()
		{
			serializedObject.Update();

			EditorGUILayout.Separator();
			EditorGUIUtility.labelWidth = 120;

			EditorGUILayout.Separator();
			DrawTitleLabel("General Settings");

			EditorGUI.BeginChangeCheck();
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PropertyField(seed);
			if (GUILayout.Button("<", GUILayout.Width(20)))
				if (seed.intValue > 0)
					seed.intValue--;
			if (GUILayout.Button(">", GUILayout.Width(20)))
				if (seed.intValue < 10000)
					seed.intValue++;
			EditorGUILayout.EndHorizontal();

			var requestNewHeightMap = false;
			if (EditorGUI.EndChangeCheck())
			{
				serializedObject.ApplyModifiedProperties();
				_editor.ApplySeed();
				serializedObject.Update();
				requestNewHeightMap = true;
			}

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PropertyField(seedNames);
			if (GUILayout.Button("<", GUILayout.Width(20)))
				if (seedNames.intValue > 0)
					seedNames.intValue--;
			if (GUILayout.Button(">", GUILayout.Width(20)))
				if (seedNames.intValue < 10000)
					seedNames.intValue++;
			EditorGUILayout.EndHorizontal();

			var requestGeneration = false;

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PropertyField(outputFolder);
			if (GUILayout.Button("Open"))
				EditorUtility.RevealInFinder(_editor.GetGenerationMapOutputPath());
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.HelpBox(
				"Map data, including heightMap, background and water mask textures, will be stored in Resources/WMSK/Geodata/" +
				outputFolder.stringValue +
				" folder.", MessageType.Info);

			EditorGUILayout.PropertyField(mapGenerationQuality, new GUIContent("Generation Quality"));
			generateButtonStyle = new GUIStyle(GUI.skin.button);
			generateButtonStyle.normal.textColor = Color.yellow;
			generateButtonStyle.fontStyle = FontStyle.Bold;
			generateButtonStyle.fixedHeight = 30;

			if (GUILayout.Button("Generate & Save Map", generateButtonStyle))
				requestGeneration = true;

			EditorGUILayout.Separator();

			DrawTitleLabel("Provinces");
			EditorGUILayout.PropertyField(numProvinces, new GUIContent("Count"));

			EditorGUILayout.Separator();

			DrawTitleLabel("Borders");

			EditorGUILayout.BeginHorizontal();
			if (numProvinces.intValue > WMSK_Editor.MAX_CELLS_FOR_RELAXATION)
			{
				GUILayout.Label("Relaxation", GUILayout.Width(120));
				DrawInfoLabel("not available with >" + WMSK_Editor.MAX_CELLS_FOR_RELAXATION + " cells");
			}
			else
			{
				EditorGUILayout.PropertyField(gridRelaxation, new GUIContent("Homogeneity"));
				if (GUILayout.Button("Reset", GUILayout.Width(80)))
					gridRelaxation.intValue = 1;
			}
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.PropertyField(edgeNoise, new GUIContent("Edge Noise"));
			GUI.enabled = edgeNoise.floatValue > 0;
			EditorGUILayout.PropertyField(edgeMaxLength, new GUIContent("Edge Max Length"));
			GUI.enabled = true;

			EditorGUILayout.Separator();

			DrawTitleLabel("Countries");
			EditorGUILayout.PropertyField(numCountries, new GUIContent("Count"));

			EditorGUILayout.Separator();

			DrawTitleLabel("Cities");

			EditorGUILayout.PropertyField(numCitiesPerCountryMin, new GUIContent("Min Per Country"));
			EditorGUILayout.PropertyField(numCitiesPerCountryMax, new GUIContent("Max Per Country"));

			EditorGUILayout.Separator();

			DrawTitleLabel("Height Map");

			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField(userHeightMapTexture,
				new GUIContent("Texture", "Optional heightmap texture"));
			if (EditorGUI.EndChangeCheck())
			{
				var tex = (Texture2D)userHeightMapTexture.objectReferenceValue;
				if (tex != null)
				{
					var path = AssetDatabase.GetAssetPath(tex);
					var texImp = (TextureImporter)AssetImporter.GetAtPath(path);
					if (!texImp.isReadable)
					{
						texImp.isReadable = true;
						texImp.SaveAndReimport();
					}
					heightmapWidth.intValue = tex.width;
					heightmapHeight.intValue = tex.height;
				}
			}

			if (userHeightMapTexture.objectReferenceValue == null)
			{
				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField(heightmapWidth);
				EditorGUILayout.PropertyField(heightmapHeight);
				EditorGUILayout.PropertyField(octavesBySeed,
					new GUIContent("Random Octaves", "Generate random octaves by seed."));
				EditorGUILayout.PropertyField(noiseOctaves, true);
				EditorGUILayout.PropertyField(islandFactor);
				EditorGUILayout.PropertyField(noisePower);
				EditorGUILayout.PropertyField(elevationShift);
				if (EditorGUI.EndChangeCheck())
					requestNewHeightMap = true;
			}
			var generatedHeightMapTexture = _editor.heightMapTexture;
			if (generatedHeightMapTexture != null)
			{
				var space = EditorGUILayout.BeginVertical();
				var aspect = (float)generatedHeightMapTexture.height / generatedHeightMapTexture.width;
				GUILayout.Space(EditorGUIUtility.currentViewWidth * aspect);
				EditorGUILayout.EndVertical();
				if (previewMat == null)
					previewMat = new Material(Shader.Find("WMSK/Editor/DepthTexPreview"));
				previewMat.mainTexture = generatedHeightMapTexture;
				previewMat.color = seaColor.colorValue;
				previewMat.SetFloat("_SeaLevel", seaLevel.floatValue);
				EditorGUI.DrawPreviewTexture(space, generatedHeightMapTexture, previewMat,
					ScaleMode.StretchToFill);
			}

			EditorGUILayout.Separator();

			EditorGUILayout.PropertyField(seaLevel);
			EditorGUILayout.PropertyField(seaColor);

			EditorGUILayout.Separator();

			DrawTitleLabel("World Textures");
			EditorGUILayout.PropertyField(generateNormalMap, new GUIContent("Normal Map"));
			if (generateNormalMap.boolValue)
				EditorGUILayout.PropertyField(normalMapBumpiness, new GUIContent("   Bumpiness"));
			EditorGUILayout.PropertyField(heightGradientPreset, new GUIContent("Land Colors"));
			if (heightGradientPreset.intValue == (int)HeightMapGradientPreset.Custom)
				EditorGUILayout.PropertyField(heightGradient, new GUIContent("Gradient"));
			EditorGUILayout.PropertyField(gradientPerPixel,
				new GUIContent("Per Pixel Gradient",
					"Apply height gradient per pixel instead of per province."));
			if (heightGradientPreset.intValue != (int)HeightMapGradientPreset.Custom)
				EditorGUILayout.PropertyField(changeStyle,
					new GUIContent("Apply Style",
						"Change frontier and label colors to match selected land colors preset. Disable to keep your current settings."));
			EditorGUILayout.PropertyField(backgroundTextureWidth, new GUIContent("Width"));
			EditorGUILayout.PropertyField(backgroundTextureHeight, new GUIContent("Height"));

			serializedObject.ApplyModifiedProperties();

			if (requestGeneration)
			{
				_editor.GenerateMap();
				return true;
			}
			else if (requestNewHeightMap)
			{
				_editor.GenerateHeightMap(true);
				return true;
			}

			return false;
		}

		#region Utility functions

		private void DrawTitleLabel(string s)
		{
			if (titleLabelStyle == null)
				titleLabelStyle = new GUIStyle(GUI.skin.label);
			titleLabelStyle.normal.textColor = EditorGUIUtility.isProSkin
				? new Color(0.52f, 0.66f, 0.9f)
				: new Color(0.22f, 0.33f, 0.6f);
			titleLabelStyle.fontStyle = FontStyle.Bold;
			GUILayout.Label(s, titleLabelStyle);
		}

		private void DrawInfoLabel(string s)
		{
			if (infoLabelStyle == null)
				infoLabelStyle = new GUIStyle(GUI.skin.label);
			infoLabelStyle.normal.textColor = EditorGUIUtility.isProSkin
				? new Color(0.76f, 0.52f, 0.52f)
				: new Color(0.46f, 0.22f, 0.22f);
			GUILayout.Label(s, infoLabelStyle);
		}

		private bool CheckTextureImportSettings(Texture2D tex)
		{
			if (tex == null)
				return false;
			var path = AssetDatabase.GetAssetPath(tex);
			var imp = (TextureImporter)AssetImporter.GetAtPath(path);
			if (!imp.isReadable)
			{
				EditorGUILayout.HelpBox("Texture is not readable. Fix it?", MessageType.Warning);
				if (GUILayout.Button("Fix texture import setting"))
				{
					imp.isReadable = true;
					imp.SaveAndReimport();
					return true;
				}
			}
			return false;
		}

		#endregion
	}
}