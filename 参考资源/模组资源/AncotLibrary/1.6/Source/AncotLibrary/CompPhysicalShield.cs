using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AncotLibrary;

[StaticConstructorOnStartup]
public class CompPhysicalShield : ThingComp
{
	private Texture2D GizmoIcon;

	public bool holdShield = false;

	private bool FullStamina;

	private float stamina;

	private static readonly SimpleCurve ArmorRatingToStaminaConsumeFactor = new SimpleCurve
	{
		new CurvePoint(0f, 2f),
		new CurvePoint(0.5f, 1.4f),
		new CurvePoint(2f, 0.5f),
		new CurvePoint(999f, 0.5f)
	};

	public int ticksToReset = -1;

	public Vector3 impactAngleVect = new Vector3(0f, 0f, 0f);

	public An_ShieldState ShieldStateNote;

	public bool AffectedByStuff => Props.affectedByStuff;

	public string BarGizmoLabel => Props.barGizmoLabel ?? parent.LabelCap;

	public float Stamina
	{
		get
		{
			return stamina;
		}
		set
		{
			if (value < stamina)
			{
				FullStamina = false;
			}
			stamina = value;
		}
	}

	private float staminaConsumeFactor
	{
		get
		{
			if (parent is Apparel thing)
			{
				return ArmorRatingToStaminaConsumeFactor.Evaluate(thing.GetStatValue(StatDefOf.ArmorRating_Sharp));
			}
			return 1f;
		}
	}

	public float MaxStamina => parent.GetStatValue(AncotDefOf.Ancot_PhysicalShieldMaxStamina, applyPostProcess: true, 15);

	public Color shieldBarColor => Props.shieldBarColor;

	public string graphicPath_Holding => Props.graphicPath_Holding;

	public string graphicPath_Ready => Props.graphicPath_Ready;

	public string graphicPath_Disabled => Props.graphicPath_Disabled;

	public EffecterDef blockEffecter
	{
		get
		{
			if (Props.blockEffecter != null)
			{
				return Props.blockEffecter;
			}
			return AncotDefOf.Ancot_ShieldBlock;
		}
	}

	public EffecterDef breakEffecter
	{
		get
		{
			if (Props.breakEffecter != null)
			{
				return Props.breakEffecter;
			}
			return AncotDefOf.Ancot_ShieldBreak;
		}
	}

	public CompProperties_PhysicalShield Props => (CompProperties_PhysicalShield)props;

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

	public float StaminaRecoverPerTick => parent.GetStatValue(AncotDefOf.Ancot_PhysicalShieldStaminaRecoverRate) / 60f;

	public float StaminaRecoverPerTick_Holding => parent.GetStatValue(AncotDefOf.Ancot_PhysicalShieldStaminaRecoverRate_Holding) / 60f;

	private float ThresholdStaminaCostPct => parent.GetStatValue(AncotDefOf.Ancot_PhysicalShieldStaminaCostThershold) * MaxStamina;

	private float DefenseAngle => parent.GetStatValue(AncotDefOf.Ancot_PhysicalShieldBlockAngle);

	private float StaminaConsumeRateMelee => parent.GetStatValue(AncotDefOf.Ancot_PhysicalShieldStaminaCost_Melee);

	private float StaminaConsumeRateRanged => parent.GetStatValue(AncotDefOf.Ancot_PhysicalShieldStaminaCost_Ranged);

	private float BlockChance_Melee => parent.GetStatValue(AncotDefOf.Ancot_PhysicalShieldBlockChance_Melee);

	private float BlockChance_Ranged => parent.GetStatValue(AncotDefOf.Ancot_PhysicalShieldBlockChance_Ranged);

	public int StartingTicksToReset => (int)parent.GetStatValue(AncotDefOf.Ancot_PhysicalShieldResetTime) * 60;

	private int BreakStanceTicks => (int)parent.GetStatValue(AncotDefOf.Ancot_PhysicalShieldBreakStance) * 60;

	public bool IsApparel => parent is Apparel;

	private bool IsBuiltIn => !IsApparel;

	private CompMechAutoFight compMechAutoFight => PawnOwner.TryGetComp<CompMechAutoFight>();

	public bool autoFightForPlayer
	{
		get
		{
			if (compMechAutoFight != null && PawnOwner != null && PawnOwner.Faction.IsPlayer)
			{
				return compMechAutoFight.AutoFight;
			}
			return false;
		}
	}

	public An_ShieldState ShieldState
	{
		get
		{
			Pawn pawn;
			if ((pawn = parent as Pawn) != null && (pawn.IsCharging() || pawn.IsSelfShutdown()) && !PawnOwner.Spawned)
			{
				return An_ShieldState.Disabled;
			}
			CompCanBeDormant compCanBeDormant = pawn?.canBeDormant;
			if ((compCanBeDormant != null && !compCanBeDormant.Awake) || (PawnOwner != null && PawnOwner.Faction.IsPlayer && !PawnOwner.Drafted && !autoFightForPlayer))
			{
				return An_ShieldState.Disabled;
			}
			if (ticksToReset <= 0)
			{
				if (holdShield && Stamina > 0f)
				{
					return An_ShieldState.Active;
				}
				return An_ShieldState.Ready;
			}
			return An_ShieldState.Resetting;
		}
	}

	public bool CanUseShield
	{
		get
		{
			Pawn pawnOwner = PawnOwner;
			if (!pawnOwner.Spawned || pawnOwner.Dead || pawnOwner.Downed)
			{
				return false;
			}
			if (pawnOwner.InAggroMentalState)
			{
				return true;
			}
			if (pawnOwner.Drafted)
			{
				return true;
			}
			if (pawnOwner.Faction.HostileTo(Faction.OfPlayer) && !pawnOwner.IsPrisoner)
			{
				return true;
			}
			if (ModsConfig.BiotechActive && pawnOwner.IsColonyMech && (pawnOwner.Drafted || autoFightForPlayer) && Find.Selector.SingleSelectedThing == pawnOwner)
			{
				return true;
			}
			return false;
		}
	}

	public override void Initialize(CompProperties props)
	{
		base.props = props;
		Stamina = MaxStamina;
		FullStamina = true;
		if (PawnOwner != null && PawnOwner.Faction != Faction.OfPlayer && Props.alwaysHoldShield)
		{
			holdShield = true;
		}
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref stamina, "stamina", 0f);
		Scribe_Values.Look(ref ticksToReset, "ticksToReset", -1);
		Scribe_Values.Look(ref holdShield, "holdShield", defaultValue: false);
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
			if ((object)GizmoIcon == null)
			{
				GizmoIcon = ContentFinder<Texture2D>.Get(Props.gizmoIconPath);
			}
			yield return new Command_Toggle
			{
				Order = Props.gizmoOrder,
				defaultLabel = Props.gizmoLabel.Translate(),
				defaultDesc = Props.gizmoDesc.Translate(),
				icon = GizmoIcon,
				toggleAction = delegate
				{
					holdShield = !holdShield;
					FullStamina = false;
				},
				isActive = () => holdShield
			};
		}
		if (Find.Selector.SingleSelectedThing == PawnOwner && (PawnOwner.Drafted || autoFightForPlayer))
		{
			yield return new Gizmo_PhysicalShieldBar
			{
				compPhysicalShield = parent.TryGetComp<CompPhysicalShield>()
			};
		}
	}

	public override void PostPreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
	{
		absorbed = false;
		if (ShieldState != An_ShieldState.Active || PawnOwner == null || !PawnOwner.Spawned)
		{
			return;
		}
		_ = PawnOwner.Rotation;
		if (0 == 0)
		{
			float asAngle = PawnOwner.Rotation.AsAngle;
			float num = dinfo.Angle + 180f;
			if (num > 360f)
			{
				num -= 360f;
			}
			if (num >= asAngle - DefenseAngle / 2f && num <= asAngle + DefenseAngle / 2f)
			{
				BlockByShield(ref dinfo, out var blocked);
				absorbed = blocked;
			}
			if (asAngle - DefenseAngle / 2f < 0f && num > 360f + asAngle - DefenseAngle / 2f && num <= 360f)
			{
				BlockByShield(ref dinfo, out var blocked2);
				absorbed = blocked2;
			}
			if (asAngle + DefenseAngle / 2f > 360f && num < asAngle + DefenseAngle / 2f - 360f && num > 0f)
			{
				BlockByShield(ref dinfo, out var blocked3);
				absorbed = blocked3;
			}
		}
	}

	private void BlockByShield(ref DamageInfo dinfo, out bool blocked)
	{
		blocked = false;
		if (PawnOwner == null)
		{
			return;
		}
		if (Props.recordLastHarmTickWhenBlocked)
		{
			PawnOwner.mindState.lastHarmTick = Find.TickManager.TicksGame;
			if (dinfo.Def.isRanged)
			{
				PawnOwner.mindState.lastRangedHarmTick = Find.TickManager.TicksGame;
			}
		}
		float amount = dinfo.Amount;
		if (!dinfo.Def.harmsHealth || dinfo.IgnoreArmor)
		{
			blocked = false;
			return;
		}
		if (dinfo.Def.isRanged)
		{
			if (Rand.Chance(BlockChance_Ranged))
			{
				float num = amount * dinfo.ArmorPenetrationInt * StaminaConsumeRateRanged * staminaConsumeFactor;
				blocked = true;
				if (num > ThresholdStaminaCostPct)
				{
					num = ThresholdStaminaCostPct;
				}
				Stamina -= num;
			}
		}
		else if (Rand.Chance(BlockChance_Melee))
		{
			float num2 = amount * dinfo.ArmorPenetrationInt * StaminaConsumeRateMelee * staminaConsumeFactor;
			blocked = true;
			if (num2 > ThresholdStaminaCostPct)
			{
				num2 = ThresholdStaminaCostPct;
			}
			Stamina -= num2;
		}
		if (Stamina < 0f)
		{
			Break();
			breakEffecter.Spawn().Trigger(PawnOwner, dinfo.Instigator ?? PawnOwner);
			MoteMaker.ThrowText(PawnOwner.DrawPos, PawnOwner.Map, "Ancot.TextMote_Break".Translate(), Color.red, 1.9f);
		}
		else
		{
			AbsorbedDamage(dinfo);
		}
		if (IsApparel)
		{
			parent.HitPoints -= (int)Mathf.Min(amount / 12f, 10f);
			if (parent.HitPoints <= 0)
			{
				parent.Destroy();
			}
		}
	}

	private void AbsorbedDamage(DamageInfo dinfo)
	{
		impactAngleVect = Vector3Utility.HorizontalVectorFromAngle(dinfo.Angle);
		blockEffecter.Spawn().Trigger(PawnOwner, dinfo.Instigator ?? PawnOwner);
		MoteMaker.ThrowText(PawnOwner.DrawPos, PawnOwner.Map, "Ancot.TextMote_Block".Translate(), 1.9f);
	}

	private void ShieldPiercing(DamageInfo dinfo)
	{
		breakEffecter.Spawn().Trigger(PawnOwner, dinfo.Instigator ?? PawnOwner);
		MoteMaker.ThrowText(PawnOwner.DrawPos, PawnOwner.Map, "Ancot.TextMote_Penetrate".Translate(), Color.red, 1.9f);
		DamageInfo dinfo2 = dinfo;
		dinfo2.SetAmount(dinfo2.Amount / 4f);
		dinfo2.SetIgnoreArmor(ignoreArmor: true);
		PawnOwner.TakeDamage(dinfo2);
		Log.Message("dinfo" + dinfo2.Amount);
	}

	public override void CompTick()
	{
		An_ShieldState shieldState = ShieldState;
		if (ShieldStateNote != shieldState)
		{
			ShieldStateNote = shieldState;
			Notify_StateChange();
			PawnOwner?.Drawer?.renderer?.renderTree?.SetDirty();
		}
		if (FullStamina)
		{
			return;
		}
		base.CompTick();
		if (impactAngleVect != new Vector3(0f, 0f, 0f))
		{
			impactAngleVect *= 0.8f;
		}
		if (PawnOwner == null)
		{
			Stamina = 0f;
		}
		else if (ShieldState == An_ShieldState.Resetting || (ShieldState == An_ShieldState.Disabled && stamina == 0f))
		{
			ticksToReset--;
			if (ticksToReset <= 0)
			{
				Reset();
			}
			RemoveShieldRaiseHediff();
		}
		else if (ShieldState == An_ShieldState.Active || ShieldState == An_ShieldState.Ready || ShieldState == An_ShieldState.Disabled)
		{
			if (holdShield && ShieldState == An_ShieldState.Active)
			{
				stamina += StaminaRecoverPerTick_Holding;
				AddShieldRaiseHediff();
			}
			else
			{
				stamina += StaminaRecoverPerTick;
				RemoveShieldRaiseHediff();
			}
			if (stamina > MaxStamina)
			{
				stamina = MaxStamina;
			}
		}
		else
		{
			RemoveShieldRaiseHediff();
		}
	}

	private void Break()
	{
		Stamina = 0f;
		ticksToReset = StartingTicksToReset;
		PawnOwner.stances.SetStance(new Stance_Cooldown(BreakStanceTicks, null, null));
	}

	public void Notify_StateChange()
	{
		if (PawnOwner == null)
		{
			return;
		}
		foreach (Hediff hediff in PawnOwner.health.hediffSet.hediffs)
		{
			hediff.TryGetComp<HediffComp_PhysicalShieldState>()?.Notify_ShieldStateChange(PawnOwner, ShieldState);
		}
	}

	public void AddShieldRaiseHediff()
	{
		if (PawnOwner != null && Props.holdShieldHediff != null)
		{
			HealthUtility.AdjustSeverity(PawnOwner, Props.holdShieldHediff, 1f);
		}
	}

	public void RemoveShieldRaiseHediff()
	{
		if (PawnOwner != null && Props.holdShieldHediff != null)
		{
			Hediff firstHediffOfDef = PawnOwner.health.hediffSet.GetFirstHediffOfDef(Props.holdShieldHediff);
			if (firstHediffOfDef != null && PawnOwner != null)
			{
				PawnOwner.health.RemoveHediff(firstHediffOfDef);
			}
		}
	}

	private void Reset()
	{
		if (PawnOwner.Spawned)
		{
			FleckMaker.ThrowLightningGlow(PawnOwner.TrueCenter(), PawnOwner.Map, 3f);
		}
		ticksToReset = -1;
		Stamina = MaxStamina;
		FullStamina = true;
	}

	public override bool CompAllowVerbCast(Verb verb)
	{
		if (Props.blocksRangedWeapons)
		{
			return !(verb is Verb_LaunchProjectile);
		}
		return true;
	}
}
