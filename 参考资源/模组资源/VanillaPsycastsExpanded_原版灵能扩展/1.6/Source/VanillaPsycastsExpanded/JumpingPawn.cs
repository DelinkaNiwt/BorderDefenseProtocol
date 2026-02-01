using RimWorld;
using UnityEngine;
using VEF.Abilities;
using Verse;
using Verse.Sound;

namespace VanillaPsycastsExpanded;

public class JumpingPawn : AbilityPawnFlyer
{
	public override void DynamicDrawPhaseAt(DrawPhase phase, Vector3 drawLoc, bool flip = false)
	{
		((PawnFlyer)this).FlyingPawn.Drawer.renderer.DynamicDrawPhaseAt(phase, drawLoc, ((Thing)this).Rotation, neverAimWeapon: true);
	}

	protected override void Tick()
	{
		((Entity)this).Tick();
		if (((Thing)this).Map != null && Find.TickManager.TicksGame % 3 == 0)
		{
			Map map = ((Thing)this).Map;
			FleckCreationData dataStatic = FleckMaker.GetDataStatic(GetDrawPos(), map, VPE_DefOf.VPE_WarlordZap);
			dataStatic.rotation = Rand.Range(0f, 360f);
			map.flecks.CreateFleck(dataStatic);
		}
	}

	private Vector3 GetDrawPos()
	{
		float num = (float)((PawnFlyer)this).ticksFlying / (float)((PawnFlyer)this).ticksFlightTime;
		Vector3 drawPos = ((Thing)(object)this).DrawPos;
		drawPos.y = AltitudeLayer.Skyfaller.AltitudeFor();
		return drawPos + Vector3.forward * (num - Mathf.Pow(num, 2f)) * 15f;
	}

	protected override void RespawnPawn()
	{
		Pawn flyingPawn = ((PawnFlyer)this).FlyingPawn;
		((AbilityPawnFlyer)this).RespawnPawn();
		VPE_DefOf.VPE_PowerLeap_Land.PlayOneShot(flyingPawn);
		FleckMaker.ThrowSmoke(flyingPawn.DrawPos, flyingPawn.Map, 1f);
		FleckMaker.ThrowDustPuffThick(flyingPawn.DrawPos, flyingPawn.Map, 2f, new Color(1f, 1f, 1f, 2.5f));
	}
}
