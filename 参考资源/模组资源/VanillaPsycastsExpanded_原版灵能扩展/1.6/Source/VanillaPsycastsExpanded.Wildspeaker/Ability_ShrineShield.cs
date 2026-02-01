using RimWorld;
using RimWorld.Planet;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded.Wildspeaker;

public class Ability_ShrineShield : Ability
{
	public override void Cast(params GlobalTargetInfo[] targets)
	{
		((Ability)this).Cast(targets);
		Map map = targets[0].Map;
		foreach (Thing item in map.listerThings.ThingsOfDef(ThingDefOf.NatureShrine_Small))
		{
			Ability_Spawn.Spawn((GlobalTargetInfo)item, VPE_DefOf.VPE_Shrineshield_Small, (Ability)(object)this);
		}
		foreach (Thing item2 in map.listerThings.ThingsOfDef(ThingDefOf.NatureShrine_Large))
		{
			Ability_Spawn.Spawn((GlobalTargetInfo)item2, VPE_DefOf.VPE_Shrineshield_Large, (Ability)(object)this);
		}
	}
}
