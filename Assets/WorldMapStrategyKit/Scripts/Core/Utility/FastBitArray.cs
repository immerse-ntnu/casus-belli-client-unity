using System.Runtime.CompilerServices;
using UnityEngine;

namespace WorldMapStrategyKit
{
	public class FastBitArray
	{
		public uint[] bits;

		public FastBitArray(int capacity) => bits = new uint[capacity];

		public FastBitArray(Color32[] colors, byte alphaThreshold)
		{
			var pixelCount = colors.Length;
			bits = new uint[pixelCount];
			for (var k = 0; k < pixelCount; k++)
				if (colors[k].a < alphaThreshold)
					bits[k >> 5] |= (uint)(1 << (k & 31));
		}
#if !UNITY_WSA
		[MethodImpl(256)] // equals to MethodImplOptions.AggressiveInlining
#endif
		public void SetBit(int index)
		{
			bits[index >> 5] |= (uint)(1 << (index & 31));
		}

#if !UNITY_WSA
		[MethodImpl(256)] // equals to MethodImplOptions.AggressiveInlining
#endif
		public void ClearBit(int index)
		{
			bits[index >> 5] &= (uint)~(1 << (index & 31));
		}

#if !UNITY_WSA
		[MethodImpl(256)] // equals to MethodImplOptions.AggressiveInlining
#endif
		public bool GetBit(int index)
		{
			return (bits[index >> 5] & (1 << (index & 31))) != 0;
		}
	}
}