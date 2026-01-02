using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace AncotLibrary;

public class CompAbilityScattershot : CompAbilityEffect
{
	public List<IntVec3> tmpCells = new List<IntVec3>();

	public LocalTargetInfo targetCached;

	public int fireTicks = 0;

	public int burstLeft = 0;

	public Effecter effecter;

	public bool shotStart = false;

	public new CompProperties_AbilityScattershot Props => (CompProperties_AbilityScattershot)props;

	public Pawn Caster => parent.pawn;

	public float AngularSpacing => Props.scatterAngle / (float)(Props.burstCount - 1);

	public float StartAngle => Props.scatterAngle / 2f;

	public override bool AICanTargetNow(LocalTargetInfo target)
	{
		return true;
	}

	public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
	{
		targetCached = target;
		fireTicks = 0;
		burstLeft = Props.burstCount;
		shotStart = true;
		float angleFlat = (target.Cell - Caster.Position).AngleFlat;
		if (Props.shotStartFleck != null)
		{
			Map mapHeld = Caster.MapHeld;
			FleckDef shotStartFleck = Props.shotStartFleck;
			Vector3 loc = Caster.Position.ToVector3Shifted();
			Color fleckColor = Props.fleckColor;
			float rotation = angleFlat + Props.fleckRotation;
			AncotFleckMaker.CustomFleckThrow(mapHeld, shotStartFleck, loc, fleckColor, default(Vector3), 1f, 0f, 0f, 0f, rotation);
		}
	}

	public override void PostApplied(List<LocalTargetInfo> targets, Map map)
	{
		Caster.jobs.StopAll();
		Job job = JobMaker.MakeJob(Props.jobDef, targetCached);
		job.count = 1;
		Caster.jobs.TryTakeOrderedJob(job, JobTag.Misc);
		if (Props.effecter != null)
		{
			effecter = Props.effecter.Spawn();
			effecter.Trigger(new TargetInfo(Caster.PositionHeld, map), new TargetInfo(targetCached.Cell, map));
			effecter.Cleanup();
		}
	}

	public override void CompTick()
	{
		if (shotStart)
		{
			if (!Caster.Spawned || Caster.Downed || Caster.Dead)
			{
				burstLeft = 0;
				effecter.ForceEnd();
			}
			if (burstLeft == 0)
			{
				shotStart = false;
			}
			fireTicks++;
			if (fireTicks % Props.ticksBetweenBurstShots == 0 && burstLeft > 0)
			{
				float num = (targetCached.Cell - Caster.Position).AngleFlat % 360f;
				float angleDegrees = num - StartAngle + AngularSpacing * (float)(Props.burstCount - burstLeft);
				IntVec3 intVec = CalculateRotatedPoint(Caster.Position, angleDegrees, Props.range);
				((Projectile)GenSpawn.Spawn(Props.projectileDef, Caster.Position, Caster.Map)).Launch(Caster, Caster.DrawPos, intVec, null, ProjectileHitFlags.IntendedTarget);
				burstLeft--;
			}
		}
	}

	public static IntVec3 CalculateRotatedPoint(IntVec3 origin, float angleDegrees, float distance)
	{
		return origin + (Vector3Utility.HorizontalVectorFromAngle(angleDegrees) * distance).ToIntVec3();
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_TargetInfo.Look(ref targetCached, "targetCached");
		Scribe_Values.Look(ref burstLeft, "burstLeft", 0);
		Scribe_Values.Look(ref effecter, "effecter");
		Scribe_Values.Look(ref shotStart, "shotStart", defaultValue: false);
	}
}
