using System;
using System.Globalization;

namespace Autoclicker.Minecraft
{
	internal sealed class SignaturePattern
	{
		public byte?[] Bytes { get; set; }

		public static SignaturePattern Parse(string text)
		{
			string[] array = text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			byte?[] array2 = new byte?[array.Length];
			for (int i = 0; i < array.Length; i++)
			{
				byte?[] array3 = array2;
				int num = i;
				string a = array[i];
				bool flag = a == "?" || a == "??";
				array3[num] = (flag ? null : new byte?(byte.Parse(array[i], NumberStyles.HexNumber, CultureInfo.InvariantCulture)));
			}
			return new SignaturePattern
			{
				Bytes = array2
			};
		}

		public SignaturePattern()
		{
		}
	}
}
