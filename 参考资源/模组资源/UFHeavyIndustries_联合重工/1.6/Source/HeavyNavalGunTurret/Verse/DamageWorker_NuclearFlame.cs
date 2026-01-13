using System.Collections.Generic;
using RimWorld;

namespace Verse;

public class DamageWorker_NuclearFlame : DamageWorker_AddInjury
{
	public override DamageResult Apply(DamageInfo dinfo, Thing victim)
	{
		Pawn pawn = victim as Pawn;
		if (pawn != null && pawn.Faction == Faction.OfPlayer)
		{
			Find.TickManager.slower.SignalForceNormalSpeedShort();
		}
		Map map = victim.Map;
		DamageResult damageResult = base.Apply(dinfo, victim);
		if (map == null)
		{
			return damageResult;
		}
		if (pawn != null && damageResult.hediffs != null)
		{
			bool flag = false;
			if (pawn.RaceProps.IsFlesh)
			{
				foreach (Hediff hediff in damageResult.hediffs)
				{
					HediffComp_GetsPermanent hediffComp_GetsPermanent = hediff.TryGetComp<HediffComp_GetsPermanent>();
					if (hediffComp_GetsPermanent != null && !hediffComp_GetsPermanent.IsPermanent)
					{
						hediffComp_GetsPermanent.IsPermanent = true;
						flag = true;
					}
				}
			}
			if (flag)
			{
				pawn.health.hediffSet.DirtyCache();
			}
		}
		if (!damageResult.deflected && !dinfo.InstantPermanentInjury && Rand.Chance(FireUtility.ChanceToAttachFireFromEvent(victim)))
		{
			victim.TryAttachFire(Rand.Range(0.1f, 0.1f), dinfo.Instigator);
		}
		if (victim.Destroyed && pawn == null)
		{
			foreach (IntVec3 item in victim.OccupiedRect())
			{
				FilthMaker.TryMakeFilth(item, map, ThingDefOf.Filth_Ash);
			}
		}
		return damageResult;
	}

	public override void ExplosionAffectCell(Explosion explosion, IntVec3 c, List<Thing> damagedThings, List<Thing> ignoredThings, bool canThrowMotes)
	{
		base.ExplosionAffectCell(explosion, c, damagedThings, ignoredThings, canThrowMotes);
		if (Rand.Chance(FireUtility.ChanceToStartFireIn(c, explosion.Map)))
		{
			FireUtility.TryStartFireIn(c, explosion.Map, Rand.Range(0.1f, 0.1f), explosion.instigator);
		}
	}

	public override void ExplosionStart(Explosion explosion, List<IntVec3> cellsToAffect)
	{
		base.ExplosionStart(explosion, cellsToAffect);
		if (explosion.Map != null)
		{
			EffecterDef named = DefDatabase<EffecterDef>.GetNamed("NuclearFlameWave");
			if (named != null)
			{
				Effecter effecter = named.Spawn();
				effecter.Trigger(new TargetInfo(explosion.Position, explosion.Map), TargetInfo.Invalid);
				effecter.Cleanup();
			}
		}
	}
}
