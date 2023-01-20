using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace TorbuTils.Giraphe
{
    /// <summary>
    /// Representation of a graph (computer science)
    /// </summary>
    [Serializable]
    public class Graph
    {
        /// <summary>
        /// Quantity of nodes that have ever been mentioned in edges or satellite data.
        /// </summary>
        public int NodeCount => Nodes.Count;
        private Edgees Edges { get; set; } = new();
        private Edgees AntiEdges { get; set; } = new();  // improves time complexity of CopyEdgesTo
        private Dictionary<(int, int), int> Weights { get; set; } = new();
        private Dictionary<int, Dictionary<string, object>> NodeSatellites { get; set; } = new();
        private HashSet<int> Nodes { get; set; } = new();

        /// <summary>
        /// Creates a new Graph object with a
        /// copy of inputGraph's satellite data
        /// </summary>
        /// <param name="inputGraph"></param>
        /// <returns></returns>
        public static Graph MakeFromSatellites(Graph inputGraph)
        {
            Graph result = new();
            for (int i = 0; i < inputGraph.NodeCount; i++)
            {
                if (!inputGraph.NodeSatellites.ContainsKey(i)) continue;
                foreach (string key in inputGraph.NodeSatellites[i].Keys)
                {
                    object value = inputGraph.NodeSatellites[i][key];
                    result.SetSatellite(i, key, value);
                }
            }
            return result;
        }
        
        /// <summary>
        /// Gets a copy of every node in this graph.
        /// A node is defined either though edges or satellite data.
        /// </summary>
        /// <returns>A collection of node IDs</returns>
        public ICollection<int> CopyNodes()
        {
            HashSet<int> result = new();
            foreach (int id in Nodes)
            {
                result.Add(id);
            }
            return result;
        }
        /// <summary>
        /// Gets a copy of every edge in this graph.
        /// </summary>
        /// <returns>
        /// A collection of tuples (from, to)
        /// </returns>
        public ICollection<(int, int)> CopyEdges()
        {
            HashSet<(int, int)> edges = new();
            foreach (int from in Edges.GetNodes())
            {
                foreach (int to in Edges.GetNodeEdges(from))
                {
                    edges.Add((from, to));
                }
            }
            return edges;
        }
        /// <summary>
        /// Gets a copy of every node directly accessible from node
        /// </summary>
        /// <returns>
        /// A collection of node ids, empty if node is nonexistent
        /// </returns>
        public ICollection<int> CopyEdgesFrom(int from)
        {
            if (!Edges.HasNode(from)) return new HashSet<int>();

            HashSet<int> result = new();
            foreach (int i in Edges.GetNodeEdges(from))
            {
                result.Add(i);
            }

            return result;
        }
        /// <summary>
        /// Gets a copy of every node with direct access to node
        /// </summary>
        /// <returns>
        /// A collection of node ids, empty if node is nonexistent
        /// </returns>
        public ICollection<int> CopyEdgesTo(int to)
        {
            if (!AntiEdges.HasNode(to)) return new HashSet<int>();

            HashSet<int> result = new();
            foreach (int i in AntiEdges.GetNodeEdges(to))
            {
                result.Add(i);
            }

            return result;
        }
        /// <summary>
        /// Gets the quantity of nodes directly accessible from node
        /// </summary>
        /// <returns>An integer, 0 if node is nonexistent</returns>
        public int GetEdgesCountFrom(int from)
        {
            if (!Edges.HasNode(from)) return 0;
            return Edges.GetNodeEdges(from).Count;
        }
        /// <summary>
        /// Gets the quantity of nodes with direct access to node
        /// </summary>
        /// <returns>An integer, 0 if node is nonexistent</returns>
        public int GetEdgesCountTo(int to)
        {
            if (!AntiEdges.HasNode(to)) return 0;
            return AntiEdges.GetNodeEdges(to).Count;
        }
        /// <summary>
        /// Adds a one-directional edge to this graph.
        /// Can be weighted.
        /// Replaces an existing edge if necessary.
        /// </summary>
        /// <param name="from">Edge start node id.</param>
        /// <param name="to">Edge end node id.</param>
        /// <param name="weight">Edge weight. Ignore this parameter in unweighted graphs.</param>
        public void AddEdge(int from, int to, int weight = 1)
        {
            Edges.Connect(from, to);
            AntiEdges.Connect(to, from);
            SetWeight(from, to, weight);
            if (!Nodes.Contains(from)) Nodes.Add(from);
            if (!Nodes.Contains(to)) Nodes.Add(to);
        }
        /// <summary>
        /// Removes the given edge from this graph.
        /// Only removes in the given direction.
        /// If the given edge is nonexistent, nothing happens.
        /// </summary>
        /// <param name="from">Edge start node id.</param>
        /// <param name="to">Edge end node id.</param>
        public void RemoveEdge(int from, int to)
        {
            Edges.Disconnect(from, to);
            AntiEdges.Disconnect(to, from);
        }
        /// <summary>
        /// Gets satellite info of a node.
        /// Can get from nonexistent nodes.
        /// </summary>
        /// <param name="id">The node id.</param>
        /// <param name="satelliteName">Specifies where the info is stored.</param>
        /// <returns>
        /// An object, null if there is no satellite info
        /// </returns>
        public object GetSatellite(int id, string satelliteName)
        {
            if (!NodeSatellites.ContainsKey(id)) return null;
            if (!NodeSatellites[id].ContainsKey(satelliteName)) return null;
            return NodeSatellites[id][satelliteName];
        }
        /// <summary>
        /// Stores satellite info on a node.
        /// Can store on nonexistent nodes.
        /// Overwrites previous info at the given satellite
        /// </summary>
        /// <param name="id">The node id.</param>
        /// <param name="satelliteName">Specifies where the info should be stored.</param>
        /// <param name="value">Specifies the object to store</param>
        public void SetSatellite(int id, string satelliteName, object value)
        {
            if (!NodeSatellites.ContainsKey(id)) NodeSatellites[id] = new();
            if (!Nodes.Contains(id)) Nodes.Add(id);
            NodeSatellites[id][satelliteName] = value;
        }
        /// <summary>
        /// Sets the weight of an edge.
        /// Can set the weight of a nonexistent edge.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="weight"></param>
        public void SetWeight(int from, int to, int weight)
        {
            Weights[(from, to)] = weight;
        }
        /// <summary>
        /// Gets the weight of an edge.
        /// </summary>
        /// <param name="from">Edge start node id.</param>
        /// <param name="to">Edge end node id.</param>
        /// <returns>An integer, null if the edge weight is nonexistent.</returns>
        public int? GetWeight(int from, int to)
        {
            if (!Weights.ContainsKey((from, to))) return null;
            return Weights[(from, to)];
        }
        /// <summary>
        /// Gets the quantity of edges between two nodes.
        /// </summary>
        /// <param name="a">Node id A.</param>
        /// <param name="b">Node id B.</param>
        /// <returns>0, 1 or 2.</returns>
        public int GetEdgeQuantityBetween(int a, int b)
        {
            int quantity = 0;
            if (Edges.HasEdge(a, b)) quantity++;
            if (Edges.HasEdge(b, a)) quantity++;
            return quantity;
        }

        /// <summary>
        /// "Edges" was taken.
        /// </summary>
        private class Edgees
        {
            private readonly Dictionary<int, HashSet<int>> edges = new();
            internal int NodeCount { get; private set; }

            internal bool HasNode(int id) => edges.ContainsKey(id);
            internal bool HasEdge(int from, int to)
                => edges.ContainsKey(from) && edges[from].Contains(to);
            internal ICollection<int> GetNodes() => edges.Keys;
            internal ICollection<int> GetNodeEdges(int id) => edges[id];
            internal void Connect(int from, int to)
            {
                if (!edges.ContainsKey(from))
                {
                    edges[from] = new();
                    NodeCount++;
                }
                edges[from].Add(to);
            }
            internal void Disconnect(int from, int to)
            {
                if (edges.ContainsKey(from))
                {
                    edges[from].Remove(to);
                    if (edges[from].Count == 0)
                    {
                        edges.Remove(from);
                        NodeCount--;
                    }
                }
            }
        }
    }
}

