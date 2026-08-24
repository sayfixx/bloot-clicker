using System;
using System.Globalization;

namespace Autoclicker.Minecraft
{
	internal static class HexBytes
	{
		public static byte[] Parse(string hex)
		{
			string[] array = hex.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			byte[] array2 = new byte[array.Length];
			for (int i = 0; i < array.Length; i++)
			{
				array2[i] = byte.Parse(array[i], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
			}
			return array2;
		}
	}
}
