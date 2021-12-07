using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;

namespace WorldMapStrategyKit
{
	public partial class WMSKEditorInspector
	{
		private static void CheckBackup(out string geoDataFolder, bool replaceBackup)
		{
			var paths = AssetDatabase.GetAllAssetPaths();
			var backupFolderExists = false;
			var rootFolder = "";
			geoDataFolder = "";
			for (var k = 0; k < paths.Length; k++)
				if (paths[k].EndsWith(WMSK.instance.geodataResourcesPath))
					geoDataFolder = paths[k];
				else if (paths[k].EndsWith("WorldMapStrategyKit"))
					rootFolder = paths[k];
				else if (paths[k].EndsWith("WorldMapStrategyKit/Backup"))
					backupFolderExists = true;

			if (!backupFolderExists || replaceBackup)
			{
				// Do the backup
				string fullFileName;
				AssetDatabase.CreateFolder(rootFolder, "Backup");
				var backupFolder = rootFolder + "/Backup";
				fullFileName = geoDataFolder + "/countries110.txt";
				if (File.Exists(fullFileName))
					AssetDatabase.CopyAsset(fullFileName, backupFolder + "/countries110.txt");
				fullFileName = geoDataFolder + "/countries10.txt";
				if (File.Exists(fullFileName))
					AssetDatabase.CopyAsset(fullFileName, backupFolder + "/countries10.txt");
				fullFileName = geoDataFolder + "/" + WMSK.instance.countryAttributeFile;
				if (File.Exists(fullFileName))
					AssetDatabase.CopyAsset(fullFileName,
						backupFolder + "/" + WMSK.instance.countryAttributeFile + ".json");
				fullFileName = geoDataFolder + "/provinces10.txt";
				if (File.Exists(fullFileName))
					AssetDatabase.CopyAsset(fullFileName, backupFolder + "/provinces10.txt");
				fullFileName = geoDataFolder + "/" + WMSK.instance.provinceAttributeFile;
				if (File.Exists(fullFileName))
					AssetDatabase.CopyAsset(fullFileName,
						backupFolder + "/" + WMSK.instance.provinceAttributeFile + ".json");
				fullFileName = geoDataFolder + "/cities10.txt";
				if (File.Exists(fullFileName))
					AssetDatabase.CopyAsset(fullFileName, backupFolder + "/cities10.txt");
				fullFileName = geoDataFolder + "/" + WMSK.instance.cityAttributeFile;
				if (File.Exists(fullFileName))
					AssetDatabase.CopyAsset(fullFileName,
						backupFolder + "/" + WMSK.instance.cityAttributeFile + ".json");
				fullFileName = geoDataFolder + "/mountPoints.json";
				if (File.Exists(fullFileName))
					AssetDatabase.CopyAsset(fullFileName, backupFolder + "/mountPoints.json");
			}
		}

		private string GetAssetsFolder()
		{
			var fullPathName = Application.dataPath;
			var pos = fullPathName.LastIndexOf("/Assets");
			if (pos > 0)
				fullPathName = fullPathName.Substring(0, pos + 1);
			return fullPathName;
		}

		private bool SaveChanges()
		{
			if (Application.isPlaying)
			{
				// preserve changes done at runtime not detected by Editor
				_editor.countryChanges = true;
				_editor.countryAttribChanges = true;
				_editor.provinceChanges = true;
				_editor.provinceAttribChanges = true;
				_editor.cityChanges = true;
				_editor.cityAttribChanges = true;
				_editor.mountPointChanges = true;
			}

			if (!_editor.countryChanges &&
			    !_editor.provinceChanges &&
			    !_editor.cityChanges &&
			    !_editor.mountPointChanges &&
			    !_editor.countryAttribChanges &&
			    !_editor.provinceAttribChanges &&
			    !_editor.cityAttribChanges)
				return false;

			// First we make a backup if it doesn't exist
			string geoDataFolder;
			CheckBackup(out geoDataFolder, false);

			string dataFileName, fullPathName;
			// Save changes to countries
			if (_editor.countryChanges)
			{
				dataFileName = _editor.GetCountryGeoDataFileName();
				fullPathName = GetAssetsFolder() + geoDataFolder + "/" + dataFileName;
				var data = _map.GetCountryGeoData();
				File.WriteAllText(fullPathName, data, Encoding.UTF8);
				_editor.countryChanges = false;
			}
			// Save changes to country attributes
			if (_editor.countryAttribChanges)
			{
				fullPathName = GetAssetsFolder() +
				               geoDataFolder +
				               "/" +
				               _map.countryAttributeFile +
				               ".json";
				var data = _map.GetCountriesAttributes(true);
				File.WriteAllText(fullPathName, data, Encoding.UTF8);
				_editor.countryAttribChanges = false;
			}
			// Save changes to provinces
			if (_editor.provinceChanges)
			{
				dataFileName = _editor.GetProvinceGeoDataFileName();
				fullPathName = GetAssetsFolder();
				var fullAssetPathName = fullPathName + geoDataFolder + "/" + dataFileName;
				var data = _map.GetProvinceGeoData();
				File.WriteAllText(fullAssetPathName, data, Encoding.UTF8);
				_editor.provinceChanges = false;
			}
			// Save changes to province attributes
			if (_editor.provinceAttribChanges)
			{
				fullPathName = GetAssetsFolder() +
				               geoDataFolder +
				               "/" +
				               _map.provinceAttributeFile +
				               ".json";
				var data = _map.GetProvincesAttributes(true);
				File.WriteAllText(fullPathName, data, Encoding.UTF8);
				_editor.provinceAttribChanges = false;
			}
			// Save changes to cities
			if (_editor.cityChanges)
			{
				_editor.FixOrphanCities();
				dataFileName = _editor.GetCityGeoDataFileName();
				fullPathName = GetAssetsFolder() + geoDataFolder + "/" + dataFileName;
				File.WriteAllText(fullPathName, _map.GetCityGeoData(), Encoding.UTF8);
				_editor.cityChanges = false;
			}
			// Save changes to cities attributes
			if (_editor.cityAttribChanges)
			{
				fullPathName = GetAssetsFolder() + geoDataFolder + "/" + _map.cityAttributeFile + ".json";
				var data = _map.GetCitiesAttributes(true);
				File.WriteAllText(fullPathName, data, Encoding.UTF8);
				_editor.cityAttribChanges = false;
			}
			// Save changes to mount points
			if (_editor.mountPointChanges)
			{
				dataFileName = _editor.GetMountPointGeoDataFileName();
				fullPathName = GetAssetsFolder() + geoDataFolder + "/" + dataFileName;
				File.WriteAllText(fullPathName, _map.GetMountPointsGeoData(), Encoding.UTF8);
				_editor.mountPointChanges = false;
			}
			AssetDatabase.Refresh();
			return true;
		}

		private static void ExportProvincesMap(string outputFile)
		{
			var map = WMSK.instance;
			if (map == null)
				return;

			// Get all triangles and its colors
			const int width = 8192;
			const int height = 4096;
			var texture = new Texture2D(width, height, TextureFormat.ARGB32, false);
			var colors = new Color[width * height];

			var provincesCount = map.provinces.Length;
			var provinceColors = new HashSet<Color>();

//			float maxEdge = width * 0.8f;
//			float minEdge = width * 0.2f;

			for (var prov = 0; prov < provincesCount; prov++)
			{
				Color color;
				do
				{
					var g = UnityEngine.Random.Range(0.1f, 1f); // avoids full black (used by background)
					color = new Color(UnityEngine.Random.value, g, UnityEngine.Random.value);
				} while (provinceColors.Contains(color));
				provinceColors.Add(color);
				var province = map.provinces[prov];
				if (province.regions == null)
					map.ReadProvincePackedString(province);
				var regionsCount = province.regions.Count;
				for (var pr = 0; pr < regionsCount; pr++)
				{
					var surf = map.ToggleProvinceRegionSurface(prov, pr, true, color);
					// Get triangles and paint over the texture
					var mf = surf.GetComponent<MeshFilter>();
					if (mf == null || mf.sharedMesh.GetTopology(0) != MeshTopology.Triangles)
						continue;
					var vertex = mf.sharedMesh.vertices;
					var index = mf.sharedMesh.GetTriangles(0);

					for (var i = 0; i < index.Length; i += 3)
					{
						var p1 = Conversion.ConvertToTextureCoordinates(vertex[index[i]], width, height);
						var p2 = Conversion.ConvertToTextureCoordinates(vertex[index[i + 1]], width,
							height);
						var p3 = Conversion.ConvertToTextureCoordinates(vertex[index[i + 2]], width,
							height);
						// Sort points
						if (p2.x > p3.x)
						{
							Vector3 p = p2;
							p2 = p3;
							p3 = p;
						}
						if (p1.x > p2.x)
						{
							Vector3 p = p1;
							p1 = p2;
							p2 = p;
							if (p2.x > p3.x)
							{
								p = p2;
								p2 = p3;
								p3 = p;
							}
						}
//						if (p1.x < minEdge && p2.x < minEdge && p3.x > maxEdge) {
//							if (p1.x < 1 && p2.x < 1) {
//								p1.x = width - p1.x;
//								p2.x = width - p2.x;
//							} else
//								p3.x = width - p3.x;
//						} else if (p1.x < minEdge && p2.x > maxEdge && p3.x > maxEdge) {
//							p1.x = width + p1.x;
//						} 
						Drawing.DrawTriangle(colors, width, height, p1, p2, p3, color);
					}
				}
			}
			texture.SetPixels(colors);
			texture.Apply();

			if (File.Exists(outputFile))
				File.Delete(outputFile);
			File.WriteAllBytes(outputFile, texture.EncodeToPNG());
			AssetDatabase.Refresh();

			map.HideProvinceSurfaces();
			var imp = (TextureImporter)AssetImporter.GetAtPath(outputFile);
			if (imp != null)
			{
				imp.maxTextureSize = 8192;
				imp.SaveAndReimport();
			}
		}
	}
}