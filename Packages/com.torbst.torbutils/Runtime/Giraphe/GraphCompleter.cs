using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

namespace TorbuTils.Giraphe
{
    public class GraphCompleter
    {
        public event Action Done;
        private readonly Graph inputGraph;
        [field: SerializeField] public Graph ResultTree { get; private set; }
        public GraphCompleter(Graph inputGraph)
        {
            this.inputGraph = inputGraph;
            ResultTree = Graph.MakeFromSatellites(inputGraph);
        }
        public IEnumerable Solve()
        {
            ICollection<int> nodes = ResultTree.CopyNodes();
            foreach (int idA in nodes)
            {
                foreach (int idB in nodes)
                {
                    if (idA == idB) continue;
                    ResultTree.AddEdge(idA, idB);
                    yield return null;
                }
            }
            Done?.Invoke();
        }
    }
}
