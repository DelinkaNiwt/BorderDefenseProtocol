using RimWorld;
using Verse;

namespace NCL;

public class Verb_AbilityTeleport : Verb_CastAbility
{
	protected override bool TryCastShot()
	{
		IntVec3 cell = currentTarget.Cell;
		if (base.TryCastShot())
		{
			Map map = caster.Map;
			bool flag2 = false;
			if (CasterIsPawn)
			{
				if (CasterPawn.drafter != null)
				{
					flag2 = CasterPawn.drafter.Drafted;
				}
				CasterPawn.teleporting = true;
			}
			caster.DeSpawn();
			GenSpawn.Spawn(caster, cell, map);
			if (CasterIsPawn)
			{
				CasterPawn.Notify_Teleported();
				CasterPawn.teleporting = false;
				if (flag2)
				{
					CasterPawn.drafter.Drafted = true;
				}
			}
			return true;
		}
		return false;
	}
}
