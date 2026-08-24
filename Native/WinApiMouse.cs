using System;
using System.Runtime.InteropServices;

namespace Autoclicker.Native
{
	// Token: 0x02000008 RID: 8
	internal static class WinApiMouse
	{
		// Token: 0x0600008D RID: 141
		[DllImport("user32.dll")]
		public static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, UIntPtr dwExtraInfo);

		// Token: 0x0600008E RID: 142
		[DllImport("user32.dll", SetLastError = true)]
		public static extern uint SendInput(uint nInputs, WinApiMouse.INPUT[] pInputs, int cbSize);

		// Token: 0x0600008F RID: 143
		[DllImport("user32.dll")]
		public static extern bool GetCursorInfo(out CURSORINFO pci);

		// Token: 0x06000090 RID: 144
		[DllImport("user32.dll")]
		public static extern bool GetCursorPos(out WinApiMouse.POINT lpPoint);

		// Token: 0x06000091 RID: 145
		[DllImport("user32.dll")]
		public static extern short GetAsyncKeyState(int vKey);

		// Token: 0x06000092 RID: 146
		[DllImport("user32.dll")]
		public static extern IntPtr GetForegroundWindow();

		// Token: 0x06000093 RID: 147
		[DllImport("user32.dll")]
		public static extern IntPtr PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

		// Token: 0x06000094 RID: 148
		[DllImport("user32.dll")]
		public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

		// Token: 0x06000095 RID: 149 RVA: 0x00055EC0 File Offset: 0x00055EC0
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

		// Token: 0x06000096 RID: 150 RVA: 0x00055F54 File Offset: 0x00055F54
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

		// Token: 0x06000097 RID: 151 RVA: 0x00055FB8 File Offset: 0x00055FB8
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

		// Token: 0x0400009D RID: 157
		public const uint MOUSEEVENTF_LEFTDOWN = 2U;

		// Token: 0x0400009E RID: 158
		public const uint MOUSEEVENTF_LEFTUP = 4U;

		// Token: 0x0400009F RID: 159
		public const uint MOUSEEVENTF_RIGHTDOWN = 8U;

		// Token: 0x040000A0 RID: 160
		public const uint MOUSEEVENTF_RIGHTUP = 16U;

		// Token: 0x040000A1 RID: 161
		public const uint MOUSEEVENTF_MIDDLEDOWN = 32U;

		// Token: 0x040000A2 RID: 162
		public const uint MOUSEEVENTF_MIDDLEUP = 64U;

		// Token: 0x040000A3 RID: 163
		public const uint MOUSEEVENTF_MOVE = 1U;

		// Token: 0x040000A4 RID: 164
		public const int CURSOR_SHOWING = 1;

		// Token: 0x040000A5 RID: 165
		public const uint INPUT_MOUSE = 0U;

		// Token: 0x040000A6 RID: 166
		public const uint INPUT_KEYBOARD = 1U;

		// Token: 0x040000A7 RID: 167
		public const uint MI_LEFTDOWN = 2U;

		// Token: 0x040000A8 RID: 168
		public const uint MI_LEFTUP = 4U;

		// Token: 0x040000A9 RID: 169
		public const uint MI_MIDDLEDOWN = 32U;

		// Token: 0x040000AA RID: 170
		public const uint MI_MIDDLEUP = 64U;

		// Token: 0x040000AB RID: 171
		public const uint WM_LBUTTONDOWN = 513U;

		// Token: 0x040000AC RID: 172
		public const uint WM_LBUTTONUP = 514U;

		// Token: 0x040000AD RID: 173
		public const uint MK_LBUTTON = 1U;

		// Token: 0x02000047 RID: 71
		public struct POINT
		{
			// Token: 0x0400016F RID: 367
			public int X;

			// Token: 0x04000170 RID: 368
			public int Y;
		}

		// Token: 0x02000048 RID: 72
		public struct MOUSEINPUT
		{
			// Token: 0x04000171 RID: 369
			public int dx;

			// Token: 0x04000172 RID: 370
			public int dy;

			// Token: 0x04000173 RID: 371
			public uint mouseData;

			// Token: 0x04000174 RID: 372
			public uint dwFlags;

			// Token: 0x04000175 RID: 373
			public uint time;

			// Token: 0x04000176 RID: 374
			public UIntPtr dwExtraInfo;
		}

		// Token: 0x02000049 RID: 73
		// Поля этой структуры заполняются WinAPI/зарезервированы под объединение INPUT_UNION
		// и не используются напрямую в управляемом коде, поэтому CS0649 подавляется.
#pragma warning disable CS0649
		public struct KEYBDINPUT
		{
			// Token: 0x04000177 RID: 375
			public ushort wVk;

			// Token: 0x04000178 RID: 376
			public ushort wScan;

			// Token: 0x04000179 RID: 377
			public uint dwFlags;

			// Token: 0x0400017A RID: 378
			public uint time;

			// Token: 0x0400017B RID: 379
			public UIntPtr dwExtraInfo;
		}
#pragma warning restore CS0649

		// Token: 0x0200004A RID: 74
		[StructLayout(LayoutKind.Explicit)]
		public struct INPUT_UNION
		{
			// Token: 0x0400017C RID: 380
			[FieldOffset(0)]
			public WinApiMouse.MOUSEINPUT mi;

			// Token: 0x0400017D RID: 381
			[FieldOffset(0)]
			public WinApiMouse.KEYBDINPUT ki;
		}

		// Token: 0x0200004B RID: 75
		public struct INPUT
		{
			// Token: 0x0400017E RID: 382
			public uint type;

			// Token: 0x0400017F RID: 383
			public WinApiMouse.INPUT_UNION u;
		}
	}
}
