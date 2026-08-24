using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;

namespace Autoclicker.Hooks
{
	internal static class KeyboardHook
	{
		public static void Install()
		{
			KeyboardHook.HookID = HookSetup.SetKeyboardHook(KeyboardHook._proc);
		}

		public static void Uninstall()
		{
			if (KeyboardHook.HookID != IntPtr.Zero)
			{
				HookSetup.UnhookWindowsHookEx(KeyboardHook.HookID);
				KeyboardHook.HookID = IntPtr.Zero;
			}
		}

		private static IntPtr Callback(int nCode, IntPtr wParam, IntPtr lParam)
		{
			if (nCode < 0)
			{
				return HookSetup.CallNextHookEx(KeyboardHook.HookID, nCode, wParam, lParam);
			}

			try
			{
				Key key = KeyInterop.KeyFromVirtualKey(Marshal.ReadInt32(lParam));
				Application application = Application.Current;
				MainWindow mainWindow = ((application != null) ? application.MainWindow : null) as MainWindow;
				if (mainWindow != null)
				{
					if (mainWindow.WaitingForKey && wParam == (IntPtr)256)
					{
						if (mainWindow.WaitingForSprintBind)
						{
							mainWindow.SetSprintBindFromKey(key);
							return HookSetup.CallNextHookEx(KeyboardHook.HookID, nCode, wParam, lParam);
						}
						mainWindow.SetClickerBindFromKey(key);
						return HookSetup.CallNextHookEx(KeyboardHook.HookID, nCode, wParam, lParam);
					}
					else if (!mainWindow.WaitingForKey && mainWindow.SelectedKey != Key.None && key == mainWindow.SelectedKey)
					{
						if (wParam == (IntPtr)256 && !mainWindow.BindKeyPressed)
						{
							mainWindow.BindKeyPressed = true;
							mainWindow.SetClickerEnabled(!mainWindow.ClickerEnabled);
							Debug.WriteLine("[DEBUG] Toggle key pressed -> ClickerEnabled = " + mainWindow.ClickerEnabled);
						}
						else if (wParam == (IntPtr)257)
						{
							mainWindow.BindKeyPressed = false;
						}
						return HookSetup.CallNextHookEx(KeyboardHook.HookID, nCode, wParam, lParam);
					}
				}
			}
			catch
			{
			}
			return HookSetup.CallNextHookEx(KeyboardHook.HookID, nCode, wParam, lParam);
		}

		public static IntPtr HookID = IntPtr.Zero;

		private static readonly HookSetup.LowLevelKeyboardProc _proc = new HookSetup.LowLevelKeyboardProc(KeyboardHook.Callback);
	}
}
