using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using MonoMod.Core.Platforms;
using MonoMod.Utils;

namespace MonoMod.Core;

[CLSCompliant(true)]
internal static class DetourFactory
{
	private static PlatformTripleDetourFactory? lazyCurrent;

	private static readonly Func<PlatformTripleDetourFactory> createDefaultFactoryFunc = CreateDefaultFactory;

	public static IDetourFactory Current
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return Helpers.GetOrInit(ref lazyCurrent, createDefaultFactoryFunc);
		}
	}

	private static PlatformTripleDetourFactory CreateDefaultFactory()
	{
		return new PlatformTripleDetourFactory(PlatformTriple.Current);
	}

	public static ICoreDetour CreateDetour(this IDetourFactory factory, MethodBase source, MethodBase target, bool applyByDefault = true)
	{
		Helpers.ThrowIfArgumentNull(factory, "factory");
		return factory.CreateDetour(new CreateDetourRequest(source, target)
		{
			ApplyByDefault = applyByDefault
		});
	}

	public static ICoreNativeDetour CreateNativeDetour(this IDetourFactory factory, IntPtr source, IntPtr target, bool applyByDefault = true)
	{
		Helpers.ThrowIfArgumentNull(factory, "factory");
		return factory.CreateNativeDetour(new CreateNativeDetourRequest(source, target)
		{
			ApplyByDefault = applyByDefault
		});
	}
}
