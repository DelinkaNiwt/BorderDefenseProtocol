using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace NCLWorm;

public class CompShieldNCLWorm : ThingComp
{
	private float power = 999f;

	public int ticksToReset = -1;

	private int lastKeepDisplayTick = -9999;

	private int lastBombTick = -9999;

	protected Vector3 impactAngleVect;

	protected int StartingTicksToReset => Props.RestTime * 60;

	public CompProperties_ShieldNCLWorm Props => (CompProperties_ShieldNCLWorm)props;

	public float MaxPower => Props.EnergyShieldEnergyMax;

	public float RePowerRate => Props.EnergyShieldRechargeRate / 60f;

	public float Power
	{
		get
		{
			return power;
		}
		set
		{
			power = value;
		}
	}

	public ShieldState shieldState
	{
		get
		{
			if (PawnOwner.IsCharging() || PawnOwner.IsSelfShutdown())
			{
				return ShieldState.Disabled;
			}
			if (ticksToReset > 0)
			{
				return ShieldState.Resetting;
			}
			return ShieldState.Active;
		}
	}

	protected bool ShouldDisplay
	{
		get
		{
			if (!PawnOwner.Spawned || PawnOwner.Dead || PawnOwner.Downed)
			{
				return false;
			}
			if (PawnOwner.InAggroMentalState)
			{
				return true;
			}
			if (PawnOwner.Drafted)
			{
				return true;
			}
			if (PawnOwner.Faction.HostileTo(Faction.OfPlayer) && !PawnOwner.IsPrisoner)
			{
				return true;
			}
			if (Find.TickManager.TicksGame < lastKeepDisplayTick + 300)
			{
				return true;
			}
			return false;
		}
	}

	protected Pawn PawnOwner => (Pawn)parent;

	public void KeepDisplaying()
	{
		lastKeepDisplayTick = Find.TickManager.TicksGame;
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref power, "power", 0f);
		Scribe_Values.Look(ref ticksToReset, "Sleeptick", -1);
		Scribe_Values.Look(ref lastBombTick, "lastBombTick", -1);
		Scribe_Values.Look(ref lastKeepDisplayTick, "lastKeepDisplayTick", 0);
	}

	public override IEnumerable<Gizmo> CompGetGizmosExtra()
	{
		foreach (Gizmo item in base.CompGetGizmosExtra())
		{
			yield return item;
		}
		yield return new Gizmo_MechNCLWorm_ShieldStatus
		{
			shield = this
		};
	}

	public override void CompTick()
	{
		base.CompTick();
		if (PawnOwner == null)
		{
			power = 0f;
		}
		else if (shieldState == ShieldState.Resetting)
		{
			ticksToReset--;
			if (ticksToReset <= 0)
			{
				Reset();
			}
		}
		else if (shieldState == ShieldState.Active)
		{
			power += RePowerRate;
			if (power > MaxPower)
			{
				power = MaxPower;
			}
		}
	}

	private void AbsorbedDamage(DamageInfo dinfo)
	{
		if (PawnOwner.Map != null)
		{
			SoundDefOf.EnergyShield_AbsorbDamage.PlayOneShot(new TargetInfo(PawnOwner.Position, PawnOwner.Map));
			impactAngleVect = Vector3Utility.HorizontalVectorFromAngle(dinfo.Angle);
			Vector3 loc = PawnOwner.TrueCenter() + impactAngleVect.RotatedBy(180f) * 0.5f;
			float num = Mathf.Min(10f, 2f + dinfo.Amount / 10f);
			FleckMaker.Static(loc, PawnOwner.Map, FleckDefOf.ExplosionFlash, num);
			int num2 = (int)num;
			for (int i = 0; i < num2; i++)
			{
				FleckMaker.ThrowDustPuff(loc, PawnOwner.Map, Rand.Range(0.8f, 1.2f));
			}
		}
		KeepDisplaying();
	}

	public override void PostPreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
	{
		absorbed = false;
		if (shieldState == ShieldState.Resetting || PawnOwner == null)
		{
			return;
		}
		if (!dinfo.Def.harmsHealth)
		{
			absorbed = true;
			return;
		}
		if (dinfo.Instigator == PawnOwner)
		{
			AbsorbedDamage(dinfo);
			absorbed = true;
			return;
		}
		AbsorbedDamage(dinfo);
		float num = power / MaxPower;
		power -= dinfo.Amount;
		if (((num >= 0.7f && power / MaxPower <= 0.7f) || (num >= 0.4f && power / MaxPower <= 0.4f) || (num >= 0.1f && power / MaxPower <= 0.1f)) && Find.TickManager.TicksGame - lastBombTick >= 6000)
		{
			GenExplosion.DoExplosion(parent.Position, parent.Map, 8f, DamageDefOf.Flame, parent, 10, 0.15f, null, null, null, null, null, 0f, 1, null, null, 255, applyDamageToExplosionCellsNeighbors: false, ThingDefOf.Filth_Fuel, 1f, 1, 1f);
			GenExplosion.DoExplosion(parent.Position, parent.Map, 8f, DamageDefOf.EMP, parent, 100);
			lastBombTick = Find.TickManager.TicksGame;
		}
		if (power < 0f)
		{
			Break();
		}
		absorbed = true;
	}

	private void Break()
	{
		if (parent.Spawned)
		{
			float scale = Mathf.Lerp(Props.minDrawSize, Props.maxDrawSize, power);
			EffecterDefOf.Shield_Break.SpawnAttached(parent, parent.MapHeld, scale);
			FleckMaker.Static(PawnOwner.TrueCenter(), PawnOwner.Map, FleckDefOf.ExplosionFlash, 12f);
			for (int i = 0; i < 6; i++)
			{
				FleckMaker.ThrowDustPuff(PawnOwner.TrueCenter() + Vector3Utility.HorizontalVectorFromAngle(Rand.Range(0, 360)) * Rand.Range(0.3f, 0.6f), PawnOwner.Map, Rand.Range(0.8f, 1.2f));
			}
		}
		power = 0f;
		ticksToReset = StartingTicksToReset;
	}

	public void Reset()
	{
		if (PawnOwner.Spawned)
		{
			SoundDefOf.EnergyShield_Reset.PlayOneShot(new TargetInfo(PawnOwner.Position, PawnOwner.Map));
			FleckMaker.ThrowLightningGlow(PawnOwner.TrueCenter(), PawnOwner.Map, 3f);
		}
		ticksToReset = -1;
		power = Props.energyOnReset * Props.EnergyShieldEnergyMax;
	}
}
