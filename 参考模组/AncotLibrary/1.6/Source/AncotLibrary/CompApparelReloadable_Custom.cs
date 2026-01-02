using System.Collections.Generic;
using RimWorld;
using RimWorld.Utility;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace AncotLibrary;

public class CompApparelReloadable_Custom : CompApparelVerbOwner_Charged, IReloadableComp, ICompWithCharges
{
	public int targetCharges;

	private int replenishInTicks = -1;

	[Unsaved(false)]
	protected Gizmo_ApparelReloadable_Custom gizmo;

	public new CompProperties_ApparelReloadable_Custom Props => props as CompProperties_ApparelReloadable_Custom;

	public int gizmoOrder => Props.gizmoOrder;

	public Thing ReloadableThing => parent;

	public Pawn PawnOwner
	{
		get
		{
			if (!(parent is Apparel { Wearer: var wearer }))
			{
				return null;
			}
			return wearer;
		}
	}

	public ThingDef AmmoDef => Props.ammoDef;

	public int BaseReloadTicks => Props.baseReloadTicks;

	public float PercentageFull => (float)base.RemainingCharges / (float)base.MaxCharges;

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref replenishInTicks, "replenishInTicks", -1);
		Scribe_Values.Look(ref targetCharges, "targetCharges", 0);
	}

	public override void PostPostMake()
	{
		remainingCharges = Props.initialCharges ?? base.MaxCharges;
		if (!Props.maxReloadConfigurable)
		{
			targetCharges = base.MaxCharges;
		}
	}

	public override void CompTickInterval(int delta)
	{
		base.CompTickInterval(delta);
		if (Props.replenishAfterCooldown && base.RemainingCharges == 0)
		{
			if (replenishInTicks > 0)
			{
				replenishInTicks -= delta;
			}
			else
			{
				remainingCharges = base.MaxCharges;
			}
		}
	}

	public string DisabledReason(int minNeeded, int maxNeeded)
	{
		if (Props.replenishAfterCooldown)
		{
			return "CommandReload_Cooldown".Translate(Props.CooldownVerbArgument, replenishInTicks.ToStringTicksToPeriod().Named("TIME"));
		}
		if (AmmoDef == null)
		{
			return "CommandReload_NoCharges".Translate(Props.ChargeNounArgument);
		}
		return "CommandReload_NoAmmo".Translate(arg3: ((Props.ammoCountToRefill != 0) ? Props.ammoCountToRefill.ToString() : ((minNeeded == maxNeeded) ? minNeeded.ToString() : $"{minNeeded}-{maxNeeded}")).Named("COUNT"), arg1: Props.ChargeNounArgument, arg2: NamedArgumentUtility.Named(AmmoDef, "AMMO"));
	}

	public bool NeedsReload(bool allowForcedReload)
	{
		if (AmmoDef == null)
		{
			return false;
		}
		if (Props.ammoCountToRefill != 0)
		{
			if (!allowForcedReload)
			{
				return remainingCharges == 0;
			}
			return base.RemainingCharges < targetCharges;
		}
		return base.RemainingCharges < targetCharges;
	}

	public void ReloadFrom(Thing ammo)
	{
		if (!NeedsReload(allowForcedReload: true))
		{
			return;
		}
		if (Props.ammoCountToRefill != 0)
		{
			if (ammo.stackCount < Props.ammoCountToRefill)
			{
				return;
			}
			ammo.SplitOff(Props.ammoCountToRefill).Destroy();
			remainingCharges = base.MaxCharges;
		}
		else
		{
			if (ammo.stackCount < Props.ammoCountPerCharge)
			{
				return;
			}
			int num = Mathf.Clamp(ammo.stackCount / Props.ammoCountPerCharge, 0, base.MaxCharges - base.RemainingCharges);
			ammo.SplitOff(num * Props.ammoCountPerCharge).Destroy();
			remainingCharges += num;
		}
		if (Props.soundReload != null)
		{
			Props.soundReload.PlayOneShot(new TargetInfo(base.Wearer.Position, base.Wearer.Map));
		}
	}

	public int MinAmmoNeeded(bool allowForcedReload)
	{
		if (!NeedsReload(allowForcedReload))
		{
			return 0;
		}
		if (Props.ammoCountToRefill != 0)
		{
			return Props.ammoCountToRefill;
		}
		return Props.ammoCountPerCharge;
	}

	public int MaxAmmoNeeded(bool allowForcedReload)
	{
		if (!NeedsReload(allowForcedReload))
		{
			return 0;
		}
		if (Props.ammoCountToRefill != 0)
		{
			return Props.ammoCountToRefill;
		}
		return Props.ammoCountPerCharge * (targetCharges - base.RemainingCharges);
	}

	public int MaxAmmoAmount()
	{
		if (AmmoDef == null)
		{
			return 0;
		}
		if (Props.ammoCountToRefill == 0)
		{
			return Props.ammoCountPerCharge * base.MaxCharges;
		}
		return Props.ammoCountToRefill;
	}

	public void UsedOnce(int num)
	{
		for (int i = 0; i < num; i++)
		{
			UsedOnce();
		}
	}

	public override void UsedOnce()
	{
		base.UsedOnce();
		if (Props.replenishAfterCooldown && remainingCharges == 0)
		{
			replenishInTicks = Props.baseReloadTicks;
		}
	}

	public override bool CanBeUsed(out string reason)
	{
		if (remainingCharges <= 0)
		{
			reason = DisabledReason(MinAmmoNeeded(allowForcedReload: false), MaxAmmoNeeded(allowForcedReload: false));
			return false;
		}
		if (!base.CanBeUsed(out reason))
		{
			return false;
		}
		return true;
	}

	public override IEnumerable<Gizmo> CompGetWornGizmosExtra()
	{
		bool drafted = base.Wearer.Drafted;
		if ((drafted && !Props.displayGizmoWhileDrafted) || (!drafted && !Props.displayGizmoWhileUndrafted))
		{
			yield break;
		}
		ThingWithComps gear = parent;
		foreach (Verb allVerb in base.VerbTracker.AllVerbs)
		{
			if (allVerb.verbProps.hasStandardCommand)
			{
				yield return CreateVerbTargetCommand(gear, allVerb);
			}
		}
		if (base.Wearer.Faction.IsPlayer && Find.Selector.SelectedObjects.Count == 1 && Props.showResourceBar)
		{
			if (gizmo == null)
			{
				gizmo = new Gizmo_ApparelReloadable_Custom(this);
			}
			yield return gizmo;
		}
		if (DebugSettings.ShowDevGizmos)
		{
			yield return new Command_Action
			{
				defaultLabel = "DEV: Reload to full",
				action = delegate
				{
					remainingCharges = base.MaxCharges;
				}
			};
		}
	}

	private Command_VerbTarget CreateVerbTargetCommand(Thing gear, Verb verb)
	{
		Command_VerbOwner command_VerbOwner = new Command_VerbOwner(this);
		command_VerbOwner.defaultDesc = Props.verbGizmoDesc ?? gear.def.description;
		command_VerbOwner.hotKey = Props.hotKey;
		command_VerbOwner.defaultLabel = verb.verbProps.label;
		command_VerbOwner.verb = verb;
		if (verb.verbProps.defaultProjectile != null && verb.verbProps.commandIcon == null)
		{
			command_VerbOwner.icon = verb.verbProps.defaultProjectile.uiIcon;
			command_VerbOwner.iconAngle = verb.verbProps.defaultProjectile.uiIconAngle;
			command_VerbOwner.iconOffset = verb.verbProps.defaultProjectile.uiIconOffset;
			command_VerbOwner.overrideColor = verb.verbProps.defaultProjectile.graphicData.color;
		}
		else
		{
			command_VerbOwner.icon = ((verb.UIIcon != BaseContent.BadTex) ? verb.UIIcon : gear.def.uiIcon);
			command_VerbOwner.iconAngle = gear.def.uiIconAngle;
			command_VerbOwner.iconOffset = gear.def.uiIconOffset;
			command_VerbOwner.defaultIconColor = gear.DrawColor;
			command_VerbOwner.overrideColor = Props.verbGizmoOverrideColor;
		}
		string reason;
		if (!base.Wearer.IsColonistPlayerControlled)
		{
			command_VerbOwner.Disable("CannotOrderNonControlled".Translate());
		}
		else if (verb.verbProps.violent && base.Wearer.WorkTagIsDisabled(WorkTags.Violent))
		{
			command_VerbOwner.Disable("IsIncapableOfViolenceLower".Translate(base.Wearer.LabelShort, base.Wearer).CapitalizeFirst() + ".");
		}
		else if (!CanBeUsed(out reason))
		{
			command_VerbOwner.Disable(reason);
		}
		return command_VerbOwner;
	}

	public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
	{
		if (AmmoDef != null)
		{
			if (Props.ammoCountToRefill != 0)
			{
				yield return new StatDrawEntry(StatCategoryDefOf.Apparel, "Stat_Thing_ReloadRefill_Name".Translate(Props.ChargeNounArgument), $"{Props.ammoCountToRefill} {AmmoDef.label}", "Stat_Thing_ReloadRefill_Desc".Translate(Props.ChargeNounArgument), 2749);
			}
			else
			{
				yield return new StatDrawEntry(StatCategoryDefOf.Apparel, "Stat_Thing_ReloadPerCharge_Name".Translate(Props.ChargeNounArgument), $"{Props.ammoCountPerCharge} {AmmoDef.label}", "Stat_Thing_ReloadPerCharge_Desc".Translate(Props.ChargeNounArgument), 2749);
			}
		}
	}
}
