using System;
using System.Runtime.InteropServices;

namespace Autoclicker.Native
{
	// Token: 0x02000009 RID: 9
	internal static class WinApiSystem
	{
		// Token: 0x06000098 RID: 152
		[DllImport("winmm.dll")]
		public static extern uint timeBeginPeriod(uint uPeriod);

		// Token: 0x06000099 RID: 153
		[DllImport("winmm.dll")]
		public static extern uint timeEndPeriod(uint uPeriod);

		// Token: 0x0600009A RID: 154
		[DllImport("ntdll.dll", SetLastError = true)]
		public static extern int NtDelayExecution(bool Alertable, ref long DelayInterval);

		// Token: 0x0600009B RID: 155
		[DllImport("ntdll.dll", SetLastError = true)]
		public static extern int NtSetTimerResolution(uint DesiredResolution, bool SetResolution, out uint CurrentResolution);

		// Token: 0x0600009C RID: 156
		[DllImport("ntdll.dll")]
		public static extern int NtSetSystemInformation(int SystemInformationClass, ref int SystemInformation, int SystemInformationLength);

		// Token: 0x0600009D RID: 157
		[DllImport("kernel32.dll")]
		public static extern uint GetCurrentThreadId();

		// Token: 0x0600009E RID: 158
		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern IntPtr GetModuleHandle(string lpModuleName);

		// Token: 0x0600009F RID: 159
		[DllImport("kernel32.dll")]
		public static extern bool SetProcessPriorityBoost(IntPtr hProcess, bool bDisablePriorityBoost);

		// Token: 0x060000A0 RID: 160
		[DllImport("kernel32.dll")]
		public static extern bool SetProcessWorkingSetSizeEx(IntPtr hProcess, UIntPtr dwMin, UIntPtr dwMax, uint Flags);

		// Token: 0x060000A1 RID: 161
		[DllImport("psapi.dll")]
		public static extern bool EmptyWorkingSet(IntPtr hProcess);

		// Token: 0x060000A2 RID: 162
		[DllImport("gdi32.dll")]
		public static extern int D3DKMTSetProcessSchedulingPriorityClass(IntPtr hProcess, int priority);

		// Token: 0x060000A3 RID: 163
		[DllImport("kernel32.dll")]
		public static extern IntPtr OpenThread(uint dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

		// Token: 0x060000A4 RID: 164
		[DllImport("kernel32.dll")]
		public static extern bool SetThreadPriority(IntPtr hThread, int nPriority);

		// Token: 0x060000A5 RID: 165
		[DllImport("kernel32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool CloseHandle(IntPtr hObject);
	}
}
