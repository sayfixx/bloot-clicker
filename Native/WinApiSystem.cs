using System;
using System.Runtime.InteropServices;

namespace Autoclicker.Native
{
	internal static class WinApiSystem
	{
		[DllImport("winmm.dll")]
		public static extern uint timeBeginPeriod(uint uPeriod);

		[DllImport("winmm.dll")]
		public static extern uint timeEndPeriod(uint uPeriod);

		[DllImport("ntdll.dll", SetLastError = true)]
		public static extern int NtDelayExecution(bool Alertable, ref long DelayInterval);

		[DllImport("ntdll.dll", SetLastError = true)]
		public static extern int NtSetTimerResolution(uint DesiredResolution, bool SetResolution, out uint CurrentResolution);

		[DllImport("ntdll.dll")]
		public static extern int NtSetSystemInformation(int SystemInformationClass, ref int SystemInformation, int SystemInformationLength);

		[DllImport("kernel32.dll")]
		public static extern uint GetCurrentThreadId();

		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern IntPtr GetModuleHandle(string lpModuleName);

		[DllImport("kernel32.dll")]
		public static extern bool SetProcessPriorityBoost(IntPtr hProcess, bool bDisablePriorityBoost);

		[DllImport("kernel32.dll")]
		public static extern bool SetProcessWorkingSetSizeEx(IntPtr hProcess, UIntPtr dwMin, UIntPtr dwMax, uint Flags);

		[DllImport("psapi.dll")]
		public static extern bool EmptyWorkingSet(IntPtr hProcess);

		[DllImport("gdi32.dll")]
		public static extern int D3DKMTSetProcessSchedulingPriorityClass(IntPtr hProcess, int priority);

		[DllImport("kernel32.dll")]
		public static extern IntPtr OpenThread(uint dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

		[DllImport("kernel32.dll")]
		public static extern bool SetThreadPriority(IntPtr hThread, int nPriority);

		[DllImport("kernel32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool CloseHandle(IntPtr hObject);
	}
}
