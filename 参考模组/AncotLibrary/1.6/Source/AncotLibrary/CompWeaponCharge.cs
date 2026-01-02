using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace AncotLibrary;

public class CompWeaponCharge : ThingComp, IPawnWeaponGizmoProvider
{
	private int charge;

	public bool FullCharge;

	private bool emptyOnce = true;

	public int ticksToReset = -1;

	private bool initialized = false;

	public int maxCharge => Mathf.RoundToInt(parent.GetStatValue(AncotDefOf.Ancot_WeaponMaxCharge, applyPostProcess: true, 15));

	public int ticksPerCharge => Mathf.RoundToInt(parent.GetStatValue(AncotDefOf.Ancot_WeaponChargeTick, applyPostProcess: true, 15));

	public int Charge
	{
		get
		{
			return charge;
		}
		set
		{
			if (value < maxCharge)
			{
				FullCharge = false;
			}
			charge = value;
		}
	}

	public float MeleeDamageFactorCharged => Props.meleeDamageFactorCharged;

	public float MeleeArmorPenetrationFactorCharged => Props.meleeArmorPenetrationFactorCharged;

	public int StartingTicksToReset => Mathf.RoundToInt(parent.GetStatValue(AncotDefOf.Ancot_EnergyWeaponResetTime, applyPostProcess: true, 15));

	public CompRangeWeaponVerbSwitch compVerbSwitch => parent.TryGetComp<CompRangeWeaponVerbSwitch>();

	public CompRangeWeaponVerbSwitch_EnergyPassive compVerbSwitch_Passive => parent.TryGetComp<CompRangeWeaponVerbSwitch_EnergyPassive>();

	public int ChargePerUse => Props.chargePerUse;

	public ThingDef projectileCharged
	{
		get
		{
			if (compVerbSwitch != null && compVerbSwitch.switched && Props.projectileCharged_Switched != null)
			{
				return Props.projectileCharged_Switched;
			}
			return Props.projectileCharged;
		}
	}

	public CompEquippable CompEquippable => parent.TryGetComp<CompEquippable>();

	public Verb Verb => CompEquippable.PrimaryVerb;

	public Pawn CasterPawn => Verb.caster as Pawn;

	public bool CanBeUsed => Charge >= Props.chargePerUse || Props.canUseIfHasCharge;

	public Color barColor => Props.barColor;

	private CompProperties_WeaponCharge Props => (CompProperties_WeaponCharge)props;

	public An_ChargeState ChargeState
	{
		get
		{
			if (Props.resetAfterEmpty)
			{
				if (ticksToReset > 0)
				{
					return An_ChargeState.Resetting;
				}
				if (Charge == 0)
				{
					return An_ChargeState.Empty;
				}
			}
			return An_ChargeState.Active;
		}
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref charge, "charge", 0);
		Scribe_Values.Look(ref ticksToReset, "ticksToReset", -1);
		Scribe_Values.Look(ref initialized, "initialized", defaultValue: false);
	}

	public override void PostSpawnSetup(bool respawningAfterLoad)
	{
		if (!initialized)
		{
			Charge = maxCharge;
			initialized = true;
		}
	}

	public override void Initialize(CompProperties props)
	{
		base.props = props;
		Charge = maxCharge;
	}

	public override void CompTick()
	{
		if (FullCharge)
		{
			return;
		}
		base.CompTick();
		if (parent == null)
		{
			Charge = 0;
			return;
		}
		switch (ChargeState)
		{
		case An_ChargeState.Resetting:
			ticksToReset--;
			if (ticksToReset <= 0)
			{
				Reset();
			}
			break;
		case An_ChargeState.Active:
			if (Props.autoRecharge && parent.IsHashIntervalTick(ticksPerCharge))
			{
				Charge++;
			}
			if (Charge > maxCharge)
			{
				Charge = maxCharge;
				FullCharge = true;
			}
			if (!emptyOnce)
			{
				emptyOnce = true;
			}
			break;
		case An_ChargeState.Empty:
			if (emptyOnce)
			{
				emptyOnce = false;
				Empty();
			}
			break;
		}
	}

	public void Empty()
	{
		Charge = 0;
		ticksToReset = StartingTicksToReset;
		if (Props.resetVerbOnEmpty)
		{
			Verb.Reset();
		}
	}

	public void Reset()
	{
		if (parent.Spawned)
		{
			SoundDefOf.EnergyShield_Reset.PlayOneShot(new TargetInfo(parent.Position, parent.Map));
		}
		ticksToReset = -1;
		Charge = (int)(Props.chargeOnResetRatio * (float)maxCharge);
		compVerbSwitch_Passive?.Notify_SwitchPassive();
	}

	public void UsedOnce(int chargeCostOverride)
	{
		if (Charge > 0)
		{
			Charge -= Mathf.Min(chargeCostOverride, Charge);
		}
		if (Charge == 0)
		{
			ForceStopBurst();
			if (Props.destroyOnEmpty && !parent.Destroyed)
			{
				parent.Destroy();
			}
		}
	}

	public void UsedOnce()
	{
		if (Charge > 0)
		{
			Charge -= Mathf.Min(Props.chargePerUse, Charge);
		}
		if (Charge == 0)
		{
			ForceStopBurst();
			if (Props.destroyOnEmpty && !parent.Destroyed)
			{
				parent.Destroy();
			}
		}
	}

	public void Notify_AISwitchSecondVerb()
	{
		if (Props.ai_SwitchSecondVerbOnlyIfChargeNotEmpty && compVerbSwitch != null && !CasterPawn.Faction.IsPlayer)
		{
			if (Charge == 0)
			{
				compVerbSwitch.Notify_VerbSwitch(switchToSecond: false);
			}
			else
			{
				compVerbSwitch.Notify_VerbSwitch(switchToSecond: true);
			}
		}
	}

	public void ChargeFireEffect(TargetInfo A, TargetInfo B)
	{
		if (Props.chargeFireEffecter != null)
		{
			Props.chargeFireEffecter.Spawn().Trigger(A, B);
		}
	}

	private void ForceStopBurst()
	{
		Verb.Reset();
		CasterPawn.stances.CancelBusyStanceSoft();
		float statValue = parent.GetStatValue(StatDefOf.RangedWeapon_Cooldown);
		CasterPawn?.stances?.SetStance(new Stance_Cooldown(Verb.verbProps.AdjustedCooldownTicks(Verb, CasterPawn), Verb.CurrentTarget, Verb));
	}

	public virtual IEnumerable<Gizmo> GetWeaponGizmos()
	{
		if (Find.Selector.SelectedObjects.Count == 1 && (CasterPawn == null || CasterPawn.Faction == null || CasterPawn.Faction.IsPlayer))
		{
			yield return new Gizmo_ChargeBar
			{
				compWeaponCharge = this
			};
		}
	}

	public override IEnumerable<Gizmo> CompGetGizmosExtra()
	{
		foreach (Gizmo item in base.CompGetGizmosExtra())
		{
			yield return item;
		}
		foreach (Gizmo weaponGizmo in GetWeaponGizmos())
		{
			yield return weaponGizmo;
		}
	}

	public string ProjectileChargedInfo()
	{
		string text = string.Concat(string.Concat("Ancot.ProjectileChargedDesc".Translate() + "\n\n" + "Ancot.ProjectileCharged_Damage".Translate() + ": ", projectileCharged.projectile.GetDamageAmount(parent).ToString(), "\n") + "Ancot.ProjectileCharged_ArmorPenetration".Translate() + ": " + projectileCharged.projectile.GetArmorPenetration(parent).ToStringPercent() + "\n" + "Ancot.ProjectileCharged_StoppingPower".Translate() + ": ", projectileCharged.projectile.stoppingPower.ToString());
		if ((double)projectileCharged.projectile.explosionRadius > 0.1)
		{
			text = string.Concat(text, "\n" + "Ancot.ProjectileCharged_ExplosionRadius".Translate() + ": ", projectileCharged.projectile.explosionRadius.ToString());
		}
		return text;
	}

	public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
	{
		if (projectileCharged != null)
		{
			yield return new StatDrawEntry(StatCategoryDefOf.Weapon_Ranged, "Ancot.ProjectileCharged".Translate(), projectileCharged.LabelCap, ProjectileChargedInfo(), 5600);
		}
	}
}
