using System;
using System.Collections.Generic;
using UnityEngine;
using WorldMapStrategyKit.MapGenerator;

namespace WorldMapStrategyKit
{
	[Serializable]
	public struct NoiseOctave
	{
		public bool disabled;
		public float frecuency;
		public float amplitude;
		public bool ridgeNoise;
	}

	public enum MapGenerationQuality
	{
		Draft = 0,
		Final = 1
	}

	public enum HeightMapGradientPreset
	{
		Colored = 0,
		ColoredLight = 1,
		Grayscale = 10,
		BlackAndWhite = 11,
		Custom = 20
	}

	public partial class WMSK_Editor : MonoBehaviour
	{
		// Custom inspector stuff
		public const int MAX_TERRITORIES = 256;
		public const int MAX_CELLS = 10000;
		public const int MAX_CELLS_SQRT = 100;
		public const int MAX_CELLS_FOR_RELAXATION = 5000;

		public string outputFolder = "CustomMap";

		[Range(0, 10000)] public int seed = 1;

		[Range(0, 10000)] public int seedNames = 1;

		[SerializeField, Range(1, 32)] public int gridRelaxation = 1;

		private int goodGridRelaxation
		{
			get
			{
				if (numProvinces >= MAX_CELLS_FOR_RELAXATION)
					return 1;
				return gridRelaxation;
			}
		}

		[SerializeField, Range(0.001f, 0.1f)] public float edgeMaxLength = 0.05f;

		[SerializeField, Range(0f, 1f)] public float edgeNoise = 0.25f;

		public MapGenerationQuality mapGenerationQuality;

		[NonSerialized] public List<MapCity> mapCities;

		public int numCitiesPerCountryMin = 3;
		public int numCitiesPerCountryMax = 10;

		[NonSerialized] public List<MapCountry> mapCountries;

		/// <summary>
		/// Gets or sets the number of territories.
		/// </summary>
		[Range(1, MAX_TERRITORIES)] public int numCountries = 32;

		public int backgroundTextureWidth = 2048;
		public int backgroundTextureHeight = 1024;
		[NonSerialized] public Texture2D backgroundTexture;

		[NonSerialized] public Texture2D heightMapTexture;
		[NonSerialized] public Texture2D waterMaskTexture;
		public Texture2D userHeightMapTexture;
		public int heightMapWidth = 2048;
		public int heightMapHeight = 1024;
		public HeightMapGradientPreset heightGradientPreset = HeightMapGradientPreset.Grayscale;
		public bool gradientPerPixel;
		public Gradient heightGradient;
		public bool changeStyle = true;

		public bool octavesBySeed = true;
		public NoiseOctave[] noiseOctaves;

		[Range(0.01f, 7f)] public float noisePower = 3f;

		[Range(0, 16)] public float islandFactor = 0.5f;

		[Range(0, 1f)] public float seaLevel = 0.2f;

		public Color seaColor = new(0, 0.4f, 1f);

		[Range(-1, 1f)] public float elevationShift;

		/// <summary>
		/// Complete array of states and cells and the territory name they belong to.
		/// </summary>
		[NonSerialized] public List<MapProvince> mapProvinces;

		[Range(2, MAX_CELLS)] public int numProvinces = 256;

		[NonSerialized] public List<Vector2> voronoiSites;

		public bool generateNormalMap;
		public float normalMapBumpiness = 0.1f;
	}
}