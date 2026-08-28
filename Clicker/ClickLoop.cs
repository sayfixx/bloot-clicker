using System;
using System.Diagnostics;
using System.Threading;
using Autoclicker.Minecraft;
using Autoclicker.Native;

namespace Autoclicker.Clicker
{
	internal static class ClickLoop
	{
		private static bool _wasClicking = false;

		public static void Run(MainWindow mw, CancellationToken token)
		{
			WinApiSystem.timeBeginPeriod(1U);
			long frequency = Stopwatch.Frequency;
			long num = 0L;
			Random random = new Random(Guid.NewGuid().GetHashCode());
			while (!token.IsCancellationRequested)
			{
				bool shouldClick = ClickLoop.ShouldClick(mw);
				if (shouldClick != ClickLoop._wasClicking)
				{
					ClickLoop._wasClicking = shouldClick;
				}
				if (!shouldClick)
				{
					num = 0L;
					ClickSender.ResetBreakingHold(mw);
					Thread.Sleep(1);
				}
				else
				{
					long timestamp = Stopwatch.GetTimestamp();
					if (num == 0L)
					{
						num = timestamp;
					}
					if (timestamp >= num)
					{
						int num2 = CpsEngine.GetNext(mw);
						if (num2 < 1)
						{
							num2 = 1;
						}
						long num3 = frequency / (long)num2;
						ClickSender.Send(mw, (long)(50 + random.Next(0, 150)));
						mw.ClickCounter++;
						mw.ClicksSincePause++;
						num += num3;
						if (Stopwatch.GetTimestamp() - num > num3 * 2L)
						{
							num = Stopwatch.GetTimestamp() + num3;
						}
					}
					else
					{
						long num4 = num - Stopwatch.GetTimestamp();
						if (num4 > 0L)
						{
							long num5 = num4 * 1000L / frequency;
							if (num5 > 2L)
							{
								Thread.Sleep((int)(num5 * 8L / 10L));
							}
							while (Stopwatch.GetTimestamp() < num)
							{
								Thread.SpinWait(10);
							}
						}
					}
				}
			}
			WinApiSystem.timeEndPeriod(1U);
		}

		private static bool ShouldClick(MainWindow mw)
		{
			if (!mw.ClickerEnabled || !mw.UserHoldingLMB)
			{
				return false;
			}
			if (!mw.OnlyMcbeMode)
			{
				return true;
			}
			if (!WindowChecker.IsActiveCached())
			{
				return false;
			}
			if (mw.ClickInInventoryMode)
			{
				return WindowChecker.IsCursorHidden();
			}
			bool easyRefilMode = mw.EasyRefilMode;
			return true;
		}
	}
}
