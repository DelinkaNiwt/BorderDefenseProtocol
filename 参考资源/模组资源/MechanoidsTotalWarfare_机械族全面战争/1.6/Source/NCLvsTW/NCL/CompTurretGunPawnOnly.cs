using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace NCL;

public class CompTurretGunPawnOnly : ThingComp, IAttackTargetSearcher
{
	public Thing gun;

	protected int burstWarmupTicksLeft;

	protected int burstCooldownTicksLeft;

	protected LocalTargetInfo currentTarget = LocalTargetInfo.Invalid;

	private bool fireAtWill = true;

	private LocalTargetInfo lastAttackedTarget = LocalTargetInfo.Invalid;

	private int lastAttackTargetTick;

	public float curRotation;

	private static readonly CachedTexture ToggleTurretIcon = new CachedTexture("UI/Gizmos/ToggleTurret");

	public Thing Thing => parent;

	public CompProperties_TurretGunPawnOnly Props => props as CompProperties_TurretGunPawnOnly;

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
			bool result;
			if (gun == null || AttackVerb == null || Props == null)
			{
				result = false;
			}
			else
			{
				if (parent is Pawn pawn)
				{
					if (!pawn.Spawned || pawn.Downed || pawn.Dead || !pawn.Awake())
					{
						return false;
					}
					if (pawn.stances.stunner.Stunned)
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
				result = parent.TryGetComp<CompCanBeDormant>()?.Awake ?? true;
			}
			return result;
		}
	}

	private bool TurretDestroyed
	{
		get
		{
			if (parent is Pawn pawn)
			{
				Verb attackVerb = AttackVerb;
				bool flag = attackVerb != null && attackVerb.verbProps?.linkedBodyPartsGroup != null;
				if (flag && AttackVerb.verbProps.ensureLinkedBodyPartsGroupAlwaysUsable)
				{
					return PawnCapacityUtility.CalculateNaturalPartsAverageEfficiency(pawn.health.hediffSet, AttackVerb.verbProps.linkedBodyPartsGroup) <= 0f;
				}
			}
			return false;
		}
	}

	public override void PostPostMake()
	{
		base.PostPostMake();
		MakeGun();
	}

	private void MakeGun()
	{
		if (Props?.turretDef == null)
		{
			Log.Error("CompTurretGunPawnOnly: turretDef is null in properties");
			return;
		}
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

	private void UpdateGunVerbs()
	{
		if (gun == null)
		{
			return;
		}
		CompEquippable gunCompEq = GunCompEq;
		if (gunCompEq == null)
		{
			return;
		}
		foreach (Verb verb in gunCompEq.AllVerbs)
		{
			verb.caster = parent;
			verb.castCompleteCallback = delegate
			{
				burstCooldownTicksLeft = AttackVerb.verbProps.defaultCooldownTime.SecondsToTicks();
			};
		}
	}

	private bool IsValidEnemyPawn(Thing target)
	{
		if (target == null)
		{
			return false;
		}
		return target is Pawn { Dead: false, Downed: false, Faction: not null } pawn && parent.Faction != null && pawn.Faction.HostileTo(parent.Faction);
	}

	public override void CompTick()
	{
		if (!CanShoot || Props == null)
		{
			return;
		}
		if (currentTarget.IsValid)
		{
			curRotation = (currentTarget.Cell.ToVector3Shifted() - parent.DrawPos).AngleFlat() + Props.angleOffset;
		}
		AttackVerb?.VerbTick();
		Verb attackVerb2 = AttackVerb;
		if (attackVerb2 != null && attackVerb2.state == VerbState.Bursting)
		{
			return;
		}
		if (WarmingUp)
		{
			burstWarmupTicksLeft--;
			if (burstWarmupTicksLeft == 0)
			{
				AttackVerb.TryStartCastOn(currentTarget, surpriseAttack: false, canHitNonTargetPawns: true, preventFriendlyFire: false, nonInterruptingSelfCast: true);
				lastAttackTargetTick = Find.TickManager.TicksGame;
				lastAttackedTarget = currentTarget;
			}
			return;
		}
		if (burstCooldownTicksLeft > 0)
		{
			burstCooldownTicksLeft--;
		}
		if (burstCooldownTicksLeft <= 0 && parent.IsHashIntervalTick(10))
		{
			IAttackTarget attackTarget = AttackTargetFinder.BestAttackTarget(this, TargetScanFlags.NeedThreat | TargetScanFlags.NeedAutoTargetable, (Thing t) => IsValidEnemyPawn(t));
			currentTarget = ((attackTarget != null) ? new LocalTargetInfo((Thing)attackTarget) : LocalTargetInfo.Invalid);
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

	public override IEnumerable<Gizmo> CompGetGizmosExtra()
	{
		return base.CompGetGizmosExtra();
	}

	public override List<PawnRenderNode> CompRenderNodes()
	{
		if (Props?.renderNodeProperties == null)
		{
			return base.CompRenderNodes();
		}
		if (!(parent is Pawn pawn))
		{
			return base.CompRenderNodes();
		}
		List<PawnRenderNode> list = new List<PawnRenderNode>();
		foreach (PawnRenderNodeProperties pawnRenderNodeProperties in Props.renderNodeProperties)
		{
			if (pawnRenderNodeProperties?.nodeClass == null)
			{
				continue;
			}
			try
			{
				Type nodeClass = pawnRenderNodeProperties.nodeClass;
				object[] array = new object[3] { pawn, pawnRenderNodeProperties, null };
				int num = 2;
				object obj = pawn.Drawer?.renderer?.renderTree;
				array[num] = obj;
				PawnRenderNode pawnRenderNode = Activator.CreateInstance(nodeClass, array) as PawnRenderNode;
				if (pawnRenderNode is PawnRenderNode_TurretPawnOnly pawnRenderNode_TurretPawnOnly)
				{
					pawnRenderNode_TurretPawnOnly.turretComp = this;
				}
				list.Add(pawnRenderNode);
			}
			catch (Exception arg)
			{
				Log.Error($"Failed to create render node: {arg}");
			}
		}
		return (list.Count > 0) ? list : base.CompRenderNodes();
	}

	public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
	{
		return base.SpecialDisplayStats();
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref burstCooldownTicksLeft, "burstCooldownTicksLeft", 0);
		Scribe_Values.Look(ref burstWarmupTicksLeft, "burstWarmupTicksLeft", 0);
		Scribe_TargetInfo.Look(ref currentTarget, "currentTarget");
		Scribe_Deep.Look(ref gun, "gun");
		Scribe_Values.Look(ref fireAtWill, "fireAtWill", defaultValue: true);
		if (Scribe.mode == LoadSaveMode.PostLoadInit)
		{
			if (gun == null)
			{
				Log.Warning("Recreating missing gun for CompTurretGunPawnOnly");
				MakeGun();
			}
			else
			{
				UpdateGunVerbs();
			}
			if (gun == null)
			{
				Log.Error("Cannot recreate gun: turretDef is null or creation failed");
			}
		}
	}
}
