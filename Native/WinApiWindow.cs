using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Autoclicker.Native
{
	internal static class WinApiWindow
	{
		[DllImport("user32.dll")]
		public static extern IntPtr GetForegroundWindow();

		[DllImport("user32.dll", CharSet = CharSet.Unicode)]
		public static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

		[DllImport("user32.dll", CharSet = CharSet.Unicode)]
		public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

		[DllImport("user32.dll")]
		public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

		[DllImport("user32.dll")]
		public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

		[DllImport("user32.dll")]
		public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

		[DllImport("user32.dll")]
		public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

		[DllImport("user32.dll")]
		public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

		[DllImport("user32.dll")]
		public static extern bool SetWindowDisplayAffinity(IntPtr hwnd, uint dwAffinity);

		[DllImport("user32.dll")]
		public static extern bool GetWindowDisplayAffinity(IntPtr hwnd, out uint dwAffinity);

		[DllImport("user32.dll")]
		public static extern IntPtr WindowFromPoint(WinApiWindow.POINT point);

		[DllImport("user32.dll")]
		public static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);

		public const uint GA_ROOT = 2U;

		public const int SW_HIDE = 0;

		public const int SW_RESTORE = 9;

		public const int GWL_EXSTYLE = -20;

		public const int WS_EX_TOOLWINDOW = 128;

		public const int WS_EX_APPWINDOW = 262144;

		public const uint WDA_NONE = 0U;

		public const uint WDA_EXCLUDEFROMCAPTURE = 17U;

		public const uint SWP_FRAMECHANGED = 32U;

		public const uint SWP_NOZORDER = 4U;

		public const uint SWP_NOMOVE = 2U;

		public const uint SWP_NOSIZE = 1U;

		public const uint SWP_SHOWWINDOW = 64U;

#pragma warning disable CS0649
		public struct POINT
		{
			public int X;

			public int Y;
		}
#pragma warning restore CS0649
	}
}
