using System;
using System.Diagnostics;
using System.Threading;
using Autoclicker.Native;

namespace Autoclicker.Clicker
{
	internal static class ClickSender
	{
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
				if (mw.HitRegMode)
				{
					long target = Stopwatch.GetTimestamp() + Stopwatch.Frequency / 1000L;
					while (Stopwatch.GetTimestamp() < target) Thread.SpinWait(32);
				}
				else
				{
					Thread.Sleep(6);
				}
				WinApiMouse.mouse_event(4U, 0, 0, 0U, MainWindow.CLICKER_EXTRA_INFO);
			}
		}

		public static void ResetBreakingHold()
		{
			if (ClickSender._breakingHoldActive)
			{
				WinApiMouse.mouse_event(4U, 0, 0, 0U, MainWindow.CLICKER_EXTRA_INFO);
				ClickSender._breakingHoldActive = false;
			}
		}

		private static bool _breakingHoldActive = false;

		private static readonly Random _jitterRandom = new Random(Guid.NewGuid().GetHashCode());
	}
}
