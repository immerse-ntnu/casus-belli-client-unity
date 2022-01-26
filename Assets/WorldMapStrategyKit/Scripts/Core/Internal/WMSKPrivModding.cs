// World Map Strategy Kit for Unity - Main Script
// (C) 2016-2020 by Ramiro Oliva (Kronnect)
// Don't modify this script - changes could be lost if you upgrade to a more recent version of WMSK

using System.IO;
using UnityEngine;

namespace WorldMapStrategyKit
{
	public partial class WMSK : MonoBehaviour
	{
		#region Region related functions

		private bool LoadProvinceMap(string path)
		{
			var bytes = File.ReadAllBytes(path);

			var tex = new Texture2D(2, 2, TextureFormat.ARGB32, false, true);
			tex.LoadImage(bytes);

			return true;
		}

		#endregion
	}
}