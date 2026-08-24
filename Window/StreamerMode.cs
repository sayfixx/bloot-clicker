using System;
using System.Runtime.CompilerServices;
using System.Windows.Interop;
using Autoclicker.Native;

namespace Autoclicker.Window
{

			internal static class StreamerMode
	{

		public static void Apply(MainWindow mw, bool enable)
		{
			try
			{
				mw.Dispatcher.Invoke(delegate()
				{
					IntPtr handle = new WindowInteropHelper(mw).Handle;
					if (handle == IntPtr.Zero)
					{
						return;
					}
					if (enable)
					{
						if (mw.OriginalWindowExStyle == IntPtr.Zero)
						{
							mw.OriginalWindowExStyle = (IntPtr)WinApiWindow.GetWindowLong(handle, -20);
						}
						mw.ShowInTaskbar = false;
						int windowLong = WinApiWindow.GetWindowLong(handle, -20);
						WinApiWindow.SetWindowLong(handle, -20, windowLong | 128);
						try
						{
							WinApiWindow.SetWindowDisplayAffinity(handle, 17U);
							return;
						}
						catch
						{
							return;
						}
					}
					if (mw.OriginalWindowExStyle != IntPtr.Zero)
					{
						WinApiWindow.SetWindowLong(handle, -20, (int)mw.OriginalWindowExStyle);
					}
					try
					{
						WinApiWindow.SetWindowDisplayAffinity(handle, 0U);
					}
					catch
					{
					}
					mw.ShowInTaskbar = true;
					int windowLong2 = WinApiWindow.GetWindowLong(handle, -20);
					WinApiWindow.SetWindowLong(handle, -20, windowLong2 | 262144);
					WinApiWindow.SetWindowPos(handle, IntPtr.Zero, 0, 0, 0, 0, 103U);
					mw.OriginalWindowExStyle = IntPtr.Zero;
				});
			}
			catch (Exception)
			{
			}
		}

		public static void Toggle(MainWindow mw)
		{
			try
			{
				mw.Dispatcher.Invoke(delegate()
				{
					IntPtr handle = new WindowInteropHelper(mw).Handle;
					if (mw.IsHidden)
					{
						WinApiWindow.ShowWindow(handle, 9);
						mw.ShowInTaskbar = true;
						mw.Show();
						mw.IsHidden = false;
						return;
					}
					WinApiWindow.ShowWindow(handle, 0);
					mw.ShowInTaskbar = false;
					mw.Hide();
					mw.IsHidden = true;
				});
			}
			catch
			{
			}
		}
	}
}
