using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Autoclicker.Native;

namespace Autoclicker.Hooks
{
	// Token: 0x0200000D RID: 13
	internal static class HookSetup
	{
		// Token: 0x060000B5 RID: 181
		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr SetWindowsHookEx(int idHook, HookSetup.LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

		// Token: 0x060000B6 RID: 182
		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr SetWindowsHookEx(int idHook, HookSetup.LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

		// Token: 0x060000B7 RID: 183
		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool UnhookWindowsHookEx(IntPtr hhk);

		// Token: 0x060000B8 RID: 184
		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

		// Token: 0x060000B9 RID: 185 RVA: 0x00056120 File Offset: 0x00056120
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

		// Token: 0x060000BA RID: 186 RVA: 0x00056198 File Offset: 0x00056198
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

		// Token: 0x0200004E RID: 78
		// (Invoke) Token: 0x060001D6 RID: 470
		public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

		// Token: 0x0200004F RID: 79
		// (Invoke) Token: 0x060001DA RID: 474
		public delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);
	}
}
