using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace AncotLibrary;

public class CompAbilityShiftForward : CompAbilityEffect
{
	private List<IntVec3> tmpCells = new List<IntVec3>();

	public new CompProperties_AbilityShiftForward Props => (CompProperties_AbilityShiftForward)props;

	public Pawn Caster => parent.pawn;

	public override bool AICanTargetNow(LocalTargetInfo target)
	{
		return true;
	}

	public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
	{
		IntVec3 position = Caster.Position;
		ForceMovementUtility.ForceMoveToTarget(TargetPosition(target, Caster), Caster);
		if (Props.shiftFleck != null)
		{
			IntVec3 position2 = Caster.Position;
			Vector3 normalized = (position2.ToVector3() - position.ToVector3()).normalized;
			float magnitude = (position2.ToVector3() - position.ToVector3()).magnitude;
			float num = normalized.ToAngleFlat();
			AncotFleckMaker.CustomFleckThrow(Caster.MapHeld, Props.shiftFleck, position.ToVector3Shifted(), new Color(1f, 1f, 1f, 0.6f), magnitude * normalized / 2f, magnitude, 0f, 0f, 0f, num + 90f);
		}
	}

	public IntVec3 TargetPosition(LocalTargetInfo target, Pawn pawn)
	{
		float angle = (target.Cell - Caster.Position).AngleFlat % 360f;
		return ForceMovementUtility.GetDestinationAngle(Caster, Props.distance, angle, null, ignoreResistance: true);
	}

	public override void DrawEffectPreview(LocalTargetInfo target)
	{
		GenDraw.DrawFieldEdges(AffectCells(target));
	}

	public List<IntVec3> AffectCells(LocalTargetInfo target)
	{
		return GenSight.PointsOnLineOfSight(Caster.Position, TargetPosition(target, Caster)).ToList();
	}
}
