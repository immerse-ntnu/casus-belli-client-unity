using System.Collections;
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

        private Graph neighbourGraph;  // Connects neighbouring regions
        private Graph boatGraph;  // Connects dockable regions
        private Graph landGraph;  // Connects land regions
        private List<Region> regions;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            // Find all regions and assign IDs.
            // IDs are necessary for the graph package to work
            regions = new();
            foreach (Region region in FindObjectsOfType<Region>())
            {
                region.Id = regions.Count;
                regions.Add(region);
            }

            // Assign "satellite information" to the base graph.
            // Satellite contains info on the nodes,
            // in this case their positions.
            // (Not necessary, but makes visualizing possible).
            neighbourGraph = new();
            for (int id = 0; id < regions.Count; id++)
            {
                Vector2 position = regions[id].transform.position;
                neighbourGraph.SetSatellite(id, "pos", position);
            }

            // Create graphs for all units.
            // All graphs should have the same satellites:
            landGraph = Graph.MakeFromSatellites(neighbourGraph);
            boatGraph = Graph.MakeFromSatellites(neighbourGraph);

            // For each region's neighbours, determine which troops
            // can move between the two (by making an edge).
            // Edges are directed by default,
            // we make them bidirectional by adding
            // two in opposing directions.
            for (int id = 0; id < regions.Count; id++)
            {
                Region a = regions[id];
                foreach (Region b in a.Neighbours)
                {
                    // These neighbours are dockable
                    if (a.IsDockable && b.IsDockable)
                        boatGraph.AddEdge(a.Id, b.Id);
                    // These neighbours are land
                    if (a.IsLand && b.IsLand)
                        landGraph.AddEdge(a.Id, b.Id);
                    // All neighbours should be added to neighbourGraph
                    neighbourGraph.AddEdge(a.Id, b.Id);
                }
            }

            // Assign graphs to visualizers
            // Useful for debugging
            if (neighbourVisualizer != null)
                neighbourVisualizer.Checkout(neighbourGraph);
            if (boatVisualizer != null)
                boatVisualizer.Checkout(boatGraph);
            if (landVisualizer != null)
                landVisualizer.Checkout(landGraph);
        }
    }
}
