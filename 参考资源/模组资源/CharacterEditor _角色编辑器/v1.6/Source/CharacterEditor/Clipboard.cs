using System;
using System.Runtime.InteropServices;

namespace CharacterEditor;

internal static class Clipboard
{
	[DllImport("user32.dll")]
	internal static extern bool OpenClipboard(IntPtr hWndNewOwner);

	[DllImport("user32.dll")]
	internal static extern bool CloseClipboard();

	[DllImport("user32.dll")]
	internal static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

	internal static void CopyToClip(string text)
	{
		OpenClipboard(IntPtr.Zero);
		SetClipboardData(13u, Marshal.StringToHGlobalUni(text));
		CloseClipboard();
	}
}
