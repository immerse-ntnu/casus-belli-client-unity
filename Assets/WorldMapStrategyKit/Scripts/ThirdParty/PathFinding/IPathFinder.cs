using System.Collections.Generic;

namespace WorldMapStrategyKit.PathFinding
{
	internal interface IPathFinder
	{
		HeuristicFormula Formula { get; set; }

		float HeuristicEstimate { get; set; }

		float MaxSearchCost { get; set; }

		int MaxSteps { get; set; }

		List<PathFinderNode> FindPath(Point start, Point end, out float totalCost);
	}
}