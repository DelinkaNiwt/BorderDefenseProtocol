using System;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace NCL;

public class HediffComp_SmokeAura : HediffComp
{
	private int ticksUntilSmoke;

	public HediffCompProperties_SmokeAura Props => (HediffCompProperties_SmokeAura)props;

	public override void CompPostTick(ref float severityAdjustment)
	{
		base.CompPostTick(ref severityAdjustment);
		if (base.Pawn.Map != null && base.Pawn.Spawned && --ticksUntilSmoke <= 0)
		{
			GenerateSmokeEffect();
			ticksUntilSmoke = Props.smokeInterval;
		}
	}

	private void GenerateSmokeEffect()
	{
		if (Props.soundDef != null)
		{
			Props.soundDef.PlayOneShot(new TargetInfo(base.Pawn.Position, base.Pawn.Map));
		}
		FleckMaker.ThrowSmoke(base.Pawn.DrawPos, base.Pawn.Map, Props.smokeSize);
		for (int i = 0; i < Props.smokesPerBurst; i++)
		{
			float angle = Rand.Range(180f, 360f);
			float distance = Rand.Range(Props.minDistance, Props.maxDistance);
			Vector3 offset = new Vector3(Mathf.Cos(angle * ((float)Math.PI / 180f)) * distance, 0f, Mathf.Sin(angle * ((float)Math.PI / 180f)) * distance);
			IntVec3 cell = (base.Pawn.Position.ToVector3Shifted() + offset).ToIntVec3();
			if (cell.InBounds(base.Pawn.Map) && cell.Walkable(base.Pawn.Map))
			{
				FleckMaker.ThrowSmoke(cell.ToVector3Shifted() + new Vector3(0f, 0f, 0.5f), base.Pawn.Map, Props.smokeSize * 5f);
			}
		}
	}
}
