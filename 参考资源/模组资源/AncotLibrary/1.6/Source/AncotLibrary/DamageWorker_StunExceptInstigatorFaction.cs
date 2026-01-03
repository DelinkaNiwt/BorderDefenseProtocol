using RimWorld;
using Verse;

namespace AncotLibrary;

public class DamageWorker_StunExceptInstigatorFaction : DamageWorker
{
	public override DamageResult Apply(DamageInfo dinfo, Thing victim)
	{
		DamageResult damageResult = base.Apply(dinfo, victim);
		int ticks = (int)(dinfo.Amount / 2f * 60f);
		if (victim.Faction != dinfo.Instigator.Faction)
		{
			if (victim is Pawn pawn)
			{
				pawn.stances.stunner.StunFor(ticks, dinfo.Instigator);
			}
			else if (victim is Building building)
			{
				building.GetComp<CompStunnable>()?.StunHandler.StunFor(ticks, dinfo.Instigator);
			}
		}
		damageResult.stunned = true;
		return damageResult;
	}
}
