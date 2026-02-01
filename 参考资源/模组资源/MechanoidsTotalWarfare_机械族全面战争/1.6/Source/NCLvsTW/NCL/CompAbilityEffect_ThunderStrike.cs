using RimWorld;
using Verse;
using Verse.Sound;

namespace NCL;

public class CompAbilityEffect_ThunderStrike : CompAbilityEffect
{
	public new CompProperties_ThunderStrike Props => (CompProperties_ThunderStrike)props;

	public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
	{
		base.Apply(target, dest);
		for (int i = 0; i < Props.lightningCount; i++)
		{
			DoSingleStrike(target.Cell, parent.pawn.Map);
		}
	}

	private void DoSingleStrike(IntVec3 center, Map map)
	{
		IntVec3 strikePos = center + GenRadial.RadialPattern[Rand.Range(8, GenRadial.NumCellsInRadius(10f))];
		GenExplosion.DoExplosion(strikePos, map, Props.radius * 0.5f, Props.damageType, parent.pawn, Props.damageAmount);
		SoundDefOf.Thunder_OnMap.PlayOneShot(new TargetInfo(strikePos, map));
		FleckMaker.ThrowMicroSparks(strikePos.ToVector3Shifted(), map);
		FleckMaker.ThrowLightningGlow(strikePos.ToVector3Shifted(), map, 10f);
		for (int i = 0; i < 3; i++)
		{
			IntVec3 offset = new IntVec3(Rand.Range(-3, 3), 0, Rand.Range(-3, 3));
			FleckMaker.ThrowSmoke((strikePos + offset).ToVector3Shifted(), map, Rand.Range(3f, 3f));
		}
		for (int j = 0; j < 8; j++)
		{
			IntVec3 offset2 = new IntVec3(Rand.Range(-5, 5), 0, Rand.Range(-5, 5));
			FleckMaker.ThrowMicroSparks((strikePos + offset2).ToVector3Shifted(), map);
		}
	}
}
