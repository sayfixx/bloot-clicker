using System;
using System.Diagnostics;
using System.Threading;
using Autoclicker.Native;
using Autoclicker.Minecraft;

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
						ClickSender.SendDown(mw);
						ClickSender._breakingHoldActive = true;
						return;
					}
					ClickSender.SendUp(mw);
					Thread.Sleep(1);
					ClickSender.SendDown(mw);
					return;
				}
				else if (ClickSender._breakingHoldActive)
				{
					ClickSender.SendUp(mw);
					ClickSender._breakingHoldActive = false;
					return;
				}
			}
			else
			{
				ClickSender._breakingHoldActive = false;
				ClickSender.SendDown(mw);
				if (mw.HitRegMode)
				{
					long target = Stopwatch.GetTimestamp() + Stopwatch.Frequency / 1000L;
					while (Stopwatch.GetTimestamp() < target) Thread.SpinWait(32);
				}
				else
				{
					Thread.Sleep(6);
				}
				ClickSender.SendUp(mw);
			}
		}

		private static void SendDown(MainWindow mw)
		{
			if (IsGdk(mw))
			{
				if (!WinApiMouse.TrySendMouseInput(WinApiMouse.MI_LEFTDOWN, MainWindow.CLICKER_EXTRA_INFO))
					WinApiMouse.SendGdkClick(true);
				return;
			}
			WinApiMouse.mouse_event(WinApiMouse.MOUSEEVENTF_LEFTDOWN, 0, 0, 0U, MainWindow.CLICKER_EXTRA_INFO);
		}

		private static void SendUp(MainWindow mw)
		{
			if (IsGdk(mw))
			{
				if (!WinApiMouse.TrySendMouseInput(WinApiMouse.MI_LEFTUP, MainWindow.CLICKER_EXTRA_INFO))
					WinApiMouse.SendGdkClick(false);
				return;
			}
			WinApiMouse.mouse_event(WinApiMouse.MOUSEEVENTF_LEFTUP, 0, 0, 0U, MainWindow.CLICKER_EXTRA_INFO);
		}

		private static bool IsGdk(MainWindow mw)
		{
			try
			{
				MinecraftInfo info = MinecraftVersionDetector.GetCached();
				return info.IsRunning && info.Edition == MinecraftEdition.Bedrock;
			}
			catch
			{
				return false;
			}
		}

		public static void ResetBreakingHold(MainWindow mw)
		{
			if (ClickSender._breakingHoldActive)
			{
				ClickSender.SendUp(mw);
				ClickSender._breakingHoldActive = false;
			}
		}

		private static bool _breakingHoldActive = false;

		private static readonly Random _jitterRandom = new Random(Guid.NewGuid().GetHashCode());
	}
}
