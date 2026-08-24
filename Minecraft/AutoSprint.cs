using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Autoclicker.Native;

namespace Autoclicker.Minecraft
{
	internal static class AutoSprint
	{
		public static void Start(MainWindow mw)
		{
			CancellationTokenSource cts = AutoSprint._cts;
			if (cts != null)
			{
				cts.Cancel();
			}
			AutoSprint._cts = new CancellationTokenSource();
			CancellationToken token = AutoSprint._cts.Token;
			Task.Run(delegate()
			{
				bool flag = false;
				bool flag2 = false;
				bool flag3 = false;
				bool flag4 = false;
				while (!token.IsCancellationRequested)
				{
					try
					{
						if (!WindowChecker.IsActive() || !WindowChecker.IsCursorHidden())
						{
							if (flag4 && flag3)
							{
								AutoSprint.SendSprintKey(mw, false);
								flag3 = false;
							}
							flag2 = (flag = (flag4 = false));
							Thread.Sleep(30);
							continue;
						}

						if (AutoSprint.IsMinecraftRunning())
						{
							if (flag4 && flag3)
							{
								flag3 = false;
							}
							flag2 = (flag = (flag4 = false));
							Thread.Sleep(30);
							continue;
						}
						bool flag5 = ((int)WinApiMouse.GetAsyncKeyState(87) & 32768) != 0;
						bool flag6 = ((int)WinApiMouse.GetAsyncKeyState(83) & 32768) != 0;
						bool flag7 = flag5 || flag6;
						bool flag8 = flag || flag2;
						if (flag7 && (!flag8 || !flag4))
						{
							AutoSprint.SendSprintKey(mw, true);
							flag3 = true;
						}
						else if (!flag7 && flag8 && flag3)
						{
							AutoSprint.SendSprintKey(mw, false);
							flag3 = false;
						}
						flag = flag5;
						flag2 = flag6;
						flag4 = true;
					}
					catch (OperationCanceledException)
					{
						break;
					}
					catch
					{
					}
					Thread.Sleep(1);
				}
				if (flag3)
				{
					try
					{
						AutoSprint.SendSprintKey(mw, false);
					}
					catch
					{
					}
				}
			}, token);
		}

		private static bool IsMinecraftRunning()
		{
			try
			{
				IntPtr foregroundWindow = WinApiWindow.GetForegroundWindow();
				if (foregroundWindow == IntPtr.Zero)
				{
					return false;
				}
				WinApiWindow.GetWindowThreadProcessId(foregroundWindow, out uint processId);
				using (System.Diagnostics.Process process = System.Diagnostics.Process.GetProcessById((int)processId))
				{
					string processName = process.ProcessName.ToLowerInvariant();
					return processName == "minecraft.windows" ||
						   processName == "minecraftlauncher" ||
						   processName.StartsWith("minecraft-") ||
						   processName == "bedrock" ||
						   processName == "mcbe" ||
						   processName == "javaw" ||
						   processName == "java" ||
						   processName.Contains("minecraft") ||
						   processName.Contains("lwjgl");
				}
			}
			catch
			{
			}
			return false;
		}

		public static void Stop()
		{
			CancellationTokenSource cts = AutoSprint._cts;
			if (cts == null)
			{
				return;
			}
			cts.Cancel();
		}

		private static void SendSprintKey(MainWindow mw, bool down)
		{
			if (mw.SprintBindMouse.GetValueOrDefault() == MouseButton.Middle)
			{
				WinApiMouse.mouse_event(down ? 32U : 64U, 0, 0, 0U, UIntPtr.Zero);
				return;
			}
			if (mw.SprintBindKey != Key.None)
			{
				int num = KeyInterop.VirtualKeyFromKey(mw.SprintBindKey);
				if (num > 0 && num < 256)
				{
					WinApiKeyboard.keybd_event((byte)num, 0, down ? 0U : 2U, UIntPtr.Zero);
				}
			}
		}

		private static CancellationTokenSource _cts;
	}
}
