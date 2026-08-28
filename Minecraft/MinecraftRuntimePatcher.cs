using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Linq;
using System.Threading;

namespace Autoclicker.Minecraft
{
	internal sealed class MinecraftRuntimePatcher : IDisposable
	{
		private const uint MemCommit = 0x1000;
		private const uint PageNoAccess = 0x01;
		private const uint PageGuard = 0x100;
		private const int ScanChunkSize = 0x1000000;

		public MinecraftRuntimePatcher(MainWindow window)
		{
			_window = window;
			_timer = new Timer(OnTick, null, -1, -1);
		}

		public void SetEnabled(bool enabled)
		{
			lock (_gate)
			{
				_enabled = enabled;
				if (enabled)
				{
					_timer.Change(0, 1000);
					SetStatus("waiting");
				}
				else
				{
					_timer.Change(-1, -1);
					RestoreActiveProcess_NoLock();
					_patchedProcessId = null;
					SetStatus("patch disabled");
				}
			}
		}

		public void NotifySelectionChanged()
		{
			lock (_gate)
			{
				if (_enabled)
				{
					RestoreActiveProcess_NoLock();
					_patchedProcessId = null;
					_timer.Change(0, 1000);
				}
			}
		}

		public void SetSelection(bool itemUseDelay, bool noCameraReset, bool noHurtCam)
		{
			SetSelection(itemUseDelay, noCameraReset, noHurtCam, true);
		}

		public void SetSelection(bool itemUseDelay, bool noCameraReset, bool noHurtCam, bool useGdk)
		{
			lock (_gate)
			{
				_itemUseDelay = itemUseDelay;
				_noCameraReset = noCameraReset;
				_noHurtCam = noHurtCam;
				_useGdk = useGdk;
				if (_enabled)
				{
					RestoreActiveProcess_NoLock();
					_patchedProcessId = null;
					_timer.Change(0, 1000);
				}
			}
		}

		public void Dispose()
		{
			lock (_gate)
			{
				_timer.Change(-1, -1);
				RestoreActiveProcess_NoLock();
				_timer.Dispose();
			}
		}

		private void OnTick(object state)
		{
			lock (_gate)
			{
				if (!_enabled)
				{
					return;
				}

				Process process = FindMinecraftProcess();
				if (process == null)
				{
					if (_patchedProcessId != null)
					{
						_applied.Clear();
						_patchedProcessId = null;
					}
					SetStatus("waiting for Minecraft");
					return;
				}

				try
				{
					if (_patchedProcessId == process.Id && _applied.Count > 0)
					{
						SetStatus("patched");
						return;
					}

					if (_patchedProcessId != null && _patchedProcessId != process.Id)
					{
						RestoreActiveProcess_NoLock();
						_patchedProcessId = null;
					}

					ApplyToProcess_NoLock(process);
				}
				finally
				{
					try { process.Dispose(); } catch { }
				}
			}
		}

		private void ApplyToProcess_NoLock(Process process)
		{
			IReadOnlyList<RuntimePatchDefinition> selectedPatches = GetSelectedPatches();
			if (selectedPatches.Count == 0)
			{
				SetStatus("idle");
				return;
			}

			IntPtr handle = NativeMethods.OpenProcess(
				NativeMethods.ProcessAccessFlags.VirtualMemoryOperation |
				NativeMethods.ProcessAccessFlags.VirtualMemoryRead |
				NativeMethods.ProcessAccessFlags.VirtualMemoryWrite |
				NativeMethods.ProcessAccessFlags.QueryInformation,
				false,
				process.Id);

			if (handle == IntPtr.Zero)
			{
				SetStatus("failed to open process");
				return;
			}

			try
			{
				List<ProcessModule> modules = GetScanModules(process);
				if (modules.Count == 0)
				{
					SetStatus("no Minecraft modules found");
					return;
				}

				_applied.Clear();
				var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

				foreach (RuntimePatchDefinition patch in selectedPatches)
				{
					if (TryFindPatch(handle, modules, patch, out IntPtr address, out byte[] originalBytes, out string moduleName))
					{
						byte[] replacement = BuildReplacement(patch);
						if (replacement.Length == 0 || replacement.Length != originalBytes.Length)
						{
							continue;
						}

						if (NativeMethods.WriteProcessMemory(handle, address, replacement, replacement.Length, out IntPtr written) && written.ToInt64() == replacement.Length)
						{
							_applied.Add(new AppliedPatch(process.Id, patch, address, originalBytes));
							found.Add(patch.Id + "@" + moduleName);
						}
					}
				}

				if (_applied.Count > 0)
				{
					_patchedProcessId = process.Id;
					SetStatus("patched: " + string.Join(", ", found));
				}
				else
				{
					_patchedProcessId = null;
					SetStatus("no signatures matched");
				}
			}
			catch
			{
				SetStatus("failed to scan or patch process");
			}
			finally
			{
				NativeMethods.CloseHandle(handle);
			}
		}

		private static byte[] BuildReplacement(RuntimePatchDefinition patch)
		{
			switch (patch.ApplyKind)
			{
				case PatchApplyKind.Nop:
					return Enumerable.Repeat((byte)0x90, patch.Length).ToArray();
				case PatchApplyKind.NopCall5:
					return Enumerable.Repeat((byte)0x90, 5).ToArray();
				case PatchApplyKind.WriteFloat:
					return BitConverter.GetBytes(patch.FloatValue);
				default:
					return HexBytes.Parse(patch.ReplacementHex);
			}
		}

		private static bool TryFindPatch(IntPtr handle, IReadOnlyList<ProcessModule> modules, RuntimePatchDefinition patch, out IntPtr address, out byte[] originalBytes, out string moduleName)
		{
			var patterns = new List<SignaturePattern> { SignaturePattern.Parse(patch.Signature) };
			if (!string.IsNullOrWhiteSpace(patch.FallbackSignature) && !string.Equals(patch.FallbackSignature, patch.Signature, StringComparison.OrdinalIgnoreCase))
			{
				patterns.Add(SignaturePattern.Parse(patch.FallbackSignature));
			}

			foreach (SignaturePattern pattern in patterns)
			{
				foreach (ProcessModule module in modules)
			{
				if (TryFindPatternInModule(handle, module, pattern, out IntPtr foundAddress))
				{
					IntPtr patchAddress = IntPtr.Add(foundAddress, patch.PatchOffset);
					byte[] replacement = BuildReplacement(patch);
					if (replacement.Length == 0)
					{
						break;
					}

					originalBytes = new byte[replacement.Length];
					if (NativeMethods.ReadProcessMemory(handle, patchAddress, originalBytes, originalBytes.Length, out IntPtr read) && read.ToInt64() == originalBytes.Length)
					{
						address = patchAddress;
						moduleName = module.ModuleName;
						return true;
					}
				}
				}
			}

			address = IntPtr.Zero;
			originalBytes = Array.Empty<byte>();
			moduleName = string.Empty;
			return false;
		}

		private static bool TryFindPatternInModule(IntPtr handle, ProcessModule module, SignaturePattern pattern, out IntPtr address)
		{
			address = IntPtr.Zero;
			long start = module.BaseAddress.ToInt64();
			long end = start + module.ModuleMemorySize;
			long cursor = start;

			while (cursor < end)
			{
				UIntPtr queried = NativeMethods.VirtualQueryEx(handle, new IntPtr(cursor), out NativeMethods.MEMORY_BASIC_INFORMATION info, (UIntPtr)MarshalSizeOfMemoryBasicInformation());
				if (queried == UIntPtr.Zero)
				{
					break;
				}

				long regionStart = info.BaseAddress.ToInt64();
				long regionSize = info.RegionSize.ToUInt64() > long.MaxValue ? long.MaxValue : (long)info.RegionSize.ToUInt64();
				if (regionSize <= 0)
				{
					break;
				}

				long regionEnd = Math.Min(end, regionStart + regionSize);
				if (info.State == MemCommit && IsReadable(info.Protect))
				{
					long readStart = Math.Max(regionStart, start);
					long readEnd = Math.Min(regionEnd, end);
					if (TryScanRange(handle, readStart, readEnd, pattern, out address))
					{
						return true;
					}
				}

				if (regionEnd <= cursor)
				{
					break;
				}
				cursor = regionEnd;
			}

			return false;
		}

		private static bool TryScanRange(IntPtr handle, long start, long end, SignaturePattern pattern, out IntPtr address)
		{
			address = IntPtr.Zero;
			int patternLength = pattern.Bytes.Length;
			long cursor = start;
			int overlap = Math.Max(0, patternLength - 1);

			while (cursor < end)
			{
				int size = (int)Math.Min(ScanChunkSize, end - cursor);
				byte[] buffer = new byte[size];
				if (!NativeMethods.ReadProcessMemory(handle, new IntPtr(cursor), buffer, buffer.Length, out IntPtr read) || read.ToInt64() != buffer.Length)
				{
					cursor += size;
					continue;
				}

				int pos = PatternScanner.Find(buffer, pattern);
				if (pos >= 0)
				{
					address = IntPtr.Add(new IntPtr(cursor), pos);
					return true;
				}

				if (size <= overlap)
				{
					break;
				}
				cursor += size - overlap;
			}

			return false;
		}

		private static bool IsReadable(uint protect)
		{
			if ((protect & PageGuard) != 0 || (protect & 0xff) == PageNoAccess)
			{
				return false;
			}

			uint mode = protect & 0xff;
			return mode == 0x02 || mode == 0x04 || mode == 0x08 || mode == 0x20 || mode == 0x40 || mode == 0x80;
		}

		private static int MarshalSizeOfMemoryBasicInformation()
		{
			return IntPtr.Size == 8 ? 48 : 28;
		}

		private static List<ProcessModule> GetScanModules(Process process)
		{
			var result = new List<ProcessModule>();
			string mainPath = SafeGetMainModuleFileName(process);
			string mainDirectory = null;
			if (!string.IsNullOrWhiteSpace(mainPath))
			{
				try { mainDirectory = Path.GetDirectoryName(mainPath); } catch { }
			}

			try
			{
				foreach (ProcessModule module in process.Modules)
				{
					string path = null;
					try { path = module.FileName; } catch { }
					bool sameDirectory = !string.IsNullOrWhiteSpace(mainDirectory) && !string.IsNullOrWhiteSpace(path) && string.Equals(Path.GetDirectoryName(path), mainDirectory, StringComparison.OrdinalIgnoreCase);
					bool minecraftName = module.ModuleName.StartsWith("Minecraft", StringComparison.OrdinalIgnoreCase);
					if (sameDirectory || minecraftName)
					{
						result.Add(module);
					}
				}
			}
			catch
			{
			}

			if (result.Count == 0)
			{
				try
				{
					if (process.MainModule != null)
					{
						result.Add(process.MainModule);
					}
				}
				catch
			{
				}
			}

			return result.GroupBy(m => m.BaseAddress).Select(g => g.First()).ToList();
		}

		private void RestoreActiveProcess_NoLock()
		{
			if (_applied.Count == 0 || _patchedProcessId == null)
			{
				_applied.Clear();
				return;
			}

			try
			{
				Process process = Process.GetProcessById(_patchedProcessId.Value);
				try
				{
					IntPtr handle = NativeMethods.OpenProcess(NativeMethods.ProcessAccessFlags.VirtualMemoryOperation | NativeMethods.ProcessAccessFlags.VirtualMemoryWrite | NativeMethods.ProcessAccessFlags.QueryInformation, false, process.Id);
					if (handle != IntPtr.Zero)
					{
						foreach (AppliedPatch patch in _applied)
						{
							NativeMethods.WriteProcessMemory(handle, patch.Address, patch.OriginalBytes, patch.OriginalBytes.Length, out _);
						}
						NativeMethods.CloseHandle(handle);
					}
				}
				finally
				{
					process.Dispose();
				}
			}
			catch
			{
			}
			finally
			{
				_applied.Clear();
			}
		}

		private IReadOnlyList<RuntimePatchDefinition> GetSelectedPatches()
		{
			var list = new List<RuntimePatchDefinition>();
			if (_itemUseDelay)
			{
				list.Add(_useGdk ? RuntimePatchCatalog.GdkItemUseDelay : RuntimePatchCatalog.LegacyItemUseDelay);
			}
			if (_noCameraReset)
			{
				list.Add(_useGdk ? RuntimePatchCatalog.GdkTeleportRotation : RuntimePatchCatalog.LegacyTeleportRotation);
			}
			if (_noHurtCam)
			{
				list.Add(_useGdk ? RuntimePatchCatalog.GdkNoHurtCam : RuntimePatchCatalog.LegacyNoHurtCam);
			}
			return list;
		}

		private Process FindMinecraftProcess()
		{
			string[] candidates = { "Minecraft.Windows", "Minecraft", "MinecraftUWP", "bedrock", "mcbe" };
			try
			{
				Process foreground = TryGetForegroundMinecraftProcess();
				if (foreground != null)
				{
					return foreground;
				}

				return Process.GetProcesses()
					.Where(p =>
					{
						try
						{
							string name = p.ProcessName;
							if (name.Equals("MinecraftLauncher", StringComparison.OrdinalIgnoreCase)) return false;
							if (candidates.Any(c => name.Equals(c, StringComparison.OrdinalIgnoreCase)) || name.StartsWith("Minecraft-", StringComparison.OrdinalIgnoreCase)) return true;
							string path = SafeGetMainModuleFileName(p);
							return path != null && path.EndsWith("Minecraft.Windows.exe", StringComparison.OrdinalIgnoreCase);
						}
						catch { return false; }
					})
					.OrderByDescending(p =>
					{
						try { return p.MainWindowHandle != IntPtr.Zero ? 1 : 0; } catch { return 0; }
					})
					.ThenByDescending(p =>
					{
						try { return p.StartTime; } catch { return DateTime.MinValue; }
					})
					.FirstOrDefault();
			}
			catch
			{
				return null;
			}
		}

		private Process TryGetForegroundMinecraftProcess()
		{
			try
			{
				IntPtr foregroundWindow = NativeMethods.GetForegroundWindow();
				if (foregroundWindow == IntPtr.Zero) return null;
				NativeMethods.GetWindowThreadProcessId(foregroundWindow, out uint processId);
				if (processId == 0) return null;
				Process process = Process.GetProcessById((int)processId);
				string path = SafeGetMainModuleFileName(process);
				if ((path != null && path.EndsWith("Minecraft.Windows.exe", StringComparison.OrdinalIgnoreCase)) || (process.MainWindowTitle ?? string.Empty).IndexOf("minecraft", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					return process;
				}
				process.Dispose();
			}
			catch
			{
			}
			return null;
		}

		private static string SafeGetMainModuleFileName(Process process)
		{
			try { return process.MainModule?.FileName; } catch { return null; }
		}

		private void SetStatus(string text)
		{
			_window.Dispatcher.BeginInvoke(new Action(() =>
			{
				if (_window.PatchStatusBlock != null)
				{
					_window.PatchStatusBlock.Text = text;
				}
			}), System.Windows.Threading.DispatcherPriority.Background);
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
