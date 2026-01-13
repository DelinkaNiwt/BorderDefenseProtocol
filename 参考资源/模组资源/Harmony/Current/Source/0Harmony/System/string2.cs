using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System;

internal static class string2
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsNullOrEmpty([_003C1308eb90_002D8064_002D4e43_002Db224_002Dde8c2b9aa062_003ENotNullWhen(false)] string? value)
	{
		return string.IsNullOrEmpty(value);
	}
}
