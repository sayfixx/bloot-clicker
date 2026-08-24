using System;
using System.Runtime.InteropServices;

namespace Autoclicker.Native
{
	internal static class WinApiKeyboard
	{
		[DllImport("user32.dll")]
		public static extern bool keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

		[DllImport("user32.dll")]
		public static extern byte MapVirtualKey(byte wCode, int wMapType);

		public const uint KEYEVENTF_KEYUP = 2U;
	}
}
