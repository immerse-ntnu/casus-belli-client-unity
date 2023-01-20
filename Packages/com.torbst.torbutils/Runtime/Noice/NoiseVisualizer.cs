using UnityEngine;

namespace TorbuTils.Noice
{
    [ExecuteAlways]
    public class NoiseVisualizer : MonoBehaviour
    {
        public enum GizmosMode
        {
            Always,
            Selected,
            Never
        }
        [field: SerializeField] public Noise Noise { get; set; }
        [field: SerializeField] public GizmosMode Mode { get; set; }
        [field: SerializeField] public Color MainColor { get; set; } = Color.white;
        [field: SerializeField] public Color MarginColor { get; set; } = Color.red;
        [field: SerializeField] public int LevelOfDetail { get; set; } = 20;
        [field: SerializeField] public int Margin { get; set; } = 0;
        [field: SerializeField] public Vector2 VisualizedValuesRange { get; set; } = new(-1f, 1f);
        public Vector2 SampleStart => new
            (sampleStartTransform.position.x, sampleStartTransform.position.z);
        public Vector2 SampleEnd => new
            (sampleEndTransform.position.x, sampleEndTransform.position.z);

        private Vector3[,] matrix;
        private Transform sampleStartTransform;
        private Transform sampleEndTransform;
        private float noiseMin;
        private float noiseMax;
        private float noiseAVG;
        private float noiseSTD;

        private void OnDrawGizmos()
        {
            if (Mode == GizmosMode.Always) DoGizmos();
        }
        private void OnDrawGizmosSelected()
        {
            if (Mode == GizmosMode.Selected) DoGizmos();
        }
        private void OnValidate()
        {
            if (sampleStartTransform == null || sampleEndTransform == null) return;
            Recalculate();
        }

        public void Recalculate()
        {
            matrix = Noise.GetNoiseArea(SampleStart, SampleEnd, LevelOfDetail, Margin);
            float noiseSum = 0f;
            float minNoise = float.MaxValue;
            float maxNoise = float.MinValue;
            foreach (var pos in matrix)
            {
                float y = pos.y;
                noiseSum += y;
                if (minNoise > y) minNoise = y;
                if (maxNoise < y) maxNoise = y;
            }
            noiseAVG = float.MinValue;
            noiseSTD = float.MinValue;
            if (matrix.Length > 0)
            {
                noiseAVG = noiseSum / matrix.Length;
                float deviationSum = 0f;
                foreach (var pos in matrix)
                {
                    deviationSum += Mathf.Abs(pos.y - noiseAVG);
                }
                noiseSTD = deviationSum / matrix.Length;
            }
            noiseMin = minNoise;
            noiseMax = maxNoise;
        }
        private void RedoSampleTransforms()
        {
            while (transform.GetComponentsInChildren<SampleHelper>().Length < 2)
            {
                GameObject go = new();
                Transform t = go.transform;
                t.parent = transform;
                t.gameObject.AddComponent<SampleHelper>();
            }
            int i = 0;
            foreach (SampleHelper helper in transform.GetComponentsInChildren<SampleHelper>())
            {
                if (i == 0)
                {
                    helper.gameObject.name = "Sample Start (Noise)";
                    sampleStartTransform = helper.transform;
                }
                else if (i == 1)
                {
                    helper.gameObject.name = "Sample End (Noise)";
                    sampleEndTransform = helper.transform;
                }
                else break;
                i++;
            }
        }
        private void DoGizmos()
        {
            if (sampleStartTransform == null || sampleEndTransform == null)
            {
                RedoSampleTransforms();
            }
            if (matrix == null) Recalculate();
            int zLength = matrix.GetLength(1);
            int xLength = matrix.GetLength(0);
            Vector3 boxSize = new(
                (SampleEnd.x - SampleStart.x)/LevelOfDetail,
                1f,
                (SampleEnd.y - SampleStart.y)/LevelOfDetail
                );
            for (int z = 0; z < zLength; z++)
            {
                for (int x = 0; x < xLength; x++)
                {
                    Vector3 pos = matrix[x, z];
                    Color color;
                    if (x < Margin || x >= xLength - Margin ||
                        z < Margin || z >= zLength - Margin)
                        color = MarginColor;
                    else color = MainColor;
                    float y = Mathf.InverseLerp(VisualizedValuesRange.x, VisualizedValuesRange.y, pos.y);
                    Gizmos.color = new Color(y, y, y, 1f) * color;
                    Gizmos.DrawCube(new(pos.x, 0f, pos.z), boxSize);
                }
            }
        }
    }
}