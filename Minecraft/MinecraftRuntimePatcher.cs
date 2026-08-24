using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Autoclicker.Minecraft
{
	internal sealed class MinecraftRuntimePatcher : IDisposable
	{
		public MinecraftRuntimePatcher(MainWindow window)
		{
			this._window = window;
			this._timer = new Timer(new TimerCallback(this.OnTick), null, -1, -1);
		}

		public void SetEnabled(bool enabled)
		{
			object gate = this._gate;
			lock (gate)
			{
				this._enabled = enabled;
				if (enabled)
				{
					this._timer.Change(0, 1000);
					this.SetStatus("waiting");
				}
				else
				{
					this._timer.Change(-1, -1);
					this.RestoreActiveProcess_NoLock();
					this._patchedProcessId = null;
					this.SetStatus("patch disabled");
				}
			}
		}

		public void NotifySelectionChanged()
		{
			object gate = this._gate;
			lock (gate)
			{
				if (this._enabled)
				{
					this.RestoreActiveProcess_NoLock();
					this._patchedProcessId = null;
					this._timer.Change(0, 1000);
				}
			}
		}

		public void SetSelection(bool itemUseDelay, bool noCameraReset, bool noHurtCam)
		{
			SetSelection(itemUseDelay, noCameraReset, noHurtCam, true);
		}

		public void SetSelection(bool itemUseDelay, bool noCameraReset, bool noHurtCam, bool useGdk)
		{
			object gate = this._gate;
			lock (gate)
			{
				this._itemUseDelay = itemUseDelay;
				this._noCameraReset = noCameraReset;
				this._noHurtCam = noHurtCam;
				this._useGdk = useGdk;
				if (this._enabled)
				{
					this.RestoreActiveProcess_NoLock();
					this._patchedProcessId = null;
					this._timer.Change(0, 1000);
				}
			}
		}

		public void Dispose()
		{
			object gate = this._gate;
			lock (gate)
			{
				this._timer.Change(-1, -1);
				this.RestoreActiveProcess_NoLock();
				this._timer.Dispose();
			}
		}

		private void OnTick(object state)
		{
			object gate = this._gate;
			lock (gate)
			{
				if (this._enabled)
				{
					Process process = this.FindMinecraftProcess();
					if (process == null)
					{
						if (this._patchedProcessId != null)
						{
							this._applied.Clear();
							this._patchedProcessId = null;
							this.SetStatus("minecraft closed");
						}
					}
					else
					{
						int? patchedProcessId = this._patchedProcessId;
						int id = process.Id;
						if ((patchedProcessId.GetValueOrDefault() == id & patchedProcessId != null) && this._applied.Count > 0)
						{
							this.SetStatus("patched");
						}
						else
						{
							if (this._patchedProcessId != null)
							{
								patchedProcessId = this._patchedProcessId;
								id = process.Id;
								if (!(patchedProcessId.GetValueOrDefault() == id & patchedProcessId != null))
								{
									this.RestoreActiveProcess_NoLock();
									this._patchedProcessId = null;
								}
							}
							this.ApplyToProcess_NoLock(process);
						}
					}
				}
			}
		}

		private void ApplyToProcess_NoLock(Process process)
		{
			IReadOnlyList<RuntimePatchDefinition> selectedPatches = this.GetSelectedPatches();
			if (selectedPatches.Count == 0)
			{
				this.SetStatus("idle");
				return;
			}
			IntPtr intPtr = NativeMethods.OpenProcess(NativeMethods.ProcessAccessFlags.VirtualMemoryOperation | NativeMethods.ProcessAccessFlags.VirtualMemoryRead | NativeMethods.ProcessAccessFlags.VirtualMemoryWrite | NativeMethods.ProcessAccessFlags.QueryInformation, false, process.Id);
			if (intPtr == IntPtr.Zero)
			{
				this.SetStatus("failed to open process");
				return;
			}
			try
			{
				ProcessModule mainModule = process.MainModule;
				if (mainModule == null)
				{
					this.SetStatus("failed to access module");
				}
				else
				{
					byte[] array = new byte[mainModule.ModuleMemorySize];
					IntPtr intPtr2;
					if (!NativeMethods.ReadProcessMemory(intPtr, mainModule.BaseAddress, array, array.Length, out intPtr2) || intPtr2.ToInt64() <= 0L)
					{
						this.SetStatus("failed to read memory");
					}
					else
					{
						this._applied.Clear();
						foreach (RuntimePatchDefinition runtimePatchDefinition in selectedPatches)
						{
							SignaturePattern pattern = SignaturePattern.Parse(runtimePatchDefinition.Signature);
							int num = PatternScanner.Find(array, pattern);
							if (num >= 0)
							{
								IntPtr intPtr3 = IntPtr.Add(mainModule.BaseAddress, num + runtimePatchDefinition.PatchOffset);
								byte[] array2;
								IntPtr intPtr4;
								byte[] array3;
								switch (runtimePatchDefinition.ApplyKind)
								{
								case PatchApplyKind.Nop:
								{
									int num2 = runtimePatchDefinition.Length;
									array2 = new byte[num2];
									if (!NativeMethods.ReadProcessMemory(intPtr, intPtr3, array2, num2, out intPtr4))
									{
										continue;
									}
									array3 = Enumerable.Repeat<byte>(144, num2).ToArray<byte>();
									break;
								}
								case PatchApplyKind.NopCall5:
								{
									int num2 = 5;
									array2 = new byte[num2];
									if (!NativeMethods.ReadProcessMemory(intPtr, intPtr3, array2, num2, out intPtr4))
									{
										continue;
									}
									array3 = new byte[]
									{
										144,
										144,
										144,
										144,
										144
									};
									break;
								}
								case PatchApplyKind.WriteFloat:
								{
									int num2 = 4;
									array2 = new byte[num2];
									if (!NativeMethods.ReadProcessMemory(intPtr, intPtr3, array2, num2, out intPtr4))
									{
										continue;
									}
									array3 = BitConverter.GetBytes(runtimePatchDefinition.FloatValue);
									break;
								}
								default:
								{
									array3 = HexBytes.Parse(runtimePatchDefinition.ReplacementHex);
									int num2 = array3.Length;
									array2 = new byte[num2];
									if (!NativeMethods.ReadProcessMemory(intPtr, intPtr3, array2, num2, out intPtr4))
									{
										continue;
									}
									break;
								}
								}
								if (NativeMethods.WriteProcessMemory(intPtr, intPtr3, array3, array3.Length, out intPtr4))
								{
									this._applied.Add(new AppliedPatch(process.Id, runtimePatchDefinition, intPtr3, array2));
								}
							}
						}
						if (this._applied.Count > 0)
						{
							this._patchedProcessId = new int?(process.Id);
							this.SetStatus("patched");
						}
						else
						{
							this._patchedProcessId = null;
							this.SetStatus("no signatures matched");
						}
					}
				}
			}
			catch
			{
				this.SetStatus("failed");
			}
			finally
			{
				NativeMethods.CloseHandle(intPtr);
				try
				{
					process.Dispose();
				}
				catch
				{
				}
			}
		}

		private void RestoreActiveProcess_NoLock()
		{
			if (this._applied.Count == 0 || this._patchedProcessId == null)
			{
				this._applied.Clear();
				return;
			}
			try
			{
				Process processById = Process.GetProcessById(this._patchedProcessId.Value);
				IntPtr intPtr = NativeMethods.OpenProcess(NativeMethods.ProcessAccessFlags.VirtualMemoryOperation | NativeMethods.ProcessAccessFlags.VirtualMemoryWrite | NativeMethods.ProcessAccessFlags.QueryInformation, false, processById.Id);
				if (intPtr != IntPtr.Zero)
				{
					foreach (AppliedPatch appliedPatch in this._applied)
					{
						IntPtr intPtr2;
						NativeMethods.WriteProcessMemory(intPtr, appliedPatch.Address, appliedPatch.OriginalBytes, appliedPatch.OriginalBytes.Length, out intPtr2);
					}
					NativeMethods.CloseHandle(intPtr);
				}
				try
				{
					processById.Dispose();
				}
				catch
				{
				}
			}
			catch
			{
			}
			finally
			{
				this._applied.Clear();
			}
		}

		private IReadOnlyList<RuntimePatchDefinition> GetSelectedPatches()
		{
			var list = new List<RuntimePatchDefinition>();
			if (this._itemUseDelay)
			{
				list.Add(this._useGdk ? RuntimePatchCatalog.GdkItemUseDelay : RuntimePatchCatalog.LegacyItemUseDelay);
			}
			if (this._noCameraReset)
			{
				list.Add(this._useGdk ? RuntimePatchCatalog.GdkTeleportRotation : RuntimePatchCatalog.LegacyTeleportRotation);
			}
			if (this._noHurtCam)
			{
				list.Add(this._useGdk ? RuntimePatchCatalog.GdkNoHurtCam : RuntimePatchCatalog.LegacyNoHurtCam);
			}
			return list;
		}

		private Process FindMinecraftProcess()
		{
			string[] candidates = new string[]
			{
				"Minecraft.Windows",
				"bedrock",
				"mcbe"
			};
			Process result;
			try
			{
				Process process = this.TryGetForegroundMinecraftProcess();
				if (process != null)
				{
					result = process;
				}
				else
				{
					result = Process.GetProcesses().Where(delegate(Process p)
					{
						bool result2;
						try
						{
							string name = p.ProcessName;
							if (string.Equals(name, "MinecraftLauncher", StringComparison.OrdinalIgnoreCase))
							{
								result2 = false;
							}
							else if (candidates.Any((string c) => string.Equals(c, name, StringComparison.OrdinalIgnoreCase)) || name.StartsWith("Minecraft-", StringComparison.OrdinalIgnoreCase))
							{
								result2 = true;
							}
							else
							{
								string text = MinecraftRuntimePatcher.SafeGetMainModuleFileName(p);
								result2 = (text != null && text.EndsWith("Minecraft.Windows.exe", StringComparison.OrdinalIgnoreCase));
							}
						}
						catch
						{
							result2 = false;
						}
						return result2;
					}).OrderByDescending(delegate(Process p)
					{
						int result2;
						try
						{
							result2 = ((p.MainWindowHandle != IntPtr.Zero) ? 1 : 0);
						}
						catch
						{
							result2 = 0;
						}
						return result2;
					}).ThenByDescending(delegate(Process p)
					{
						DateTime result2;
						try
						{
							result2 = p.StartTime;
						}
						catch
						{
							result2 = DateTime.MinValue;
						}
						return result2;
					}).FirstOrDefault<Process>();
				}
			}
			catch
			{
				result = null;
			}
			return result;
		}

		private Process TryGetForegroundMinecraftProcess()
		{
			try
			{
				IntPtr foregroundWindow = NativeMethods.GetForegroundWindow();
				if (foregroundWindow == IntPtr.Zero)
				{
					return null;
				}
				uint num;
				NativeMethods.GetWindowThreadProcessId(foregroundWindow, out num);
				if (num == 0U)
				{
					return null;
				}
				Process processById = Process.GetProcessById((int)num);
				string text = MinecraftRuntimePatcher.SafeGetMainModuleFileName(processById);
				if (text != null && text.EndsWith("Minecraft.Windows.exe", StringComparison.OrdinalIgnoreCase))
				{
					return processById;
				}
				if ((processById.MainWindowTitle ?? string.Empty).IndexOf("minecraft", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					return processById;
				}
			}
			catch
			{
			}
			return null;
		}

		private static string SafeGetMainModuleFileName(Process process)
		{
			string result;
			try
			{
				ProcessModule mainModule = process.MainModule;
				result = ((mainModule != null) ? mainModule.FileName : null);
			}
			catch
			{
				result = null;
			}
			return result;
		}

		private void SetStatus(string text)
		{
			this._window.Dispatcher.BeginInvoke(new Action(delegate()
			{

			}), Array.Empty<object>());
		}

		private readonly MainWindow _window;

		private readonly object _gate = new object();

		private readonly Timer _timer;

		private bool _enabled;

		private int? _patchedProcessId;

		private readonly List<AppliedPatch> _applied = new List<AppliedPatch>();

		private bool _itemUseDelay;
		private bool _noCameraReset;
		private bool _noHurtCam;
		private bool _useGdk = true;
	}
}
