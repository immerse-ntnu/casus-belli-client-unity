using System;

namespace WorldMapStrategyKit
{
	public static class ArrayExtensions
	{
		public static void Fill<T>(this T[] destinationArray, params T[] value)
		{
			if (destinationArray == null)
				throw new ArgumentNullException(nameof(destinationArray));

			if (value.Length >= destinationArray.Length)
				throw new ArgumentException(
					"Length of value array must be less than length of destination");

			// set the initial array value
			Array.Copy(value, destinationArray, value.Length);

			var arrayToFillHalfLength = destinationArray.Length / 2;
			int copyLength;

			for (copyLength = value.Length; copyLength < arrayToFillHalfLength; copyLength <<= 1)
				Array.Copy(destinationArray, 0, destinationArray, copyLength, copyLength);

			Array.Copy(destinationArray, 0, destinationArray, copyLength,
				destinationArray.Length - copyLength);
		}

		public static void Fill<T>(this T[] destinationArray, T value)
		{
			if (destinationArray == null)
				throw new ArgumentNullException(nameof(destinationArray));

			if (0 >= destinationArray.Length)
				throw new ArgumentException(
					"Length of value array must be less than length of destination");

			// set the initial array value
			destinationArray[0] = value;

			var arrayToFillHalfLength = destinationArray.Length / 2;
			int copyLength;

			for (copyLength = 1; copyLength < arrayToFillHalfLength; copyLength <<= 1)
				Array.Copy(destinationArray, 0, destinationArray, copyLength, copyLength);

			Array.Copy(destinationArray, 0, destinationArray, copyLength,
				destinationArray.Length - copyLength);
		}

		public static T[] Purge<T>(this T[] array, int index)
		{
			var newArray = new T[array.Length - 1];
			for (int k = 0, c = 0; k < array.Length; k++)
				if (k != index)
					newArray[c++] = array[k];
			return newArray;
		}

		public static T[] Purge<T>(this T[] array, int index1, int index2)
		{
			var newArray = new T[array.Length - 2];
			for (int k = 0, c = 0; k < array.Length; k++)
				if (k != index1 && k != index2)
					newArray[c++] = array[k];
			return newArray;
		}
	}
}