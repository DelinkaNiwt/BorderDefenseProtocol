using System.Collections.Generic;
using Verse;

namespace NCLWorm;

public class NCL_Pawn_Worm : Pawn
{
	public List<DamageDefWithTick> damageAbsLi = new List<DamageDefWithTick>();

	public long lastApplyDamagetick = Find.TickManager.gameStartAbsTick;

	public bool Sleep = false;

	public float LADHP = 1f;

	public float LADSH = 1f;

	protected override void Tick()
	{
		base.Tick();
		for (int num = damageAbsLi.Count - 1; num >= 0; num--)
		{
			damageAbsLi[num].tick--;
			if (damageAbsLi[num].tick <= 0)
			{
				damageAbsLi.Remove(damageAbsLi[num]);
			}
		}
		for (int num2 = health.hediffSet.hediffs.Count - 1; num2 >= 0; num2--)
		{
			if (health.hediffSet.hediffs[num2].def.isBad && !(health.hediffSet.hediffs[num2] is Hediff_Injury))
			{
				health.RemoveHediff(health.hediffSet.hediffs[num2]);
			}
		}
	}

	public override void PreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
	{
		base.PreApplyDamage(ref dinfo, out absorbed);
		if (dinfo.Instigator == this || Sleep)
		{
			absorbed = true;
			return;
		}
		foreach (DamageDefWithTick item in damageAbsLi)
		{
			if (item.damageDef == dinfo.Def)
			{
				absorbed = true;
				return;
			}
		}
		damageAbsLi.Add(new DamageDefWithTick(dinfo.Def, 60));
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Collections.Look(ref damageAbsLi, "damageAbsLi", LookMode.Deep);
	}
}
