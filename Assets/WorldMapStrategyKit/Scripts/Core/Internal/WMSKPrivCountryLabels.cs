// World Map Strategy Kit for Unity - Main Script
// (C) 2016-2020 by Ramiro Oliva (Kronnect)
// Don't modify this script - changes could be lost if you upgrade to a more recent version of WMSK

// Comment this macro to disable TextMesh Pro labels. IMPORTANT! TextMesh Pro must be present in the project (download it from the Asset Store)
// TextMesh Pro does not expose any project-level setting that enables automatic detection yet so for now it's neccesary to manually uncomment this define

#define USE_TEXTMESH_PRO

using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;

#if USE_TEXTMESH_PRO
using TMPro;
#endif

namespace WorldMapStrategyKit
{
	public partial class WMSK : MonoBehaviour
	{
		private const string OVERLAY_TEXT_ROOT = "TextRoot";
		private const float WMSK_TERRAIN_MODE_Y_OFFSET = 5000f;

		private GameObject textRoot;
		private Font labelsFont;
		private UnityEngine.Object labelsFontTMPro;
		private Material labelsShadowMaterial;

#if USE_TEXTMESH_PRO
		private float[] labelsCurve;
#endif
		private List<MeshRect> meshRects;

		private int antarctica, greenland, brazil, india, usa, canada, china, russia;

		#region Country Labels

		private void ResetDefaultCountryLabelOffset()
		{
			antarctica = -1;
			greenland = -1;
			brazil = -1;
			india = -1;
			usa = -1;
			canada = -1;
			china = -1;
			russia = -1;
		}

		/// <summary>
		/// Custom label offset for default map
		/// </summary>
		/// <returns>The default country label offset.</returns>
		private Vector2 GetDefaultCountryLabelOffset(int countryIndex)
		{
			if (antarctica < 0)
				antarctica = GetCountryIndex("Antarctica");
			if (greenland < 0)
				greenland = GetCountryIndex("Greenland");
			if (brazil < 0)
				brazil = GetCountryIndex("Brazil");
			if (india < 0)
				india = GetCountryIndex("India");
			if (usa < 0)
				usa = GetCountryIndex("United States of America");
			if (canada < 0)
				canada = GetCountryIndex("Canada");
			if (china < 0)
				china = GetCountryIndex("China");
			if (russia < 0)
				russia = GetCountryIndex("Russia");

			var zoomFactor = transform.localScale.x / 200.0f;
			if (_frontiersDetail == FRONTIERS_DETAIL.Low)
			{
				if (countryIndex == antarctica)
					return zoomFactor * new Vector2(0, 3f);
				if (countryIndex == greenland)
					return zoomFactor * new Vector2(0, -3f);
				if (countryIndex == brazil)
					return zoomFactor * new Vector2(1f, 4f);
				if (countryIndex == india)
					return zoomFactor * new Vector2(-2f, 0);
				if (countryIndex == usa)
					return zoomFactor * new Vector2(-1f, 0);
				if (countryIndex == canada)
					return zoomFactor * new Vector2(-3f, 0);
				if (countryIndex == china)
					return zoomFactor * new Vector2(-1f, -1f);
			}
			else
			{
				if (countryIndex == antarctica)
					return zoomFactor * new Vector2(0, 2f);
				if (countryIndex == brazil)
					return zoomFactor * new Vector2(2f, 4f);
				if (countryIndex == usa)
					return zoomFactor * new Vector2(-2f, 0);
				if (countryIndex == canada)
					return zoomFactor * new Vector2(-7f, 0);
				if (countryIndex == china)
					return zoomFactor * new Vector2(-3f, -0f);
			}

			return Misc.Vector2zero;
		}

		private void ReloadFont()
		{
			if (_countryLabelsFont != null && _countryLabelsFont.dynamic)
			{
				Debug.LogWarning("Dynamic fonts are not yet supported in WMSK.");
				_countryLabelsFont = null;
			}
			if (_countryLabelsFont == null)
			{
				var font = Resources.Load<Font>("WMSK/Font/Lato");
				if (font != null)
					labelsFont = Instantiate(font);
			}
			else
				labelsFont = Instantiate(_countryLabelsFont);
			if (labelsFont == null)
				return;
			if (disposalManager != null)
				disposalManager.MarkForDisposal(labelsFont);
			var fontMaterial =
				Instantiate(
					Resources.Load<Material>(
						"WMSK/Materials/Font")); // this material is linked to a shader that has into account zbuffer
			if (labelsFont.material != null)
				fontMaterial.mainTexture = labelsFont.material.mainTexture;
			if (disposalManager != null)
				disposalManager
					.MarkForDisposal(fontMaterial); // fontMaterial.hideFlags = HideFlags.DontSave;
			labelsFont.material = fontMaterial;
			labelsShadowMaterial = Instantiate(fontMaterial);
			if (disposalManager != null)
				disposalManager.MarkForDisposal(
					labelsShadowMaterial); // labelsShadowMaterial.hideFlags = HideFlags.DontSave;
			labelsShadowMaterial.renderQueue--;

			if (_countryLabelsFontTMPro == null)
				labelsFontTMPro =
					null; // dummy assignment to avoid compiler warning if TMPro is not installed
#if USE_TEXTMESH_PRO
			if (_countryLabelsFontTMPro == null)
			{
				_countryLabelsFontTMPro = Resources.Load("WMSK/Font/TextMeshPro/Lato SDF");
				if (_countryLabelsFontTMPro == null)
					Debug.LogWarning(
						"Please assign an SDF Font to World Map Strategy Kit inspector. You can create SDF Fonts using TextMesh Pro Font Asset creator tool.");
			}
			labelsFontTMPro = Instantiate(_countryLabelsFontTMPro);

			if (labelsFontTMPro != null)
			{
				((TMP_FontAsset)labelsFontTMPro).material =
					Instantiate(((TMP_FontAsset)labelsFontTMPro).material);
				((TMP_FontAsset)labelsFontTMPro).material.hideFlags = HideFlags.DontSave;
			}
#endif
			if (labelsFontTMPro != null)
				if (disposalManager != null)
					disposalManager
						.MarkForDisposal(
							labelsFontTMPro); // labelsFontTMPro.hideFlags = HideFlags.DontSave;
		}

		/// <summary>
		/// Draws the map labels. Note that it will update cached textmesh objects if labels are already drawn. Used internally.
		/// </summary>
		public void DrawMapLabels()
		{
			if (!gameObject.activeInHierarchy)
				return;
			DrawCountryLabels();
			RedrawProvinceLabels(_countryHighlighted);
		}

		/// <summary>
		/// Draws the country labels. Note that it will update cached textmesh objects if labels are already drawn. Used internally.
		/// </summary>
		private void DrawCountryLabels()
		{
			if (!_showCountryNames || !gameObject.activeInHierarchy || _countries == null)
				return;

			// Set colors
			labelsFont.material.color = _countryLabelsColor;
			labelsShadowMaterial.color = _countryLabelsShadowColor;

			// Create texts
			DestroyCountryLabels();
			var overlay = GetOverlayLayer(true);
			var t = overlay.transform.Find(OVERLAY_TEXT_ROOT);
			if (t == null)
			{
				textRoot = new GameObject(OVERLAY_TEXT_ROOT);
				if (disposalManager != null)
					disposalManager.MarkForDisposal(textRoot); // textRoot.hideFlags = HideFlags.DontSave;
				textRoot.layer = overlay.layer;
			}
			else
				textRoot = t.gameObject;

			if (_countryLabelsTextEngine == TEXT_ENGINE.TextMeshPro)
			{
#if USE_TEXTMESH_PRO
				// assign root before text creation so it doesn't invoke mesh regeneration twice by Text Mesh Pro
				textRoot.transform.SetParent(overlay.transform, false);
				DrawTextMeshProLabels();
#else
				DrawUnityStandardTextLabels ();
#endif
			}
			else
				DrawUnityStandardTextLabels();

			// Adjusts parent
			if (textRoot.transform.parent != overlay.transform)
				textRoot.transform.SetParent(overlay.transform, false);
			var textElevation = renderViewportIsEnabled ? -0.001f : -labelsElevation;
			textRoot.transform.localPosition = new Vector3(0, 0, textElevation);
			textRoot.transform.localRotation = Misc.QuaternionZero;
			textRoot.transform.localScale = new Vector3(1.0f / mapWidth, 1.0f / mapHeight, 1);

			// Adjusts alpha based on distance to camera
			if (Application.isPlaying)
				FadeCountryLabels();
		}

		#region TextMesh Pro support

#if USE_TEXTMESH_PRO

		private void DrawTextMeshProLabels()
		{
			if (labelsFontTMPro == null)
			{
				ReloadFont();
				if (labelsFontTMPro == null)
					return;
			}

			var mw = mapWidth;
			var mh = mapHeight;

			if (labelsCurve == null || labelsCurve.Length == 0)
				ComputeLabelsCurve();

			var center = Misc.Vector2zero;
			for (var countryIndex = 0; countryIndex < _countries.Length; countryIndex++)
			{
				var country = _countries[countryIndex];
				if (country.hidden ||
				    !country.labelVisible ||
				    country.mainRegionIndex < 0 ||
				    country.mainRegionIndex >= country.regions.Count)
					continue;

				var region = country.regions[country.mainRegionIndex];
				if (!ComputeCurvedLabelData(region))
					continue;

				if (country.labelOffset.x != 0 || country.labelOffset.y != 0)
					center = country.center + country.labelOffset;
				else
					FastVector.Average(ref region.curvedLabelInfo.axisStart,
						ref region.curvedLabelInfo.axisEnd,
						ref center); // (curvedLabel.axisStart + curvedLabel.axisEnd) * 0.5f;
				center.x *= mw;
				center.y *= mh;

				// Adjusts country name length
				var countryName = country.customLabel != null
					? country.customLabel
					: country.name.ToUpper();
				if (countryName.Length == 0)
					continue;

				if (countryName.Length > 15)
					countryName = BreakOneLineString(countryName);

				// add caption
				GameObject textObj;
				TextMeshPro tm;
				var labelColor = country.labelColorOverride ? country.labelColor : _countryLabelsColor;
				if (country.labelTextMeshGO == null)
				{
					// create base text
					textObj = new GameObject(countryName);
					textObj.hideFlags = HideFlags.DontSave;
					textObj.hideFlags |= HideFlags.HideInHierarchy;
					tm = textObj.AddComponent<TextMeshPro>();
					tm.alignment = TextAlignmentOptions.Center;
					tm.enableWordWrapping = false;
					country.labelTextMeshPro = tm;
					country.labelTextMeshGO = tm.gameObject;
					textObj.transform.SetParent(textRoot.transform, false);
					textObj.layer = textRoot.gameObject.layer;
				}
				else
				{
					tm = (TextMeshPro)country.labelTextMeshPro;
					textObj = tm.gameObject;
					textObj.transform.localPosition = center;
				}
				tm.font = (TMP_FontAsset)labelsFontTMPro;
				tm.color = labelColor;

				Material fontMat;
				if (_countryLabelsEnableAutomaticFade)
				{
					// By using fontMaterial we're forcing to instantiate the material which will enable individual colors and alpha
					fontMat = tm.fontMaterial;
					fontMat.hideFlags = HideFlags.DontSave;
				}
				else
					fontMat = tm.fontSharedMaterial;

				if (_countryLabelsOutlineWidth > 0)
				{
					fontMat.SetColor("_OutlineColor", _countryLabelsOutlineColor);
					fontMat.SetFloat("_OutlineWidth", _countryLabelsOutlineWidth);
					fontMat.EnableKeyword("OUTLINE_ON");
				}
				else
					fontMat.DisableKeyword("OUTLINE_ON");

				tm.text = countryName;
				textObj.transform.localPosition = center;
				country.labelMeshWidth = tm.preferredWidth;
				country.labelMeshHeight = tm.preferredHeight;
				country.labelMeshCenter = center;

				var meshWidth = country.labelMeshWidth;
				var meshHeight = country.labelMeshHeight;

				// adjusts scale to fit in region
				var axis = region.curvedLabelInfo.axisEnd - region.curvedLabelInfo.axisStart;
				float scale;
				if (country.labelFontSizeOverride)
					scale = country.labelFontSize;
				else
				{
					// axisWidth represents the length of the label along the longest axis
					var axisWidth = new Vector2(axis.x * mw, axis.y * mh).magnitude;
					// axisAveragedWidth represents the average length of the region (used as a maximum height for the label)
					var axisAveragedThickness =
						new Vector2(region.curvedLabelInfo.axisAveragedThickness.x * mw,
							region.curvedLabelInfo.axisAveragedThickness.y * mh).magnitude;
					var scaleheight = axisAveragedThickness / meshHeight;
					var scaleWidth = axisWidth / meshWidth;
					scale = Mathf.Min(scaleWidth, scaleheight);
					if (meshHeight * scale < _countryLabelsAbsoluteMinimumSize)
						scale = _countryLabelsAbsoluteMinimumSize / meshHeight;
					scale *= _countryLabelsSize * 2f;
				}

				// apply scale
				textObj.transform.localScale = new Vector3(scale, scale, 1);

				// Apply axis rotation or user defined rotation
				if (country.labelRotation > 0)
					textObj.transform.localRotation = Quaternion.Euler(0, 0, country.labelRotation);
				else
					textObj.transform.localRotation =
						Quaternion.Euler(0, 0, region.curvedLabelInfo.axisAngle);

				if (_countryLabelsCurvature > 0)
				{
					// Compute fitting curve
					tm.havePropertiesChanged = true; // Need to force the TextMeshPro Object to be updated.
					tm.ForceMeshUpdate(); // Generate the mesh and populate the textInfo with data we can use and manipulate.

					var textInfo = tm.textInfo;
					var characterCount = textInfo.characterCount;

					var boundsMinX = tm.bounds.min.x;
					var boundsMaxX = tm.bounds.max.x;
					// map bounds to axis length
					var axisLengthWS = new Vector2(axis.x * mw / scale, axis.y * mh / scale).magnitude;
					var boundsLength = boundsMaxX - boundsMinX;
					var boundsMid = (boundsMaxX + boundsMinX) * 0.5f;
					boundsMinX = boundsMid - (boundsMid - boundsMinX) * axisLengthWS / boundsLength;
					boundsMaxX = boundsMid + (boundsMaxX - boundsMid) * axisLengthWS / boundsLength;

					var curveMultiplier =
						new Vector2(region.curvedLabelInfo.axisMidDisplacement.x * mw / scale,
							region.curvedLabelInfo.axisMidDisplacement.y * mh / scale).magnitude *
						_countryLabelsCurvature;
					// check if axisAveragedThickness is above or below axis
					var a = axis * 0.5f + region.curvedLabelInfo.axisMidDisplacement;
					var dot = a.x * -axis.y + a.y * axis.x;
					if (dot < 0)
						curveMultiplier *= -1f;
					var boundsWidth = boundsMaxX - boundsMinX;

					// Get the index of the mesh used by this character.
					var materialIndex = textInfo.characterInfo[0].materialReferenceIndex;
					var vertices = textInfo.meshInfo[materialIndex].vertices;

					for (var i = 0; i < characterCount; i++)
					{
						if (!textInfo.characterInfo[i].isVisible)
							continue;

						var vertexIndex = textInfo.characterInfo[i].vertexIndex;

						// Compute the baseline mid point for each character
						var offsetToMidBaseline =
							new Vector2((vertices[vertexIndex + 0].x + vertices[vertexIndex + 2].x) / 2,
								textInfo.characterInfo[i].baseLine);
						if (float.IsNaN(offsetToMidBaseline.x) || float.IsNaN(offsetToMidBaseline.y))
							continue;
						//offsetToMidBaseline.x = offsetToMidBaseline.y = 0;

						// Apply offset to adjust our pivot point.
						vertices[vertexIndex + 0].x -= offsetToMidBaseline.x;
						vertices[vertexIndex + 0].y -= offsetToMidBaseline.y;
						vertices[vertexIndex + 1].x -= offsetToMidBaseline.x;
						vertices[vertexIndex + 1].y -= offsetToMidBaseline.y;
						vertices[vertexIndex + 2].x -= offsetToMidBaseline.x;
						vertices[vertexIndex + 2].y -= offsetToMidBaseline.y;
						vertices[vertexIndex + 3].x -= offsetToMidBaseline.x;
						vertices[vertexIndex + 3].y -= offsetToMidBaseline.y;

						// Compute the angle of rotation for each character based on the animation curve
						var x0 = (offsetToMidBaseline.x - boundsMinX) /
						         boundsWidth; // Character's position relative to the bounds of the mesh.
						var x1 = x0 + 0.01f;
						const float minT = 0.0f;
						const float maxT = 0.9999f;
						if (x0 < minT)
							x0 = minT;
						else if (x0 > maxT)
							x0 = maxT;
						var ix0 = (int)(x0 * labelsCurve.Length);
						var y0 = labelsCurve[ix0] * curveMultiplier;
						if (x1 < minT)
							x1 = minT;
						else if (x1 > maxT)
							x1 = maxT;
						var ix1 = (int)(x1 * labelsCurve.Length);
						var y1 = labelsCurve[ix1] * curveMultiplier;

						var tangent = new Vector2(x1 * boundsWidth + boundsMinX - offsetToMidBaseline.x,
							y1 - y0);
						dot = Mathf.Acos(tangent.normalized.x) * Mathf.Rad2Deg;
						var angle = tangent.y > 0 ? dot : 360 - dot;

						var matrix =
							Matrix4x4.TRS(
								new Vector3(offsetToMidBaseline.x, y0 + offsetToMidBaseline.y, 0),
								Quaternion.Euler(0, 0, angle), Misc.Vector3one);

						vertices[vertexIndex + 0] = matrix.MultiplyPoint3x4(vertices[vertexIndex + 0]);
						vertices[vertexIndex + 1] = matrix.MultiplyPoint3x4(vertices[vertexIndex + 1]);
						vertices[vertexIndex + 2] = matrix.MultiplyPoint3x4(vertices[vertexIndex + 2]);
						vertices[vertexIndex + 3] = matrix.MultiplyPoint3x4(vertices[vertexIndex + 3]);
					}

					// Upload the mesh with the revised information
					tm.UpdateVertexData();
				}
				country.labelMeshWidth = tm.bounds.size.x;
				country.labelMeshHeight = tm.bounds.size.y;

				//																GameObject sphere3 = GameObject.CreatePrimitive (PrimitiveType.Sphere);
				//																sphere3.name = "Axis Middle";
				//																sphere3.transform.SetParent (o.transform, true);
				//																sphere3.transform.localPosition = (curvedLabel.axisStart + curvedLabel.axisEnd) * 0.5f;
				//																sphere3.transform.localScale *= 0.3f;
				//
				//																GameObject sphere4 = GameObject.CreatePrimitive (PrimitiveType.Sphere);
				//																sphere4.name = "avgAxisPoint";
				//																sphere4.transform.SetParent (o.transform, true);
				//																sphere4.transform.localPosition = (curvedLabel.axisStart + curvedLabel.axisEnd) * 0.5f + curvedLabel.axisMidDisplacement;
				//																sphere4.GetComponent<Renderer> ().material.color = Color.red;
				//																sphere4.transform.localScale *= 0.2f;
			}

			StartCoroutine(RepositionTexts());
		}

		private IEnumerator RepositionTexts()
		{
			// Workaround for TMPPro / Unity's RectTransform bug
			yield return null;
			for (var k = 0; k < _countries.Length; k++)
				_countries[k].labelTextMeshGO.transform.localPosition = _countries[k].labelMeshCenter;
		}

		private void ComputeLabelsCurve()
		{
			const int NUM_KEYS = 255;
			labelsCurve = new float[NUM_KEYS + 1];
			for (var k = 0; k <= NUM_KEYS; k++)
			{
				var x = (float)k / NUM_KEYS;
				var y = Mathf.Sin(x * Mathf.PI);
				labelsCurve[k] = y;
			}
		}

#endif

		#endregion

		private string BreakOneLineString(string s)
		{
			if (s.Length <= 15)
				return s;
			var spaceIndex = s.IndexOf(' ', s.Length / 2);
			if (spaceIndex < 0)
				spaceIndex = s.LastIndexOf(' ');
			if (spaceIndex >= 0)
				s = s.Substring(0, spaceIndex) + "\n" + s.Substring(spaceIndex + 1);
			return s;
		}

		private void DrawUnityStandardTextLabels()
		{
			ResetDefaultCountryLabelOffset();

			if (meshRects == null)
				meshRects = new List<MeshRect>(_countries.Length);
			else
				meshRects.Clear();
			var mw = mapWidth;
			var mh = mapHeight;
			for (var countryIndex = 0; countryIndex < _countries.Length; countryIndex++)
			{
				var country = _countries[countryIndex];
				if (country.hidden ||
				    !country.labelVisible ||
				    country.mainRegionIndex < 0 ||
				    country.mainRegionIndex >= country.regions.Count)
					continue;

				var center = new Vector2(country.center.x * mapWidth, country.center.y * mh) +
				             country.labelOffset;
				center += GetDefaultCountryLabelOffset(countryIndex);

				var region = country.regions[country.mainRegionIndex];

				// Adjusts country name length
				var countryName = country.customLabel != null
					? country.customLabel
					: country.name.ToUpper();
				var introducedCarriageReturn = false;
				if (countryName.Length > 15)
				{
					countryName = BreakOneLineString(countryName);
					introducedCarriageReturn = true;
				}

				// add caption
				GameObject textObj;
				TextMesh tm;
				Renderer tmRenderer;
				TextMesh tmShadow = null;
				if (country.labelTextMeshGO == null)
				{
					var labelColor = country.labelColorOverride ? country.labelColor : _countryLabelsColor;
					var customFont = country.labelFontOverride ?? labelsFont;
					if ((object)customFont == null)
						continue;
					var customLabelShadowMaterial =
						country.labelFontShadowMaterial ?? labelsShadowMaterial;
					tm = Drawing.CreateText(countryName, null, center, customFont, labelColor,
						_showLabelsShadow, customLabelShadowMaterial, _countryLabelsShadowColor,
						out tmShadow);
					textObj = tm.gameObject;
					country.labelTextMesh = tm;
					country.labelTextMeshGO = tm.gameObject;
					tmRenderer = textObj.GetComponent<Renderer>();
					var bounds = tmRenderer.bounds;
					country.labelMeshWidth = bounds.size.x;
					country.labelMeshHeight = bounds.size.y;
					country.labelMeshCenter = center;
					textObj.transform.SetParent(textRoot.transform, false);
					textObj.transform.localPosition = center;
					textObj.layer = textRoot.gameObject.layer;
					if (_showLabelsShadow)
					{
						country.labelShadowTextMesh =
							tmShadow; //textObj.transform.Find ("shadow").GetComponent<TextMesh> ();
						country.labelShadowTextMesh.gameObject.layer = textObj.layer;
					}
				}
				else
				{
					tm = country.labelTextMesh;
					textObj = tm.gameObject;
					textObj.transform.localPosition = center;
					tmRenderer = textObj.GetComponent<Renderer>();
				}

				var meshWidth = country.labelMeshWidth;
				var meshHeight = country.labelMeshHeight;

				// adjusts caption
				var rect = new Rect(region.rect2D.xMin * mw, region.rect2D.yMin * mh,
					region.rect2D.width * mw, region.rect2D.height * mh);
				float absoluteHeight;
				if (country.labelRotation > 0)
				{
					textObj.transform.localRotation = Quaternion.Euler(0, 0, country.labelRotation);
					absoluteHeight = Mathf.Min(rect.height * _countryLabelsSize, rect.width);
				}
				else if (rect.height > rect.width * 1.45f)
				{
					float angle;
					if (rect.height > rect.width * 1.5f)
						angle = 90;
					else
						angle = Mathf.Atan2(rect.height, rect.width) * Mathf.Rad2Deg;
					textObj.transform.localRotation = Quaternion.Euler(0, 0, angle);
					absoluteHeight = Mathf.Min(rect.width * _countryLabelsSize, rect.height);
				}
				else
					absoluteHeight = Mathf.Min(rect.height * _countryLabelsSize, rect.width);

				// adjusts scale to fit width in rect
				var adjustedMeshHeight = introducedCarriageReturn ? meshHeight * 0.5f : meshHeight;
				var scale = absoluteHeight / adjustedMeshHeight;
				if (country.labelFontSizeOverride)
					scale = country.labelFontSize;
				else
				{
					var desiredWidth = meshWidth * scale;
					if (desiredWidth > rect.width)
						scale = rect.width / meshWidth;
					if (adjustedMeshHeight * scale < _countryLabelsAbsoluteMinimumSize)
						scale = _countryLabelsAbsoluteMinimumSize / adjustedMeshHeight;
				}

				// stretchs out the caption
				var displayedMeshWidth = meshWidth * scale;
				var displayedMeshHeight = meshHeight * scale;
				string wideName;
				var times = Mathf.FloorToInt(rect.width * 0.45f / (meshWidth * scale));
				if (times > 10)
					times = 10;
				if (times > 0)
				{
					var sb = new StringBuilder();
					var spaces = new string(' ', times * 2);
					for (var c = 0; c < countryName.Length; c++)
					{
						sb.Append(countryName[c]);
						if (c < countryName.Length - 1)
							sb.Append(spaces);
					}
					wideName = sb.ToString();
				}
				else
					wideName = countryName;

				if (tm.text.Length != wideName.Length)
				{
					tm.text = wideName;
					displayedMeshWidth = tmRenderer.bounds.size.x * scale;
					displayedMeshHeight = tmRenderer.bounds.size.y * scale;
					if (_showLabelsShadow)
						tmShadow.text = wideName;
				}

				// apply scale
				textObj.transform.localScale = new Vector3(scale, scale, 1);

				// Save mesh rect for overlapping checking
				if (country.labelOffset == Misc.Vector2zero)
				{
					var xMin = center.x - displayedMeshWidth * 0.5f;
					var yMin = center.y - displayedMeshHeight * 0.5f;
					var xMax = xMin + displayedMeshWidth;
					var yMax = yMin + displayedMeshHeight;
					var mr = new MeshRect(countryIndex, new Vector4(xMin, yMin, xMax, yMax));
					meshRects.Add(mr);
				}
			}

			// Simple-fast overlapping checking
			var cont = 0;
			var needsResort = true;

			var meshRectsCount = meshRects.Count;
			while (needsResort && ++cont < 10)
			{
				meshRects.Sort(overlapComparer);
				needsResort = false;
				for (var c = 1; c < meshRectsCount; c++)
				{
					var r1 = meshRects[c].rect;
					for (var prevc = c - 1; prevc >= 0; prevc--)
					{
						var r2 = meshRects[prevc].rect;
						var overlaps = !(r2.x > r1.z || r2.z < r1.x || r2.y > r1.w || r2.w < r1.y);
						if (overlaps)
						{
							needsResort = true;
							var thisCountryIndex = meshRects[c].entityIndex;
							var country = _countries[thisCountryIndex];
							var thisLabel = country.labelTextMeshGO;

							// displaces this label
							var offsety = r1.w - r2.y;
							offsety = Mathf.Min(
								country.regions[country.mainRegionIndex].rect2D.height * mh * 0.35f,
								offsety);
							thisLabel.transform.localPosition = new Vector3(country.labelMeshCenter.x,
								country.labelMeshCenter.y - offsety, thisLabel.transform.localPosition.z);
							var width = r1.z - r1.x;
							var height = r1.w - r1.y;
							var xMin = thisLabel.transform.localPosition.x - width * 0.5f;
							var yMin = thisLabel.transform.localPosition.y - height * 0.5f;
							var xMax = xMin + width;
							var yMax = yMin + height;
							r1 = new Vector4(xMin, yMin, xMax, yMax);
							meshRects[c].rect = r1;
						}
					}
				}
			}
		}

		private int overlapComparer(MeshRect r1, MeshRect r2)
		{
			var r1MidY = (r1.rect.w + r1.rect.y) * 0.5f;
			var r2MidY = (r2.rect.w + r2.rect.y) * 0.5f;
			if (r2MidY < r1MidY)
				return -1;
			else if (r2MidY > r1MidY)
				return 1;
			else
				return 0;
			//			return (r2.rect.center.y).CompareTo(r1.rect.center.y);
		}

		private class MeshRect
		{
			public int entityIndex;
			public Vector4 rect;

			public MeshRect(int entityIndex, Vector4 rect)
			{
				this.entityIndex = entityIndex;
				this.rect = rect;
			}
		}

		private void DestroyCountryLabels()
		{
			if (_countries != null)
				for (var k = 0; k < _countries.Length; k++)
				{
					_countries[k].labelTextMeshGO = null;
					_countries[k].labelTextMesh = null;
				}
			if (textRoot != null)
				DestroyImmediate(textRoot);
			// Security check: if there're still gameObjects under TextRoot, also delete it
			if (overlayLayer != null)
			{
				var t = overlayLayer.transform.Find(OVERLAY_TEXT_ROOT);
				if (t != null && t.childCount > 0)
					DestroyImmediate(t.gameObject);
			}
		}

#if USE_TEXTMESH_PRO
		private void FadeCountryLabels()
		{
			// Automatically fades in/out country labels based on their screen size
			if (!_countryLabelsEnableAutomaticFade)
				return;

			float th;

			var oldRot = _currentCamera.transform.rotation;
			if (renderViewportIsTerrain) // workaround for terrain mode
				_currentCamera.transform.forward = transform.forward;
			Vector2 y0 = _currentCamera.WorldToViewportPoint(transform.TransformPoint(0, -0.5f, 0));
			Vector2 y1 = _currentCamera.WorldToViewportPoint(transform.TransformPoint(0, 0.5f, 0));
			th = Vector2.Distance(y0, y1);
			if (renderViewportIsTerrain)
				_currentCamera.transform.rotation = oldRot;

			var maxAlpha = _countryLabelsColor.a;
			var labelFadeMinSize = _countryLabelsAutoFadeMinHeight; // 0.018f;
			var labelFadeMaxSize = _countryLabelsAutoFadeMaxHeight; // 0.2f;
			var labelFadeMinFallOff = _countryLabelsAutoFadeMinHeightFallOff; // 0.005f;
			var labelFadeMaxFallOff = _countryLabelsAutoFadeMaxHeightFallOff; // 0.5f;

			var mh = mapHeight;
			for (var k = 0; k < _countries.Length; k++)
			{
				var country = _countries[k];
				var tm = (TextMeshPro)country.labelTextMeshPro;
				if (tm != null)
				{
					// Fade label
					var labelSize = (country.labelMeshHeight + country.labelMeshWidth) * 0.5f;
					var screenHeight = labelSize * tm.transform.localScale.y * th / mh;
					float ad;
					if (screenHeight < labelFadeMinSize)
						ad = Mathf.Lerp(1.0f, 0, (labelFadeMinSize - screenHeight) / labelFadeMinFallOff);
					else if (screenHeight > labelFadeMaxSize)
						ad = Mathf.Lerp(1.0f, 0, (screenHeight - labelFadeMaxSize) / labelFadeMaxFallOff);
					else
						ad = 1.0f;
					var newAlpha = ad * maxAlpha;
					if (tm.color.a != newAlpha)
					{
						tm.fontSharedMaterial.SetColor("_FaceColor",
							new Color(tm.color.r, tm.color.g, tm.color.b, newAlpha));
						tm.fontSharedMaterial.SetColor("_OutlineColor",
							new Color(_countryLabelsOutlineColor.r, _countryLabelsOutlineColor.g,
								_countryLabelsOutlineColor.b, _countryLabelsOutlineColor.a * newAlpha));
					}
				}
			}
		}

		/// <summary>
		/// Restores country labels alpha to 1.0. Used by editor since in edit mode one need to see labels!
		/// </summary>
		public void ResetCountryLabelsAlpha()
		{
			for (var k = 0; k < _countries.Length; k++)
			{
				var country = _countries[k];
				var tm = (TextMeshPro)country.labelTextMeshPro;
				if (tm != null)
					if (tm.color.a != 1f)
					{
						tm.fontSharedMaterial.SetColor("_FaceColor", tm.color);
						tm.fontSharedMaterial.SetColor("_OutlineColor", _countryLabelsOutlineColor);
					}
			}
		}

#else
		void FadeCountryLabels () {

			// Automatically fades in/out country labels based on their screen size
			if (!_countryLabelsEnableAutomaticFade)
				return;

			float th;

			Quaternion oldRot = _currentCamera.transform.rotation;
			if (renderViewPortIsTerrain) { // workaround for terrain mode
				_currentCamera.transform.forward = transform.forward;
			}
			Vector2 y0 = _currentCamera.WorldToViewportPoint (transform.TransformPoint (0, -0.5f, 0));
			Vector2 y1 = _currentCamera.WorldToViewportPoint (transform.TransformPoint (0, 0.5f, 0));
			th = Vector2.Distance (y0, y1);
			if (renderViewPortIsTerrain) {
				_currentCamera.transform.rotation = oldRot;
			}

			float maxAlpha = _countryLabelsColor.a;
			float maxAlphaShadow = _countryLabelsShadowColor.a;
			float labelFadeMinSize = _countryLabelsAutoFadeMinHeight; // 0.018f;
			float labelFadeMaxSize = _countryLabelsAutoFadeMaxHeight; // 0.2f;
			float labelFadeMinFallOff = _countryLabelsAutoFadeMinHeightFallOff; // 0.005f;
			float labelFadeMaxFallOff = _countryLabelsAutoFadeMaxHeightFallOff; // 0.5f;

			float mh = mapHeight;
			for (int k = 0; k < _countries.Length; k++) {
				Country country = _countries [k];
				TextMesh tm = country.labelTextMesh;
				if (tm != null) {
					// Fade label
					float labelSize = (country.labelMeshHeight + country.labelMeshWidth) * 0.5f;
					float screenHeight = labelSize * tm.transform.localScale.y * th / mh;
					float ad;
					if (screenHeight < labelFadeMinSize) {
						ad = Mathf.Lerp (1.0f, 0, (labelFadeMinSize - screenHeight) / labelFadeMinFallOff);
					} else if (screenHeight > labelFadeMaxSize) {
						ad = Mathf.Lerp (1.0f, 0, (screenHeight - labelFadeMaxSize) / labelFadeMaxFallOff);
					} else {
						ad = 1.0f;
					}
					float newAlpha = ad * maxAlpha;
					if (tm.color.a != newAlpha) {
						tm.color = new Color (tm.color.r, tm.color.g, tm.color.b, newAlpha);
					}
					// Fade label shadow
					TextMesh tmShadow = country.labelShadowTextMesh;
					if (tmShadow != null) {
						newAlpha = ad * maxAlphaShadow;
						if (tmShadow.color.a != newAlpha) {
							tmShadow.color =
 new Color (tmShadow.color.r, tmShadow.color.g, tmShadow.color.b, maxAlphaShadow * ad);
						}
					}
				}
			}
		}

		/// <summary>
		/// Restores country labels alpha to 1.0. Used by editor since in edit mode one need to see labels!
		/// </summary>
		public void ResetCountryLabelsAlpha () {
			for (int k = 0; k < _countries.Length; k++) {
				Country country = _countries [k];
				TextMesh tm = country.labelTextMesh;
				if (tm != null) {
					if (tm.color.a != 1f) {
						tm.color = new Color (tm.color.r, tm.color.g, tm.color.b, 1f);
					}
					// Fade label shadow
					TextMesh tmShadow = country.labelShadowTextMesh;
					if (tmShadow != null) {
						if (tmShadow.color.a != 1f) {
							tmShadow.color =
 new Color (tmShadow.color.r, tmShadow.color.g, tmShadow.color.b, 1f);
						}
					}
				}
			}
		}
#endif

		private void DestroyMapLabels()
		{
			DestroyCountryLabels();
			DestroyProvinceLabels();
		}

		#endregion
	}
}