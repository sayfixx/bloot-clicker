using System;
using System.Runtime.InteropServices;

namespace Autoclicker.Native
{
	internal static class WinApiMouse
	{
		[DllImport("user32.dll")]
		public static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, UIntPtr dwExtraInfo);

		[DllImport("user32.dll", SetLastError = true)]
		public static extern uint SendInput(uint nInputs, WinApiMouse.INPUT[] pInputs, int cbSize);

		[DllImport("user32.dll")]
		public static extern bool GetCursorInfo(out CURSORINFO pci);

		[DllImport("user32.dll")]
		public static extern bool GetCursorPos(out WinApiMouse.POINT lpPoint);

		[DllImport("user32.dll")]
		public static extern short GetAsyncKeyState(int vKey);

		[DllImport("user32.dll")]
		public static extern IntPtr GetForegroundWindow();

		[DllImport("user32.dll")]
		public static extern IntPtr PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

		[DllImport("user32.dll")]
		public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

		public static void SendMouseInput(uint flags, UIntPtr extraInfo)
		{
			WinApiMouse.INPUT input = new WinApiMouse.INPUT
			{
				type = 0U,
				u = new WinApiMouse.INPUT_UNION
				{
					mi = new WinApiMouse.MOUSEINPUT
					{
						dx = 0,
						dy = 0,
						mouseData = 0U,
						dwFlags = flags,
						time = 0U,
						dwExtraInfo = extraInfo
					}
				}
			};
			WinApiMouse.SendInput(1U, new WinApiMouse.INPUT[] { input }, Marshal.SizeOf(typeof(WinApiMouse.INPUT)));
		}

		public static void SendGdkClick(bool down)
		{
			IntPtr foregroundWindow = WinApiMouse.GetForegroundWindow();
			if (foregroundWindow == IntPtr.Zero)
			{
				return;
			}
			WinApiMouse.POINT point;
			WinApiMouse.GetCursorPos(out point);
			IntPtr intPtr = (IntPtr)((point.Y << 16) | (point.X & 65535));
			if (down)
			{
				WinApiMouse.PostMessage(foregroundWindow, 513U, (IntPtr)1, intPtr);
				return;
			}
			WinApiMouse.PostMessage(foregroundWindow, 514U, IntPtr.Zero, intPtr);
		}

		public static void ApplyJitter(int strength, Random rnd)
		{
			if (strength <= 0)
			{
				return;
			}
			int num = rnd.Next(-strength, strength + 1);
			int num2 = rnd.Next(-strength, strength + 1);
			WinApiMouse.mouse_event(1U, num, num2, 0U, UIntPtr.Zero);
		}

		public const uint MOUSEEVENTF_LEFTDOWN = 2U;

		public const uint MOUSEEVENTF_LEFTUP = 4U;

		public const uint MOUSEEVENTF_RIGHTDOWN = 8U;

		public const uint MOUSEEVENTF_RIGHTUP = 16U;

		public const uint MOUSEEVENTF_MIDDLEDOWN = 32U;

		public const uint MOUSEEVENTF_MIDDLEUP = 64U;

		public const uint MOUSEEVENTF_MOVE = 1U;

		public const int CURSOR_SHOWING = 1;

		public const uint INPUT_MOUSE = 0U;

		public const uint INPUT_KEYBOARD = 1U;

		public const uint MI_LEFTDOWN = 2U;

		public const uint MI_LEFTUP = 4U;

		public const uint MI_MIDDLEDOWN = 32U;

		public const uint MI_MIDDLEUP = 64U;

		public const uint WM_LBUTTONDOWN = 513U;

		public const uint WM_LBUTTONUP = 514U;

		public const uint MK_LBUTTON = 1U;

		public struct POINT
		{
			public int X;

			public int Y;
		}

		public struct MOUSEINPUT
		{
			public int dx;

			public int dy;

			public uint mouseData;

			public uint dwFlags;

			public uint time;

			public UIntPtr dwExtraInfo;
		}

#pragma warning disable CS0649
		public struct KEYBDINPUT
		{
			public ushort wVk;

			public ushort wScan;

			public uint dwFlags;

			public uint time;

			public UIntPtr dwExtraInfo;
		}
#pragma warning restore CS0649

		[StructLayout(LayoutKind.Explicit)]
		public struct INPUT_UNION
		{
			[FieldOffset(0)]
			public WinApiMouse.MOUSEINPUT mi;

			[FieldOffset(0)]
			public WinApiMouse.KEYBDINPUT ki;
		}

		public struct INPUT
		{
			public uint type;

			public WinApiMouse.INPUT_UNION u;
		}
	}
}
