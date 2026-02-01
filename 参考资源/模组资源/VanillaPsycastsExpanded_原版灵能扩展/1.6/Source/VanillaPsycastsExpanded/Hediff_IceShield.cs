using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace VanillaPsycastsExpanded;

public class Hediff_IceShield : Hediff_Overlay
{
	public override float OverlaySize => 1.5f;

	public override string OverlayPath => "Effects/Frostshaper/FrostShield/Frostshield";

	public override void PostAdd(DamageInfo? dinfo)
	{
		base.PostAdd(dinfo);
		foreach (Hediff item in ((Hediff)(object)this).pawn.health.hediffSet.hediffs.Where((Hediff x) => x.def == HediffDefOf.Hypothermia || x.def == VPE_DefOf.VFEP_HypothermicSlowdown || x.def == VPE_DefOf.HypothermicSlowdown).ToList())
		{
			((Hediff)(object)this).pawn.health.RemoveHediff(item);
		}
	}

	public override void Notify_PawnPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
	{
		((HediffWithComps)(object)this).Notify_PawnPostApplyDamage(dinfo, totalDamageDealt);
		if (dinfo.Instigator is Pawn pawn && Vector3.Distance(pawn.DrawPos, ((Hediff)(object)this).pawn.DrawPos) <= OverlaySize && pawn.CanReceiveHypothermia(out var hypothermiaHediff))
		{
			HealthUtility.AdjustSeverity(pawn, hypothermiaHediff, 0.05f);
		}
	}

	public override void Draw()
	{
		Vector3 drawPos = ((Hediff)(object)this).pawn.DrawPos;
		drawPos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
		Matrix4x4 matrix = default(Matrix4x4);
		matrix.SetTRS(drawPos, Quaternion.identity, new Vector3(OverlaySize, 1f, OverlaySize));
		UnityEngine.Graphics.DrawMesh(MeshPool.plane10, matrix, base.OverlayMat, 0, null, 0, MatPropertyBlock);
	}
}
