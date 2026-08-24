using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using Autoclicker.Minecraft;
using Autoclicker.Native;

namespace Autoclicker.Hooks
{
	// Token: 0x0200000F RID: 15
	internal static class MouseHook
	{
		// Token: 0x060000BF RID: 191 RVA: 0x00056434 File Offset: 0x00056434
		public static void Install()
		{
			MouseHook.HookID = HookSetup.SetMouseHook(MouseHook._proc);
		}

		// Token: 0x060000C0 RID: 192 RVA: 0x00056448 File Offset: 0x00056448
		public static void Uninstall()
		{
			if (MouseHook.HookID != IntPtr.Zero)
			{
				HookSetup.UnhookWindowsHookEx(MouseHook.HookID);
				MouseHook.HookID = IntPtr.Zero;
			}
		}

		// Token: 0x060000C1 RID: 193 RVA: 0x0005646C File Offset: 0x0005646C
		private static IntPtr Callback(int nCode, IntPtr wParam, IntPtr lParam)
		{
			if (nCode < 0)
			{
				return HookSetup.CallNextHookEx(MouseHook.HookID, nCode, wParam, lParam);
			}
			try
			{
				Application application = Application.Current;
				MainWindow mainWindow = ((application != null) ? application.MainWindow : null) as MainWindow;
				if (mainWindow == null)
				{
					return HookSetup.CallNextHookEx(MouseHook.HookID, nCode, wParam, lParam);
				}
				int num = wParam.ToInt32();
				MSLLHOOKSTRUCT msllhookstruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
				if (msllhookstruct.dwExtraInfo.ToInt64() == (long)MainWindow.CLICKER_EXTRA_INFO.ToUInt64())
				{
					if (mainWindow.OnlyMcbeMode && !WindowChecker.IsActive())
					{
						return (IntPtr)1;
					}
					return HookSetup.CallNextHookEx(MouseHook.HookID, nCode, wParam, lParam);
				}
				else
				{
					if (mainWindow.WaitingForKey)
					{
						MouseButton? mouseButton = MouseHook.ResolveButton(num, msllhookstruct);
						if (mouseButton != null)
						{
							if (mainWindow.WaitingForSprintBind)
							{
								mainWindow.SetSprintBindFromMouse(mouseButton.Value);
								return HookSetup.CallNextHookEx(MouseHook.HookID, nCode, wParam, lParam);
							}
							mainWindow.SetClickerBindFromMouse(mouseButton.Value);
							return HookSetup.CallNextHookEx(MouseHook.HookID, nCode, wParam, lParam);
						}
					}
					if (!mainWindow.WaitingForKey && mainWindow.SelectedMouseButton != null)
					{
						bool flag = MouseHook.IsBindDown(num, msllhookstruct, mainWindow.SelectedMouseButton.Value);
						bool flag2 = MouseHook.IsBindUp(num, msllhookstruct, mainWindow.SelectedMouseButton.Value);
						if (flag && !mainWindow.BindKeyPressed)
						{
							mainWindow.BindKeyPressed = true;
							mainWindow.SetClickerEnabled(!mainWindow.ClickerEnabled);
							return HookSetup.CallNextHookEx(MouseHook.HookID, nCode, wParam, lParam);
						}
						if (flag2)
						{
							mainWindow.BindKeyPressed = false;
							return HookSetup.CallNextHookEx(MouseHook.HookID, nCode, wParam, lParam);
						}
					}
					if (num == 513)
					{
						MouseHook._lastDownTimestamp = Stopwatch.GetTimestamp();
						mainWindow.UserHoldingLMB = true;
						Debug.WriteLine("[DEBUG] Real LMB down -> UserHoldingLMB = true, ClickerEnabled = " + mainWindow.ClickerEnabled + ", OnlyMcbeMode = " + mainWindow.OnlyMcbeMode);
					}
					else if (num == 514)
					{
						MouseHook._lastUpTimestamp = Stopwatch.GetTimestamp();
						if (mainWindow.BreakingMode && mainWindow.ClickerEnabled)
						{
							mainWindow.UserHoldingLMB = false;
							mainWindow.ClicksSincePause = 0;
							return HookSetup.CallNextHookEx(MouseHook.HookID, nCode, wParam, lParam);
						}
						if (mainWindow.ClickerEnabled && !mainWindow.BreakingMode && (MouseHook._lastUpTimestamp - MouseHook._lastDownTimestamp) * 1000L / Stopwatch.Frequency < 8L)
						{
							return HookSetup.CallNextHookEx(MouseHook.HookID, nCode, wParam, lParam);
						}
						mainWindow.UserHoldingLMB = false;
						mainWindow.ClicksSincePause = 0;
					}
				}
			}
			catch
			{
			}
			return HookSetup.CallNextHookEx(MouseHook.HookID, nCode, wParam, lParam);
		}

		// Token: 0x060000C2 RID: 194 RVA: 0x00056700 File Offset: 0x00056700
		private static MouseButton? ResolveButton(int msg, MSLLHOOKSTRUCT hs)
		{
			if (msg == 513)
			{
				return new MouseButton?(MouseButton.Left);
			}
			if (msg == 516)
			{
				return new MouseButton?(MouseButton.Right);
			}
			if (msg == 519)
			{
				return new MouseButton?(MouseButton.Middle);
			}
			if (msg != 523)
			{
				return null;
			}
			uint num = (hs.mouseData >> 16) & 65535U;
			if (num == 1U)
			{
				return new MouseButton?(MouseButton.XButton1);
			}
			if (num != 2U)
			{
				return null;
			}
			return new MouseButton?(MouseButton.XButton2);
		}

		// Token: 0x060000C3 RID: 195 RVA: 0x0005677C File Offset: 0x0005677C
		private static bool IsBindDown(int msg, MSLLHOOKSTRUCT hs, MouseButton bind)
		{
			if (msg == 513 && bind == MouseButton.Left)
			{
				return true;
			}
			if (msg == 516 && bind == MouseButton.Right)
			{
				return true;
			}
			if (msg == 519 && bind == MouseButton.Middle)
			{
				return true;
			}
			if (msg == 523)
			{
				uint num = (hs.mouseData >> 16) & 65535U;
				return (num == 1U && bind == MouseButton.XButton1) || (num == 2U && bind == MouseButton.XButton2);
			}
			return false;
		}

		// Token: 0x060000C4 RID: 196 RVA: 0x000567E0 File Offset: 0x000567E0
		private static bool IsBindUp(int msg, MSLLHOOKSTRUCT hs, MouseButton bind)
		{
			if (msg == 514 && bind == MouseButton.Left)
			{
				return true;
			}
			if (msg == 517 && bind == MouseButton.Right)
			{
				return true;
			}
			if (msg == 520 && bind == MouseButton.Middle)
			{
				return true;
			}
			if (msg == 524)
			{
				uint num = (hs.mouseData >> 16) & 65535U;
				return (num == 1U && bind == MouseButton.XButton1) || (num == 2U && bind == MouseButton.XButton2);
			}
			return false;
		}

		// Token: 0x040000CD RID: 205
		public static IntPtr HookID = IntPtr.Zero;

		// Token: 0x040000CE RID: 206
		private static readonly HookSetup.LowLevelMouseProc _proc = new HookSetup.LowLevelMouseProc(MouseHook.Callback);

		// Token: 0x040000CF RID: 207
		private static long _lastDownTimestamp = 0L;

		// Token: 0x040000D0 RID: 208
		private static long _lastUpTimestamp = 0L;
	}
}
