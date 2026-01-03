using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace AncotLibrary;

[StaticConstructorOnStartup]
public class CompTurretGun_Building : ThingComp, IAttackTargetSearcher
{
	public static Material ForcedTargetLineMat = MaterialPool.MatFrom(GenDraw.LineTexPath, ShaderDatabase.Transparent, AncotLibrarySettings.turretSystem_IndicatorColor_Player);

	public static Material ForcedTargetLineMat_NonPlayer = MaterialPool.MatFrom(GenDraw.LineTexPath, ShaderDatabase.Transparent, AncotLibrarySettings.turretSystem_IndicatorColor_NonPlayer);

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

	private bool IsWarmingup;

	private int warmupStartedTick;

	private Mote aimLineMote;

	private Mote aimChargeMote;

	private Mote aimTargetMote;

	protected Effecter effecter;

	private Sustainer sustainer;

	private bool needsReInitAfterLoad;

	public bool isForcetargetDowned;

	public Thing Thing => parent;

	public Building_Aerocraft Aerocraft => Thing as Building_Aerocraft;

	public bool IsFlying => Aerocraft == null || Aerocraft.FlightState == AerocraftState.Flying;

	public float CurRotation
	{
		get
		{
			if (currentTarget.IsValid)
			{
				return (currentTarget.Cell.ToVector3Shifted() - parent.DrawPos).AngleFlat() + Props.angleOffset;
			}
			if (!(parent is Building_Aerocraft { FlightState: not AerocraftState.Grounded, CurDirection: var curDirection }))
			{
				return parent.Rotation.AsAngle;
			}
			return curDirection;
		}
	}

	public CompProperties_TurretGun_Building Props => (CompProperties_TurretGun_Building)props;

	public Verb CurrentEffectiveVerb => AttackVerb;

	public LocalTargetInfo LastAttackedTarget => lastAttackedTarget;

	public int LastAttackTargetTick => lastAttackTargetTick;

	public CompEquippable GunCompEq => gun.TryGetComp<CompEquippable>();

	public Verb AttackVerb => GunCompEq.PrimaryVerb;

	private bool WarmingUp => burstWarmupTicksLeft > 0;

	public int WarmupTick => gun.GetStatValue(AncotDefOf.Ancot_TurretWarmUpTime).SecondsToTicks();

	private CompThingCarrier_Custom CompThingCarrier => parent.TryGetComp<CompThingCarrier_Custom>();

	private CompRefuelable CompRefuelable => parent.TryGetComp<CompRefuelable>();

	private float RemainingCharges
	{
		get
		{
			if (CompThingCarrier != null)
			{
				return CompThingCarrier.IngredientCount;
			}
			if (CompRefuelable != null)
			{
				return CompRefuelable.Fuel;
			}
			return 0f;
		}
	}

	public bool ShowGizmo => parent.TryGetComp<CompIntegrationWeaponSystem>()?.activate ?? true;

	public bool CanShoot
	{
		get
		{
			if (parent.Spawned)
			{
				if (Props.consumeChargeAmountPerShot != 0f && RemainingCharges < Props.consumeChargeAmountPerShot)
				{
					return false;
				}
				if (!fireAtWill && !forcedTarget.IsValid)
				{
					return false;
				}
				if (!IsFlying)
				{
					return false;
				}
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
			bool flag = parent.Faction?.IsPlayer ?? false;
			bool flag2 = Find.Selector.SelectedObjects.Contains(parent);
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

	public override void DrawAt(Vector3 drawLoc, bool flip = false)
	{
		Quaternion quaternion = Quaternion.AngleAxis(CurRotation, Vector3.up);
		Vector3 vector = quaternion * new Vector3(Props.turretDrawOffset.x, 0f, Props.turretDrawOffset.y);
		if (parent is Building_Aerocraft building_Aerocraft)
		{
			Quaternion quaternion2 = Quaternion.AngleAxis(building_Aerocraft.CurDirection, Vector3.up);
			if (!currentTarget.IsValid && building_Aerocraft.FlightState == AerocraftState.Grounded)
			{
				quaternion = quaternion2;
			}
			vector = quaternion2 * new Vector3(Props.turretDrawOffset.x, 0f, Props.turretDrawOffset.y);
		}
		Vector3 pos = parent.DrawPos + vector + new Vector3(0f, -0.1f, 0f);
		Matrix4x4 matrix = Matrix4x4.TRS(pos, quaternion, new Vector3(Props.turretDrawScale, 0f, Props.turretDrawScale));
		Graphics.DrawMesh(MeshPool.plane10, matrix, gun.Graphic.MatAt(Rot4.North), 0);
	}

	public override void PostDrawExtraSelectionOverlays()
	{
		DrawTargetLine();
		DrawAimPie();
		GenDraw.DrawRadiusRing(parent.Position, AttackVerb.EffectiveRange);
	}

	private void DrawAimPie()
	{
		if (IsWarmingup && currentTarget.IsValid && Find.Selector.SingleSelectedThing == parent)
		{
			GenDraw.DrawAimPie(parent, currentTarget, (int)((float)burstWarmupTicksLeft * 1f), 0.2f);
		}
	}

	public void DrawTargetLine()
	{
		if (CanDrawTargetLine && parent.Spawned && currentTarget.IsValid && (!currentTarget.HasThing || currentTarget.Thing.Spawned))
		{
			Vector3 b = ((!currentTarget.HasThing) ? currentTarget.Cell.ToVector3Shifted() : currentTarget.Thing.TrueCenter());
			Vector3 a = parent.TrueCenter();
			b.y = AltitudeLayer.MetaOverlays.AltitudeFor();
			a.y = b.y;
			GenDraw.DrawLineBetween(a, b, parent.Faction.IsPlayer ? ForcedTargetLineMat : ForcedTargetLineMat_NonPlayer);
		}
	}

	public override void PostPostMake()
	{
		base.PostPostMake();
		MakeGun();
	}

	private void MakeGun()
	{
		gun = ThingMaker.MakeThing(Props.turretDef);
		UpdateGunVerbs();
	}

	private void UpdateGunVerbs()
	{
		List<Verb> allVerbs = gun.TryGetComp<CompEquippable>().AllVerbs;
		for (int i = 0; i < allVerbs.Count; i++)
		{
			Verb verb = allVerbs[i];
			verb.caster = parent;
			verb.castCompleteCallback = delegate
			{
				burstCooldownTicksLeft = AttackVerb.verbProps.defaultCooldownTime.SecondsToTicks();
			};
		}
	}

	public override void CompTick()
	{
		base.CompTick();
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
		AttackVerb.VerbTick();
		if (AttackVerb.state == VerbState.Bursting)
		{
			return;
		}
		if (WarmingUp)
		{
			if (!IsWarmingup && currentTarget.IsValid)
			{
				warmupStartedTick = Find.TickManager.TicksGame;
				burstWarmupTicksLeft = WarmupTick;
				IsWarmingup = true;
				InitEffects();
			}
			if (IsWarmingup && !currentTarget.IsValid)
			{
				ResetWarmup();
				ResetCurrentTarget();
				effecter?.Cleanup();
			}
			if (IsWarmingup && currentTarget.IsValid)
			{
				EffecterTick();
				burstWarmupTicksLeft--;
				if (!CanWarmup())
				{
					ResetWarmup();
					ResetCurrentTarget();
				}
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
		}
		if (burstCooldownTicksLeft <= 0 && parent.IsHashIntervalTick(10))
		{
			if (!forcedTarget.IsValid || (forcedTarget.HasThing && !forcedTarget.Thing.Spawned))
			{
				ResetCurrentTarget();
				forcedTarget = LocalTargetInfo.Invalid;
				currentTarget = (Thing)AttackTargetFinder.BestShootTargetFromCurrentPosition(this, TargetScanFlags.NeedThreat | TargetScanFlags.NeedAutoTargetable);
			}
			if (currentTarget.IsValid)
			{
				burstWarmupTicksLeft = WarmupTick + 1;
			}
			else
			{
				ResetCurrentTarget();
			}
		}
	}

	private bool CanWarmup()
	{
		if (!isForcetargetDowned && forcedTarget.HasThing && forcedTarget.Thing is Pawn && ((Pawn)forcedTarget.Thing).Downed)
		{
			return false;
		}
		if (currentTarget.HasThing && (!currentTarget.Thing.Spawned || AttackVerb == null || !AttackVerb.CanHitTargetFrom(parent.Position, currentTarget)))
		{
			return false;
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
			SoundInfo info = SoundInfo.InMap(parent, MaintenanceType.PerTick);
			info.pitchFactor = 1f;
			sustainer = verbProps.soundAiming.TrySpawnSustainer(info);
		}
		if (verbProps.warmupEffecter != null && parent != null)
		{
			effecter = verbProps.warmupEffecter.Spawn(AttackVerb.Caster, AttackVerb.Caster.Map);
			effecter.Trigger(parent, currentTarget.ToTargetInfo(AttackVerb.Caster.Map));
		}
		if (parent == null)
		{
			return;
		}
		Map map = parent.Map;
		if (verbProps.aimingLineMote != null)
		{
			Vector3 vector = TargetPos();
			IntVec3 cell = vector.ToIntVec3();
			aimLineMote = MoteMaker.MakeInteractionOverlay(verbProps.aimingLineMote, parent, new TargetInfo(cell, map), Vector3.zero, vector - cell.ToVector3Shifted());
			if (afterReload)
			{
				aimLineMote?.ForceSpawnTick(warmupStartedTick);
			}
		}
		if (verbProps.aimingChargeMote != null)
		{
			aimChargeMote = MoteMaker.MakeStaticMote(parent.DrawPos, map, verbProps.aimingChargeMote, 1f, makeOffscreen: true);
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
			effecter = verbProps.aimingTargetEffecter.Spawn(new TargetInfo(parent), currentTarget.ToTargetInfo(map));
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
		effecter?.EffectTick(parent, currentTarget.ToTargetInfo(parent.Map));
		Vector3 vector = AimDir();
		float exactRotation = vector.AngleFlat();
		bool paused = parent.GetComp<CompStunnable>()?.StunHandler.Stunned ?? false;
		if (aimLineMote != null)
		{
			aimLineMote.paused = paused;
			aimLineMote.Maintain();
			Vector3 vector2 = TargetPos();
			IntVec3 cell = vector2.ToIntVec3();
			((MoteDualAttached)aimLineMote).UpdateTargets(parent, new TargetInfo(cell, parent.Map), Vector3.zero, vector2 - cell.ToVector3Shifted());
		}
		if (aimTargetMote != null)
		{
			aimTargetMote.paused = paused;
			aimTargetMote.exactPosition = currentTarget.CenterVector3;
			aimTargetMote.exactRotation = exactRotation;
			aimTargetMote?.Maintain();
		}
		if (aimChargeMote != null)
		{
			aimChargeMote.paused = paused;
			aimChargeMote.exactRotation = exactRotation;
			aimChargeMote.exactPosition = parent.Position.ToVector3Shifted() + vector * AttackVerb.verbProps.aimingChargeMoteOffset;
			aimChargeMote?.Maintain();
		}
	}

	private Vector3 TargetPos()
	{
		VerbProperties verbProps = AttackVerb.verbProps;
		Vector3 result = currentTarget.CenterVector3;
		if (verbProps.aimingLineMoteFixedLength.HasValue)
		{
			result = parent.DrawPos + AimDir() * verbProps.aimingLineMoteFixedLength.Value;
		}
		return result;
	}

	private Vector3 AimDir()
	{
		Vector3 result = currentTarget.CenterVector3 - parent.DrawPos;
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

	public override IEnumerable<Gizmo> CompGetGizmosExtra()
	{
		foreach (Gizmo item in base.CompGetGizmosExtra())
		{
			yield return item;
		}
		if (IsFlying)
		{
			Gizmo_TurretGunBuilding command_Toggle = new Gizmo_TurretGunBuilding
			{
				defaultLabel = (Props.gizmoLable ?? gun.LabelCap) + Props.statLabelPostfix,
				defaultDesc = (Props.gizmoDesc ?? null),
				range = CurrentEffectiveVerb.EffectiveRange,
				minRange = CurrentEffectiveVerb.verbProps.minRange,
				forcedTarget = forcedTarget,
				cooldownTicksTotal = AttackVerb.verbProps.defaultCooldownTime.SecondsToTicks(),
				cooldownTicksRemaining = burstCooldownTicksLeft,
				targetingParams = AttackVerb.targetParams,
				icon = ((Props.gizmoIconPath != null) ? ContentFinder<Texture2D>.Get(Props.gizmoIconPath) : ToggleTurretIcon.Texture)
			};
			command_Toggle.AddComp(this);
			yield return command_Toggle;
		}
	}

	public void ShotOnce()
	{
		if (Props.consumeChargeAmountPerShot != 0f)
		{
			CompThingCarrier?.TryRemoveThingInCarrier((int)Props.consumeChargeAmountPerShot);
			CompRefuelable.ConsumeFuel(Props.consumeChargeAmountPerShot);
		}
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		string text = (Props.saveKeysPrefix.NullOrEmpty() ? null : (Props.saveKeysPrefix + "_"));
		Scribe_Values.Look(ref burstCooldownTicksLeft, text + "burstCooldownTicksLeft", 0);
		Scribe_Values.Look(ref burstWarmupTicksLeft, text + "burstWarmupTicksLeft", 0);
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
			if (gun == null && parent != null)
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
