using System;

namespace Autoclicker.Minecraft
{
	internal static class RuntimePatchCatalog
	{
		public static readonly RuntimePatchDefinition LegacyNoHurtCam = new RuntimePatchDefinition("NoHurtCam", "EB ? 0F 57 C0 F3 0F 11 0B", 0, PatchApplyKind.ReplaceBytes, "90 90", 0, 0f);

		public static readonly RuntimePatchDefinition LegacyGuiScale = new RuntimePatchDefinition("GuiScale", "00 00 ? ? 00 00 A0 40 00 00 C0 40", 0, PatchApplyKind.ReplaceBytes, "00 00 E0", 0, 0f);

		public static readonly RuntimePatchDefinition LegacyTeleportRotation = new RuntimePatchDefinition("TeleportRotation", "E8 ? ? ? ? 48 8B 03 48 8D 94 24 ? ? ? ? 48 8B 4B", 0, PatchApplyKind.ReplaceBytes, "90 90 90 90 90", 0, 0f);

		public static readonly RuntimePatchDefinition LegacyItemUseDelay = new RuntimePatchDefinition(
			"ItemUseDelay",
			"FF 15 ? ? ? ? 48 8B ? 48 8B ? 48 8B ? ? ? ? ? FF 15 ? ? ? ? 32 DB",
			0, PatchApplyKind.Nop, "", 6, 0f);

		public static readonly RuntimePatchDefinition GdkNoHurtCam = new RuntimePatchDefinition("NoHurtCam", "EB ? 0F 57 C0 F3 0F 11 0B", 0, PatchApplyKind.Nop, "", 2, 0f);

		public static readonly RuntimePatchDefinition GdkGuiScale = new RuntimePatchDefinition("GuiScale", "00 00 ? ? 00 00 A0 40 00 00 C0 40", 0, PatchApplyKind.WriteFloat, "", 0, 2f);

		public static readonly RuntimePatchDefinition GdkTeleportRotation = new RuntimePatchDefinition("TeleportRotation", "E8 ? ? ? ? 48 8B 03 48 8D 54 24 ? 48 8B 4B", 0, PatchApplyKind.NopCall5, "", 0, 0f);

		public static readonly RuntimePatchDefinition GdkItemUseDelay = new RuntimePatchDefinition(
			"ItemUseDelay",
			"FF 15 ? ? ? ? 48 8B 06 48 8B CE 48 8B 80 ? ? ? ? FF 15 ? ? ? ? 40 84 FF 75",
			0, PatchApplyKind.Nop, "", 6, 0f);

	}
}
