using RimWorld;
using UnityEngine;
using Verse;

namespace TurbojetBackpack;

public class Verb_TurbojetDash : Verb_CastAbilityJump
{
	public override ThingDef JumpFlyerDef => TurboJumpUtility.DashFlyerDef;

	public override bool TryStartCastOn(LocalTargetInfo castTarg, LocalTargetInfo destTarg, bool surpriseAttack = false, bool canHitNonTargetPawns = true, bool preventFriendlyFire = false, bool nonInterruptible = false)
	{
		bool flag = base.TryStartCastOn(castTarg, destTarg, surpriseAttack, canHitNonTargetPawns, preventFriendlyFire, nonInterruptible);
		if (flag && CasterPawn != null && CasterPawn.Spawned)
		{
			TurboJumpUtility.DoWarmupBurst(CasterPawn, ability);
		}
		return flag;
	}

	public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
	{
		if ((float)root.DistanceToSquared(targ.Cell) > EffectiveRange * EffectiveRange)
		{
			return false;
		}
		if (!GenSight.LineOfSight(root, targ.Cell, caster.Map))
		{
			return false;
		}
		return TurboJumpUtility.IsValidTargetBase(caster.Map, targ.Cell);
	}

	public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
	{
		if (caster == null)
		{
			return false;
		}
		if (!GenSight.LineOfSight(caster.Position, target.Cell, caster.Map))
		{
			if (showMessages)
			{
				Messages.Message("CannotFlyThroughWall".Translate(), MessageTypeDefOf.RejectInput, historical: false);
			}
			return false;
		}
		if (!CanHitTarget(target))
		{
			return false;
		}
		if (!TurboJumpUtility.IsValidTargetBase(caster.Map, target.Cell))
		{
			if (showMessages)
			{
				Messages.Message("MessageJumpTargetInvalid".Translate(), MessageTypeDefOf.RejectInput, historical: false);
			}
			return false;
		}
		return true;
	}

	protected override bool TryCastShot()
	{
		return TurboJumpUtility.SpawnFlyer(CasterPawn, currentTarget.Cell, verbProps, ability, isDash: true);
	}

	public override void DrawHighlight(LocalTargetInfo target)
	{
		if (caster?.Map != null)
		{
			if (target.IsValid && ValidateTarget(target, showMessages: false))
			{
				GenDraw.DrawTargetHighlightWithLayer(target.CenterVector3, AltitudeLayer.MetaOverlays);
			}
			GenDraw.DrawRadiusRing(caster.Position, EffectiveRange, Color.cyan, (IntVec3 c) => TurboJumpUtility.IsValidTargetBase(caster.Map, c) && GenSight.LineOfSight(caster.Position, c, caster.Map));
		}
	}
}
