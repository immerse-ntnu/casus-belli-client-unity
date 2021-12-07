// World Map Strategy Kit for Unity - Main Script
// (C) 2016-2020 by Ramiro Oliva (Kronnect)
// Don't modify this script - changes could be lost if you upgrade to a more recent version of WMSK

using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace WorldMapStrategyKit
{
	public partial class WMSK : MonoBehaviour
	{
		private const string OVERLAY_TEXT_PROVINCE_ROOT = "TextProvinceRoot";

		private GameObject textProvinceRoot;
		private Font provLabelsFont;
		private Material provLabelsShadowMaterial;
		private Country countryProvincesLabelsShown;

		#region Province Labels

		private void ReloadProvinceFont()
		{
			if (_provinceLabelsFont != null && _provinceLabelsFont.dynamic)
			{
				Debug.LogWarning("Dynamic fonts are not yet supported in WMSK.");
				_provinceLabelsFont = null;
			}
			if (_provinceLabelsFont == null)
				provLabelsFont = Instantiate(Resources.Load<Font>("WMSK/Font/Lato"));
			else
				provLabelsFont = Instantiate(_provinceLabelsFont);
			if (disposalManager != null)
				disposalManager
					.MarkForDisposal(provLabelsFont); //provLabelsFont.hideFlags = HideFlags.DontSave;
			var fontMaterial =
				Instantiate(
					Resources.Load<Material>(
						"WMSK/Materials/Font")); // this material is linked to a shader that has into account zbuffer
			if (provLabelsFont.material != null)
				fontMaterial.mainTexture = provLabelsFont.material.mainTexture;
			if (disposalManager != null)
				disposalManager
					.MarkForDisposal(fontMaterial); // fontMaterial.hideFlags = HideFlags.DontSave;
			provLabelsFont.material = fontMaterial;
			provLabelsShadowMaterial = Instantiate(fontMaterial);
			if (disposalManager != null)
				disposalManager.MarkForDisposal(
					provLabelsShadowMaterial); // provLabelsShadowMaterial.hideFlags = HideFlags.DontSave;
			provLabelsShadowMaterial.renderQueue--;
		}

		private void RedrawProvinceLabels(Country country)
		{
			DestroyProvinceLabels();
			DrawProvinceLabelsInt(country);
		}

		/// <summary>
		/// Draws the province labels for a given country. Note that it will update cached textmesh objects if labels are already drawn. Used internally.
		/// </summary>
		private void DrawProvinceLabelsInt(Country country)
		{
			if (!Application.isPlaying ||
			    !_showProvinceNames ||
			    !gameObject.activeInHierarchy ||
			    country == null ||
			    country.hidden ||
			    provinces == null ||
			    country.provinces == null)
				return;

			countryProvincesLabelsShown = country;

			// Set colors
			provLabelsFont.material.color = _provinceLabelsColor;
			provLabelsShadowMaterial.color = _provinceLabelsShadowColor;

			// Create texts
			var overlay = GetOverlayLayer(true);
			var t = overlay.transform.Find(OVERLAY_TEXT_PROVINCE_ROOT);
			if (t == null)
			{
				textProvinceRoot = new GameObject(OVERLAY_TEXT_PROVINCE_ROOT);
				if (disposalManager != null)
					disposalManager
						.MarkForDisposal(
							textProvinceRoot); // textProvinceRoot.hideFlags = HideFlags.DontSave;
				textProvinceRoot.layer = overlay.layer;
			}
			else
				textProvinceRoot = t.gameObject;

			if (meshRects == null)
				meshRects = new List<MeshRect>(country.provinces.Length);
			else
				meshRects.Clear();
			var mw = mapWidth;
			var mh = mapHeight;

			for (var p = 0; p < country.provinces.Length; p++)
			{
				var province = country.provinces[p];
				if (province.regions == null)
					ReadProvincePackedString(province);
				if (province == null ||
				    province.hidden ||
				    !province.labelVisible ||
				    province.regions == null ||
				    province.mainRegionIndex < 0 ||
				    province.mainRegionIndex >= province.regions.Count)
					continue;

				if (_provinceLabelsVisibility == PROVINCE_LABELS_VISIBILITY.Automatic &&
				    !_showAllCountryProvinceNames &&
				    province != _provinceHighlighted)
					continue;

				var center = new Vector2(province.center.x * mapWidth, province.center.y * mh) +
				             province.labelOffset;
				var region = province.regions[province.mainRegionIndex];

				// Adjusts province name length
				var provinceName = province.customLabel != null
					? province.customLabel
					: province.name.ToUpper();
				var introducedCarriageReturn = false;
				if (provinceName.Length > 15)
				{
					var spaceIndex = provinceName.IndexOf(' ', provinceName.Length / 2);
					if (spaceIndex >= 0)
					{
						provinceName = provinceName.Substring(0, spaceIndex) +
						               "\n" +
						               provinceName.Substring(spaceIndex + 1);
						introducedCarriageReturn = true;
					}
				}

				// add caption
				GameObject textObj;
				TextMesh tm;
				Renderer tmRenderer;
				TextMesh tmShadow = null;
				if (province.labelTextMeshGO == null)
				{
					var labelColor = province.labelColorOverride
						? province.labelColor
						: _provinceLabelsColor;
					var customFont = province.labelFontOverride ?? provLabelsFont;
					var customLabelShadowMaterial =
						province.labelFontShadowMaterial ?? provLabelsShadowMaterial;
					tm = Drawing.CreateText(provinceName, null, center, customFont, labelColor,
						_showProvinceLabelsShadow, customLabelShadowMaterial, _provinceLabelsShadowColor,
						out tmShadow);
					textObj = tm.gameObject;
					province.labelTextMesh = tm;
					province.labelTextMeshGO = tm.gameObject;
					tmRenderer = textObj.GetComponent<Renderer>();
					var bounds = tmRenderer.bounds;
					province.labelMeshWidth = bounds.size.x;
					province.labelMeshHeight = bounds.size.y;
					province.labelMeshCenter = center;
					textObj.transform.SetParent(textProvinceRoot.transform, false);
					textObj.transform.localPosition = center;
					textObj.layer = textProvinceRoot.gameObject.layer;
					if (_showProvinceLabelsShadow)
					{
						province.labelShadowTextMesh = tmShadow;
						province.labelShadowTextMesh.gameObject.layer = textObj.layer;
					}
				}
				else
				{
					tm = province.labelTextMesh;
					textObj = tm.gameObject;
					textObj.transform.localPosition = center;
					tmRenderer = textObj.GetComponent<Renderer>();
				}

				var meshWidth = province.labelMeshWidth;
				var meshHeight = province.labelMeshHeight;

				// adjusts caption
				var rect = new Rect(region.rect2D.xMin * mw, region.rect2D.yMin * mh,
					region.rect2D.width * mw, region.rect2D.height * mh);
				float absoluteHeight;
				if (province.labelRotation > 0)
				{
					textObj.transform.localRotation = Quaternion.Euler(0, 0, province.labelRotation);
					absoluteHeight = Mathf.Min(rect.height * _provinceLabelsSize, rect.width);
				}
				else if (rect.height > rect.width * 1.45f)
				{
					float angle;
					if (rect.height > rect.width * 1.5f)
						angle = 90;
					else
						angle = Mathf.Atan2(rect.height, rect.width) * Mathf.Rad2Deg;
					textObj.transform.localRotation = Quaternion.Euler(0, 0, angle);
					absoluteHeight = Mathf.Min(rect.width * _provinceLabelsSize, rect.height);
				}
				else
					absoluteHeight = Mathf.Min(rect.height * _provinceLabelsSize, rect.width);

				// adjusts scale to fit width in rect
				var adjustedMeshHeight = introducedCarriageReturn ? meshHeight * 0.5f : meshHeight;
				var scale = absoluteHeight / adjustedMeshHeight;
				if (province.labelFontSizeOverride)
					scale = province.labelFontSize;
				else
				{
					var desiredWidth = meshWidth * scale;
					if (desiredWidth > rect.width)
						scale = rect.width / meshWidth;
					if (adjustedMeshHeight * scale < _provinceLabelsAbsoluteMinimumSize)
						scale = _provinceLabelsAbsoluteMinimumSize / adjustedMeshHeight;
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
					for (var c = 0; c < provinceName.Length; c++)
					{
						sb.Append(provinceName[c]);
						if (c < provinceName.Length - 1)
							sb.Append(spaces);
					}
					wideName = sb.ToString();
				}
				else
					wideName = provinceName;

				if (tm.text.Length != wideName.Length)
				{
					tm.text = wideName;
					displayedMeshWidth = tmRenderer.bounds.size.x * scale;
					displayedMeshHeight = tmRenderer.bounds.size.y * scale;
					if (_showProvinceLabelsShadow)
						tmShadow.text = wideName;
				}

				// apply scale
				textObj.transform.localScale = new Vector3(scale, scale, 1);

				// Save mesh rect for overlapping checking
				if (province.labelOffset == Misc.Vector2zero)
				{
					var provinceIndex = GetProvinceIndex(province);
					var xMin = center.x - displayedMeshWidth * 0.5f;
					var yMin = center.y - displayedMeshHeight * 0.5f;
					var xMax = xMin + displayedMeshWidth;
					var yMax = yMin + displayedMeshHeight;
					var mr = new MeshRect(provinceIndex, new Vector4(xMin, yMin, xMax, yMax));
//					MeshRect mr = new MeshRect(provinceIndex, new Rect(center.x - displayedMeshWidth * 0.5f, center.y - displayedMeshHeight * 0.5f, displayedMeshWidth, displayedMeshHeight));
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
							var thisProvinceIndex = meshRects[c].entityIndex;
							var province = _provinces[thisProvinceIndex];
							var thisLabel = province.labelTextMeshGO;

							// displaces this label
							var offsety = r1.w - r2.y;
							offsety = Mathf.Min(
								province.regions[province.mainRegionIndex].rect2D.height * mh * 0.35f,
								offsety);
							thisLabel.transform.localPosition = new Vector3(province.labelMeshCenter.x,
								province.labelMeshCenter.y - offsety, thisLabel.transform.localPosition.z);
//							r1 = new Rect(thisLabel.transform.localPosition.x - r1.width * 0.5f,
//								thisLabel.transform.localPosition.y - r1.height * 0.5f,
//								r1.width, r1.height);
//							meshRects[c].rect = r1;
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

			// Adjusts parent
			textProvinceRoot.transform.SetParent(overlay.transform, false);
			textProvinceRoot.transform.localPosition = new Vector3(0, 0, -0.001f);
			textProvinceRoot.transform.localRotation = Misc.QuaternionZero;
			textProvinceRoot.transform.localScale = new Vector3(1.0f / mw, 1.0f / mh, 1);

			// Adjusts alpha based on distance to camera
			if (Application.isPlaying)
				FadeProvinceLabels();
		}

		private void DestroyProvinceLabels()
		{
			if (_provinces != null)
				for (var k = 0; k < _provinces.Length; k++)
				{
					_provinces[k].labelTextMesh = null;
					_provinces[k].labelTextMeshGO = null;
				}
			if (textProvinceRoot != null)
				DestroyImmediate(textProvinceRoot);
			countryProvincesLabelsShown = null;
		}

		private void FadeProvinceLabels()
		{
			if (countryProvincesLabelsShown == null || countryProvincesLabelsShown.provinces == null)
				return;

			// Automatically fades in/out province labels based on their screen size

			var y0 = _currentCamera.WorldToViewportPoint(transform.TransformPoint(0, 0, 0)).y;
			var y1 = _currentCamera.WorldToViewportPoint(transform.TransformPoint(0, 1.0f, 0)).y;
			var th = y1 - y0;

			var maxAlpha = _provinceLabelsColor.a;
			var maxAlphaShadow = _provinceLabelsShadowColor.a;
			var labelFadeMinSize = 0.018f;
			var labelFadeMaxSize = 0.2f;
			var labelFadeMinFallOff = 0.005f;
			var labelFadeMaxFallOff = 0.5f;

			var mh = mapHeight;
			for (var k = 0; k < countryProvincesLabelsShown.provinces.Length; k++)
			{
				var province = countryProvincesLabelsShown.provinces[k];
				var tm = province.labelTextMesh;
				if (tm != null)
				{
					// Fade label
					var labelSize = (province.labelMeshHeight + province.labelMeshWidth) * 0.5f;
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
						tm.color = new Color(tm.color.r, tm.color.g, tm.color.b, newAlpha);
					// Fade label shadow
					var tmShadow = province.labelShadowTextMesh;
					if (tmShadow != null)
					{
						newAlpha = ad * maxAlphaShadow;
						if (tmShadow.color.a != newAlpha)
							tmShadow.color = new Color(tmShadow.color.r, tmShadow.color.g,
								tmShadow.color.b, maxAlphaShadow * ad);
					}
				}
			}
		}

		#endregion
	}
}