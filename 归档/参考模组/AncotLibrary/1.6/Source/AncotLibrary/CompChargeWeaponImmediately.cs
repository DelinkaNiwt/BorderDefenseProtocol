using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AncotLibrary;

public class CompChargeWeaponImmediately : ThingComp
{
	private int ticksToReset = -1;

	public CompProperties_ChargeWeaponImmediately Props => (CompProperties_ChargeWeaponImmediately)props;

	protected Pawn PawnOwner
	{
		get
		{
			if (!(parent is Apparel { Wearer: var wearer }))
			{
				if (parent is Pawn result)
				{
					return result;
				}
				return null;
			}
			return wearer;
		}
	}

	private bool IsApparel => parent is Apparel;

	private bool IsBuiltIn => !IsApparel;

	private ThingWithComps weapon => PawnOwner.equipment.Primary;

	private CompWeaponCharge compWeaponCharge
	{
		get
		{
			if (weapon != null)
			{
				return weapon.TryGetComp<CompWeaponCharge>();
			}
			return null;
		}
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref ticksToReset, "ticksToReset", -1);
	}

	public override void CompTickInterval(int delta)
	{
		if (PawnOwner != null)
		{
			if (ticksToReset > 0)
			{
				ticksToReset -= delta;
			}
			if (ticksToReset <= 0 && weapon != null && compWeaponCharge != null && compWeaponCharge.ChargeState == An_ChargeState.Resetting)
			{
				ChargeWeaponAtOnce();
			}
		}
	}

	public override IEnumerable<Gizmo> CompGetWornGizmosExtra()
	{
		foreach (Gizmo item in base.CompGetWornGizmosExtra())
		{
			yield return item;
		}
		if (!IsApparel)
		{
			yield break;
		}
		foreach (Gizmo gizmo in GetGizmos())
		{
			yield return gizmo;
		}
	}

	public override IEnumerable<Gizmo> CompGetGizmosExtra()
	{
		foreach (Gizmo item in base.CompGetGizmosExtra())
		{
			yield return item;
		}
		if (!IsBuiltIn)
		{
			yield break;
		}
		foreach (Gizmo gizmo in GetGizmos())
		{
			yield return gizmo;
		}
	}

	private IEnumerable<Gizmo> GetGizmos()
	{
		if (PawnOwner.Faction == Faction.OfPlayer && PawnOwner.Drafted)
		{
			Command_ActionWithCooldown command_Action = new Command_ActionWithCooldown
			{
				cooldownPercentGetter = () => Mathf.InverseLerp(Props.chargeCooldownTime * 60f, 0f, ticksToReset),
				defaultLabel = "Ancot.ChargeImmediately".Translate(),
				defaultDesc = "Ancot.ChargeImmediatelyDesc".Translate(),
				icon = AncotLibraryIcon.SwitchA,
				action = delegate
				{
					ChargeWeaponAtOnce();
				}
			};
			if ((float)ticksToReset > 0f)
			{
				command_Action.Disable("Ancot.ChargeImmediatelyReason".Translate());
			}
			yield return command_Action;
		}
	}

	public void ChargeWeaponAtOnce()
	{
		int charge = compWeaponCharge.Charge;
		ticksToReset = (int)(Props.chargeCooldownTime * 60f);
		compWeaponCharge.Reset();
		float statValue = weapon.GetStatValue(AncotDefOf.Ancot_EnergyWeaponCorrectionFactor);
		compWeaponCharge.Charge = Mathf.Min(Mathf.FloorToInt((float)charge + statValue * (float)compWeaponCharge.maxCharge), compWeaponCharge.maxCharge);
	}
}
