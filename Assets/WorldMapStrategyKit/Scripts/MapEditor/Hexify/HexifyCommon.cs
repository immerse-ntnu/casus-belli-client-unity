using UnityEngine;
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using WorldMapStrategyKit.ClipperLib;

namespace WorldMapStrategyKit
{
	public delegate bool HexifyOperationProgress(float percentage, string title, string text);

	public delegate void HexifyOperationFinish(bool cancelled);

	public struct HexifyOpContext
	{
		public string title;
		public HexifyOperationProgress progress;
		public HexifyOperationFinish finish;
	}

	public partial class WMSK_Editor : MonoBehaviour
	{
		private bool cancelled;
		private RegionCell[] procCells;

		private HexifyOpContext hexifyContext;
		private Vector2[] hexagonPoints;

		/// <summary>
		/// Adjusts all countries frontiers to match the hexagonal grid
		/// </summary>
		public IEnumerator HexifyAll(HexifyOperationProgress progressOp, HexifyOperationFinish finishOp)
		{
			var cc = new HexifyOpContext
				{ title = "Hexifying Countries...", progress = progressOp, finish = null };
			yield return HexifyCountries(cc);
			cc.title = "Hexifying Provinces...";
			cc.finish = finishOp;
			yield return HexifyProvinces(cc);
		}

		private IEnumerator RemoveSmallRegions(float minArea, IAdminEntity[] _entities)
		{
			// Clear small regions
			for (var c = 0; c < _entities.Length; c++)
			{
				if (c % 10 == 0)
				{
					if (hexifyContext.progress != null)
						if (hexifyContext.progress((float)c / _entities.Length, hexifyContext.title,
							"Pass 1/6: removing small regions..."))
						{
							cancelled = true;
							hexifyContext.finish(true);
							yield break;
						}
					yield return null;
				}

				var entity = _entities[c];
				var rCount = entity.regions.Count;
				var recalc = false;
				for (var r = 0; r < rCount; r++)
					if (entity.regions[r].rect2DArea < minArea)
					{
						entity.regions[r].Clear();
						recalc = true;
					}
				if (recalc)
				{
					if (entity is Country)
						_map.RefreshCountryGeometry(entity);
					else
						_map.RefreshProvinceGeometry(entity);
				}
			}
		}

		private IEnumerator AssignRegionCenters(IAdminEntity[] entities)
		{
			var regions = new List<Region>();
			for (var k = 0; k < entities.Length; k++)
			{
				var entity = entities[k];
				var rCount = entity.regions.Count;
				for (var r = 0; r < rCount; r++)
				{
					var region = entity.regions[r];
					if (region.points.Length > 0)
						regions.Add(region);
				}
			}
			regions.Sort((Region x, Region y) => { return y.rect2DArea.CompareTo(x.rect2DArea); });

			var regionsCount = regions.Count;
			for (var r = 0; r < regionsCount; r++)
			{
				var region = regions[r];
				var cellIndex = _map.GetCellIndex(region.center);
				if (cellIndex >= 0 && procCells[cellIndex].entityIndex < 0)
				{
					var entity = region.entity;
					if (entity is Country)
						procCells[cellIndex].entityIndex = _map.GetCountryIndex((Country)region.entity);
					else
						procCells[cellIndex].entityIndex = _map.GetProvinceIndex((Province)region.entity);
					procCells[cellIndex].entityRegion = region;
				}
			}

			// Pass 2: iterate all frontier points
			for (var c = 0; c < entities.Length; c++)
			{
				if (c % 10 == 0)
				{
					if (hexifyContext.progress != null)
						if (hexifyContext.progress((float)c / entities.Length, hexifyContext.title,
							"Pass 2/6: assigning centers..."))
						{
							cancelled = true;
							hexifyContext.finish(true);
							yield break;
						}
					yield return null;
				}
				var entity = entities[c];
				var rCount = entity.regions.Count;
				for (var cr = 0; cr < rCount; cr++)
				{
					var region = entity.regions[cr];
					for (var p = 0; p < region.points.Length; p++)
					{
						var cellIndex = _map.GetCellIndex(region.points[p]);
						if (cellIndex >= 0 && procCells[cellIndex].entityIndex < 0)
						{
							procCells[cellIndex].entityIndex = c;
							procCells[cellIndex].entityRegion = region;
						}
					}
				}
			}
		}

		private IEnumerator AddHexagons(IAdminEntity[] entities)
		{
			var cells = _map.cells;
			var clipper = new Clipper();
			for (var j = 0; j < cells.Length; j++)
			{
				if (j % 100 == 0)
				{
					if (hexifyContext.progress != null)
						if (hexifyContext.progress((float)j / cells.Length, hexifyContext.title,
							"Pass 3/6: adding hexagons to frontiers..."))
						{
							cancelled = true;
							hexifyContext.finish(true);
							yield break;
						}
					yield return null;
				}

				var entityIndex = procCells[j].entityIndex;
				if (entityIndex < 0)
					continue;
				var cell = cells[j];

				// Create a region for the cell
				var entity = entities[entityIndex];
				var newPoints = new Vector2[6];
				for (var k = 0; k < 6; k++)
				{
					newPoints[k].x = hexagonPoints[k].x + cell.center.x;
					newPoints[k].y = hexagonPoints[k].y + cell.center.y;
				}
				procCells[j].cellRegion = new Region(entity, entity.regions.Count);
				procCells[j].cellRegion.UpdatePointsAndRect(newPoints);

				// Add region to target entity's polygon - only if the entity is touching or crossing target entity frontier
				var targetRegion = procCells[j].entityRegion;
				clipper.Clear();
				clipper.AddPath(targetRegion, PolyType.ptSubject);
				clipper.AddPath(procCells[j].cellRegion, PolyType.ptClip);
				clipper.Execute(ClipType.ctUnion);
			}
		}

		private IEnumerator MergeAdjacentRegions(IAdminEntity[] entities)
		{
			for (var k = 0; k < entities.Length; k++)
			{
				if (k % 10 == 0)
				{
					if (hexifyContext.progress != null)
						if (hexifyContext.progress((float)k / entities.Length, hexifyContext.title,
							"Pass 4/6: merging adjacent regions..."))
						{
							cancelled = true;
							hexifyContext.finish(true);
							yield break;
						}
					yield return null;
				}
				_map.MergeAdjacentRegions(entities[k]);
			}
		}

		private IEnumerator RemoveHexagons(IAdminEntity[] entities)
		{
			var clipper = new Clipper();
			var cells = _map.cells;
			for (var j = 0; j < cells.Length; j++)
			{
				if (j % 100 == 0)
				{
					if (hexifyContext.progress != null)
						if (hexifyContext.progress((float)j / cells.Length, hexifyContext.title,
							"Pass 5/6: removing cells from neighbours..."))
						{
							cancelled = true;
							hexifyContext.finish(true);
							yield break;
						}
					yield return null;
				}

				var entityIndex = procCells[j].entityIndex;
				if (entityIndex < 0)
					continue;

				var regionCell = procCells[j];
				var entity = entities[entityIndex];

				// Substract cell region from any other entity
				List<Region> otherRegions;
				if (entity is Country)
					otherRegions = _map.GetCountryRegionsOverlap(regionCell.cellRegion);
				else
					otherRegions = _map.GetProvinceRegionsOverlap(regionCell.cellRegion);
				var orCount = otherRegions.Count;
				for (var o = 0; o < orCount; o++)
				{
					var otherRegion = otherRegions[o];
					var otherEntity = otherRegion.entity;
					if (otherEntity == entity)
						continue;
					clipper.Clear();
					clipper.AddPath(otherRegion, PolyType.ptSubject);
					clipper.AddPath(regionCell.cellRegion, PolyType.ptClip);
					clipper.Execute(ClipType.ctDifference, otherEntity);
				}
			}
		}
	}
}