using System;

namespace Autoclicker.Native
{

	public struct CURSORINFO
	{

		public int cbSize;

		public int flags;

		public IntPtr hCursor;

		public POINT ptScreenPos;
	}
}
