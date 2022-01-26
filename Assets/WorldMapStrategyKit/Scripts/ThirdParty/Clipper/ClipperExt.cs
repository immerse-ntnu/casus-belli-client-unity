using System.Collections.Generic;
using UnityEngine;

namespace WorldMapStrategyKit.ClipperLib
{
	public partial class Clipper
	{
		private const float MULTIPLIER = 5000000;

		private Region subject;
		private List<Region> subjects;
		private List<List<IntPoint>> solution;

		public override void Clear()
		{
			subject = null;
			base.Clear();
		}

		public void AddPaths(List<Region> regions, PolyType polyType)
		{
			var regionCount = regions.Count;
			for (var k = 0; k < regionCount; k++)
				AddPath(regions[k], polyType);
		}

		public void AddPath(Region region, PolyType polyType)
		{
			if (region == null || region.points == null)
				return;
			var count = region.points.Length;
			var points = new List<IntPoint>(count);
			for (var k = 0; k < count; k++)
			{
				var ix = region.points[k].x * MULTIPLIER;
				var iy = region.points[k].y * MULTIPLIER;
				var p = new IntPoint(ix, iy);
				points.Add(p);
			}
			AddPath(points, polyType, true);

			if (polyType == PolyType.ptSubject)
			{
				subject = region;
				if (subjects == null)
					subjects = new List<Region>();
				subjects.Add(region);
			}
		}

		public void AddPath(Vector2[] points, PolyType polyType)
		{
			if (points == null)
				return;
			if (polyType == PolyType.ptSubject)
			{
				Debug.LogError("Subject polytype needs a Region object.");
				return;
			}
			var count = points.Length;
			var newPoints = new List<IntPoint>(count);
			for (var k = 0; k < count; k++)
			{
				var ix = points[k].x * MULTIPLIER;
				var iy = points[k].y * MULTIPLIER;
				var p = new IntPoint(ix, iy);
				newPoints.Add(p);
			}
			AddPath(newPoints, polyType, true);
		}

		public void Execute(ClipType clipType, IAdminEntity entity)
		{
			if (solution == null)
				solution = new List<List<IntPoint>>();
			Execute(clipType, solution);
			var contourCount = solution.Count;
			// Remove subject from entity
			if (subjects != null)
				for (var k = 0; k < subjects.Count; k++)
				{
					var sub = subjects[k];
					if (entity.regions.Contains(sub))
						entity.regions.Remove(sub);
				}
			// Add resulting regions
			for
			(var c = 0;
				c < contourCount;
				c++) // In the case of difference operations, the resulting polytongs could be artifacts if the frontiers do not match perfectly. In that case, we ignore small isolated triangles.
				if (clipType == ClipType.ctUnion || solution[c].Count >= 5)
				{
					var newPoints = BuildPointArray(solution[c]);
					var region = new Region(entity, entity.regions.Count);
					region.UpdatePointsAndRect(newPoints);
					region.sanitized = true;
					entity.regions.Add(region);
				}
			entity.mainRegionIndex = 0;
		}

		private Vector2[] BuildPointArray(List<IntPoint> points)
		{
			var count = points.Count;
			var newPoints = new Vector2[count];
			for (var k = 0; k < count; k++)
			{
				newPoints[k].x = points[k].X / MULTIPLIER;
				newPoints[k].y = points[k].Y / MULTIPLIER;
			}
			return newPoints;
		}

		public void Execute(ClipType clipType, Region output)
		{
			if (solution == null)
				solution = new List<List<IntPoint>>();
			Execute(clipType, solution);
			var contourCount = solution.Count;
			if (contourCount == 0)
				output.Clear();
			else
			{
				// Use the largest contour
				var best = 0;
				var pointCount = solution[0].Count;
				for (var k = 1; k < contourCount; k++)
				{
					var candidatePointCount = solution[k].Count;
					if (candidatePointCount > pointCount)
					{
						pointCount = candidatePointCount;
						best = k;
					}
				}
				var newPoints = BuildPointArray(solution[best]);
				output.UpdatePointsAndRect(newPoints);
			}
		}

		public void Execute(ClipType clipType)
		{
			if (subject == null)
			{
				Debug.LogError("Clipper.Execute called without defined subject");
				return;
			}
			Execute(clipType, subject);
		}
	}
}