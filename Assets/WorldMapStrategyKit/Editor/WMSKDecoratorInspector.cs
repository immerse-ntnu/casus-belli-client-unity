using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine.SceneManagement;

namespace WorldMapStrategyKit
{
	[CustomEditor(typeof(WMSK_Decorator))]
	public class WMSKDecoratorInspector : Editor
	{
		private WMSK_Decorator _decorator;
		private string[] groupNames;
		private CountryDecorator decorator;
		private CountryDecoratorGroupInfo decoratorGroup;
		private Vector3 oldMapPosition;
		private Quaternion oldMapRotation;
		private bool zoomState;

		private WMSK _map => _decorator.map;

		private void OnEnable()
		{
			_decorator = (WMSK_Decorator)target;
			groupNames = new string[WMSK_Decorator.NUM_GROUPS];
			ReloadGroupNames();
			_decorator.ReloadCountryNames();
		}

		public override void OnInspectorGUI()
		{
			if (_decorator == null)
				return;

			var requestChanges = false;

			EditorGUILayout.Separator();
			EditorGUILayout.BeginVertical();

			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Group", GUILayout.Width(120));
			var oldGroup = _decorator.GUIGroupIndex;
			_decorator.GUIGroupIndex =
				EditorGUILayout.Popup(_decorator.GUIGroupIndex, groupNames, GUILayout.MaxWidth(200));
			if (_decorator.GUIGroupIndex != oldGroup || decoratorGroup == null)
				decoratorGroup = _decorator.GetDecoratorGroup(_decorator.GUIGroupIndex, true);

			if (GUILayout.Button("Clear"))
			{
				_decorator.ClearDecoratorGroup(_decorator.GUIGroupIndex);
				if (decorator != null)
					decorator.isNew = true;
				ReloadGroupNames();
				_decorator.ReloadCountryNames();
			}

			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("   Enabled", GUILayout.Width(120));
			decoratorGroup.active = EditorGUILayout.Toggle(decoratorGroup.active, GUILayout.Width(20));
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.Separator();

			// country selector
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Country", GUILayout.Width(120));
			if (_decorator.lastCountryCount != _map.countries.Length)
				_decorator.ReloadCountryNames();
			var selection = EditorGUILayout.Popup(_decorator.GUICountryIndex, _decorator.countryNames);
			if (selection != _decorator.GUICountryIndex)
				SetCountryFromCombo(selection);

			var prevc = _decorator.groupByContinent;
			GUILayout.Label("Grouped");
			_decorator.groupByContinent =
				EditorGUILayout.Toggle(_decorator.groupByContinent, GUILayout.Width(20));
			if (_decorator.groupByContinent != prevc)
				_decorator.ReloadCountryNames();

			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("", GUILayout.Width(120));
			DrawWarningLabel("(Click on SceneView to select a country)");
			EditorGUILayout.EndHorizontal();

			// type of decoration
			if (_decorator.GUICountryName.Length > 0)
			{
				EditorGUILayout.BeginHorizontal();
				GUILayout.Label("", GUILayout.Width(120));
				if (GUILayout.Button("Toggle Zoom"))
					ToggleZoomState();
				EditorGUILayout.EndHorizontal();

				var existingDecorator =
					_decorator.GetCountryDecorator(_decorator.GUIGroupIndex, _decorator.GUICountryName);
				if (existingDecorator != null)
					decorator = existingDecorator;
				else if (decorator == null || !decorator.countryName.Equals(_decorator.GUICountryName))
					decorator = new CountryDecorator(_decorator.GUICountryName);

				EditorGUILayout.BeginHorizontal();
				GUILayout.Label(
					new GUIContent("Persistent",
						"The attributes set by this decorator will replace any other change done at runtime"),
					GUILayout.Width(120));
				decorator.isPersistent = EditorGUILayout.Toggle(decorator.isPersistent);
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.BeginHorizontal();
				GUILayout.Label("Hidden", GUILayout.Width(120));
				var prevHidden = decorator.hidden;
				decorator.hidden = EditorGUILayout.Toggle(decorator.hidden);
				if (prevHidden != decorator.hidden)
					requestChanges = true;
				EditorGUILayout.EndHorizontal();

				if (!decorator.hidden)
				{
					EditorGUILayout.BeginHorizontal();
					GUILayout.Label("Label Visible", GUILayout.Width(120));
					var prevLabelVisible = decorator.labelVisible;
					decorator.labelVisible = EditorGUILayout.Toggle(decorator.labelVisible);
					if (prevLabelVisible != decorator.labelVisible)
						requestChanges = true;
					EditorGUILayout.EndHorizontal();

					if (decorator.labelVisible)
					{
						EditorGUILayout.BeginHorizontal();
						GUILayout.Label("   Custom Label", GUILayout.Width(120));
						decorator.labelOverride =
							EditorGUILayout.Toggle(decorator.labelOverride, GUILayout.Width(40));
						if (decorator.labelOverride)
						{
							GUILayout.Label("Text", GUILayout.Width(40));
							var prevLabel = decorator.customLabel;
							decorator.customLabel = EditorGUILayout.TextField(decorator.customLabel);
							if (!prevLabel.Equals(decorator.customLabel))
								requestChanges = true;
						}
						EditorGUILayout.EndHorizontal();

						EditorGUILayout.BeginHorizontal();
						GUILayout.Label("   Font", GUILayout.Width(120));
						var prevFont = decorator.labelFontOverride;
						decorator.labelFontOverride =
							(Font)EditorGUILayout.ObjectField(decorator.labelFontOverride, typeof(Font),
								false);
						if (decorator.labelFontOverride != prevFont)
							requestChanges = true;
						EditorGUILayout.EndHorizontal();

						EditorGUILayout.BeginHorizontal();
						GUILayout.Label("   Custom Color", GUILayout.Width(120));
						decorator.labelOverridesColor =
							EditorGUILayout.Toggle(decorator.labelOverridesColor, GUILayout.Width(40));
						if (decorator.labelOverridesColor)
						{
							GUILayout.Label("Color", GUILayout.Width(40));
							var prevColor = decorator.labelColor;
							decorator.labelColor =
								EditorGUILayout.ColorField(decorator.labelColor, GUILayout.Width(50));
							if (prevColor != decorator.labelColor)
								requestChanges = true;
						}
						EditorGUILayout.EndHorizontal();

						EditorGUILayout.BeginHorizontal();
						GUILayout.Label("   Custom Size", GUILayout.Width(120));
						decorator.labelOverridesFontSize =
							EditorGUILayout.Toggle(decorator.labelOverridesFontSize, GUILayout.Width(40));
						if (decorator.labelOverridesFontSize)
						{
							GUILayout.Label("Size", GUILayout.Width(40));
							var prevSize = decorator.labelFontSize;
							decorator.labelFontSize = Mathf.Max(0.01f,
								EditorGUILayout.FloatField(decorator.labelFontSize));
							if (prevSize != decorator.labelFontSize)
								requestChanges = true;
						}
						EditorGUILayout.EndHorizontal();

						EditorGUILayout.BeginHorizontal();
						GUILayout.Label("   Offset", GUILayout.Width(120));
						var prevLabelOffset = decorator.labelOffset;
						decorator.labelOffset = EditorGUILayout.Vector2Field("", decorator.labelOffset);
						if (prevLabelOffset != decorator.labelOffset)
							requestChanges = true;
						EditorGUILayout.EndHorizontal();

						EditorGUILayout.BeginHorizontal();
						GUILayout.Label("   Rotation", GUILayout.Width(120));
						var prevLabelRotation = decorator.labelRotation;
						decorator.labelRotation = EditorGUILayout.Slider(decorator.labelRotation, 0, 359);
						if (prevLabelRotation != decorator.labelRotation)
							requestChanges = true;
						EditorGUILayout.EndHorizontal();
					}

					EditorGUILayout.BeginHorizontal();
					GUILayout.Label("Colorized", GUILayout.Width(120));
					var prevColorized = decorator.isColorized;
					decorator.isColorized = EditorGUILayout.Toggle(decorator.isColorized);
					if (prevColorized != decorator.isColorized)
						requestChanges = true;
					if (decorator.isColorized)
					{
						GUILayout.Label("Fill Color", GUILayout.Width(120));
						var prevColor = decorator.fillColor;
						decorator.fillColor =
							EditorGUILayout.ColorField(decorator.fillColor, GUILayout.Width(50));
						if (prevColor != decorator.fillColor)
							requestChanges = true;
						EditorGUILayout.EndHorizontal();

						EditorGUILayout.BeginHorizontal();
						GUILayout.Label("   Include All Regions", GUILayout.Width(120));
						var prevIncludeAllRegions = decorator.includeAllRegions;
						decorator.includeAllRegions = EditorGUILayout.Toggle(decorator.includeAllRegions);
						if (prevIncludeAllRegions != decorator.includeAllRegions)
							requestChanges = true;
						EditorGUILayout.EndHorizontal();

						EditorGUILayout.BeginHorizontal();
						GUILayout.Label("Texture", GUILayout.Width(120));
						var prevTexture = decorator.texture;
						decorator.texture =
							(Texture2D)EditorGUILayout.ObjectField(decorator.texture, typeof(Texture2D),
								false);
						if (decorator.texture != prevTexture)
							requestChanges = true;

						if (decorator.texture != null)
						{
							EditorGUILayout.EndHorizontal();
							EditorGUILayout.BeginHorizontal();
							GUILayout.Label("   Include All Regions", GUILayout.Width(120));
							prevIncludeAllRegions = decorator.applyTextureToAllRegions;
							decorator.applyTextureToAllRegions =
								EditorGUILayout.Toggle(decorator.applyTextureToAllRegions);
							if (prevIncludeAllRegions != decorator.applyTextureToAllRegions)
								requestChanges = true;
							EditorGUILayout.EndHorizontal();
							EditorGUILayout.BeginHorizontal();
							GUILayout.Label("   Scale", GUILayout.Width(120));
							var prevVector = decorator.textureScale;
							decorator.textureScale =
								EditorGUILayout.Vector2Field("", decorator.textureScale);
							if (prevVector != decorator.textureScale)
								requestChanges = true;
							EditorGUILayout.EndHorizontal();

							EditorGUILayout.BeginHorizontal();
							GUILayout.Label("   Offset", GUILayout.Width(120));
							prevVector = decorator.textureOffset;
							decorator.textureOffset =
								EditorGUILayout.Vector2Field("", decorator.textureOffset);
							if (prevVector != decorator.textureOffset)
								requestChanges = true;
							EditorGUILayout.EndHorizontal();

							EditorGUILayout.BeginHorizontal();
							GUILayout.Label("   Rotation", GUILayout.Width(120));
							var prevFloat = decorator.textureRotation;
							decorator.textureRotation =
								EditorGUILayout.Slider(decorator.textureRotation, 0, 360);
							if (prevFloat != decorator.textureRotation)
								requestChanges = true;
						}
						EditorGUILayout.EndHorizontal();
					}
					else
						EditorGUILayout.EndHorizontal();
				}
				EditorGUILayout.BeginHorizontal();
				if (decorator.isNew)
				{
					if (GUILayout.Button("Assign"))
					{
						_decorator.SetCountryDecorator(_decorator.GUIGroupIndex, _decorator.GUICountryName,
							decorator);
						ReloadGroupNames();
						_decorator.ReloadCountryNames();
					}
				}
				else if (GUILayout.Button("Remove"))
				{
					decorator = null;
					_decorator.RemoveCountryDecorator(_decorator.GUIGroupIndex, _decorator.GUICountryName);
					ReloadGroupNames();
					_decorator.ReloadCountryNames();
				}
				EditorGUILayout.EndHorizontal();

				if (!decoratorGroup.active)
					DrawWarningLabel("Enable the decoration group to activate changes");
			}

			EditorGUILayout.EndVertical();

			if (requestChanges)
			{
				_decorator.ForceUpdateDecorators();
				SceneView.RepaintAll();
				EditorUtility.SetDirty(_map);
				UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
					SceneManager.GetActiveScene());
			}
		}

		private void OnSceneGUI()
		{
			// Check mouse buttons state 
			var e = Event.current;
			var controlID = GUIUtility.GetControlID(FocusType.Passive);
			if (GUIUtility.hotControl == controlID) // release hot control to allow standard navigation
				GUIUtility.hotControl = 0;
			// locks control on map
			var eventType = e.GetTypeForControl(controlID);

			if (eventType == EventType.MouseDown && Event.current.button == 0)
			{
				// Check if clicked over a country
				int targetCountryIndex, targetRegionIndex;
				var ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
				if (_map.GetCountryIndex(ray, out targetCountryIndex, out targetRegionIndex))
				{
					_decorator.GUICountryName = _map.countries[targetCountryIndex].name;
					_decorator.ReloadCountryNames();
				}
			}
		}

		private void ReloadGroupNames()
		{
			for (var k = 0; k < groupNames.Length; k++)
			{
				var dc = _decorator.GetCountryDecoratorCount(k);
				if (dc > 0)
					groupNames[k] = k.ToString() + " (" + dc + " decorators)";
				else
					groupNames[k] = k.ToString();
			}
		}

		private void DrawWarningLabel(string s)
		{
			var warningLabelStyle = new GUIStyle(GUI.skin.label);
			warningLabelStyle.normal.textColor = new Color(0.31f, 0.38f, 0.56f);
			GUILayout.Label(s, warningLabelStyle);
		}

		private void ToggleZoomState()
		{
			zoomState = !zoomState;
			var cameraMain = _map.cameraMain;
			_map.transform.rotation = cameraMain.transform.rotation;
			if (zoomState)
			{
				oldMapPosition = _map.transform.position;
				oldMapRotation = _map.transform.rotation;
				_map.transform.position = cameraMain.transform.position +
				                          cameraMain.transform.forward * WMSK.mapWidth * 0.1f;
				var countryIndex = _map.GetCountryIndex(_decorator.GUICountryName);
				if (countryIndex >= 0)
				{
					var center = _map.countries[countryIndex].center;
					_map.transform.Translate(new Vector2(-center.x * WMSK.mapWidth,
						-center.y * WMSK.mapHeight));
				}
			}
			else
			{
				_map.transform.position = oldMapPosition;
				_map.transform.rotation = oldMapRotation;
			}
		}

		private void SetCountryFromCombo(int selection)
		{
			_decorator.GUICountryName = "";
			_decorator.GUICountryIndex = selection;
			var s = _decorator.countryNames[_decorator.GUICountryIndex].Split(new char[]
			{
				'(',
				')'
			}, StringSplitOptions.RemoveEmptyEntries);
			if (s.Length >= 2)
				_decorator.GUICountryName = s[0].Trim();
		}
	}
}