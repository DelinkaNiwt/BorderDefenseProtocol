using System;
using System.Diagnostics.CodeAnalysis;

namespace MonoMod.Core.Platforms.Memory;

internal abstract class QueryingMemoryPageAllocatorBase
{
	public abstract uint PageSize { get; }

	public abstract bool TryQueryPage(IntPtr pageAddr, out bool isFree, out IntPtr allocBase, out nint allocSize);

	public abstract bool TryAllocatePage(nint size, bool executable, out IntPtr allocated);

	public abstract bool TryAllocatePage(IntPtr pageAddr, nint size, bool executable, out IntPtr allocated);

	public abstract bool TryFreePage(IntPtr pageAddr, [_003C49fa8ad6_002D48f4_002D474b_002Db220_002D56b3c5b5008b_003ENotNullWhen(false)] out string? errorMsg);
}
