using System;

namespace Autoclicker.Minecraft
{

	internal enum PatchApplyKind
	{

		ReplaceBytes,

		Nop,

		NopCall5,

		WriteFloat
	}
}
