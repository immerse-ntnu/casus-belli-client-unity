using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TorbuTils
{
    namespace EzPools
    {
        public class Pools : MonoBehaviour
        {
            public static Pools Instance { get; private set; }
            internal int GlobalCount { get; private set; } = 0;
            private Dictionary<GameObject, Pool> pools = new();
            private void Awake()
            {
                Instance = this;
            }
            private void OnTransformChildrenChanged()
            {
                pools = new();
                foreach (var pool in GetComponentsInChildren<Pool>())
                {
                    pools.Add(pool.Prefab, pool);
                }
            }

            public void Enpool(GameObject go)
            {
                PoolObject poolObject = go.GetComponent<PoolObject>();
                if (poolObject == null)
                {
                    Destroy(go);
                    return;
                }
                Pool origin = poolObject.Origin;
                if (origin == null)
                {
                    Destroy(go);
                    return;
                }
                go.SetActive(false);
                origin.Queue.Enqueue(go);
            }
            public GameObject Depool(GameObject prefab)
            {
                Pool pool;
                if (!pools.ContainsKey(prefab))
                {
                    GameObject poolGO = new(prefab.name);
                    pool = poolGO.AddComponent<Pool>();
                    pool.Prefab = prefab;
                    poolGO.transform.SetParent(transform);
                }
                else pool = pools[prefab];

                GameObject go;
                PoolObject poolObject;
                if (pool.Queue.Count > 0)
                {
                    go = pool.Queue.Dequeue();
                    poolObject = go.GetComponent<PoolObject>();
                }
                else
                {
                    go = Instantiate(prefab);
                    poolObject = go.AddComponent<PoolObject>();
                    poolObject.Origin = pool;
                    poolObject.LocalId = pool.LocalCount;
                    poolObject.GlobalId = GlobalCount;
                    pool.LocalCount++;
                    GlobalCount++;
                }
                go.SetActive(true);
                return go;
            }
        }
    }
}