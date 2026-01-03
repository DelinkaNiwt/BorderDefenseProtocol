using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace AncotLibrary;

public class HediffComp_TurretGun : HediffComp, IAttackTargetSearcher
{
	public static Material ForcedTargetLineMat = MaterialPool.MatFrom(GenDraw.LineTexPath, ShaderDatabase.Transparent, AncotLibrarySettings.turretSystem_IndicatorColor_Player);

	public static Material ForcedTargetLineMat_NonPlayer = MaterialPool.MatFrom(GenDraw.LineTexPath, ShaderDatabase.Transparent, AncotLibrarySettings.turretSystem_IndicatorColor_NonPlayer);

	private Texture2D OverrideGizmoIcon;

	public LocalTargetInfo forcedTarget = LocalTargetInfo.Invalid;

	private const int StartShootIntervalTicks = 10;

	private static readonly CachedTexture ToggleTurretIcon = new CachedTexture("UI/Gizmos/ToggleTurret");

	public Thing gun;

	protected int burstCooldownTicksLeft;

	protected int burstWarmupTicksLeft;

	public LocalTargetInfo currentTarget = LocalTargetInfo.Invalid;

	public bool fireAtWill = true;

	private LocalTargetInfo lastAttackedTarget = LocalTargetInfo.Invalid;

	private int lastAttackTargetTick;

	public float curRotation;

	private float? cachedCurRotation;

	public float floatOffset_xAxis = 0f;

	public float floatOffset_yAxis = 0f;

	public float randTime = Rand.Range(0f, 300f);

	private bool IsWarmingup;

	private int warmupStartedTick;

	private Mote aimLineMote;

	private Mote aimChargeMote;

	private Mote aimTargetMote;

	protected Effecter effecter;

	private Sustainer sustainer;

	private bool needsReInitAfterLoad;

	public bool isForcetargetDowned;

	public bool ShowGizmo = true;

	private HediffCompProperties_TurretGun Props => (HediffCompProperties_TurretGun)props;

	public Thing Thing => PawnOwner;

	public Verb CurrentEffectiveVerb => AttackVerb;

	public LocalTargetInfo LastAttackedTarget => lastAttackedTarget;

	public int LastAttackTargetTick => lastAttackTargetTick;

	public CompEquippable GunCompEq => gun.TryGetComp<CompEquippable>();

	public Verb AttackVerb => GunCompEq.PrimaryVerb;

	private bool WarmingUp => burstWarmupTicksLeft > 0;

	public int WarmupTick => gun.GetStatValue(AncotDefOf.Ancot_TurretWarmUpTime).SecondsToTicks();

	public Pawn PawnOwner => parent.pawn;

	private CompMechAutoFight compMechAutoFight => PawnOwner.TryGetComp<CompMechAutoFight>();

	private bool IsAutoFight
	{
		get
		{
			if (compMechAutoFight == null)
			{
				return false;
			}
			return compMechAutoFight.AutoFight;
		}
	}

	public bool CanShoot
	{
		get
		{
			if (PawnOwner != null)
			{
				if (!PawnOwner.Spawned || PawnOwner.Downed || PawnOwner.Dead || !PawnOwner.Awake())
				{
					return false;
				}
				if (!Props.attackUndrafted && PawnOwner.IsPlayerControlled && !PawnOwner.Drafted)
				{
					if (IsAutoFight)
					{
						return fireAtWill || forcedTarget.IsValid;
					}
					return false;
				}
				if (PawnOwner.stances.stunner.Stunned)
				{
					return false;
				}
				if (TurretDestroyed)
				{
					return false;
				}
				if (!fireAtWill && !forcedTarget.IsValid)
				{
					return false;
				}
				CompCanBeDormant compCanBeDormant = PawnOwner.TryGetComp<CompCanBeDormant>();
				if (compCanBeDormant != null && !compCanBeDormant.Awake)
				{
					return false;
				}
				return true;
			}
			return false;
		}
	}

	public bool TurretDestroyed
	{
		get
		{
			if (AttackVerb.verbProps.linkedBodyPartsGroup != null && AttackVerb.verbProps.ensureLinkedBodyPartsGroupAlwaysUsable && PawnCapacityUtility.CalculateNaturalPartsAverageEfficiency(PawnOwner.health.hediffSet, AttackVerb.verbProps.linkedBodyPartsGroup) <= 0f)
			{
				return true;
			}
			return false;
		}
	}

	public bool AutoAttack => Props.autoAttack;

	public bool CanDrawTargetLine
	{
		get
		{
			if (!AncotLibrarySettings.turretSystem_AimingIndicator)
			{
				return false;
			}
			bool flag = PawnOwner.Faction?.IsPlayer ?? false;
			bool flag2 = Find.Selector.SelectedObjects.Contains(PawnOwner);
			if (!flag && !AncotLibrarySettings.turretSystem_AimingIndicator_NonPlayer)
			{
				return false;
			}
			if (AncotLibrarySettings.turretSystem_IndicatorOnlyDrawForced && !forcedTarget.IsValid && flag)
			{
				return false;
			}
			if (AncotLibrarySettings.turretSystem_IndicatorOnlyDrawSelected && !flag2 && flag)
			{
				return false;
			}
			if (AncotLibrarySettings.turretSystem_IndicatorOnlyDrawSelected_NonPlayer && !flag2 && !flag)
			{
				return false;
			}
			return true;
		}
	}

	public void DrawAimPie()
	{
		if (IsWarmingup && currentTarget.IsValid && Find.Selector.SingleSelectedThing == PawnOwner)
		{
			GenDraw.DrawAimPie(PawnOwner, currentTarget, (int)((float)burstWarmupTicksLeft * 1f), 0.2f);
		}
	}

	public void DrawTargetLine()
	{
		if (CanDrawTargetLine && PawnOwner.Spawned && currentTarget.IsValid && (!currentTarget.HasThing || currentTarget.Thing.Spawned))
		{
			Vector3 b = ((!currentTarget.HasThing) ? currentTarget.Cell.ToVector3Shifted() : currentTarget.Thing.TrueCenter());
			Vector3 a = PawnOwner.TrueCenter();
			b.y = AltitudeLayer.MetaOverlays.AltitudeFor();
			a.y = b.y;
			GenDraw.DrawLineBetween(a, b, PawnOwner.IsPlayerControlled ? ForcedTargetLineMat : ForcedTargetLineMat_NonPlayer);
		}
	}

	public override void CompPostMake()
	{
		base.CompPostMake();
		MakeGun();
	}

	private void MakeGun()
	{
		gun = ThingMaker.MakeThing(Props.turretDef);
		UpdateGunVerbs();
	}

	private void UpdateGunVerbs()
	{
		if (PawnOwner == null)
		{
			return;
		}
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

	public override void CompPostTick(ref float severityAdjustment)
	{
		base.CompPostTick(ref severityAdjustment);
		if (burstCooldownTicksLeft > 0)
		{
			burstCooldownTicksLeft--;
		}
		if (!CanShoot)
		{
			if (IsWarmingup)
			{
				ResetWarmup();
			}
			if (forcedTarget.IsValid)
			{
				forcedTarget = LocalTargetInfo.Invalid;
			}
			return;
		}
		if (Props.float_xAxis != null)
		{
			floatOffset_xAxis = Mathf.Sin(((float)Find.TickManager.TicksGame + randTime) * Props.float_xAxis.floatSpeed) * Props.float_xAxis.floatAmplitude;
		}
		if (Props.float_yAxis != null)
		{
			floatOffset_yAxis = Mathf.Sin(((float)Find.TickManager.TicksGame + randTime) * Props.float_yAxis.floatSpeed) * Props.float_yAxis.floatAmplitude;
		}
		if (currentTarget.IsValid)
		{
			curRotation = (currentTarget.Cell.ToVector3Shifted() - PawnOwner.DrawPos).AngleFlat() + Props.angleOffset;
		}
		AttackVerb.VerbTick();
		if (AttackVerb.state == VerbState.Bursting)
		{
			return;
		}
		if (WarmingUp && currentTarget.IsValid)
		{
			if (CanWarmup())
			{
				if (!IsWarmingup)
				{
					warmupStartedTick = Find.TickManager.TicksGame;
					burstWarmupTicksLeft = WarmupTick;
					IsWarmingup = true;
					cachedCurRotation = null;
					InitEffects();
					return;
				}
				EffecterTick();
				burstWarmupTicksLeft--;
				if (burstWarmupTicksLeft == 0)
				{
					IsWarmingup = false;
					AttackVerb.TryStartCastOn(currentTarget, surpriseAttack: false, canHitNonTargetPawns: true, preventFriendlyFire: false, nonInterruptingSelfCast: true);
					lastAttackTargetTick = Find.TickManager.TicksGame;
					lastAttackedTarget = currentTarget;
					effecter?.Cleanup();
				}
				return;
			}
			ResetWarmup();
			ResetCurrentTarget();
		}
		if (burstCooldownTicksLeft > 0 || !PawnOwner.IsHashIntervalTick(10))
		{
			return;
		}
		if (!forcedTarget.IsValid || (forcedTarget.HasThing && !forcedTarget.Thing.Spawned))
		{
			ResetCurrentTarget();
			forcedTarget = LocalTargetInfo.Invalid;
			currentTarget = (Thing)AttackTargetFinder.BestShootTargetFromCurrentPosition(this, TargetScanFlags.NeedThreat | TargetScanFlags.NeedAutoTargetable);
		}
		if (currentTarget.IsValid)
		{
			if (!forcedTarget.IsValid && !AttackVerb.CanHitTargetFrom(PawnOwner.Position, currentTarget))
			{
				ResetCurrentTarget();
			}
			burstWarmupTicksLeft = WarmupTick + 1;
		}
		else
		{
			ResetCurrentTarget();
		}
	}

	public bool CanWarmup()
	{
		if (!PawnOwner.stances.stunner.Stunned)
		{
			if (!isForcetargetDowned && forcedTarget.HasThing && forcedTarget.Thing is Pawn && ((Pawn)forcedTarget.Thing).Downed)
			{
				return false;
			}
			if (currentTarget.HasThing && (!currentTarget.Thing.Spawned || AttackVerb == null || !AttackVerb.CanHitTargetFrom(PawnOwner.Position, currentTarget)))
			{
				return false;
			}
		}
		return true;
	}

	public void InitEffects(bool afterReload = false)
	{
		if (AttackVerb == null)
		{
			return;
		}
		VerbProperties verbProps = AttackVerb.verbProps;
		if (verbProps.soundAiming != null)
		{
			SoundInfo info = SoundInfo.InMap(PawnOwner, MaintenanceType.PerTick);
			info.pitchFactor = 1f;
			sustainer = verbProps.soundAiming.TrySpawnSustainer(info);
		}
		if (verbProps.warmupEffecter != null && PawnOwner != null)
		{
			effecter = verbProps.warmupEffecter.Spawn(AttackVerb.Caster, AttackVerb.Caster.Map);
			effecter.Trigger(PawnOwner, currentTarget.ToTargetInfo(AttackVerb.Caster.Map));
		}
		if (PawnOwner == null)
		{
			return;
		}
		Map map = PawnOwner.Map;
		if (verbProps.aimingLineMote != null)
		{
			Vector3 vector = TargetPos();
			IntVec3 cell = vector.ToIntVec3();
			aimLineMote = MoteMaker.MakeInteractionOverlay(verbProps.aimingLineMote, PawnOwner, new TargetInfo(cell, map), Vector3.zero, vector - cell.ToVector3Shifted());
			if (afterReload)
			{
				aimLineMote?.ForceSpawnTick(warmupStartedTick);
			}
		}
		if (verbProps.aimingChargeMote != null)
		{
			aimChargeMote = MoteMaker.MakeStaticMote(PawnOwner.DrawPos, map, verbProps.aimingChargeMote, 1f, makeOffscreen: true);
			if (afterReload)
			{
				aimChargeMote?.ForceSpawnTick(warmupStartedTick);
			}
		}
		if (verbProps.aimingTargetMote != null)
		{
			aimTargetMote = MoteMaker.MakeStaticMote(currentTarget.CenterVector3, map, verbProps.aimingTargetMote, 1f, makeOffscreen: true);
			if (aimTargetMote != null)
			{
				aimTargetMote.exactRotation = AimDir().ToAngleFlat();
				if (afterReload)
				{
					aimTargetMote.ForceSpawnTick(warmupStartedTick);
				}
			}
		}
		if (verbProps.aimingTargetEffecter != null)
		{
			effecter = verbProps.aimingTargetEffecter.Spawn(new TargetInfo(PawnOwner), currentTarget.ToTargetInfo(map));
		}
	}

	public void EffecterTick()
	{
		if (needsReInitAfterLoad)
		{
			InitEffects(afterReload: true);
			needsReInitAfterLoad = false;
		}
		if (sustainer != null && !sustainer.Ended)
		{
			sustainer.Maintain();
		}
		effecter?.EffectTick(PawnOwner, currentTarget.ToTargetInfo(PawnOwner.Map));
		Vector3 vector = AimDir();
		float exactRotation = vector.AngleFlat();
		bool stunned = PawnOwner.stances.stunner.Stunned;
		if (aimLineMote != null)
		{
			aimLineMote.paused = stunned;
			aimLineMote.Maintain();
			Vector3 vector2 = TargetPos();
			IntVec3 cell = vector2.ToIntVec3();
			((MoteDualAttached)aimLineMote).UpdateTargets(PawnOwner, new TargetInfo(cell, PawnOwner.Map), Vector3.zero, vector2 - cell.ToVector3Shifted());
		}
		if (aimTargetMote != null)
		{
			aimTargetMote.paused = stunned;
			aimTargetMote.exactPosition = currentTarget.CenterVector3;
			aimTargetMote.exactRotation = exactRotation;
			aimTargetMote?.Maintain();
		}
		if (aimChargeMote != null)
		{
			aimChargeMote.paused = stunned;
			aimChargeMote.exactRotation = exactRotation;
			aimChargeMote.exactPosition = PawnOwner.Position.ToVector3Shifted() + vector * AttackVerb.verbProps.aimingChargeMoteOffset;
			aimChargeMote?.Maintain();
		}
	}

	private Vector3 TargetPos()
	{
		VerbProperties verbProps = AttackVerb.verbProps;
		Vector3 result = currentTarget.CenterVector3;
		if (verbProps.aimingLineMoteFixedLength.HasValue)
		{
			result = PawnOwner.DrawPos + AimDir() * verbProps.aimingLineMoteFixedLength.Value;
		}
		return result;
	}

	private Vector3 AimDir()
	{
		Vector3 result = currentTarget.CenterVector3 - PawnOwner.DrawPos;
		result.y = 0f;
		result.Normalize();
		return result;
	}

	private void ResetWarmup()
	{
		IsWarmingup = false;
		burstWarmupTicksLeft = WarmupTick + 1;
	}

	public void ResetCurrentTarget()
	{
		currentTarget = LocalTargetInfo.Invalid;
		burstWarmupTicksLeft = WarmupTick;
	}

	public override void CompExposeData()
	{
		base.CompExposeData();
		string text = (Props.saveKeysPrefix.NullOrEmpty() ? null : (Props.saveKeysPrefix + "_"));
		Scribe_Values.Look(ref burstCooldownTicksLeft, text + "burstCooldownTicksLeft", 0);
		Scribe_Values.Look(ref burstWarmupTicksLeft, text + "burstWarmupTicksLeft", 0);
		Scribe_Values.Look(ref floatOffset_xAxis, text + "floatOffset_xAxis", 0f);
		Scribe_Values.Look(ref floatOffset_yAxis, text + "floatOffset_yAxis", 0f);
		Scribe_Values.Look(ref cachedCurRotation, text + "cachedCurRotation");
		Scribe_Values.Look(ref IsWarmingup, text + "IsWarmingup", defaultValue: false);
		Scribe_Values.Look(ref isForcetargetDowned, text + "isForcetargetDowned", defaultValue: false);
		Scribe_Values.Look(ref needsReInitAfterLoad, text + "needsReInitAfterLoad", defaultValue: false);
		Scribe_Values.Look(ref warmupStartedTick, text + "warmupStartedTick", 0);
		Scribe_TargetInfo.Look(ref currentTarget, text + "currentTarget");
		Scribe_Deep.Look(ref gun, text + "gun");
		Scribe_Deep.Look(ref aimChargeMote, text + "aimChargeMote");
		Scribe_Deep.Look(ref aimLineMote, text + "aimLineMote");
		Scribe_Deep.Look(ref aimTargetMote, text + "aimTargetMote");
		Scribe_Values.Look(ref fireAtWill, text + "fireAtWill", defaultValue: true);
		if (Scribe.mode == LoadSaveMode.PostLoadInit)
		{
			needsReInitAfterLoad = true;
			if (gun == null && PawnOwner != null)
			{
				Log.Error("CompTurrentGun had null gun after loading. Recreating.");
				MakeGun();
			}
			else
			{
				UpdateGunVerbs();
			}
		}
	}
}
