using System.IO;
using UnityEditor;
using UnityEngine;

namespace WorldMapStrategyKit
{
	[ExecuteInEditMode]
	public class WMSKTerrainImporter : EditorWindow
	{
		private const string DEFAULT_HEIGHTMAP_FILENAME = "/terrain_heightmap.png";
		private const string DEFAULT_TERRAIN_TEXTURE_FILENAME = "/terrain_texture.png";

		private enum TerrainResolution
		{
			Low_1024x1024,
			Medium_2048x2048,
			High_4096x4096,
			Maximum_8192x8192
		}

		private Terrain terrain;
		private TerrainResolution resolution;
		private bool rotate180;

		public static void ShowWindow()
		{
			var w = 400;
			var h = 180;
			var rect = new Rect(Screen.currentResolution.width / 2 - w / 2,
				Screen.currentResolution.height / 2 - h / 2, w, h);
			var window = GetWindowWithRect<WMSKTerrainImporter>(rect, true, "Terrain Importer", true);
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
				"This tool will import the heightmap and combined texture of an existing terrain.",
				MessageType.Info);
			EditorGUILayout.Separator();
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Terrain", GUILayout.Width(120));
			terrain = (Terrain)EditorGUILayout.ObjectField(terrain, typeof(Terrain), true);
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Resolution", GUILayout.Width(120));
			resolution = (TerrainResolution)EditorGUILayout.EnumPopup(resolution);
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Rotate 180°", GUILayout.Width(120));
			rotate180 = EditorGUILayout.Toggle(rotate180);
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.Separator();
			EditorGUILayout.Separator();
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.HelpBox("This operation can take some time. Please wait until it finishes.",
				MessageType.Warning);
			if (terrain == null)
				GUI.enabled = false;
			if (GUILayout.Button("Start"))
				ImportTerrain();
			GUI.enabled = true;
			if (GUILayout.Button("Cancel"))
				Close();
		}

		private void OnDestroy()
		{
			WMSK.instance.editor.terrainImporterActive = false;
		}

		private void ImportTerrain()
		{
			if (terrain == null)
			{
				EditorUtility.DisplayDialog("Import Terrain Features",
					"Please select an existing terrain first.", "Ok");
				return;
			}

			// Extract heightmap
			var terrainData = terrain.terrainData;
			const int hmWidth = 2048;
			const int hmHeight = 1024;
			var heights = new float[hmWidth * hmHeight];
			var maxHeight = float.MinValue;
			var index = 0;
			if (rotate180)
				for (var j = 0; j < hmHeight; j++)
				{
					var z = 1.0f - (float)j / hmHeight;
					for (var k = 0; k < hmWidth; k++)
					{
						var x = 1.0f - (float)k / hmWidth;
						var h = terrain.terrainData.GetInterpolatedHeight(x, z);
						heights[index++] = h;
						if (h > maxHeight)
							maxHeight = h;
					}
				}
			else
				for (var j = 0; j < hmHeight; j++)
				{
					var z = (float)j / hmHeight;
					for (var k = 0; k < hmWidth; k++)
					{
						var x = (float)k / hmWidth;
						var h = terrain.terrainData.GetInterpolatedHeight(x, z);
						heights[index++] = h;
						if (h > maxHeight)
							maxHeight = h;
					}
				}

			var colors = new Color[heights.Length];
			for (var k = 0; k < heights.Length; k++)
			{
				var h = heights[k] / maxHeight;
				colors[k] = new Color(h, h, h, 1.0f);
			}

			var hm = new Texture2D(hmWidth, hmHeight, TextureFormat.ARGB32, false);
			hm.SetPixels(colors);
			hm.Apply();

			// Save heightmap
			var wmskResourcesPath = GetWMSKResourcesPath();
			var texPath = wmskResourcesPath + "/Terrain";
			Directory.CreateDirectory(texPath);

			var bytes = hm.EncodeToPNG();
			var terrainHeightMapFullFilename = texPath + DEFAULT_HEIGHTMAP_FILENAME;
			File.WriteAllBytes(terrainHeightMapFullFilename, bytes);

			// Snapshot terrain
			const int snapshotLayer = 21;
			var oldLayer = terrain.gameObject.layer;
			var oldPos = terrain.gameObject.transform.position;
			var lightTransform = GetSceneLight();
			var oldLightRot = lightTransform.rotation;

			// Setup terrain
			terrain.gameObject.layer = snapshotLayer;
			terrain.transform.position = new Vector3(-1000 - terrainData.size.x / 2, -10000,
				-1000 - terrainData.size.z / 2);
			// Setup lighting
			lightTransform.rotation = Misc.QuaternionX90;

			// Create snapshot cam
			var camGO = new GameObject("SnapshotCam");
			var cam = camGO.AddComponent<Camera>();
			cam.orthographic = true;
			var maxSize = Mathf.Max(terrainData.size.x, terrainData.size.z);
			cam.orthographicSize = maxSize / 2;
			var camh = 10 + maxHeight * terrainData.size.y;
			cam.transform.position = new Vector3(-1000, -10000 + camh, -1000);
			cam.transform.rotation = Quaternion.Euler(90, 0, rotate180 ? 180 : 0);
			cam.farClipPlane = camh + 1f;

			int texSize;
			switch (resolution)
			{
				case TerrainResolution.Low_1024x1024:
					texSize = 1024;
					break;
				case TerrainResolution.High_4096x4096:
					texSize = 4096;
					break;
				case TerrainResolution.Maximum_8192x8192:
					texSize = 8192;
					break;
				default:
					texSize = 2048;
					break;
			}
			var rt = new RenderTexture(texSize, texSize, 24, RenderTextureFormat.ARGB32);
			var terrainTex = new Texture2D(texSize, texSize, TextureFormat.ARGB32, false);
			cam.targetTexture = rt;
			cam.cullingMask = 1 << snapshotLayer;
			cam.Render();

			// Obtain result texture
			RenderTexture.active = rt;
			terrainTex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
			terrainTex.Apply();

			// Clean up
			cam.targetTexture = null;
			RenderTexture.active = null;
			rt.Release();
			DestroyImmediate(camGO);
			if (lightTransform.gameObject.name.Equals("WMSK Temporary Light"))
				DestroyImmediate(lightTransform.gameObject);

			lightTransform.rotation = oldLightRot;
			terrain.gameObject.layer = oldLayer;
			terrain.transform.position = oldPos;

			// Save terrain texture
			bytes = terrainTex.EncodeToPNG();
			var terrainTextureFullFilename = texPath + DEFAULT_TERRAIN_TEXTURE_FILENAME;
			File.WriteAllBytes(terrainTextureFullFilename, bytes);

			AssetDatabase.Refresh();

			var texImp = (TextureImporter)AssetImporter.GetAtPath(terrainHeightMapFullFilename);
			texImp.isReadable = true;
			texImp.SaveAndReimport();

			texImp = (TextureImporter)AssetImporter.GetAtPath(terrainTextureFullFilename);
			texImp.isReadable = true;
			texImp.SaveAndReimport();

			// Assign imported terrain
			WMSK.instance.earthStyle = EARTH_STYLE.Texture;
			var resourceEarthTex = "WMSK/Terrain/" +
			                       Path.GetFileNameWithoutExtension(
				                       DEFAULT_TERRAIN_TEXTURE_FILENAME);
			WMSK.instance.earthTexture = Resources.Load<Texture2D>(resourceEarthTex);
			var resourceHeightmap = "WMSK/Terrain/" +
			                        Path.GetFileNameWithoutExtension(DEFAULT_HEIGHTMAP_FILENAME);
			WMSK.instance.heightMapTexture = Resources.Load<Texture2D>(resourceHeightmap);
			EditorUtility.SetDirty(WMSK.instance);
			EditorUtility.DisplayDialog("Terrain Import Complete",
				"Heightmap and Earth texture created at Resources/" + texPath, "Ok");
			Close();
		}

		private string GetWMSKResourcesPath()
		{
			var paths = AssetDatabase.GetAllAssetPaths();
			for (var k = 0; k < paths.Length; k++)
				if (paths[k].EndsWith("Resources/WMSK/Textures"))
					return Path.GetDirectoryName(paths[k]); // Get parent of directory
			return "";
		}

		private Transform GetSceneLight()
		{
			if (WMSK.instance.sun != null)
				return WMSK.instance.transform;
			var lights = FindObjectsOfType<Light>();
			for (var k = 0; k < lights.Length; k++)
				if (lights[k].type == LightType.Directional)
					return lights[k].transform;
			var lightGO = new GameObject("WMSK Temporary Light", typeof(Light));
			return lightGO.transform;
		}
	}
}