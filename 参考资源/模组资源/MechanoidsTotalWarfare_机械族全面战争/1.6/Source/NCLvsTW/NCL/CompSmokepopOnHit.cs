using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace NCL;

public class CompSmokepopOnHit : ThingComp
{
	private int lastTriggerTick = -1;

	private bool enabled = true;

	private CompProperties_SmokepopOnHit Props => (CompProperties_SmokepopOnHit)props;

	private bool OnCooldown
	{
		get
		{
			if (lastTriggerTick < 0)
			{
				return false;
			}
			int cooldownTicks = (int)(Props.cooldownSeconds * 60f);
			return Find.TickManager.TicksGame < lastTriggerTick + cooldownTicks;
		}
	}

	public override IEnumerable<Gizmo> CompGetGizmosExtra()
	{
		foreach (Gizmo item in base.CompGetGizmosExtra())
		{
			yield return item;
		}
		if (parent is Pawn pawn && pawn.Faction == Faction.OfPlayer && !pawn.Dead)
		{
			yield return new Command_Toggle
			{
				defaultLabel = "NCL.SmokePopToggle".Translate(),
				defaultDesc = "NCL.SmokePopToggleDesc".Translate(),
				icon = ContentFinder<Texture2D>.Get("ModIcon/SmokePopToggle"),
				isActive = () => enabled,
				toggleAction = delegate
				{
					enabled = !enabled;
				}
			};
		}
	}

	public override void PostPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
	{
		base.PostPostApplyDamage(dinfo, totalDamageDealt);
		if (enabled && !(dinfo.Amount <= 0f) && !OnCooldown && !parent.Destroyed && parent.Map != null)
		{
			TriggerSmokepopEffect();
		}
	}

	private void TriggerSmokepopEffect()
	{
		lastTriggerTick = Find.TickManager.TicksGame;
		if (Props.soundOnActivate != null)
		{
			Props.soundOnActivate.PlayOneShot(new TargetInfo(parent.Position, parent.Map));
		}
		GenExplosion.DoExplosion(parent.Position, parent.Map, Props.smokeRadius, DamageDefOf.Smoke, null, -1, -1f, null, null, null, null, null, 0f, 1, GasType.BlindSmoke);
	}

	public override string CompInspectStringExtra()
	{
		if (!enabled)
		{
			return "NCL.SmokePopDisabled".Translate();
		}
		int ticksRemaining = lastTriggerTick + (int)(Props.cooldownSeconds * 60f) - Find.TickManager.TicksGame;
		if (ticksRemaining > 0)
		{
			return "NCL.SmokePopCoolingDown".Translate((float)ticksRemaining / 60f);
		}
		return "NCL.SmokePopReady".Translate();
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref lastTriggerTick, "lastTriggerTick", -1);
		Scribe_Values.Look(ref enabled, "smokePopEnabled", defaultValue: true);
	}
}
