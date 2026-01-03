using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompAbilityShiftBackward : CompAbilityEffect
{
	private List<IntVec3> tmpCells = new List<IntVec3>();

	public new CompProperties_AbilityShiftBackward Props => (CompProperties_AbilityShiftBackward)props;

	public Pawn Caster => parent.pawn;

	public override bool AICanTargetNow(LocalTargetInfo target)
	{
		return true;
	}

	public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
	{
		ForceMovementUtility.ForceMoveToTarget(TargetPosition(target, Caster), Caster);
	}

	public IntVec3 TargetPosition(LocalTargetInfo target, Pawn pawn)
	{
		float num = (target.Cell - Caster.Position).AngleFlat % 360f;
		return ForceMovementUtility.GetDestinationAngle(Caster, Props.distance, num + 180f, null, ignoreResistance: true);
	}

	public override void DrawEffectPreview(LocalTargetInfo target)
	{
		List<IntVec3> cells = GenSight.PointsOnLineOfSight(Caster.Position, TargetPosition(target, Caster)).ToList();
		GenDraw.DrawFieldEdges(cells);
	}
}
