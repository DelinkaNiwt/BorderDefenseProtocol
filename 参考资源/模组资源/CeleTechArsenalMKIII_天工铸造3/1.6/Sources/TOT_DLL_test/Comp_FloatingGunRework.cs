using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TOT_DLL_test;

[StaticConstructorOnStartup]
public class Comp_FloatingGunRework : ThingComp, IAttackTargetSearcher
{
	public Vector3 targetPosition;

	public bool launching;

	public bool Released = false;

	private bool AutoRelease = false;

	public int tickactive = -1;

	public static readonly Vector3 PosDefault = Vector3.zero;

	private int burstCooldownTicksLeft;

	private int burstWarmupTicksLeft;

	public LocalTargetInfo currentTarget;

	public Thing gun;

	public bool fireAtWill;

	public Vector3 currentPosition;

	public Vector3 currentVelocity;

	private Vector3 targetOffset;

	private float lastUpdateTime;

	private const float PositionChangeInterval = 1.5f;

	private const float MaxSpeed = 12f;

	private const float Acceleration = 4.8f;

	private const float Damping = 4f;

	private static readonly CachedTexture ReleaseIcon = new CachedTexture("UI/UI_ReleaseUAV");

	private static readonly CachedTexture AutoReleaseIcon = new CachedTexture("UI/UI_ReleaseUAV_Auto");

	private static readonly CachedTexture CallIcon = new CachedTexture("UI/UI_Return");

	private static readonly CachedTexture ForceAttackIcon = new CachedTexture("UI/UI_ForceAttack");

	private static readonly CachedTexture RadiusIcon = new CachedTexture("UI/UI_TargetRange");

	private Gizmo_UAVPowerCell gizmo_UAVPowerCell;

	public float curRotation;

	private LocalTargetInfo lastAttackedTarget = LocalTargetInfo.Invalid;

	private int lastAttackTargetTick;

	public bool RenderRadius;

	private int ModifiedBatteryTickSaved = -1;

	private int ModifiedBatteryChargingTick = -1;

	public CompProperties_FloatGunRework Props => (CompProperties_FloatGunRework)props;

	public Thing Thing => PawnOwner;

	public Verb CurrentEffectiveVerb => AttackVerb;

	public LocalTargetInfo LastAttackedTarget => lastAttackedTarget;

	public int LastAttackTargetTick => lastAttackTargetTick;

	private bool WarmingUp => burstWarmupTicksLeft > 0;

	public int ModifiedBatteryLifeTick
	{
		get
		{
			if (ModifiedBatteryTickSaved <= 0)
			{
				ModifiedBatteryTickSaved = (int)((float)Props.BatteryLifeTick * GameComponent_CeleTech.Instance.FloatingGun_EnergyCap);
			}
			return ModifiedBatteryTickSaved;
		}
	}

	private int ModChargingSpeed
	{
		get
		{
			if (ModifiedBatteryChargingTick <= 0)
			{
				ModifiedBatteryChargingTick = (int)((float)(Props.BatteryRecoverPerSec * GameComponent_CeleTech.Instance.FloatingGun_ChargingRate) * Props.ChargingSpeedMutiplier);
			}
			return ModifiedBatteryChargingTick;
		}
	}

	public Pawn PawnOwner
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

	public Verb AttackVerb => GunCompEq.PrimaryVerb;

	public CompEquippable GunCompEq => gun.TryGetComp<CompEquippable>();

	public bool TempDestroyed()
	{
		return parent.HitPoints < parent.MaxHitPoints / 5;
	}

	public bool Active()
	{
		bool result = false;
		if (parent == null || PawnOwner == null)
		{
			result = false;
		}
		else if (tickactive > ModifiedBatteryLifeTick / 10 && Released && !PawnOwner.DeadOrDowned && !PawnOwner.InBed())
		{
			result = true;
		}
		return result;
	}

	private bool ParentHeld()
	{
		if (PawnOwner != null && PawnOwner.ParentHolder != null)
		{
			return PawnOwner.ParentHolder is Map;
		}
		return true;
	}

	private void ResetCurrentTarget()
	{
		currentTarget = LocalTargetInfo.Invalid;
		burstWarmupTicksLeft = 0;
	}

	private static bool IsHashIntervalTick(Thing t, int interval)
	{
		return t.HashOffsetTicks() % interval == 0;
	}

	public override void CompTick()
	{
		bool flag = TempDestroyed();
		base.CompTick();
		if (IsHashIntervalTick(parent, 300))
		{
			CheckRelease();
		}
		if (Active() && Released && !flag && ParentHeld())
		{
			if (RenderRadius)
			{
				float effectiveRange = AttackVerb.EffectiveRange;
				GenDraw.DrawCircleOutline(currentPosition, effectiveRange, Props.RadiusColor);
				GenDraw.DrawCircleOutline(currentPosition, effectiveRange - 0.1f, Props.RadiusColor);
				GenDraw.DrawCircleOutline(currentPosition, effectiveRange - 0.2f, Props.RadiusColor);
			}
			UpdatePosition();
			tickactive--;
			if (fireAtWill)
			{
				if (currentTarget.IsValid)
				{
					curRotation = (currentTarget.Cell.ToVector3Shifted() - PawnOwner.DrawPos).AngleFlat();
				}
				AttackVerb.VerbTick();
				if (AttackVerb.state != VerbState.Bursting)
				{
					if (WarmingUp)
					{
						burstWarmupTicksLeft--;
						if (burstWarmupTicksLeft == 0)
						{
							launching = true;
							AttackVerb.TryStartCastOn(currentTarget, surpriseAttack: false, canHitNonTargetPawns: true, preventFriendlyFire: false, nonInterruptingSelfCast: true);
							lastAttackTargetTick = Find.TickManager.TicksGame;
							lastAttackedTarget = currentTarget;
							launching = false;
						}
					}
					else
					{
						if (burstCooldownTicksLeft > 0)
						{
							burstCooldownTicksLeft--;
						}
						if (burstCooldownTicksLeft <= 0 && PawnOwner.IsHashIntervalTick(10))
						{
							currentTarget = (Thing)AttackTargetFinder.BestAttackTarget(this, TargetScanFlags.NeedThreat | TargetScanFlags.NeedAutoTargetable, null, 0f, 9999f, currentPosition.ToIntVec3());
							if (currentTarget.IsValid)
							{
								burstWarmupTicksLeft = 1;
							}
							else
							{
								ResetCurrentTarget();
							}
						}
					}
				}
			}
		}
		else
		{
			Released = false;
			if (IsHashIntervalTick(parent, 60) && !flag)
			{
				tickactive = Mathf.Min(tickactive + ModChargingSpeed, ModifiedBatteryLifeTick);
			}
			if (flag)
			{
				Released = false;
			}
		}
		tickactive = Mathf.Clamp(tickactive, 0, ModifiedBatteryLifeTick);
	}

	private void CheckRelease()
	{
		if (ParentHeld() && AutoRelease && parent != null && PawnOwner != null && !PawnOwner.Dead && tickactive > Props.BatteryLifeTick / 2 && (PawnOwner.drafter.Drafted || PawnOwner.CurJob.def == JobDefOf.AttackStatic || PawnOwner.CurJob.def == JobDefOf.FleeAndCower) && !Released)
		{
			Released = true;
			currentPosition = PawnOwner.DrawPos;
		}
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref burstCooldownTicksLeft, "burstCooldownTicksLeft", 0);
		Scribe_Values.Look(ref burstWarmupTicksLeft, "burstWarmupTicksLeft", 0);
		Scribe_Values.Look(ref tickactive, "BatteryLifeLeft", 0);
		Scribe_TargetInfo.Look(ref currentTarget, "currentTarget");
		Scribe_Deep.Look(ref gun, "gun");
		Scribe_Values.Look(ref fireAtWill, "fireAtWill", defaultValue: true);
		Scribe_Values.Look(ref Released, "UAVreleased", defaultValue: false);
		Scribe_Values.Look(ref RenderRadius, "RenderRadius", defaultValue: false);
		Scribe_Values.Look(ref AutoRelease, "autorelease", defaultValue: false);
		if (Scribe.mode == LoadSaveMode.PostLoadInit)
		{
			if (gun == null && PawnOwner != null)
			{
				Log.Error("CompTurretGun: null gun after load. Recreating.");
				MakeGun();
			}
			else
			{
				UpdateGunVerbs();
			}
		}
	}

	public override IEnumerable<Gizmo> CompGetWornGizmosExtra()
	{
		foreach (Gizmo item in base.CompGetWornGizmosExtra())
		{
			yield return item;
		}
		foreach (Gizmo gizmo in GetGizmos())
		{
			yield return gizmo;
		}
	}

	private IEnumerable<Gizmo> GetGizmos()
	{
		bool Isdestroyed = TempDestroyed();
		if (!PawnOwner.IsPlayerControlled || !PawnOwner.Drafted)
		{
			yield break;
		}
		if (Find.Selector.SingleSelectedThing == PawnOwner)
		{
			if (gizmo_UAVPowerCell == null)
			{
				gizmo_UAVPowerCell = new Gizmo_UAVPowerCell(this);
			}
			yield return gizmo_UAVPowerCell;
		}
		if (!Active() && !AutoRelease && !Isdestroyed)
		{
			yield return new Command_Action
			{
				defaultLabel = "Command_CMC_ReleaseUAV".Translate(),
				icon = ReleaseIcon.Texture,
				action = delegate
				{
					Released = true;
					currentPosition = PawnOwner.DrawPos;
				}
			};
		}
		if (Active())
		{
			yield return new Command_Toggle
			{
				defaultLabel = "CommandToggleTurret".Translate(),
				defaultDesc = "CommandToggleTurretDesc".Translate(),
				isActive = () => fireAtWill,
				icon = ForceAttackIcon.Texture,
				toggleAction = delegate
				{
					fireAtWill = !fireAtWill;
				}
			};
			yield return new Command_Action
			{
				defaultLabel = "Command_CMC_CallBACKUAV".Translate(),
				icon = CallIcon.Texture,
				action = delegate
				{
					Released = false;
				}
			};
		}
		yield return new Command_Toggle
		{
			defaultLabel = "Command_CMC_AutoReleaseUAV".Translate(),
			defaultDesc = "Command_CMC_AutoReleaseUAV_Desc".Translate(),
			isActive = () => AutoRelease,
			icon = AutoReleaseIcon.Texture,
			toggleAction = delegate
			{
				AutoRelease = !AutoRelease;
			}
		};
		yield return new Command_Toggle
		{
			defaultLabel = "Command_CMCUAVDrawRadius".Translate(),
			defaultDesc = "Command_CMCUAVDrawRadiusDesc".Translate(),
			isActive = () => RenderRadius,
			icon = RadiusIcon.Texture,
			toggleAction = delegate
			{
				RenderRadius = !RenderRadius;
			}
		};
	}

	public override void Notify_Equipped(Pawn pawn)
	{
		base.PostPostMake();
		MakeGun();
		UpdateCurrentPos();
	}

	private void MakeGun()
	{
		gun = ThingMaker.MakeThing(Props.turretDef);
		UpdateGunVerbs();
	}

	private void UpdateCurrentPos()
	{
		currentPosition = PawnOwner.DrawPos;
	}

	private void UpdateGunVerbs()
	{
		List<Verb> allVerbs = gun.TryGetComp<CompEquippable>().AllVerbs;
		for (int i = 0; i < allVerbs.Count; i++)
		{
			Verb verb = allVerbs[i];
			verb.caster = PawnOwner;
			verb.castCompleteCallback = delegate
			{
				burstCooldownTicksLeft = AttackVerb.verbProps.defaultCooldownTime.SecondsToTicks();
			};
		}
	}

	public Vector3 UpdatePosition()
	{
		float deltaTime = Time.deltaTime;
		if (Time.time - lastUpdateTime > 1.5f)
		{
			targetOffset = GetRandomOffset();
			lastUpdateTime = Time.time;
		}
		targetPosition = PawnOwner.DrawPos + targetOffset;
		Vector3 vector = (targetPosition - currentPosition) * 4.8f;
		currentVelocity += vector * deltaTime;
		currentVelocity *= Mathf.Clamp01(1f - 4f * deltaTime);
		if (currentVelocity.magnitude > 12f)
		{
			currentVelocity = currentVelocity.normalized * 12f;
		}
		currentPosition += currentVelocity * deltaTime;
		return currentPosition;
	}

	private Vector3 GetRandomOffset()
	{
		float num = Rand.Range(0f, 360f);
		float num2 = Rand.Range(1.78f, 3.4f);
		return new Vector3(Mathf.Cos(num * ((float)Math.PI / 180f)) * num2, 0f, Mathf.Sin(num * ((float)Math.PI / 180f)) * num2);
	}

	public override void PostPreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
	{
		absorbed = false;
		if (dinfo.Def == DamageDefOf.EMP && !CMC_Def.CMC_FloatingGunIII.IsFinished)
		{
			parent.HitPoints -= 80;
		}
		else if (Rand.Chance(0.25f))
		{
			parent.HitPoints -= 40;
		}
	}
}
