using System;
using System.Diagnostics;
using Autoclicker.Minecraft;

namespace Autoclicker.Clicker
{
	// Token: 0x02000019 RID: 25
	internal static class CpsEngine
	{
		// Token: 0x06000102 RID: 258 RVA: 0x00058948 File Offset: 0x00058948
		public static int GetNext(MainWindow mw)
		{
			if (mw.EasyRefilMode && WindowChecker.IsCursorVisible() && WindowChecker.IsActiveCached())
			{
				return 100;
			}
			long timestamp = Stopwatch.GetTimestamp();
			long frequency = Stopwatch.Frequency;
			if (CpsEngine._nextChangeIntervalTicks == 0L)
			{
				CpsEngine._nextChangeIntervalTicks = frequency * (long)mw.CpsRandom.Next(2000, 5000) / 1000L;
				CpsEngine._lastChangeTick = timestamp;
			}
			if (timestamp - CpsEngine._lastChangeTick >= CpsEngine._nextChangeIntervalTicks)
			{
				CpsEngine.GenerateNewTarget(mw);
				CpsEngine._nextChangeIntervalTicks = frequency * (long)mw.CpsRandom.Next(2000, 5000) / 1000L;
				CpsEngine._lastChangeTick = timestamp;
			}
			if (mw.CurrentCps != mw.TargetCps)
			{
				int num = ((mw.TargetCps > mw.CurrentCps) ? 1 : (-1));
				mw.CurrentCps += num;
				mw.CurrentCps = Math.Max(mw.MinCps, Math.Min(mw.MaxCps, mw.CurrentCps));
			}
			return mw.CurrentCps;
		}

		// Token: 0x06000103 RID: 259 RVA: 0x00058A48 File Offset: 0x00058A48
		private static void GenerateNewTarget(MainWindow mw)
		{
			try
			{
				if (mw.MinCps >= mw.MaxCps || mw.MinCps < 1)
				{
					mw.TargetCps = mw.MinCps;
				}
				else
				{
					int num = mw.MaxCps - mw.MinCps;
					if (num == 0)
					{
						mw.TargetCps = mw.MinCps;
					}
					else
					{
						double num2 = 0.0;
						for (int i = 0; i < 12; i++)
						{
							num2 += mw.CpsRandom.NextDouble();
						}
						num2 -= 6.0;
						double num3 = Math.Max(-0.5, Math.Min(0.5, num2 / 3.0));
						int num4 = (int)((double)mw.MinCps + (double)num * 0.5 + (double)num * num3);
						num4 = Math.Max(mw.MinCps, Math.Min(mw.MaxCps, num4));
						int num5 = Math.Max(2, num / 3);
						if (Math.Abs(num4 - mw.CurrentCps) > num5)
						{
							num4 = mw.CurrentCps + Math.Sign(num4 - mw.CurrentCps) * num5;
						}
						mw.TargetCps = num4;
					}
				}
			}
			catch
			{
				mw.TargetCps = (mw.MinCps + mw.MaxCps) / 2;
			}
		}

		// Token: 0x040000DD RID: 221
		private static long _lastChangeTick;

		// Token: 0x040000DE RID: 222
		private static long _nextChangeIntervalTicks;
	}
}
