using UnityEngine;
using Verse;

namespace NCLWorm;

[StaticConstructorOnStartup]
public static class NCLWormTexCommand
{
	public static readonly Texture2D NCLCourier = ContentFinder<Texture2D>.Get("UI/Misc/NCLCourier");

	public static readonly Texture2D NeedUnitDividerTex = ContentFinder<Texture2D>.Get("UI/Misc/NeedUnitDivider");

	public static readonly Texture2D ShieldGzimoBase = ContentFinder<Texture2D>.Get("UI/Misc/ShieldGzimoBase");

	public static readonly Texture2D WindowBase = ContentFinder<Texture2D>.Get("UI/Misc/WindowBase");
}
