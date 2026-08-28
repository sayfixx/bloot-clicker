using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Autoclicker.Native;

namespace Autoclicker.Hooks
{
	internal static class HookSetup
	{
		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr SetWindowsHookEx(int idHook, HookSetup.LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr SetWindowsHookEx(int idHook, HookSetup.LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool UnhookWindowsHookEx(IntPtr hhk);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

		public static IntPtr SetKeyboardHook(HookSetup.LowLevelKeyboardProc proc)
		{
			IntPtr intPtr;
			try
			{
				using (Process currentProcess = Process.GetCurrentProcess())
				{
					using (ProcessModule mainModule = currentProcess.MainModule)
					{
						intPtr = HookSetup.SetWindowsHookEx(13, proc, WinApiSystem.GetModuleHandle(mainModule.ModuleName), 0U);
					}
				}
			}
			catch
			{
				intPtr = IntPtr.Zero;
			}
			return intPtr;
		}

		public static IntPtr SetMouseHook(HookSetup.LowLevelMouseProc proc)
		{
			IntPtr intPtr;
			try
			{
				using (Process currentProcess = Process.GetCurrentProcess())
				{
					using (ProcessModule mainModule = currentProcess.MainModule)
					{
						intPtr = HookSetup.SetWindowsHookEx(14, proc, WinApiSystem.GetModuleHandle(mainModule.ModuleName), 0U);
					}
				}
			}
			catch
			{
				intPtr = IntPtr.Zero;
			}
			return intPtr;
		}

		public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

		public delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);
	}
}
