using UnityEngine;
using System;
using System.Collections.Generic;

namespace TorbuTils
{
    namespace EzPools
    {
        public class Pool : MonoBehaviour
        {
            internal GameObject Prefab { get; set; }
            internal Queue<GameObject> Queue { get; private set; } = new();
            internal int LocalCount { get; set; } = 0;
        }
    }
}

