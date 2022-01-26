// World Map Strategy Kit for Unity - Main Script
// (C) 2016-2020 by Ramiro Oliva (Kronnect)
// Don't modify this script - changes could be lost if you upgrade to a more recent version of WMSK

using System;
using UnityEngine;
using UnityEngine.Rendering;
using WorldMapStrategyKit.Poly2Tri;

namespace WorldMapStrategyKit
{
	public partial class WMSK : MonoBehaviour
	{
		#region Region related functions

		private struct BorderIntersection
		{
			public Vector2 p;
			public float dist;
		}

		private Material extrudedMat;
		private BorderIntersection[] intersections;

		private bool ComputeCurvedLabelData(Region region)
		{
			if (!region.curvedLabelInfo.isDirty)
				return true;
			region.curvedLabelInfo.isDirty = false;

			var points = region.points;
			if (points == null)
				return false;

			// Step 1: compute maximum axis distance
			int iStart = 0, iEnd = points.Length / 2;
			var maxDist = float.MinValue;

			for (var k = 0; k < points.Length; k += 2)
			{
				for (var j = k + 1; j < points.Length; j += 2)
				{
					var dx = points[k].x - points[j].x;
					dx *= _countryLabelsHorizontality;
					var dy = points[k].y - points[j].y;
					var dist = dx * dx + dy * dy;
					if (dist > maxDist)
					{
						maxDist = dist;
						iStart = k;
						iEnd = j;
					}
				}
			}

			if (points[iStart].x > points[iEnd].x)
			{
				var u = iEnd;
				iEnd = iStart;
				iStart = u;
			}

			// Step 2: compute intersections between segment which is perpendicular to max axis to find a good centroid inside the polygon
			var axisStart = points[iStart];
			var axisEnd = points[iEnd];
			var axis = axisEnd - axisStart;
			var axisMid = (axisStart + axisEnd) * 0.5f;
			var dir = new Vector2(-axis.y, axis.x).normalized;
			var s0 = axisMid - dir;
			var s1 = axisMid + dir;
			if (intersections == null || intersections.Length < 10)
				intersections = new BorderIntersection[10];

			// Compute all intersections
			var intersectionIndex = -1;
			for (var k = 0; k < points.Length; k++)
			{
				Vector2 intersectionPoint;
				var next = k < points.Length - 1 ? k + 1 : 0;
				if (TestSegmentIntersection(s0, s1, points[k], points[next], out intersectionPoint))
				{
					intersectionIndex++;
					if (intersectionIndex >= intersections.Length)
						break;
					var dx = intersectionPoint.x - s0.x;
					var dy = intersectionPoint.y - s0.y;
					var dist = dx * dx + dy * dy;
					intersections[intersectionIndex].p = intersectionPoint;
					intersections[intersectionIndex].dist = dist;
				}
			}

			Vector2 p0 = axisStart, p1 = axisEnd;
			if (intersectionIndex % 2 == 0)
				intersectionIndex--;

			// Sort intersections by distance
			for (var k = 0; k < intersectionIndex; k++)
			{
				var changes = false;
				for (var j = k + 1; j <= intersectionIndex; j++)
					if (intersections[j].dist < intersections[k].dist)
					{
						var oldp = intersections[k].p;
						var oldDist = intersections[k].dist;
						intersections[k].p = intersections[j].p;
						intersections[k].dist = intersections[j].dist;
						intersections[j].p = oldp;
						intersections[j].dist = oldDist;
						changes = true;
					}
				if (!changes)
					break;
			}

			// Iterate intersections in pairs and get the thicker one
			maxDist = float.MinValue;
			for (var k = 0; k < intersectionIndex; k += 2)
			{
				var diff = intersections[k + 1].dist - intersections[k].dist;
				if (diff > maxDist)
				{
					maxDist = diff;
					p0 = intersections[k].p;
					p1 = intersections[k + 1].p;
				}
			}

			// Land intersection points
			region.curvedLabelInfo.p0 = p0;
			region.curvedLabelInfo.p1 = p1;

			// Corrected centroid
			var centroid = (p0 + p1) * 0.5f;
			region.curvedLabelInfo.axisAveragedThickness = p1 - p0;

			// Reduce axis length
			region.curvedLabelInfo.axisStart = centroid + (axisStart - centroid) * _countryLabelsLength;
			region.curvedLabelInfo.axisEnd = centroid + (axisEnd - centroid) * _countryLabelsLength;

			// Final axis and displacement values
			axisMid = (axisStart + axisEnd) * 0.5f;
			region.curvedLabelInfo.axisMidDisplacement = centroid - axisMid;
			axis = axisEnd - axisStart;
			// note the multiplication of axis.x by 2 to compensate map aspect ratio
			region.curvedLabelInfo.axisAngle = Mathf.Atan2(axis.y, axis.x * 2f) * Mathf.Rad2Deg;

			return true;
		}

		// Find the point of intersection between
		// the lines p1 --> p2 and p3 --> p4.
		private bool TestSegmentIntersection(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4,
			out Vector2 intersectionPoint)
		{
			var dx12 = p2.x - p1.x;
			var dy12 = p2.y - p1.y;
			var dx34 = p4.x - p3.x;
			var dy34 = p4.y - p3.y;
			intersectionPoint.x = 0;
			intersectionPoint.y = 0;

			// Solve for t1 and t2
			var denominator = dy12 * dx34 - dx12 * dy34;
			if (denominator == 0)
				return false;
			var t1 = ((p1.x - p3.x) * dy34 + (p3.y - p1.y) * dx34) / denominator;
			if (t1 < 0 || t1 > 1)
				return false;

			var t2 = ((p3.x - p1.x) * dy12 + (p1.y - p3.y) * dx12) / -denominator;

			if (t2 >= 0 && t2 <= 1)
			{
				intersectionPoint.x = p1.x + dx12 * t1;
				intersectionPoint.y = p1.y + dy12 * t1;
				return true;
			}
			return false;
		}

		private Vector2 ConvertToTextureCoordinates(Vector3 vertex, int width, int height)
		{
			Vector2 v;
			v.x = (int)((vertex.x + 0.5f) * width);
			v.y = (int)((vertex.y + 0.5f) * height);
			return v;
		}

		/// <summary>
		/// Paints the region into a given texture color array.
		/// </summary>
		/// <param name="region">Region.</param>
		/// <param name="color">Color.</param>
		public void RegionPaint(Color[] colors, int textureWidth, int textureHeight, Region region,
			Color color)
		{
			// Get the region mesh
			int entityIndex, regionIndex;
			var isCountry = region.entity is Country;
			var hideSurface = false;
			GameObject surf;

			if (isCountry)
			{
				entityIndex = GetCountryIndex((Country)region.entity);
				regionIndex = GetCountryRegionIndex(entityIndex, region);
				surf = GetCountryRegionSurfaceGameObject(entityIndex, regionIndex);
				if (surf == null)
				{
					surf = ToggleCountryRegionSurface(entityIndex, regionIndex, true, Color.white);
					hideSurface = true;
				}
			}
			else
			{
				entityIndex = GetProvinceIndex((Province)region.entity);
				regionIndex = GetProvinceRegionIndex(entityIndex, region);
				surf = GetCountryRegionSurfaceGameObject(entityIndex, regionIndex);
				if (surf == null)
				{
					surf = ToggleProvinceRegionSurface(entityIndex, regionIndex, true, Color.white);
					hideSurface = true;
				}
			}
			if (surf == null)
				return;

			// Get triangles and paint over the texture
			var mf = surf.GetComponent<MeshFilter>();
			if (mf == null || mf.sharedMesh.GetTopology(0) != MeshTopology.Triangles)
				return;
			var vertices = mf.sharedMesh.vertices;
			var indices = mf.sharedMesh.GetTriangles(0);

			var maxEdge = textureWidth * 0.8f;
			var minEdge = textureWidth * 0.2f;
			for (var i = 0; i < indices.Length; i += 3)
			{
				var p1 = ConvertToTextureCoordinates(vertices[indices[i]], textureWidth, textureHeight);
				var p2 = ConvertToTextureCoordinates(vertices[indices[i + 1]], textureWidth,
					textureHeight);
				var p3 = ConvertToTextureCoordinates(vertices[indices[i + 2]], textureWidth,
					textureHeight);
				// Sort points
				if (p2.x > p3.x)
				{
					var p = p2;
					p2 = p3;
					p3 = p;
				}
				if (p1.x > p2.x)
				{
					var p = p1;
					p1 = p2;
					p2 = p;
					if (p2.x > p3.x)
					{
						p = p2;
						p2 = p3;
						p3 = p;
					}
				}
				if (p1.x < minEdge && p2.x < minEdge && p3.x > maxEdge)
				{
					if (p1.x < 1 && p2.x < 1)
					{
						p1.x = textureWidth - p1.x;
						p2.x = textureWidth - p2.x;
					}
					else
						p3.x = textureWidth - p3.x;
				}
				else if (p1.x < minEdge && p2.x > maxEdge && p3.x > maxEdge)
					p1.x = textureWidth + p1.x;
				Drawing.DrawTriangle(colors, textureWidth, textureHeight, p1, p2, p3, color);
			}

			if (hideSurface)
			{
				if (isCountry)
					ToggleCountryRegionSurface(entityIndex, regionIndex, false, Color.white);
				else
					ToggleProvinceRegionSurface(entityIndex, regionIndex, false, Color.white);
			}
		}

		/// <summary>
		/// Paints the region into a given texture color array.
		/// </summary>
		/// <param name="region">Region.</param>
		/// <param name="color">Color.</param>
		public void RegionPaintHeights(Color[] colors, int textureWidth, int textureHeight, Region region,
			float[] heights, float minHeight, int heightsWidth, int heightsHeight, Gradient gradient)
		{
			// Get the region mesh
			int entityIndex, regionIndex;
			var isCountry = region.entity is Country;
			var hideSurface = false;
			GameObject surf;

			if (isCountry)
			{
				entityIndex = GetCountryIndex((Country)region.entity);
				regionIndex = GetCountryRegionIndex(entityIndex, region);
				surf = GetCountryRegionSurfaceGameObject(entityIndex, regionIndex);
				if (surf == null)
				{
					surf = ToggleCountryRegionSurface(entityIndex, regionIndex, true, Color.white);
					hideSurface = true;
				}
			}
			else
			{
				entityIndex = GetProvinceIndex((Province)region.entity);
				regionIndex = GetProvinceRegionIndex(entityIndex, region);
				surf = GetCountryRegionSurfaceGameObject(entityIndex, regionIndex);
				if (surf == null)
				{
					surf = ToggleProvinceRegionSurface(entityIndex, regionIndex, true, Color.white);
					hideSurface = true;
				}
			}
			if (surf == null)
				return;

			// Get triangles and paint over the texture
			var mf = surf.GetComponent<MeshFilter>();
			if (mf == null || mf.sharedMesh.GetTopology(0) != MeshTopology.Triangles)
				return;
			var vertices = mf.sharedMesh.vertices;
			var indices = mf.sharedMesh.GetTriangles(0);

			var maxEdge = textureWidth * 0.8f;
			var minEdge = textureWidth * 0.2f;
			for (var i = 0; i < indices.Length; i += 3)
			{
				var p1 = ConvertToTextureCoordinates(vertices[indices[i]], textureWidth, textureHeight);
				var p2 = ConvertToTextureCoordinates(vertices[indices[i + 1]], textureWidth,
					textureHeight);
				var p3 = ConvertToTextureCoordinates(vertices[indices[i + 2]], textureWidth,
					textureHeight);
				// Sort points
				if (p2.x > p3.x)
				{
					var p = p2;
					p2 = p3;
					p3 = p;
				}
				if (p1.x > p2.x)
				{
					var p = p1;
					p1 = p2;
					p2 = p;
					if (p2.x > p3.x)
					{
						p = p2;
						p2 = p3;
						p3 = p;
					}
				}
				if (p1.x < minEdge && p2.x < minEdge && p3.x > maxEdge)
				{
					if (p1.x < 1 && p2.x < 1)
					{
						p1.x = textureWidth - p1.x;
						p2.x = textureWidth - p2.x;
					}
					else
						p3.x = textureWidth - p3.x;
				}
				else if (p1.x < minEdge && p2.x > maxEdge && p3.x > maxEdge)
					p1.x = textureWidth + p1.x;
				Drawing.DrawTriangle(colors, textureWidth, textureHeight, p1, p2, p3, heights, minHeight,
					heightsWidth, heightsHeight, gradient);
			}

			if (hideSurface)
			{
				if (isCountry)
					ToggleCountryRegionSurface(entityIndex, regionIndex, false, Color.white);
				else
					ToggleProvinceRegionSurface(entityIndex, regionIndex, false, Color.white);
			}
		}

		/// <summary>
		/// Creates an extruded version of a given region
		/// </summary>
		/// <returns>The generate extrude game object.</returns>
		/// <param name="name">Name.</param>
		/// <param name="extrusionAmount">Size of the extrusion.</param>
		/// <param name="region">Region.</param>
		/// <param name="material">Material.</param>
		/// <param name="textureScale">Texture scale.</param>
		/// <param name="textureOffset">Texture offset.</param>
		/// <param name="textureRotation">Texture rotation.</param>
		public GameObject RegionGenerateExtrudeGameObject(string name, Region region,
			float extrusionAmount, Color sideColor)
		{
			var sideMaterial = Instantiate(extrudedMat);
			sideMaterial.color = sideColor;
			var topMaterial = Instantiate(extrudedMat);
			topMaterial.mainTexture = earthMat.mainTexture;
			return RegionGenerateExtrudeGameObject(name, region, extrusionAmount, topMaterial,
				sideMaterial, Misc.Vector2one, Misc.Vector2zero, 0, false);
		}

		/// <summary>
		/// Creates an extruded version of a given region
		/// </summary>
		/// <returns>The generate extrude game object.</returns>
		/// <param name="name">Name.</param>
		/// <param name="extrusionAmount">Size of the extrusion.</param>
		/// <param name="region">Region.</param>
		/// <param name="material">Material.</param>
		/// <param name="textureScale">Texture scale.</param>
		/// <param name="textureOffset">Texture offset.</param>
		/// <param name="textureRotation">Texture rotation.</param>
		public GameObject RegionGenerateExtrudeGameObject(string name, Region region,
			float extrusionAmount, Material material, Material sideMaterial) =>
			RegionGenerateExtrudeGameObject(name, region, extrusionAmount, material, sideMaterial,
				Misc.Vector2one, Misc.Vector2zero, 0);

		/// <summary>
		/// Creates an extruded version of a given region
		/// </summary>
		/// <returns>The generate extrude game object.</returns>
		/// <param name="name">Name.</param>
		/// <param name="extrusionAmount">Size of the extrusion.</param>
		/// <param name="region">Region.</param>
		/// <param name="material">Material.</param>
		/// <param name="textureScale">Texture scale.</param>
		/// <param name="textureOffset">Texture offset.</param>
		/// <param name="textureRotation">Texture rotation.</param>
		public GameObject RegionGenerateExtrudeGameObject(string name, Region region,
			float extrusionAmount, Material topMaterial, Material sideMaterial, Vector2 textureScale,
			Vector2 textureOffset, float textureRotation, bool useRegionRect = true)
		{
			if (region == null || region.points.Length < 3)
				return null;

			var rect = useRegionRect ? region.rect2D : new Rect(0.5f, 0.5f, 1f, 1f);
			var go = new GameObject(name);
			go.transform.SetParent(transform, false);

			var poly = new Polygon(region.points);
			P2T.Triangulate(poly);

			// Creates surface mesh
			var surf = Drawing.CreateSurface("RegionTop", poly, topMaterial, rect, textureScale,
				textureOffset, textureRotation, null);
			surf.transform.SetParent(go.transform, false);
			surf.transform.localPosition = new Vector3(0, 0, -extrusionAmount);

			// Create side band
			var pointCount = region.points.Length;
			var vertices = new Vector3[pointCount * 2];
			var indices = new int[pointCount * 6];
			int vi = 0, ii = -1;
			for (var k = 0; k < pointCount; k++, vi += 2)
			{
				vertices[vi] = region.points[k];
				vertices[vi].z = -extrusionAmount;
				vertices[vi + 1] = vertices[vi];
				vertices[vi + 1].z = 0;
				if (k == pointCount - 1)
				{
					indices[++ii] = vi + 1;
					indices[++ii] = vi;
					indices[++ii] = 1;
					indices[++ii] = vi + 1;
					indices[++ii] = 1;
					indices[++ii] = 0;
				}
				else
				{
					indices[++ii] = vi;
					indices[++ii] = vi + 1;
					indices[++ii] = vi + 2;
					indices[++ii] = vi + 1;
					indices[++ii] = vi + 3;
					indices[++ii] = vi + 2;
				}
			}

			var band = new GameObject("RegionBand");
			band.transform.SetParent(go.transform, false);
			var mesh = new Mesh();
			mesh.vertices = vertices;
			mesh.triangles = indices;
			mesh.RecalculateNormals();
			var mf = band.AddComponent<MeshFilter>();
			mf.mesh = mesh;
			var mr = band.AddComponent<MeshRenderer>();
			mr.sharedMaterial = sideMaterial;

			if (region.entity.allowHighlight &&
			    (region.entity is Country && _enableCountryHighlight ||
			     region.entity is Province && _enableProvinceHighlight))
			{
				var interaction = go.AddComponent<ExtrudedRegionInteraction>();
				interaction.map = this;
				interaction.region = region;
				interaction.topMaterial = topMaterial;
				interaction.sideMaterial = sideMaterial;
				interaction.highlightColor = region.entity is Country ? _fillColor : _provincesFillColor;
			}
			return go;
		}

		private GameObject DrawRegionOutlineMesh(string name, Region region,
			bool overridesAnimationSpeed = false, float animationSpeed = 0f)
		{
			var indices = new int[region.points.Length + 1];
			for (var k = 0; k < indices.Length; k++)
				indices[k] = k;
			indices[indices.Length - 1] = 0;
			var boldFrontiers = new GameObject(name);

			var customBorder = region.customBorderTexture != null;

			if (_outlineDetail == OUTLINE_DETAIL.Simple && !customBorder)
			{
				var mesh = new Mesh();
				var points = new Vector3[region.points.Length];
				for (var k = 0; k < region.points.Length; k++)
					points[k] = region.points[k];
				mesh.vertices = points;
				mesh.SetIndices(indices, MeshTopology.LineStrip, 0);
				mesh.RecalculateBounds();
				if (disposalManager != null)
					disposalManager.MarkForDisposal(mesh); //mesh.hideFlags = HideFlags.DontSave;

				var mf = boldFrontiers.AddComponent<MeshFilter>();
				mf.sharedMesh = mesh;

				var mr = boldFrontiers.AddComponent<MeshRenderer>();
				mr.receiveShadows = false;
				mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
				mr.shadowCastingMode = ShadowCastingMode.Off;
				mr.sharedMaterial = outlineMatSimple;
			}
			else
			{
				var lr = boldFrontiers.AddComponent<LineRenderer>();
				lr.useWorldSpace = false;
				if (customBorder)
				{
					lr.startWidth = region.customBorderWidth;
					lr.endWidth = region.customBorderWidth;
				}
				else
				{
					lr.startWidth = _outlineWidth;
					lr.endWidth = _outlineWidth;
				}
				var pCount = region.points.Length;
				lr.positionCount = pCount + 1;
				lr.textureMode = LineTextureMode.Tile;
				for (var k = 0; k < pCount; k++)
					lr.SetPosition(k, region.points[k]);
				lr.SetPosition(pCount, region.points[0]);
				lr.loop = true;
				if (customBorder && region.customBorderTexture != outlineMatTextured.mainTexture)
				{
					var mat = Instantiate(outlineMatTextured);
					if (disposalManager != null)
						disposalManager.MarkForDisposal(mat); //mat.hideFlags = HideFlags.DontSave;
					mat.name = outlineMatTextured.name;
					mat.mainTexture = region.customBorderTexture;
					mat.mainTextureScale = new Vector2(region.customBorderTextureTiling, 1f);
					mat.color = region.customBorderTintColor;
					if (!overridesAnimationSpeed)
						animationSpeed = region.customBorderAnimationSpeed;
					mat.SetFloat("_AnimationAcumOffset", region.customBorderAnimationAcumOffset);
					region.customBorderAnimationStartTime = time;
					mat.SetFloat("_AnimationStartTime", time);
					mat.SetFloat("_AnimationSpeed", animationSpeed);
					lr.sharedMaterial = mat;
				}
				else
					lr.sharedMaterial = outlineMatTextured;
			}
			return boldFrontiers;
		}

		private bool CheckScreenAreaSizeOfRegion(Region region)
		{
			var cam = currentCamera;
			Vector2 scrTR = cam.WorldToViewportPoint(transform.TransformPoint(region.rect2D.max));
			Vector2 scrBL = cam.WorldToViewportPoint(transform.TransformPoint(region.rect2D.min));
			var scrRect = new Rect(scrBL.x, scrTR.y, Math.Abs(scrTR.x - scrBL.x),
				Mathf.Abs(scrTR.y - scrBL.y));
			var highlightedArea = Mathf.Clamp01(scrRect.width * scrRect.height);
			return highlightedArea < _highlightMaxScreenAreaSize;
		}

		#endregion
	}
}