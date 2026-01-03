using RimWorld;
using UnityEngine;
using Verse;

namespace AncotLibrary;

public class PawnRenderNodeWorker_CombatPlatform : PawnRenderNodeWorker
{
	public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
	{
		if (!base.CanDrawNow(node, parms))
		{
			return false;
		}
		PawnRenderNode_CombatPlatform pawnRenderNode_CombatPlatform = node as PawnRenderNode_CombatPlatform;
		PawnRenderNodeProperties_CombatPlatform pawnRenderNodeProperties_CombatPlatform = node.Props as PawnRenderNodeProperties_CombatPlatform;
		if (pawnRenderNode_CombatPlatform?.compCombatPlatform != null)
		{
			Pawn pawnOwner = pawnRenderNode_CombatPlatform.compCombatPlatform.PawnOwner;
			Faction faction = pawnOwner.Faction;
			if (faction != null && !faction.IsPlayer)
			{
				return true;
			}
			if (!pawnRenderNodeProperties_CombatPlatform.drawUndrafted && !pawnOwner.Drafted)
			{
				return false;
			}
		}
		return true;
	}

	public override Quaternion RotationFor(PawnRenderNode node, PawnDrawParms parms)
	{
		return base.RotationFor(node, parms);
	}

	public override Vector3 OffsetFor(PawnRenderNode node, PawnDrawParms parms, out Vector3 pivot)
	{
		if (node is PawnRenderNode_CombatPlatform { compCombatPlatform: not null, compCombatPlatform: var compCombatPlatform })
		{
			if (compCombatPlatform.Props.float_xAxis != null)
			{
				compCombatPlatform.floatOffset_xAxis = Mathf.Sin(((float)Find.TickManager.TicksGame + compCombatPlatform.randTime) * compCombatPlatform.Props.float_xAxis.floatSpeed) * compCombatPlatform.Props.float_xAxis.floatAmplitude;
			}
			if (compCombatPlatform.Props.float_yAxis != null)
			{
				compCombatPlatform.floatOffset_yAxis = Mathf.Sin(((float)Find.TickManager.TicksGame + compCombatPlatform.randTime) * compCombatPlatform.Props.float_yAxis.floatSpeed) * compCombatPlatform.Props.float_yAxis.floatAmplitude;
			}
			Vector3 vector = new Vector3(compCombatPlatform.floatOffset_xAxis, 0f, compCombatPlatform.floatOffset_yAxis);
			return base.OffsetFor(node, parms, out pivot) + vector;
		}
		return base.OffsetFor(node, parms, out pivot);
	}
}
