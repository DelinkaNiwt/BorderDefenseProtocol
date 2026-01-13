using System.Diagnostics.CodeAnalysis;

namespace MonoMod.Core.Platforms;

internal interface IMemoryAllocator
{
	int MaxSize { get; }

	bool TryAllocate(AllocationRequest request, [_003C49fa8ad6_002D48f4_002D474b_002Db220_002D56b3c5b5008b_003EMaybeNullWhen(false)] out IAllocatedMemory allocated);

	bool TryAllocateInRange(PositionedAllocationRequest request, [_003C49fa8ad6_002D48f4_002D474b_002Db220_002D56b3c5b5008b_003EMaybeNullWhen(false)] out IAllocatedMemory allocated);
}
