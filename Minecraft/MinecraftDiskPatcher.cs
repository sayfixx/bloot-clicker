using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Autoclicker.Minecraft
{
	internal static class MinecraftDiskPatcher
	{
		public static string GetDefaultExecutablePath()
		{
			string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			string legacy = Path.Combine(folderPath, "Packages", "Microsoft.MinecraftUWP_8wekyb3d8bbwe", "LocalState", "games", "com.mojang", "minecraftpe", "Minecraft.Windows.exe");
			if (File.Exists(legacy)) return legacy;

			try
			{
				string windowsApps = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "WindowsApps");
				if (Directory.Exists(windowsApps))
				{
					string[] candidates = Directory.GetDirectories(windowsApps, "Microsoft.MinecraftUWP_*");
					foreach (string dir in candidates.OrderByDescending(d => d, StringComparer.OrdinalIgnoreCase))
					{
						string exe = Path.Combine(dir, "Minecraft.Windows.exe");
						if (File.Exists(exe)) return exe;
					}
				}
			}
			catch { }

			return legacy;
		}

		public static bool HasAnyMatch(string exePath, PatchSelection selection)
		{
			try
			{
				if (!File.Exists(exePath)) return false;
				byte[] buffer = File.ReadAllBytes(exePath);
				foreach (BinaryPatchDefinition p in GetSelectedPatches(false, selection))
				{
					if (PatternScanner.Find(buffer, SignaturePattern.Parse(p.Signature)) >= 0)
						return true;
				}
				return false;
			}
			catch { return false; }
		}

		public static PatchOperationResult Apply(string exePath, bool gdk, PatchSelection selection)
		{
			try
			{
				List<BinaryPatchDefinition> selected = GetSelectedPatches(gdk, selection);
				if (selected.Count == 0)
					return new PatchOperationResult(false, "no patches selected");
				if (!File.Exists(exePath))
					return new PatchOperationResult(false, "failed to find minecraft");

				string backupPath = GetBackupPath(exePath);
				if (!File.Exists(backupPath))
					File.Copy(exePath, backupPath, true);

				byte[] array = File.ReadAllBytes(backupPath);
				var applied = new List<string>();
				var failed = new List<string>();

				foreach (BinaryPatchDefinition p in selected)
				{
					int pos = PatternScanner.Find(array, SignaturePattern.Parse(p.Signature));
					if (pos >= 0)
					{
						Buffer.BlockCopy(p.Replacement, 0, array, pos, p.Replacement.Length);
						applied.Add(p.Id);
					}
					else
					{
						failed.Add(p.Id);
					}
				}

				if (applied.Count == 0)
				{
					string detail = failed.Count > 0
						? "none matched — wrong version or wrong signatures:\n" + string.Join(", ", failed)
						: "no signatures matched — wrong Minecraft version";
					return new PatchOperationResult(false, detail);
				}

				File.WriteAllBytes(exePath, array);

				string msg = applied.Count + " patch(es) applied: " + string.Join(", ", applied);
				if (failed.Count > 0)
					msg += "\nfailed: " + string.Join(", ", failed);
				return new PatchOperationResult(true, msg);
			}
			catch (Exception ex)
			{
				string msg = ex.Message;
				if (msg.Contains("being used by another process") || msg.Contains("access"))
					return new PatchOperationResult(false, "Minecraft is running. Close the game and try again.");
				return new PatchOperationResult(false, "failed: " + ex.Message);
			}
		}

		public static PatchOperationResult Restore(string exePath)
		{
			try
			{
				string backupPath = GetBackupPath(exePath);
				if (!File.Exists(backupPath) || !File.Exists(exePath))
					return new PatchOperationResult(false, "no backup found");
				File.Copy(backupPath, exePath, true);
				return new PatchOperationResult(true, "original restored");
			}
			catch { return new PatchOperationResult(false, "failed"); }
		}

		public static bool HasBackup(string exePath) => !string.IsNullOrWhiteSpace(exePath) && File.Exists(GetBackupPath(exePath));

		private static string GetBackupPath(string exePath) => exePath + ".bak";

		private static List<BinaryPatchDefinition> GetSelectedPatches(bool gdk, PatchSelection selection)
		{
			var list = new List<BinaryPatchDefinition>();
			if (selection.NoHurtCam)
				list.Add(gdk ? GdkNoHurtCam : LegacyNoHurtCam);
			if (selection.GuiScale)
				list.Add(gdk ? GdkGuiScale : LegacyGuiScale);
			if (selection.TeleportRotation)
				list.Add(gdk ? GdkTeleportRotation : LegacyTeleportRotation);
			if (selection.DelayFix)
				list.Add(gdk ? GdkDelayFix : LegacyDelayFix);
			if (selection.MinimalViewBobbing)
				list.Add(gdk ? GdkMinimalViewBobbing : LegacyMinimalViewBobbing);
			if (selection.ItemUseDelay)
				list.Add(gdk ? GdkItemUseDelay : LegacyItemUseDelay);
			if (selection.ThirdPersonNametag)
				list.Add(gdk ? GdkThirdPersonNametag : LegacyThirdPersonNametag);
			if (selection.PlayScreenFix && !gdk)
				list.Add(LegacyPlayScreenFix);
			return list;
		}

		private static readonly BinaryPatchDefinition LegacyNoHurtCam = new BinaryPatchDefinition("NoHurtCam", "eb ? 0f 57 c0 f3 0f 11 0b", HexBytes.Parse("90 90"));
		private static readonly BinaryPatchDefinition LegacyGuiScale = new BinaryPatchDefinition("GuiScale", "00 00 ? ? 00 00 A0 40 00 00 C0 40", HexBytes.Parse("00 00 E0"));
		private static readonly BinaryPatchDefinition LegacyTeleportRotation = new BinaryPatchDefinition("TeleportRotation", "E8 ? ? ? ? 48 8B 03 48 8D 94 24 ? ? ? ? 48 8B 4B", HexBytes.Parse("90 90 90 90 90"));
		private static readonly BinaryPatchDefinition LegacyDelayFix = new BinaryPatchDefinition("DelayFix", "FF 15 ? ? ? ? 48 8B ? 48 8B ? 48 8B ? ? ? ? ? FF 15 ? ? ? ? 32 DB", HexBytes.Parse("90 90 90 90 90 90"));
		private static readonly BinaryPatchDefinition LegacyMinimalViewBobbing = new BinaryPatchDefinition("MinimalViewBobbing", "FF 15 ? ? ? ? 80 7C 24 60 ? 0F 84 ? ? ? ? 48 89", HexBytes.Parse("90 90 90 90 90 90"));
		private static readonly BinaryPatchDefinition LegacyItemUseDelay = new BinaryPatchDefinition("ItemUseDelay", "FF 15 ? ? ? ? 48 8B ? 48 8B ? 48 8B ? ? ? ? ? FF 15 ? ? ? ? 32 DB", HexBytes.Parse("90 90 90 90 90 90"));
		private static readonly BinaryPatchDefinition LegacyThirdPersonNametag = new BinaryPatchDefinition("ThirdPersonNametag", "0F 84 ? ? ? ? 49 8B 45 ? 49 8B CD 48 8B 80 ? ? ? ? FF 15 ? ? ? ? 84 C0 0F 85", HexBytes.Parse("90 90 90 90 90 90"));
		private static readonly BinaryPatchDefinition LegacyPlayScreenFix = new BinaryPatchDefinition("PlayScreenFix", "6D 63 2D 61 62 2D 6E 65 77 2D 70 6C 61 79 2D 73 63 72 65 65 6E 2D", HexBytes.Parse("20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20 20"));

		private static readonly BinaryPatchDefinition GdkNoHurtCam = new BinaryPatchDefinition("NoHurtCam", "EB ? 0F 57 C0 F3 0F 11 0B", Enumerable.Repeat<byte>(144, 2).ToArray());
		private static readonly BinaryPatchDefinition GdkGuiScale = new BinaryPatchDefinition("GuiScale", "00 00 ? ? 00 00 A0 40 00 00 C0 40", BitConverter.GetBytes(2f));
		private static readonly BinaryPatchDefinition GdkTeleportRotation = new BinaryPatchDefinition("TeleportRotation", "E8 ? ? ? ? 48 8B 03 48 8D 54 24 ? 48 8B 4B", Enumerable.Repeat<byte>(144, 5).ToArray());
		private static readonly BinaryPatchDefinition GdkDelayFix = new BinaryPatchDefinition("DelayFix", "FF 15 ? ? ? ? 48 8B ? 48 8B ? 48 8B ? ? ? ? ? FF 15 ? ? ? ? 32 DB", Enumerable.Repeat<byte>(144, 6).ToArray());
		private static readonly BinaryPatchDefinition GdkMinimalViewBobbing = new BinaryPatchDefinition("MinimalViewBobbing", "FF 15 ? ? ? ? 80 7C 24 60 ? 0F 84 ? ? ? ? 48 89", Enumerable.Repeat<byte>(144, 6).ToArray());
		private static readonly BinaryPatchDefinition GdkItemUseDelay = new BinaryPatchDefinition("ItemUseDelay", "FF 15 ? ? ? ? 48 8B ? 48 8B ? 48 8B ? ? ? ? ? FF 15 ? ? ? ? 32 DB", Enumerable.Repeat<byte>(144, 6).ToArray());
		private static readonly BinaryPatchDefinition GdkThirdPersonNametag = new BinaryPatchDefinition("ThirdPersonNametag", "0F 84 ? ? ? ? 49 8B 45 ? 49 8B CD 48 8B 80 ? ? ? ? FF 15 ? ? ? ? 84 C0 0F 85", Enumerable.Repeat<byte>(144, 6).ToArray());
	}
}
