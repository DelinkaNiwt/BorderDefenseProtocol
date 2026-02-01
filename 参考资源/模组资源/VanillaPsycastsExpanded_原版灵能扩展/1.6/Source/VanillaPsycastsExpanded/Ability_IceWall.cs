using System.Collections.Generic;
using RimWorld.Planet;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded;

public class Ability_IceWall : Ability
{
	public override void Cast(params GlobalTargetInfo[] targets)
	{
		((Ability)this).Cast(targets);
		IEnumerable<IntVec3> enumerable = GenRadial.RadialCellsAround(targets[0].Cell, 5f, 5.9f);
		AbilityExtension_Building modExtension = ((Def)(object)base.def).GetModExtension<AbilityExtension_Building>();
		foreach (IntVec3 item in enumerable)
		{
			if (item.GetEdifice(base.pawn.Map) == null)
			{
				GenSpawn.Spawn(modExtension.building, item, base.pawn.Map, WipeMode.VanishOrMoveAside).SetFactionDirect(base.pawn.Faction);
			}
		}
	}
}
