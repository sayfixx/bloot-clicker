using System;

namespace Autoclicker.Minecraft
{
	internal static class PatternScanner
	{
		public static int Find(byte[] buffer, SignaturePattern pattern)
		{
			int num = buffer.Length - pattern.Bytes.Length;
			for (int i = 0; i <= num; i++)
			{
				bool flag = true;
				for (int j = 0; j < pattern.Bytes.Length; j++)
				{
					byte? b = pattern.Bytes[j];
					if (b != null && buffer[i + j] != b.Value)
					{
						flag = false;
						break;
					}
				}
				if (flag)
				{
					return i;
				}
			}
			return -1;
		}
	}
}
