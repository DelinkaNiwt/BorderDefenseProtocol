using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace NCL;

public class HediffComp_EffectAura : HediffComp
{
	private int ticksUntilEffect;

	public HediffCompProperties_EffectAura Props => (HediffCompProperties_EffectAura)props;

	public override void CompPostTick(ref float severityAdjustment)
	{
		base.CompPostTick(ref severityAdjustment);
		if (base.Pawn.Map != null && base.Pawn.Spawned && --ticksUntilEffect <= 0)
		{
			GenerateEffectRing();
			ticksUntilEffect = Props.EffectInterval;
		}
	}

	private void GenerateEffectRing()
	{
		Map map = base.Pawn.Map;
		Vector3 center = base.Pawn.DrawPos;
		for (int i = 0; i < Props.EffectsPerBurst; i++)
		{
			float angle = Rand.Range(0f, 360f);
			float distance = Rand.Range(Props.minDistance, Props.maxDistance);
			Vector3 offset = Quaternion.AngleAxis(angle, Vector3.up) * Vector3.forward * distance;
			IntVec3 cell = (center + offset).ToIntVec3();
			if (cell.InBounds(map) && !cell.Fogged(map))
			{
				if (Props.soundDef != null)
				{
					Props.soundDef.PlayOneShot(new TargetInfo(cell, map));
				}
				FleckCreationData data = FleckMaker.GetDataStatic(cell.ToVector3Shifted() + new Vector3(0f, 0f, 1f), map, Props.EffectFleckDef, Rand.Range(1.5f, 2f));
				data.rotationRate = Rand.Range(-3f, 3f);
				map.flecks.CreateFleck(data);
			}
		}
	}
}
