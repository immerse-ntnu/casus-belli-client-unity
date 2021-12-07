#if !UNITY_WEBGL && !UNITY_IOS && !UNITY_WSA
using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Scripting;

namespace WorldMapStrategyKit
{
	public sealed class GeometryUtilityNonAlloc
	{
		private static Action<Plane[], Matrix4x4> _calculateFrustumPlanes_Imp;

		public static void CalculateFrustumPlanes(Plane[] planes, Matrix4x4 worldToProjectMatrix)
		{
			if (planes == null)
				throw new ArgumentNullException("planes");
			if (planes.Length < 6)
				throw new ArgumentException("Output array must be at least 6 in length.", "planes");

			if (_calculateFrustumPlanes_Imp == null)
			{
				var meth = typeof(GeometryUtility).GetMethod("Internal_ExtractPlanes",
					System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic, null,
					new Type[]
					{
						typeof(Plane[]),
						typeof(Matrix4x4)
					}, null);
				if (meth == null)
					throw new Exception(
						"Failed to reflect internal method. Your Unity version may not contain the presumed named method in GeometryUtility.");

				_calculateFrustumPlanes_Imp =
					Delegate.CreateDelegate(typeof(Action<Plane[], Matrix4x4>), meth) as
						Action<Plane[], Matrix4x4>;
				if (_calculateFrustumPlanes_Imp == null)
					throw new Exception(
						"Failed to reflect internal method. Your Unity version may not contain the presumed named method in GeometryUtility.");
			}

			_calculateFrustumPlanes_Imp(planes, worldToProjectMatrix);
		}
	}
}
#endif