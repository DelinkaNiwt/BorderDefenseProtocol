using System.Collections.Generic;
using System.Linq;
using RimWorld.Planet;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded;

public class Ability_IceBlock : Ability
{
	public override void Cast(params GlobalTargetInfo[] targets)
	{
		((Ability)this).Cast(targets);
		List<IntVec3> source = CellRect.CenteredOn(targets[0].Cell, 5, 5).Cells.InRandomOrder().ToList();
		source = source.Take(source.Count() - 5).ToList();
		AbilityExtension_Building modExtension = ((Def)(object)base.def).GetModExtension<AbilityExtension_Building>();
		foreach (IntVec3 item in source)
		{
			if (item.GetEdifice(base.pawn.Map) == null)
			{
				GenSpawn.Spawn(modExtension.building, item, base.pawn.Map, WipeMode.VanishOrMoveAside).SetFactionDirect(base.pawn.Faction);
			}
		}
	}
}
