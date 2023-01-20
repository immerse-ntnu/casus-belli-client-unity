using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace TorbuTils.Giraphe
{
    [Serializable]
    public class Dijkstra
    {
        public event Action Done;
        private readonly Graph inputGraph;
        private readonly int startId;
        private readonly int maxDistance;
        [field: SerializeField] public Graph ResultTree { get; private set; }
        public Dijkstra(Graph inputGraph, int startId, int maxDistance = int.MaxValue)
        {
            this.inputGraph = inputGraph;
            this.startId = startId;
            this.maxDistance = maxDistance;
            ResultTree = Graph.MakeFromSatellites(inputGraph);
        }
        public IEnumerable Solve()
        {
            Queue<int> queue = new();  // ids
            queue.Enqueue(startId);
            ResultTree.SetSatellite(startId, "costhere", 0);

            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                int? ch = (int?)ResultTree.GetSatellite(current, "costhere");
                int costHere = ch == null ? 0 : ch.Value;
                foreach (int next in inputGraph.CopyEdgesFrom(current))
                {
                    yield return null;
                    int hypoCost = costHere + (int)inputGraph.GetWeight(current, next);
                    if (hypoCost > maxDistance) continue;
                    int? prevCost = (int?)ResultTree.GetSatellite(next, "costhere");
                    if (prevCost == null || hypoCost < prevCost)
                    {

                        if (prevCost != null)
                        {
                            foreach (int backtrack in ResultTree.CopyEdgesTo(next))
                            {
                                ResultTree.RemoveEdge(backtrack, next);
                            }
                        }

                        ResultTree.AddEdge(current, next);
                        ResultTree.SetSatellite(next, "costhere", hypoCost);
                        queue.Enqueue(next);
                    }
                }
            }
            Done?.Invoke();
        }
    }
}