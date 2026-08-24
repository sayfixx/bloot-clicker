using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Autoclicker.Native;

namespace Autoclicker.Minecraft
{

	internal static class WindowChecker
	{

		public static bool IsActiveCached()
		{
			long timestamp = Stopwatch.GetTimestamp();
			long num = Stopwatch.Frequency / 1000L * 50L;
			if (timestamp - WindowChecker._lastCheckTick >= num)
			{
				WindowChecker._cached = WindowChecker.IsActive();
				WindowChecker._lastCheckTick = timestamp;
			}
			return WindowChecker._cached;
		}

		public static bool IsActive()
		{
			bool result;
			try
			{
				IntPtr foregroundWindow = WinApiWindow.GetForegroundWindow();
				if (foregroundWindow == IntPtr.Zero)
				{
					result = false;
				}
				else
				{
					uint processId;
					WinApiWindow.GetWindowThreadProcessId(foregroundWindow, out processId);
					try
					{
						string text = Process.GetProcessById((int)processId).ProcessName.ToLowerInvariant();
						if (text == "minecraft.windows" || text == "minecraftlauncher" || text.StartsWith("minecraft-") || text == "bedrock" || text == "mcbe")
						{
							return true;
						}
					}
					catch
					{
					}
					StringBuilder stringBuilder = new StringBuilder(256);
					WinApiWindow.GetWindowText(foregroundWindow, stringBuilder, stringBuilder.Capacity);
					string text2 = stringBuilder.ToString().ToLowerInvariant();
					result = (text2.Contains("minecraft") || text2.Contains("bedrock") || text2.Contains("mcbe"));
				}
			}
			catch
			{
				result = false;
			}
			return result;
		}

		public static bool IsCursorVisible()
		{
			bool result;
			try
			{
				CURSORINFO cursorinfo = new CURSORINFO
				{
					cbSize = Marshal.SizeOf(typeof(CURSORINFO))
				};
				result = (WinApiMouse.GetCursorInfo(out cursorinfo) && (cursorinfo.flags & 1) != 0);
			}
			catch
			{
				result = true;
			}
			return result;
		}

		public static bool IsCursorHidden()
		{
			bool result;
			try
			{
				CURSORINFO cursorinfo = new CURSORINFO
				{
					cbSize = Marshal.SizeOf(typeof(CURSORINFO))
				};
				result = (WinApiMouse.GetCursorInfo(out cursorinfo) && (cursorinfo.flags & 1) == 0);
			}
			catch
			{
				result = false;
			}
			return result;
		}

		private static volatile bool _cached;

		private static long _lastCheckTick;

		private const long CHECK_INTERVAL_MS = 50L;
	}
}
