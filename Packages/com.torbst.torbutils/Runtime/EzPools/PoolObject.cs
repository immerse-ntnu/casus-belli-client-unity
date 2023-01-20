using UnityEngine;

namespace TorbuTils
{
    namespace EzPools
    {
        public class PoolObject : MonoBehaviour
        {
            public Pool Origin { get; internal set; }
            public int LocalId { get; internal set; } = -1;
            public int GlobalId { get; internal set; } = -1;
        }
    }
}