using AncotLibrary;
using RimWorld;
using RimWorld.Utility;
using UnityEngine;
using Verse;

namespace Milira;

public class Verb_CastAbilityMiliraFly_Hammer : Verb_CastAbility
{
	private float cachedEffectiveRange = -1f;

	public override bool MultiSelect => true;

	public CompWeaponCharge compCharge => ((Thing)CasterPawn.equipment.Primary).TryGetComp<CompWeaponCharge>();

	public virtual ThingDef JumpFlyerDef => MiliraDefOf.Milira_PawnJumper_Hammer;

	public override float EffectiveRange
	{
		get
		{
			if (cachedEffectiveRange < 0f)
			{
				if (base.EquipmentSource != null)
				{
					cachedEffectiveRange = base.EquipmentSource.GetStatValue(StatDefOf.JumpRange);
				}
				else
				{
					cachedEffectiveRange = verbProps.range;
				}
			}
			return cachedEffectiveRange;
		}
	}

	protected override bool TryCastShot()
	{
		if (base.TryCastShot())
		{
			return MiliraFlyUtility_Hammer.DoJump(CasterPawn, currentTarget, compCharge, verbProps, ability, base.CurrentTarget, JumpFlyerDef);
		}
		return false;
	}

	public override void OnGUI(LocalTargetInfo target)
	{
		if (CanHitTarget(target) && MiliraFlyUtility_Hammer.ValidJumpTarget(caster.Map, target.Cell))
		{
			base.OnGUI(target);
		}
		else
		{
			GenUI.DrawMouseAttachment(TexCommand.CannotShoot);
		}
	}

	public override void OrderForceTarget(LocalTargetInfo target)
	{
		MiliraFlyUtility_Hammer.OrderJump(CasterPawn, target, this, EffectiveRange);
	}

	public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
	{
		if (caster == null)
		{
			return false;
		}
		if (!CanHitTarget(target) || !MiliraFlyUtility_Hammer.ValidJumpTarget(caster.Map, target.Cell))
		{
			return false;
		}
		if (!ReloadableUtility.CanUseConsideringQueuedJobs(CasterPawn, base.EquipmentSource))
		{
			return false;
		}
		if (compCharge == null || !compCharge.CanBeUsed)
		{
			return false;
		}
		return true;
	}

	public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
	{
		return MiliraFlyUtility_Hammer.CanHitTargetFrom(CasterPawn, root, targ, EffectiveRange);
	}

	public override void DrawHighlight(LocalTargetInfo target)
	{
		if (target.IsValid && MiliraFlyUtility_Hammer.ValidJumpTarget(caster.Map, target.Cell))
		{
			GenDraw.DrawTargetHighlightWithLayer(target.CenterVector3, AltitudeLayer.MetaOverlays);
		}
		GenDraw.DrawRadiusRing(caster.Position, EffectiveRange, Color.white, (IntVec3 c) => GenSight.LineOfSight(caster.Position, c, caster.Map) && MiliraFlyUtility_Hammer.ValidJumpTarget(caster.Map, c));
		GenDraw.DrawRadiusRing(target.Cell, 4f);
	}
}
