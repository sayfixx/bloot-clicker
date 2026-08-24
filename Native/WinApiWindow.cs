using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Autoclicker.Native
{
	// Token: 0x0200000A RID: 10
	internal static class WinApiWindow
	{
		// Token: 0x060000A6 RID: 166
		[DllImport("user32.dll")]
		public static extern IntPtr GetForegroundWindow();

		// Token: 0x060000A7 RID: 167
		[DllImport("user32.dll", CharSet = CharSet.Unicode)]
		public static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

		// Token: 0x060000A8 RID: 168
		[DllImport("user32.dll", CharSet = CharSet.Unicode)]
		public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

		// Token: 0x060000A9 RID: 169
		[DllImport("user32.dll")]
		public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

		// Token: 0x060000AA RID: 170
		[DllImport("user32.dll")]
		public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

		// Token: 0x060000AB RID: 171
		[DllImport("user32.dll")]
		public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

		// Token: 0x060000AC RID: 172
		[DllImport("user32.dll")]
		public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

		// Token: 0x060000AD RID: 173
		[DllImport("user32.dll")]
		public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

		// Token: 0x060000AE RID: 174
		[DllImport("user32.dll")]
		public static extern bool SetWindowDisplayAffinity(IntPtr hwnd, uint dwAffinity);

		// Token: 0x060000AF RID: 175
		[DllImport("user32.dll")]
		public static extern bool GetWindowDisplayAffinity(IntPtr hwnd, out uint dwAffinity);

		// Token: 0x060000B0 RID: 176
		[DllImport("user32.dll")]
		public static extern IntPtr WindowFromPoint(WinApiWindow.POINT point);

		// Token: 0x060000B1 RID: 177
		[DllImport("user32.dll")]
		public static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);

		// Token: 0x040000AE RID: 174
		public const uint GA_ROOT = 2U;

		// Token: 0x040000AF RID: 175
		public const int SW_HIDE = 0;

		// Token: 0x040000B0 RID: 176
		public const int SW_RESTORE = 9;

		// Token: 0x040000B1 RID: 177
		public const int GWL_EXSTYLE = -20;

		// Token: 0x040000B2 RID: 178
		public const int WS_EX_TOOLWINDOW = 128;

		// Token: 0x040000B3 RID: 179
		public const int WS_EX_APPWINDOW = 262144;

		// Token: 0x040000B4 RID: 180
		public const uint WDA_NONE = 0U;

		// Token: 0x040000B5 RID: 181
		public const uint WDA_EXCLUDEFROMCAPTURE = 17U;

		// Token: 0x040000B6 RID: 182
		public const uint SWP_FRAMECHANGED = 32U;

		// Token: 0x040000B7 RID: 183
		public const uint SWP_NOZORDER = 4U;

		// Token: 0x040000B8 RID: 184
		public const uint SWP_NOMOVE = 2U;

		// Token: 0x040000B9 RID: 185
		public const uint SWP_NOSIZE = 1U;

		// Token: 0x040000BA RID: 186
		public const uint SWP_SHOWWINDOW = 64U;

		// Token: 0x0200004C RID: 76
		// Поля заполняются самой WinAPI (WindowFromPoint/GetCursorPos), поэтому CS0649 подавляется.
#pragma warning disable CS0649
		public struct POINT
		{
			// Token: 0x04000180 RID: 384
			public int X;

			// Token: 0x04000181 RID: 385
			public int Y;
		}
#pragma warning restore CS0649
	}
}
