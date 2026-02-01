using UnityEngine;
using Verse;
using Verse.AI;

namespace VanillaPsycastsExpanded;

public class Hediff_IceBlock : Hediff_Overlay
{
	public override string OverlayPath => "Effects/Frostshaper/IceBlock/IceBlock";

	public override void PostAdd(DamageInfo? dinfo)
	{
		base.PostAdd(dinfo);
		IntVec3 facingCell = ((Hediff)(object)this).pawn.Rotation.FacingCell;
		int ticksToDisappear = ((Hediff)(object)this).TryGetComp<HediffComp_Disappears>().ticksToDisappear;
		Job job = JobMaker.MakeJob(VPE_DefOf.VPE_StandFreeze);
		job.expiryInterval = ticksToDisappear;
		job.overrideFacing = ((Hediff)(object)this).pawn.Rotation;
		((Hediff)(object)this).pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
		((Hediff)(object)this).pawn.pather.StopDead();
		((Hediff)(object)this).pawn.stances.SetStance(new Stance_Stand(ticksToDisappear, facingCell, null));
	}

	public override void Draw()
	{
		Vector3 drawPos = ((Hediff)(object)this).pawn.DrawPos;
		drawPos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
		Matrix4x4 matrix = default(Matrix4x4);
		float num = 1.5f;
		matrix.SetTRS(drawPos, Quaternion.identity, new Vector3(num, 1f, num));
		UnityEngine.Graphics.DrawMesh(MeshPool.plane10, matrix, base.OverlayMat, 0, null, 0, MatPropertyBlock);
	}

	public override void Tick()
	{
		((Hediff)(object)this).Tick();
		if (((Hediff)(object)this).pawn.IsHashIntervalTick(60) && ((Hediff)(object)this).pawn.CanReceiveHypothermia(out var hypothermiaHediff))
		{
			HealthUtility.AdjustSeverity(((Hediff)(object)this).pawn, hypothermiaHediff, 0.05f);
		}
	}
}
