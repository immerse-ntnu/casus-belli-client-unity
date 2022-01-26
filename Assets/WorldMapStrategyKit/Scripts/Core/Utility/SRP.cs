using UnityEngine;
using UnityEngine.Rendering;

namespace WorldMapStrategyKit
{
	public static class SRP
	{
		private static bool isLWRP
		{
			get
			{
#if UNITY_2019_1_OR_NEWER
				if (GraphicsSettings.renderPipelineAsset == null)
					return false;
				var pipe = GraphicsSettings.renderPipelineAsset.name;
				return pipe.Contains("LWRP") ||
				       pipe.Contains("Lightweight") ||
				       pipe.Contains("Universal") ||
				       pipe.Contains("URP");
#else
                return false;
#endif
			}
		}

		public static void Configure(Material mat)
		{
			if (mat == null || mat.shader == null)
				return;

			var n = mat.shader.name;
			var mat_LWRP_PrefixIndex = n.IndexOf("LWRP");
			var LWRP = isLWRP;
			if (mat_LWRP_PrefixIndex < 0 && LWRP)
			{
				var i = n.LastIndexOf('/');
				if (i >= 0)
					n = n.Substring(i + 1);
				var sn = "Shader Graphs/LWRP " + n;
				var comp = Shader.Find(sn);
				if (comp != null)
					mat.shader = comp;
			}
			else if (mat_LWRP_PrefixIndex >= 0 && !LWRP)
			{
				var sn = "WMSK/" + n.Substring(mat_LWRP_PrefixIndex + 5);
				var comp = Shader.Find(sn);
				if (comp != null)
					mat.shader = comp;
			}
		}

		public static void ConfigureTerrainShader(Material mat)
		{
			if (mat == null || mat.shader == null)
				return;

			var n = mat.shader.name;
			var mat_LWRP_PrefixIndex = n.IndexOf("URP");
			var LWRP = isLWRP;
			if (mat_LWRP_PrefixIndex < 0 && LWRP)
			{
				var i = n.LastIndexOf('/');
				if (i >= 0)
					n = n.Substring(i + 1);
				var sn = "WMSK/URP/" + n;
				var comp = Shader.Find(sn);
				if (comp != null)
					mat.shader = comp;
				else
					Debug.LogError(
						"World Map Strategy Kit: URP compatible terrain shader not found. Please import the URP terrain shaders package from WMSK/Resources/WMSK/Shaders/LWRP/TerrainShaders folder.");
			}
			else if (mat_LWRP_PrefixIndex >= 0 && !LWRP)
			{
				var sn = "WMSK" + n.Substring(mat_LWRP_PrefixIndex + 3);
				var comp = Shader.Find(sn);
				if (comp != null)
					mat.shader = comp;
			}
		}
	}
}