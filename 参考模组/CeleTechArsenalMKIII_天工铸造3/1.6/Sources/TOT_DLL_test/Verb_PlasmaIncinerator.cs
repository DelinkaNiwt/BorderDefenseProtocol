using LudeonTK;
using RimWorld;
using UnityEngine;
using Verse;

namespace TOT_DLL_test;

public class Verb_PlasmaIncinerator : Verb_ShootBeam
{
	[TweakValue("Incinerator", 0f, 10f)]
	public static float DistanceToLifetimeScalar = 5f;

	[TweakValue("Incinerator", -2f, 7f)]
	public static float BarrelOffset = 5f;

	private IncineratorSpray sprayer;

	private VerbProp_Flame Props => (VerbProp_Flame)verbProps;

	public override void WarmupComplete()
	{
		sprayer = GenSpawn.Spawn(ThingDefOf.IncineratorSpray, caster.Position, caster.Map) as IncineratorSpray;
		base.WarmupComplete();
		BattleLog battleLog = Find.BattleLog;
		Thing initiator = caster;
		Thing target = (currentTarget.HasThing ? currentTarget.Thing : null);
		battleLog.Add(new BattleLogEntry_RangedFire(initiator, target, base.EquipmentSource?.def, null, burst: false));
	}

	protected override bool TryCastShot()
	{
		bool result = base.TryCastShot();
		Vector3 vector = base.InterpolatedPosition.Yto0();
		IntVec3 intVec = vector.ToIntVec3();
		Vector3 drawPos = caster.DrawPos;
		Vector3 normalized = (vector - drawPos).normalized;
		drawPos += normalized * BarrelOffset;
		IntVec3 position = caster.Position;
		MoteDualAttached moteDualAttached = MoteMaker.MakeInteractionOverlay(Props.MotedDef, new TargetInfo(position, caster.Map), new TargetInfo(intVec, caster.Map));
		float num = Vector3.Distance(vector, drawPos);
		float num2 = ((num < BarrelOffset) ? 0.5f : 1f);
		IncineratorSpray incineratorSpray = sprayer;
		if (incineratorSpray == null)
		{
			return result;
		}
		incineratorSpray.Add(new IncineratorProjectileMotion
		{
			mote = moteDualAttached,
			targetDest = intVec,
			worldSource = drawPos,
			worldTarget = vector,
			moveVector = (vector - drawPos).normalized,
			startScale = 1f * num2,
			endScale = (1f + Rand.Range(0.15f, 0.18f)) * num2,
			lifespanTicks = Mathf.FloorToInt(num * DistanceToLifetimeScalar)
		});
		return result;
	}
}
