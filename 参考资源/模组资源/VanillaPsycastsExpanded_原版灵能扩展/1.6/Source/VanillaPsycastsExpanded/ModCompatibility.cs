using AlienRace;
using UnityEngine;
using Verse;

namespace VanillaPsycastsExpanded;

[StaticConstructorOnStartup]
public static class ModCompatibility
{
	public static bool AlienRacesIsActive;

	static ModCompatibility()
	{
		AlienRacesIsActive = ModsConfig.IsActive("erdelf.HumanoidAlienRaces") || ModsConfig.IsActive("erdelf.HumanoidAlienRaces_steam");
	}

	public static Color GetSkinColorFirst(Pawn pawn)
	{
		AlienComp val = ((Thing)pawn).TryGetComp<AlienComp>();
		if (val != null)
		{
			return val.GetChannel("skin").first;
		}
		return Color.white;
	}

	public static Color GetSkinColorSecond(Pawn pawn)
	{
		AlienComp val = ((Thing)pawn).TryGetComp<AlienComp>();
		if (val != null)
		{
			return val.GetChannel("skin").second;
		}
		return Color.white;
	}

	public static void SetSkinColorFirst(Pawn pawn, Color color)
	{
		AlienComp val = ((Thing)pawn).TryGetComp<AlienComp>();
		if (val != null)
		{
			val.OverwriteColorChannel("skin", (Color?)color, (Color?)null);
		}
	}

	public static void SetSkinColorSecond(Pawn pawn, Color color)
	{
		AlienComp val = ((Thing)pawn).TryGetComp<AlienComp>();
		if (val != null)
		{
			val.OverwriteColorChannel("skin", (Color?)null, (Color?)color);
		}
	}
}
