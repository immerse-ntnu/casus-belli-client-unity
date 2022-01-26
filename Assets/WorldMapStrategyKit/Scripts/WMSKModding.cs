// World Strategy Kit for Unity - Main Script
// (C) 2016-2020 by Ramiro Oliva (Kronnect)
// Don't modify this script - changes could be lost if you upgrade to a more recent version of WMSK

using System.IO;
using UnityEngine;

namespace WorldMapStrategyKit
{
	public enum RESOURCE_FILE_TYPE
	{
		PROVINCES = 10
	}

	public partial class WMSK : MonoBehaviour
	{
		#region Public API area

		/// <summary>
		/// Loads a resource file at runtime
		/// </summary>
		/// <param name="path">Path.</param>
		/// <param name="type">Type.</param>
		public bool Load(string path, RESOURCE_FILE_TYPE type)
		{
			if (!File.Exists(path))
				return false;

			switch (type)
			{
				case RESOURCE_FILE_TYPE.PROVINCES:
					return LoadProvinceMap(path);
			}

			return false;
		}

		#endregion
	}
}