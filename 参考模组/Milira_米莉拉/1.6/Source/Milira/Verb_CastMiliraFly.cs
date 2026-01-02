using RimWorld;
using RimWorld.Utility;
using UnityEngine;
using Verse;

namespace Milira;

public class Verb_CastMiliraFly : Verb
{
	private float cachedEffectiveRange = -1f;

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

	public virtual ThingDef JumpFlyerDef => MiliraDefOf.Milira_PawnJumper;

	public override bool MultiSelect => true;

	protected override bool TryCastShot()
	{
		return MiliraFlyUtility.DoJump(CasterPawn, currentTarget, base.ReloadableCompSource, verbProps, null, base.CurrentTarget, JumpFlyerDef);
	}

	public override void OrderForceTarget(LocalTargetInfo target)
	{
		MiliraFlyUtility.OrderJump(CasterPawn, target, this, EffectiveRange);
	}

	public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
	{
		if (caster == null)
		{
			return false;
		}
		if (!CanHitTarget(target) || !MiliraFlyUtility.ValidJumpTarget(caster.Map, target.Cell))
		{
			return false;
		}
		if (!ReloadableUtility.CanUseConsideringQueuedJobs(CasterPawn, base.EquipmentSource))
		{
			return false;
		}
		return true;
	}

	public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
	{
		return MiliraFlyUtility.CanHitTargetFrom(CasterPawn, root, targ, EffectiveRange);
	}

	public override void OnGUI(LocalTargetInfo target)
	{
		if (CanHitTarget(target) && MiliraFlyUtility.ValidJumpTarget(caster.Map, target.Cell))
		{
			base.OnGUI(target);
		}
		else
		{
			GenUI.DrawMouseAttachment(TexCommand.CannotShoot);
		}
	}

	public override void DrawHighlight(LocalTargetInfo target)
	{
		if (caster == null || caster.Spawned)
		{
			if (target.IsValid && MiliraFlyUtility.ValidJumpTarget(caster.Map, target.Cell))
			{
				GenDraw.DrawTargetHighlightWithLayer(target.CenterVector3, AltitudeLayer.MetaOverlays);
			}
			GenDraw.DrawRadiusRing(caster.Position, EffectiveRange, Color.white, (IntVec3 c) => GenSight.LineOfSight(caster.Position, c, caster.Map) && MiliraFlyUtility.ValidJumpTarget(caster.Map, c));
		}
	}
}
