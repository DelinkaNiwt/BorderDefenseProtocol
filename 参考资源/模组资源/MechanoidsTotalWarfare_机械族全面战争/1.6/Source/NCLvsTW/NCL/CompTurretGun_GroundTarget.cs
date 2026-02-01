using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace NCL;

public class CompTurretGun_GroundTarget : ThingComp, IAttackTargetSearcher
{
	public Thing gun;

	protected int burstWarmupTicksLeft;

	protected int burstCooldownTicksLeft;

	protected LocalTargetInfo currentTarget = LocalTargetInfo.Invalid;

	private bool fireAtWill = true;

	private bool holdFire = false;

	private LocalTargetInfo lastAttackedTarget = LocalTargetInfo.Invalid;

	private int lastAttackTargetTick;

	public float curRotation;

	private static readonly CachedTexture ToggleTurretIcon = new CachedTexture("UI/Gizmos/ToggleTurret");

	public Thing Thing => parent;

	public CompProperties_TurretGun Props => (CompProperties_TurretGun)props;

	public Verb CurrentEffectiveVerb => AttackVerb;

	public LocalTargetInfo LastAttackedTarget => lastAttackedTarget;

	public int LastAttackTargetTick => lastAttackTargetTick;

	public CompEquippable GunCompEq => gun?.TryGetComp<CompEquippable>();

	public Verb AttackVerb => GunCompEq?.PrimaryVerb;

	private bool WarmingUp => burstWarmupTicksLeft > 0;

	public bool AutoAttack => Props?.autoAttack ?? true;

	private bool CanShoot
	{
		get
		{
			if (holdFire)
			{
				return false;
			}
			if (gun == null || AttackVerb == null || Props == null)
			{
				return false;
			}
			if (parent is Pawn pawn)
			{
				if (!pawn.Spawned || pawn.Downed || pawn.Dead || !pawn.Awake())
				{
					return false;
				}
				if (pawn.stances?.stunner?.Stunned == true)
				{
					return false;
				}
				if (TurretDestroyed)
				{
					return false;
				}
				if (pawn.IsColonyMechPlayerControlled && !fireAtWill)
				{
					return false;
				}
			}
			return parent.TryGetComp<CompCanBeDormant>()?.Awake ?? true;
		}
	}

	private bool TurretDestroyed
	{
		get
		{
			if (!(parent is Pawn pawn))
			{
				return false;
			}
			Verb attackVerb = AttackVerb;
			if (attackVerb?.verbProps?.linkedBodyPartsGroup == null)
			{
				return false;
			}
			return attackVerb.verbProps.ensureLinkedBodyPartsGroupAlwaysUsable && PawnCapacityUtility.CalculateNaturalPartsAverageEfficiency(pawn.health.hediffSet, attackVerb.verbProps.linkedBodyPartsGroup) <= 0f;
		}
	}

	private LocalTargetInfo GetGroundTarget(LocalTargetInfo original)
	{
		return (original.IsValid && original.Thing != null) ? new LocalTargetInfo(original.Thing.Position) : original;
	}

	public override IEnumerable<Gizmo> CompGetGizmosExtra()
	{
		foreach (Gizmo item in base.CompGetGizmosExtra())
		{
			yield return item;
		}
		if (parent.Faction == Faction.OfPlayer)
		{
			yield return new Command_Toggle
			{
				defaultLabel = "NCL.HoldFire".Translate(),
				defaultDesc = "NCL.HoldFireDesc".Translate(),
				icon = ContentFinder<Texture2D>.Get("UI/Commands/HoldFire"),
				isActive = () => holdFire,
				toggleAction = delegate
				{
					holdFire = !holdFire;
					if (holdFire)
					{
						ResetCurrentTarget();
					}
				}
			};
		}
		if (parent is Pawn pawn && pawn.IsColonyMechPlayerControlled)
		{
			yield return new Command_Toggle
			{
				defaultLabel = "CommandToggleTurret".Translate(),
				defaultDesc = "CommandToggleTurretDesc".Translate(),
				isActive = () => fireAtWill,
				icon = ToggleTurretIcon.Texture,
				toggleAction = delegate
				{
					fireAtWill = !fireAtWill;
				}
			};
		}
	}

	public override void PostPostMake()
	{
		base.PostPostMake();
		MakeGun();
	}

	private void MakeGun()
	{
		if (Props?.turretDef != null)
		{
			try
			{
				gun = ThingMaker.MakeThing(Props.turretDef);
				UpdateGunVerbs();
			}
			catch (Exception arg)
			{
				Log.Error($"Failed to create turret gun: {arg}");
			}
		}
	}

	private void UpdateGunVerbs()
	{
		CompEquippable compEq = GunCompEq;
		if (compEq == null)
		{
			return;
		}
		foreach (Verb verb in compEq.AllVerbs)
		{
			if (verb == null || parent == null)
			{
				continue;
			}
			verb.caster = parent;
			verb.castCompleteCallback = delegate
			{
				if (this?.AttackVerb?.verbProps != null)
				{
					burstCooldownTicksLeft = AttackVerb.verbProps.defaultCooldownTime.SecondsToTicks();
				}
			};
		}
	}

	public override void CompTick()
	{
		if (!CanShoot)
		{
			return;
		}
		Verb attackVerb = AttackVerb;
		if (attackVerb == null)
		{
			return;
		}
		LocalTargetInfo groundTarget = GetGroundTarget(currentTarget);
		if (groundTarget.IsValid)
		{
			Vector3 targetPos = groundTarget.Cell.ToVector3Shifted();
			Vector3 parentPos = parent.DrawPos;
			if (parentPos != Vector3.zero)
			{
				curRotation = (targetPos - parentPos).AngleFlat() + Props.angleOffset;
			}
		}
		if (attackVerb.caster != null)
		{
			attackVerb.VerbTick();
		}
		if (attackVerb.state == VerbState.Bursting)
		{
			return;
		}
		if (WarmingUp)
		{
			burstWarmupTicksLeft--;
			if (burstWarmupTicksLeft == 0)
			{
				attackVerb.TryStartCastOn(groundTarget, surpriseAttack: false, canHitNonTargetPawns: true, preventFriendlyFire: false, nonInterruptingSelfCast: true);
				lastAttackTargetTick = Find.TickManager.TicksGame;
				lastAttackedTarget = groundTarget;
			}
			return;
		}
		if (burstCooldownTicksLeft > 0)
		{
			burstCooldownTicksLeft--;
		}
		if (burstCooldownTicksLeft <= 0 && parent.IsHashIntervalTick(10))
		{
			IAttackTarget attackTarget = AttackTargetFinder.BestShootTargetFromCurrentPosition(this, TargetScanFlags.NeedThreat | TargetScanFlags.NeedAutoTargetable);
			currentTarget = ((attackTarget != null) ? GetGroundTarget(new LocalTargetInfo((Thing)attackTarget)) : LocalTargetInfo.Invalid);
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

	private void ResetCurrentTarget()
	{
		currentTarget = LocalTargetInfo.Invalid;
		burstWarmupTicksLeft = 0;
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref burstCooldownTicksLeft, "burstCooldownTicksLeft", 0);
		Scribe_Values.Look(ref burstWarmupTicksLeft, "burstWarmupTicksLeft", 0);
		Scribe_Values.Look(ref holdFire, "holdFire", defaultValue: false);
		Scribe_TargetInfo.Look(ref currentTarget, "currentTarget");
		Scribe_Deep.Look(ref gun, "gun");
		Scribe_Values.Look(ref fireAtWill, "fireAtWill", defaultValue: true);
		if (Scribe.mode == LoadSaveMode.PostLoadInit)
		{
			if (gun == null)
			{
				Log.Warning("CompTurretGun_GroundTarget had null gun after loading. Recreating.");
				MakeGun();
			}
			UpdateGunVerbs();
		}
	}
}
