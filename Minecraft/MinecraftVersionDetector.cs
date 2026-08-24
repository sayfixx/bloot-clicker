using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Autoclicker.Minecraft
{

	internal enum MinecraftEdition
	{
		None,
		Bedrock,
		Java
	}

	internal readonly struct MinecraftInfo
	{
		public MinecraftInfo(bool isRunning, string version, MinecraftEdition edition)
		{
			IsRunning = isRunning;
			Version = version;
			Edition = edition;
		}

		public bool IsRunning { get; }

		public string Version { get; }

		public MinecraftEdition Edition { get; }

		public static MinecraftInfo NotRunning { get; } = new MinecraftInfo(false, null, MinecraftEdition.None);

		public string DisplayText
		{
			get
			{
				if (!IsRunning)
				{
					return "Minecraft not detected";
				}

				string editionLabel = Edition == MinecraftEdition.Bedrock
					? "Bedrock"
					: (Edition == MinecraftEdition.Java ? "Java" : null);

				if (Version == null)
				{
					return editionLabel != null
						? $"Minecraft ({editionLabel}) — version unknown"
						: "Minecraft detected";
				}

				return editionLabel != null
					? $"Minecraft {Version} ({editionLabel})"
					: $"Minecraft {Version}";
			}
		}
	}

	internal static class MinecraftVersionDetector
	{
		private static readonly string[] BedrockProcessNames =
		{
			"Minecraft.Windows",
			"Minecraft",
			"MinecraftUWP",
			"bedrock",
			"mcbe"
		};

		private static readonly string[] JavaProcessNames =
		{
			"javaw",
			"java"
		};

		private static readonly Regex JavaTitleVersionRegex = new Regex(
			@"Minecraft\*?\s+([0-9]+\.[0-9]+(?:\.[0-9]+)?(?:\s*Pre-Release\s*[0-9]+)?|[0-9]+w[0-9]+[a-z])",
			RegexOptions.IgnoreCase | RegexOptions.Compiled);

		private static readonly Regex GenericVersionRegex = new Regex(
			@"\b([0-9]+\.[0-9]+(?:\.[0-9]+)?)\b",
			RegexOptions.Compiled);

		private static MinecraftInfo _cached = MinecraftInfo.NotRunning;
		private static long _lastCheckTick;
		private const long CheckIntervalMs = 1500L;

		public static MinecraftInfo GetCached()
		{
			long timestamp = Stopwatch.GetTimestamp();
			long intervalTicks = Stopwatch.Frequency / 1000L * CheckIntervalMs;
			if (timestamp - _lastCheckTick >= intervalTicks)
			{
				_cached = Detect();
				_lastCheckTick = timestamp;
			}
			return _cached;
		}

		public static MinecraftInfo Detect()
		{
			try
			{
				MinecraftInfo bedrock = TryDetectBedrock();
				if (bedrock.IsRunning)
				{
					return bedrock;
				}

				MinecraftInfo java = TryDetectJava();
				if (java.IsRunning)
				{
					return java;
				}
			}
			catch
			{

			}

			return MinecraftInfo.NotRunning;
		}

		private static MinecraftInfo TryDetectBedrock()
		{
			foreach (string name in BedrockProcessNames)
			{
				foreach (Process process in SafeGetProcessesByName(name))
				{
					using (process)
					{
						if (process.HasExited)
						{
							continue;
						}

						string path = SafeGetMainModuleFileName(process);

						bool looksLikeBedrock =
							(path != null && path.EndsWith("Minecraft.Windows.exe", StringComparison.OrdinalIgnoreCase)) ||
							process.ProcessName.StartsWith("Minecraft-", StringComparison.OrdinalIgnoreCase) ||
							string.Equals(process.ProcessName, "Minecraft.Windows", StringComparison.OrdinalIgnoreCase) ||
							string.Equals(process.ProcessName, "bedrock", StringComparison.OrdinalIgnoreCase) ||
							string.Equals(process.ProcessName, "mcbe", StringComparison.OrdinalIgnoreCase);

						if (!looksLikeBedrock)
						{
							continue;
						}

						string version = path != null ? TryGetBedrockFileVersion(path) : null;
						if (string.IsNullOrWhiteSpace(version))
						{
							version = TryGetPackageVersion(process);
						}
						if (string.IsNullOrWhiteSpace(version) && path != null)
						{
							version = TryGetVersionFromPackagePath(path);
						}
						return new MinecraftInfo(true, version, MinecraftEdition.Bedrock);
					}
				}
			}

			return MinecraftInfo.NotRunning;
		}

		private static string TryGetBedrockFileVersion(string exePath)
		{
			try
			{
				FileVersionInfo info = FileVersionInfo.GetVersionInfo(exePath);
				string raw = !string.IsNullOrWhiteSpace(info.ProductVersion) ? info.ProductVersion : info.FileVersion;
				return NormalizeBedrockVersion(raw);
			}
			catch
			{
				return null;
			}
		}

		private static string NormalizeBedrockVersion(string raw)
		{
			if (string.IsNullOrWhiteSpace(raw))
			{
				return null;
			}

			string cleaned = raw.Split(' ')[0].Trim();
			string[] parts = cleaned.Split('.');

			if (parts.Length >= 4 && parts[3] == "0")
			{
				return string.Join(".", parts[0], parts[1], parts[2]);
			}

			if (parts.Length >= 4)
			{
				return string.Join(".", parts[0], parts[1], parts[2], parts[3]);
			}

			return cleaned;
		}

		private static string TryGetPackageVersion(Process process)
		{
			try
			{
				uint length = 0;
				int rc = GetPackageFullName(process.Handle, ref length, null);
				if (rc != 122 || length == 0) return null;
				var buffer = new System.Text.StringBuilder((int)length);
				if (GetPackageFullName(process.Handle, ref length, buffer) != 0) return null;
				return ExtractPackageVersion(buffer.ToString());
			}
			catch { return null; }
		}

		private static string TryGetVersionFromPackagePath(string exePath)
		{
			try
			{
				DirectoryInfo dir = new DirectoryInfo(Path.GetDirectoryName(exePath) ?? string.Empty);
				return ExtractPackageVersion(dir.Name);
			}
			catch { return null; }
		}

		private static string ExtractPackageVersion(string packageName)
		{
			if (string.IsNullOrWhiteSpace(packageName)) return null;
			Match match = Regex.Match(packageName, @"_(\d+\.\d+\.\d+(?:\.\d+)?)_(?:x64|x86|arm64)_", RegexOptions.IgnoreCase);
			if (!match.Success) return null;
			return NormalizeBedrockVersion(match.Groups[1].Value);
		}

		[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		private static extern int GetPackageFullName(IntPtr hProcess, ref uint packageFullNameLength, System.Text.StringBuilder packageFullName);

		private static MinecraftInfo TryDetectJava()
		{
			foreach (string name in JavaProcessNames)
			{
				foreach (Process process in SafeGetProcessesByName(name))
				{
					using (process)
					{
						if (process.HasExited)
						{
							continue;
						}

						string title = SafeGetMainWindowTitle(process);
						if (string.IsNullOrWhiteSpace(title))
						{
							continue;
						}

						if (title.IndexOf("minecraft", StringComparison.OrdinalIgnoreCase) < 0)
						{

							continue;
						}

						Match match = JavaTitleVersionRegex.Match(title);
						if (!match.Success)
						{
							match = GenericVersionRegex.Match(title);
						}

						string version = match.Success ? match.Groups[1].Value.Trim() : null;
						return new MinecraftInfo(true, version, MinecraftEdition.Java);
					}
				}
			}

			return MinecraftInfo.NotRunning;
		}

		private static IEnumerable<Process> SafeGetProcessesByName(string name)
		{
			try
			{
				return Process.GetProcessesByName(name);
			}
			catch
			{
				return Array.Empty<Process>();
			}
		}

		private static string SafeGetMainModuleFileName(Process process)
		{
			try
			{
				return process.MainModule?.FileName;
			}
			catch
			{

				return null;
			}
		}

		private static string SafeGetMainWindowTitle(Process process)
		{
			try
			{
				return process.MainWindowTitle;
			}
			catch
			{
				return null;
			}
		}
	}
}
