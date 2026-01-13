using RimWorld;
using UnityEngine;
using Verse;

namespace TurbojetBackpack;

public class Verb_TurbojetJump : Verb_CastAbilityJump
{
	public override ThingDef JumpFlyerDef => TurboJumpUtility.JumpFlyerDef;

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
		float effectiveRange = EffectiveRange;
		if ((float)root.DistanceToSquared(targ.Cell) > effectiveRange * effectiveRange)
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
		if (!CanHitTarget(target))
		{
			return false;
		}
		Map map = caster.Map;
		if (map.roofGrid.Roofed(caster.Position))
		{
			if (showMessages)
			{
				Messages.Message("CannotJumpUnderRoof".Translate(), MessageTypeDefOf.RejectInput, historical: false);
			}
			return false;
		}
		if (map.roofGrid.Roofed(target.Cell))
		{
			if (showMessages)
			{
				Messages.Message("CannotJumpToRoof".Translate(), MessageTypeDefOf.RejectInput, historical: false);
			}
			return false;
		}
		if (!TurboJumpUtility.IsValidTargetBase(map, target.Cell))
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
		return TurboJumpUtility.SpawnFlyer(CasterPawn, currentTarget.Cell, verbProps, ability, isDash: false);
	}

	public override void DrawHighlight(LocalTargetInfo target)
	{
		if (caster?.Map != null && !caster.Map.roofGrid.Roofed(caster.Position))
		{
			if (target.IsValid && ValidateTarget(target, showMessages: false))
			{
				GenDraw.DrawTargetHighlightWithLayer(target.CenterVector3, AltitudeLayer.MetaOverlays);
			}
			GenDraw.DrawRadiusRing(caster.Position, EffectiveRange, Color.white, (IntVec3 c) => TurboJumpUtility.IsValidTargetBase(caster.Map, c) && !caster.Map.roofGrid.Roofed(c));
		}
	}

	public override void OrderForceTarget(LocalTargetInfo target)
	{
		TurboJumpUtility.OrderJump(CasterPawn, target, this, EffectiveRange);
	}
}
