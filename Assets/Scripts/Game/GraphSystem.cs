using System.Collections.Generic;
using UnityEngine;
using TorbuTils.Giraphe;

namespace Immerse.BfhClient.Game
{
    public class GraphSystem : MonoBehaviour
    {
        public static GraphSystem Instance { get; private set; }

        [SerializeField] private GraphVisualizer neighbourVisualizer;
        [SerializeField] private GraphVisualizer boatVisualizer;
        [SerializeField] private GraphVisualizer landVisualizer;

        private Graph _neighbourGraph;  // Connects neighbouring regions
        private Graph _boatGraph;  // Connects dockable regions
        private Graph _landGraph;  // Connects land regions
        private readonly List<Region> _regions = new();

        private void Awake() => Instance = this;

        private void Start()
        {
            // Find all regions and assign IDs.
            // IDs are necessary for the graph package to work
            foreach (var region in FindObjectsOfType<Region>())
            {
                region.Id = _regions.Count;
                _regions.Add(region);
            }

            // Assign "satellite information" to the base graph.
            // Satellite contains info on the nodes,
            // in this case their positions.
            // (Not necessary, but makes visualizing possible).
            _neighbourGraph = new();
            for (var id = 0; id < _regions.Count; id++)
            {
                Vector2 position = _regions[id].transform.position;
                _neighbourGraph.SetSatellite(id, "pos", position);
            }

            // Create graphs for all units.
            // All graphs should have the same satellites:
            _landGraph = Graph.MakeFromSatellites(_neighbourGraph);
            _boatGraph = Graph.MakeFromSatellites(_neighbourGraph);

            // For each region's neighbours, determine which troops
            // can move between the two (by making an edge).
            // Edges are directed by default,
            // we make them bidirectional by adding
            // two in opposing directions.
            for (var id = 0; id < _regions.Count; id++)
            {
                var a = _regions[id];
                foreach (var b in a.Neighbours)
                {
                    // These neighbours are dockable
                    if (a.IsDockable && b.IsDockable)
                        _boatGraph.AddEdge(a.Id, b.Id);
                    // These neighbours are land
                    if (a.IsLand && b.IsLand)
                        _landGraph.AddEdge(a.Id, b.Id);
                    // All neighbours should be added to neighbourGraph
                    _neighbourGraph.AddEdge(a.Id, b.Id);
                }
            }

            // Assign graphs to visualizers
            // Useful for debugging
            if (neighbourVisualizer != null)
                neighbourVisualizer.Set(_neighbourGraph);
            if (boatVisualizer != null)
                boatVisualizer.Set(_boatGraph);
            if (landVisualizer != null)
                landVisualizer.Set(_landGraph);
        }
    }
}
