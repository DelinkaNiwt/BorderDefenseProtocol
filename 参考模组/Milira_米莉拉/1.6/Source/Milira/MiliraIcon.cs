using UnityEngine;
using Verse;

namespace Milira;

[StaticConstructorOnStartup]
public class MiliraIcon
{
	public static readonly Texture2D MilianConfig = ContentFinder<Texture2D>.Get("Milira/UI/Icons/MilianConfig");

	public static readonly Texture2D MilianCheckApparel = ContentFinder<Texture2D>.Get("Milira/UI/CheckApparel");

	public static readonly Texture2D MiliraGivePawn = ContentFinder<Texture2D>.Get("Milira/Gizmo/MiliraGivePawn");

	public static readonly Texture2D FlySwitch_Always = ContentFinder<Texture2D>.Get("Milira/Gizmo/FlySwitch_Always");

	public static readonly Texture2D FlySwitch_Never = ContentFinder<Texture2D>.Get("Milira/Gizmo/FlySwitch_Never");

	public static readonly Texture2D FlySwitch_OnlyForMove = ContentFinder<Texture2D>.Get("Milira/Gizmo/FlySwitch_OnlyForMove");
}
