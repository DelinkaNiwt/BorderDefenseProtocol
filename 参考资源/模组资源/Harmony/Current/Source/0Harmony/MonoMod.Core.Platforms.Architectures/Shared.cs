using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using MonoMod.Utils;

namespace MonoMod.Core.Platforms.Architectures;

internal static class Shared
{
	public unsafe static IAllocatedMemory CreateSingleExecutableStub(ISystem system, ReadOnlySpan<byte> stubBytes)
	{
		IControlFlowGuard controlFlowGuard = system as IControlFlowGuard;
		if (controlFlowGuard != null && !controlFlowGuard.IsSupported)
		{
			controlFlowGuard = null;
		}
		Helpers.Assert(system.MemoryAllocator.TryAllocate(new AllocationRequest(stubBytes.Length)
		{
			Executable = true,
			Alignment = (controlFlowGuard?.TargetAlignmentRequirement ?? 1)
		}, out IAllocatedMemory allocated), null, "system.MemoryAllocator.TryAllocate(new(stubBytes.Length)\n            {\n                Executable = true,\n                Alignment = cfg is not null ? cfg.TargetAlignmentRequirement : 1, // if CFG is supported, use that alignment\n            }, out var alloc)");
		system.PatchData(PatchTargetKind.Executable, allocated.BaseAddress, stubBytes, default(Span<byte>));
		controlFlowGuard?.RegisterValidIndirectCallTargets((void*)allocated.BaseAddress, allocated.Size, new ReadOnlySpan<IntPtr>(_003C49fa8ad6_002D48f4_002D474b_002Db220_002D56b3c5b5008b_003E_003CPrivateImplementationDetails_003E.DF3F619804A92FDB4057192DC43DD748EA778ADC52BC498CE80524C014B81119_B8 ?? (_003C49fa8ad6_002D48f4_002D474b_002Db220_002D56b3c5b5008b_003E_003CPrivateImplementationDetails_003E.DF3F619804A92FDB4057192DC43DD748EA778ADC52BC498CE80524C014B81119_B8 = new IntPtr[1])));
		return allocated;
	}

	public unsafe static ReadOnlyMemory<IAllocatedMemory> CreateVtableStubs(ISystem system, IntPtr vtableBase, int vtableSize, ReadOnlySpan<byte> stubData, int indexOffs, bool premulOffset)
	{
		IControlFlowGuard controlFlowGuard = system as IControlFlowGuard;
		if (controlFlowGuard != null && !controlFlowGuard.IsSupported)
		{
			controlFlowGuard = null;
		}
		int num = stubData.Length;
		if (controlFlowGuard != null)
		{
			int targetAlignmentRequirement = controlFlowGuard.TargetAlignmentRequirement;
			num = ((num - 1) / targetAlignmentRequirement + 1) * targetAlignmentRequirement;
		}
		int maxSize = system.MemoryAllocator.MaxSize;
		int num2 = num * vtableSize;
		int num3 = num2 / maxSize;
		int num4 = maxSize / num;
		int num5 = num4 * num;
		int num6 = num2 % num5;
		IAllocatedMemory[] array = new IAllocatedMemory[num3 + ((num6 != 0) ? 1 : 0)];
		byte[] array2 = ArrayPool<byte>.Shared.Rent(num5);
		IntPtr[] array3 = ArrayPool<IntPtr>.Shared.Rent(num4);
		Span<byte> backup = array2.AsSpan();
		Span<byte> span = backup.Slice(0, num5);
		for (int i = 0; i < num4; i++)
		{
			stubData.CopyTo(span.Slice(i * num));
		}
		ref IntPtr vtblBase = ref Unsafe.AsRef<IntPtr>((void*)vtableBase);
		AllocationRequest allocationRequest = new AllocationRequest(num5);
		allocationRequest.Alignment = controlFlowGuard?.TargetAlignmentRequirement ?? IntPtr.Size;
		allocationRequest.Executable = true;
		AllocationRequest allocationRequest2 = allocationRequest;
		for (int j = 0; j < num3; j++)
		{
			Helpers.Assert(system.MemoryAllocator.TryAllocate(allocationRequest2, out IAllocatedMemory allocated), null, "system.MemoryAllocator.TryAllocate(allocReq, out var alloc)");
			array[j] = allocated;
			FillBufferIndicies(num, indexOffs, num4, j, span, premulOffset);
			FillVtbl(num, num4 * j, ref vtblBase, num4, allocated.BaseAddress, array3);
			IntPtr baseAddress = allocated.BaseAddress;
			ReadOnlySpan<byte> data = span;
			backup = default(Span<byte>);
			system.PatchData(PatchTargetKind.Executable, baseAddress, data, backup);
			controlFlowGuard?.RegisterValidIndirectCallTargets((void*)allocated.BaseAddress, allocated.Size, (ReadOnlySpan<IntPtr>)array3.AsSpan(0, num4));
		}
		if (num6 > 0)
		{
			allocationRequest2 = allocationRequest2 with
			{
				Size = num6
			};
			Helpers.Assert(system.MemoryAllocator.TryAllocate(allocationRequest2, out IAllocatedMemory allocated2), null, "system.MemoryAllocator.TryAllocate(allocReq, out var alloc)");
			array[array.Length - 1] = allocated2;
			int num7 = num6 / num;
			FillBufferIndicies(num, indexOffs, num4, num3, span, premulOffset);
			FillVtbl(num, num4 * num3, ref vtblBase, num7, allocated2.BaseAddress, array3);
			IntPtr baseAddress2 = allocated2.BaseAddress;
			ReadOnlySpan<byte> data2 = span.Slice(0, num6);
			backup = default(Span<byte>);
			system.PatchData(PatchTargetKind.Executable, baseAddress2, data2, backup);
			controlFlowGuard?.RegisterValidIndirectCallTargets((void*)allocated2.BaseAddress, allocated2.Size, (ReadOnlySpan<IntPtr>)array3.AsSpan(0, num7));
		}
		ArrayPool<IntPtr>.Shared.Return(array3);
		ArrayPool<byte>.Shared.Return(array2);
		return array;
		static void FillBufferIndicies(int stubSize, int num8, int numPerAlloc, int num10, Span<byte> mainAllocBuf, bool premul)
		{
			for (int k = 0; k < numPerAlloc; k++)
			{
				ref byte destination = ref mainAllocBuf[k * stubSize + num8];
				uint num9 = (uint)(numPerAlloc * num10 + k);
				if (premul)
				{
					num9 *= (uint)IntPtr.Size;
				}
				Unsafe.WriteUnaligned(ref destination, num9);
			}
		}
		static void FillVtbl(int stubSize, int baseIndex, ref IntPtr source, int numEntries, nint baseAddr, nint[] offsets)
		{
			for (int k = 0; k < numEntries; k++)
			{
				nint num8 = (offsets[k] = stubSize * k);
				Unsafe.Add(ref source, baseIndex + k) = baseAddr + num8;
			}
		}
	}
}
