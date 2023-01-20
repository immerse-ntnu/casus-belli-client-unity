using UnityEngine;

namespace TorbuTils.Noice
{
    [ExecuteAlways]
    internal class SampleHelper : MonoBehaviour
    {
        private Vector2 prevPos;
        private NoiseVisualizer parent;

        private void Update()
        {
            Vector2 pos = transform.position;
            if (prevPos != pos)
            {
                prevPos = pos;
                if (parent == null) parent = transform.parent.GetComponent<NoiseVisualizer>();
                parent.Recalculate();
            }
        }
    }
}