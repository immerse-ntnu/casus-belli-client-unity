using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TorbuTils.Noice
{
    [System.Serializable]
    public class Noise
    {
        [field: SerializeField] public int Seed { get; set; }
        [field: SerializeField] public int Octaves { get; set; } = 4;
        [field: SerializeField] public float NoiseScale { get; set; } = 1.5f;
        [field: SerializeField] public float Persistence { get; set; } = 0.5f;
        [field: SerializeField] public float Lacunarity { get; set; } = 2f;

        private int prevSeed;
        private int prevOctaves;
        private Vector2[] octaveOffsets = new Vector2[0];

        /// <summary>
        /// Gets the noise values in an area.
        /// </summary>
        /// <param name="lod">
        /// Level Of Detail.
        /// Must be [1, ->}.
        /// The main area will consist of (lod+1)^2 nodes.
        /// </param>
        /// <param name="margin">
        /// Margin width.
        /// Must be [0, ->}.
        /// The margin area will consist of (lod+1+2*margin)^2 - (lod+1)^2 nodes.
        /// </param>
        /// <returns>A matrix of Vector3s (sample.x, noiseValue, sample.z).
        /// The matrix size is (lod+1+2*margin)^2.</returns>
        public Vector3[,]
            GetNoiseArea(Vector2 sampleStart, Vector2 sampleEnd, int lod, int margin = 0)
        {
            if (lod <= 0)
            {
                Debug.LogWarning
                    ("Tried GetNoiseArea with LOD " + lod + ", which should be [1, ->}.");
                lod = 1;
            }
            if (margin < 0)
            {
                Debug.LogWarning
                    ("Tried GetNoiseArea with margin " + margin + ", which should be [0, ->}.");
                margin = 0;
            }

            Vector3[,] coord3s = new Vector3[lod + 1 + 2*margin, lod + 1 + 2*margin];
            for (int z = -margin; z < lod + 1 + margin; z++)
            {
                for (int x = -margin; x < lod + 1 + margin; x++)
                {
                    float coordX = sampleStart.x + (float)x / lod * (sampleEnd.x - sampleStart.x);
                    float coordZ = sampleStart.y + (float)z / lod * (sampleEnd.y - sampleStart.y);

                    Vector2 coord2 = new(coordX, coordZ);
                    float coordY = GetNoiseAt(coord2);
                    coord3s[x+margin, z+margin] = new Vector3(coordX, coordY, coordZ);
                }
            }
            return coord3s;
        }
        /// <summary>
        /// Gets the noise value at sample.
        /// </summary>
        /// <returns>A float *usually* in the [0, 1] range.</returns>
        public virtual float GetNoiseAt(Vector2 sample)
        {
            if (Octaves != prevOctaves || Seed != prevSeed) ResetOctaves();

            int octaves = Octaves;
            float noiseScale = NoiseScale;
            float persistence = Persistence;
            float lacunarity = Lacunarity;


            if (noiseScale == 0)
            {
                Debug.LogWarning
                    ("Tried GetNoiseAt when NoiseScale = " + noiseScale + ", cannot be 0.");
                noiseScale = 0.1f;
            }
            if (noiseScale == 0)
            {
                Debug.LogWarning
                    ("Tried GetNoiseAt when NoiseScale = " + noiseScale + ", cannot be 0.");
                noiseScale = 0.1f;
            }

            float result;

            float amplitude = 1f;
            float frequency = 1f;
            float noiseHeight = 0f;

            for (int i = 0; i < octaves; i++)
            {
                float sampleX = sample.x / noiseScale * frequency + octaveOffsets[i].x;
                float sampleZ = sample.y / noiseScale * frequency + octaveOffsets[i].y;
                float perlinValue = Mathf.PerlinNoise(sampleX, sampleZ) * 2 - 1;
                noiseHeight += perlinValue * amplitude;

                amplitude *= persistence;
                frequency *= lacunarity;
            }

            result = noiseHeight;
            return result;
        }
        private void ResetOctaves()
        {
            if (Octaves < 0)
            {
                Debug.LogWarning
                    ("Tried GetNoiseAt when Octaves = " + Octaves + ", must be [0, ->}.");
                Octaves = 0;
            }
            prevOctaves = Octaves;
            prevSeed = Seed;

            System.Random random = new(Seed);
            octaveOffsets = new Vector2[Octaves];
            for (int i = 0; i < Octaves; i++)
            {
                float offsetX = random.Next(-100000, 100000);
                float offsetY = random.Next(-100000, 100000);
                octaveOffsets[i] = new Vector2(offsetX, offsetY);
            }
        }
    }
}