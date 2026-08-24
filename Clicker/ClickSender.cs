using System;
using System.Diagnostics;
using System.Threading;
using Autoclicker.Native;

namespace Autoclicker.Clicker
{
	// Token: 0x02000018 RID: 24
	internal static class ClickSender
	{
		// Token: 0x060000FF RID: 255 RVA: 0x0005882C File Offset: 0x0005882C
		public static void Send(MainWindow mw, long holdMicroseconds)
		{
			if (mw.JitterEnabled && mw.JitterStrength > 0)
			{
				WinApiMouse.ApplyJitter(mw.JitterStrength, ClickSender._jitterRandom);
			}
			if (mw.BreakingMode)
			{
				if (mw.UserHoldingLMB)
				{
					if (!ClickSender._breakingHoldActive)
					{
						WinApiMouse.mouse_event(2U, 0, 0, 0U, MainWindow.CLICKER_EXTRA_INFO);
						ClickSender._breakingHoldActive = true;
						return;
					}
					WinApiMouse.mouse_event(4U, 0, 0, 0U, MainWindow.CLICKER_EXTRA_INFO);
					Thread.Sleep(1);
					WinApiMouse.mouse_event(2U, 0, 0, 0U, MainWindow.CLICKER_EXTRA_INFO);
					return;
				}
				else if (ClickSender._breakingHoldActive)
				{
					WinApiMouse.mouse_event(4U, 0, 0, 0U, MainWindow.CLICKER_EXTRA_INFO);
					ClickSender._breakingHoldActive = false;
					return;
				}
			}
			else
			{
				ClickSender._breakingHoldActive = false;
				WinApiMouse.mouse_event(2U, 0, 0, 0U, MainWindow.CLICKER_EXTRA_INFO);
				Thread.Sleep(6);
				WinApiMouse.mouse_event(4U, 0, 0, 0U, MainWindow.CLICKER_EXTRA_INFO);
				Debug.WriteLine("[DEBUG] ClickSender.Send: mouse_event down+up dispatched");
			}
		}

		// Token: 0x06000100 RID: 256 RVA: 0x000588F8 File Offset: 0x000588F8
		public static void ResetBreakingHold()
		{
			if (ClickSender._breakingHoldActive)
			{
				WinApiMouse.mouse_event(4U, 0, 0, 0U, MainWindow.CLICKER_EXTRA_INFO);
				ClickSender._breakingHoldActive = false;
			}
		}

		// Token: 0x040000DB RID: 219
		private static bool _breakingHoldActive = false;

		// Token: 0x040000DC RID: 220
		private static readonly Random _jitterRandom = new Random(Guid.NewGuid().GetHashCode());
	}
}
