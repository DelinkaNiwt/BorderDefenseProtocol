using Verse;

namespace NCL;

public class CompExplodeWhenStunned : ThingComp
{
	private int lastExplosionTick = -9999;

	public CompProperties_ExplodeWhenStunned Props => (CompProperties_ExplodeWhenStunned)props;

	public override void CompTick()
	{
		base.CompTick();
		if (parent is Pawn pawn && pawn.stances.stunner.Stunned && (float)Find.TickManager.TicksGame >= (float)lastExplosionTick + Props.cooldownTicks)
		{
			lastExplosionTick = Find.TickManager.TicksGame;
			DoExplosion(pawn);
			ApplyVulnerability(pawn);
		}
	}

	private void DoExplosion(Pawn pawn)
	{
		IntVec3 position = pawn.Position;
		Map map = pawn.Map;
		float explosionRadius = Props.explosionRadius;
		DamageDef damageType = Props.damageType;
		int explosionDamage = Props.explosionDamage;
		float armorPenetration = Props.armorPenetration;
		bool applyDamageToExplosionCellsNeighbors = Props.applyDamageToExplosionCellsNeighbors;
		float chanceToStartFire = Props.chanceToStartFire;
		bool damageFalloff = Props.damageFalloff;
		GenExplosion.DoExplosion(position, map, explosionRadius, damageType, pawn, explosionDamage, armorPenetration, null, null, null, null, null, 0f, 0, null, null, 255, applyDamageToExplosionCellsNeighbors, null, 0f, 0, chanceToStartFire, damageFalloff);
	}

	private void ApplyVulnerability(Pawn pawn)
	{
		if (Props.vulnerabilityHediff != null)
		{
			Hediff existingHediff = pawn.health.hediffSet.GetFirstHediffOfDef(Props.vulnerabilityHediff);
			if (existingHediff != null)
			{
				pawn.health.RemoveHediff(existingHediff);
			}
			Hediff hediff = HediffMaker.MakeHediff(Props.vulnerabilityHediff, pawn);
			pawn.health.AddHediff(hediff);
		}
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref lastExplosionTick, "lastExplosionTick", -9999);
	}
}
