using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace TOT_DLL_test;

[StaticConstructorOnStartup]
public class Building_CMCTurretGun_MainBattery : Building_Turret
{
	public CMCTurretTop_MainBattery turrettop;

	public const int highpowerCost = 3;

	public float rotationVelocity;

	public int burstCooldownTicksLeft;

	public int burstWarmupTicksLeft = 6;

	public LocalTargetInfo currentTargetInt = LocalTargetInfo.Invalid;

	public bool holdFire;

	public bool burstActivated;

	public Thing gun;

	public bool IsTargrtingWorld;

	public CompPowerTrader powerComp;

	public CompCanBeDormant dormantComp;

	public CompInitiatable initiatableComp;

	public CompMannable mannableComp;

	public CompInteractable interactableComp;

	public CompRefuelable refuelableComp;

	public Effecter progressBarEffecter;

	public CompMechPowerCell powerCellComp;

	public static Material ForcedTargetLineMat = MaterialPool.MatFrom(GenDraw.LineTexPath, ShaderDatabase.Transparent, new Color(1f, 0.5f, 0.5f));

	public virtual CMCTurretTop_MainBattery TurretTop => turrettop;

	public bool CanToggleHoldFire => PlayerControlled;

	public bool PlayerControlled => (base.Faction == Faction.OfPlayer || MannedByColonist) && !MannedByNonColonist && !IsActivable;

	public bool MannedByColonist => mannableComp != null && mannableComp.ManningPawn != null && mannableComp.ManningPawn.Faction == Faction.OfPlayer;

	public bool MannedByNonColonist => mannableComp != null && mannableComp.ManningPawn != null && mannableComp.ManningPawn.Faction != Faction.OfPlayer;

	public bool IsActivable => interactableComp != null;

	public override Verb AttackVerb => GunCompEq.PrimaryVerb;

	public CompEquippable GunCompEq => gun.TryGetComp<CompEquippable>();

	public bool IsMortar => def.building.IsMortar;

	protected virtual bool HideForceTargetGizmo => false;

	public override LocalTargetInfo CurrentTarget => currentTargetInt;

	public virtual bool CanSetForcedTarget
	{
		get
		{
			if (base.Faction == Faction.OfPlayer)
			{
				return true;
			}
			return false;
		}
	}

	public bool WarmingUp => burstWarmupTicksLeft > 0;

	public bool CanExtractShell
	{
		get
		{
			if (!PlayerControlled)
			{
				return false;
			}
			return gun.TryGetComp<CompChangeableProjectile>()?.Loaded ?? false;
		}
	}

	public bool Active => (powerComp == null || powerComp.PowerOn) && (initiatableComp == null || initiatableComp.Initiated) && (interactableComp == null || burstActivated) && (powerCellComp == null || !powerCellComp.depleted);

	public bool IsMortarOrProjectileFliesOverhead => AttackVerb.ProjectileFliesOverhead() || IsMortar;

	protected override void DrawAt(Vector3 drawLoc, bool flip = false)
	{
		Vector3 zero = Vector3.zero;
		float recoilAngleOffset = 0f;
		turrettop.DrawTurret(drawLoc, zero, recoilAngleOffset);
		base.DrawAt(drawLoc, flip);
	}

	public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
	{
		IEnumerable<StatDrawEntry> enumerable = base.SpecialDisplayStats();
		if (enumerable != null)
		{
			foreach (StatDrawEntry item in enumerable)
			{
				yield return item;
			}
		}
		List<Verb> allVerbs = gun.TryGetComp<CompEquippable>().AllVerbs;
		for (int i = 0; i < allVerbs.Count; i++)
		{
			Verb verb = allVerbs[i];
			if (verb is Verb_ShootMultiTarget)
			{
				Verb_ShootMultiTarget verb_ShootMultiTarget = (Verb_ShootMultiTarget)verb;
				yield return new StatDrawEntry(StatCategoryDefOf.Weapon_Ranged, "StatShootNum_Label".Translate(), "StatShootNum_Desc".Translate(verb_ShootMultiTarget.ShootNum * verb_ShootMultiTarget.verbProps.burstShotCount), "StatShootNum_Text".Translate(verb_ShootMultiTarget.ShootNum * verb_ShootMultiTarget.verbProps.burstShotCount), 25);
			}
		}
	}

	public void MakeGun()
	{
		gun = ThingMaker.MakeThing(def.building.turretGunDef);
		UpdateGunVerbs();
	}

	protected void BurstComplete()
	{
		burstCooldownTicksLeft = BurstCooldownTime().SecondsToTicks();
	}

	public float CalculateRecoil()
	{
		int num = BurstCooldownTime().SecondsToTicks() - burstCooldownTicksLeft;
		if (num > 600)
		{
			return 0f;
		}
		if (num <= 20)
		{
			float num2 = (float)num / 20f;
			return 1.5f * num2 * num2;
		}
		if (num <= 80)
		{
			return 1.5f;
		}
		float num3 = (float)(num - 80) / 520f;
		float num4 = num3 * num3;
		float num5 = 1f - (1f - num3) * (1f - num3);
		float num6 = num4 * 0.4f + num5 * 0.6f;
		return 1.5f * (1f - num6);
	}

	protected float BurstCooldownTime()
	{
		if (def.building.turretBurstCooldownTime >= 0f)
		{
			return def.building.turretBurstCooldownTime;
		}
		return AttackVerb.verbProps.defaultCooldownTime;
	}

	public override AcceptanceReport ClaimableBy(Faction by)
	{
		AcceptanceReport result = base.ClaimableBy(by);
		if (!result.Accepted)
		{
			return result;
		}
		if (mannableComp != null && mannableComp.ManningPawn != null)
		{
			return false;
		}
		if (Active && mannableComp == null)
		{
			return false;
		}
		if (((dormantComp != null && !dormantComp.Awake) || (initiatableComp != null && !initiatableComp.Initiated)) && (powerComp == null || powerComp.PowerOn))
		{
			return false;
		}
		return true;
	}

	public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
	{
		base.DeSpawn(mode);
		ResetCurrentTarget();
		progressBarEffecter?.Cleanup();
	}

	public override void DrawExtraSelectionOverlays()
	{
		base.DrawExtraSelectionOverlays();
		if (!IsTargrtingWorld)
		{
			float num = AttackVerb.verbProps.EffectiveMinRange(allowAdjacentShot: true);
			if (num < 51f && num > 0.1f)
			{
				GenDraw.DrawCircleOutline(DrawPos, num, SimpleColor.White);
				GenDraw.DrawCircleOutline(DrawPos, num - 0.1f, SimpleColor.White);
				GenDraw.DrawCircleOutline(DrawPos, num - 0.2f, SimpleColor.White);
			}
			if (forcedTarget.IsValid && (!forcedTarget.HasThing || forcedTarget.Thing.Spawned))
			{
				Vector3 b = ((!forcedTarget.HasThing) ? forcedTarget.Cell.ToVector3Shifted() : forcedTarget.Thing.TrueCenter());
				Vector3 a = this.TrueCenter();
				b.y = AltitudeLayer.MetaOverlays.AltitudeFor();
				a.y = b.y;
				GenDraw.DrawLineBetween(a, b, Building_TurretGun.ForcedTargetLineMat);
			}
		}
	}

	public Building_CMCTurretGun_MainBattery()
	{
		turrettop = new CMCTurretTop_MainBattery(this);
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref rotationVelocity, "rotationVelocity", 1f);
		Scribe_Values.Look(ref burstCooldownTicksLeft, "burstCooldownTicksLeft", 0);
		Scribe_Values.Look(ref burstWarmupTicksLeft, "burstWarmupTicksLeft", 0);
		Scribe_TargetInfo.Look(ref currentTargetInt, "currentTarget");
		Scribe_Values.Look(ref holdFire, "holdFire", defaultValue: false);
		Scribe_Values.Look(ref burstActivated, "burstActivated", defaultValue: false);
		Scribe_Values.Look(ref IsTargrtingWorld, "IsTargrtingWorld", defaultValue: false);
		Scribe_Deep.Look(ref gun, "gun");
		Scribe_Values.Look(ref turrettop.curRotationInt, "curRotationInt", 0f);
		Scribe_Values.Look(ref turrettop.destRotationInt, "destRotationInt", 0f);
		BackCompatibility.PostExposeData(this);
		if (Scribe.mode == LoadSaveMode.PostLoadInit)
		{
			if (gun == null)
			{
				Log.Error("Turret had null gun after loading. Recreating.");
				MakeGun();
			}
			else
			{
				UpdateGunVerbs();
			}
		}
	}

	public void ExtractShell()
	{
		GenPlace.TryPlaceThing(gun.TryGetComp<CompChangeableProjectile>().RemoveShell(), base.Position, base.Map, ThingPlaceMode.Near, null, null, default(Rot4));
	}

	public override IEnumerable<Gizmo> GetGizmos()
	{
		foreach (Gizmo gizmo in base.GetGizmos())
		{
			yield return gizmo;
		}
		if (CanExtractShell)
		{
			CompChangeableProjectile compChangeableProjectile = gun.TryGetComp<CompChangeableProjectile>();
			yield return new Command_Action
			{
				defaultLabel = "CommandExtractShell".Translate(),
				defaultDesc = "CommandExtractShellDesc".Translate(),
				icon = compChangeableProjectile.LoadedShell.uiIcon,
				iconAngle = compChangeableProjectile.LoadedShell.uiIconAngle,
				iconOffset = compChangeableProjectile.LoadedShell.uiIconOffset,
				iconDrawScale = GenUI.IconDrawScale(compChangeableProjectile.LoadedShell),
				action = delegate
				{
					ExtractShell();
				}
			};
		}
		CompChangeableProjectile compChangeableProjectile2 = gun.TryGetComp<CompChangeableProjectile>();
		if (compChangeableProjectile2 != null)
		{
			StorageSettings storeSettings = compChangeableProjectile2.GetStoreSettings();
			foreach (Gizmo item in StorageSettingsClipboard.CopyPasteGizmosFor(storeSettings))
			{
				yield return item;
			}
		}
		if (!HideForceTargetGizmo && !IsTargrtingWorld)
		{
			if (CanSetForcedTarget)
			{
				Command_VerbTarget command_VerbTarget = new Command_VerbTarget
				{
					defaultLabel = "CommandSetForceAttackTarget".Translate(),
					defaultDesc = "CommandSetForceAttackTargetDesc".Translate(),
					icon = ContentFinder<Texture2D>.Get("UI/Commands/Attack"),
					verb = AttackVerb,
					hotKey = KeyBindingDefOf.Misc4,
					drawRadius = false,
					requiresAvailableVerb = false
				};
				if (base.Spawned && IsMortarOrProjectileFliesOverhead && base.Position.Roofed(base.Map))
				{
					command_VerbTarget.Disable("CannotFire".Translate() + ": " + "Roofed".Translate().CapitalizeFirst());
				}
				yield return command_VerbTarget;
			}
			if (forcedTarget.IsValid)
			{
				Command_Action command_Action = new Command_Action
				{
					defaultLabel = "CommandStopForceAttack".Translate(),
					defaultDesc = "CommandStopForceAttackDesc".Translate(),
					icon = ContentFinder<Texture2D>.Get("UI/Commands/Halt"),
					action = delegate
					{
						ResetForcedTarget();
						SoundDefOf.Tick_Low.PlayOneShotOnCamera();
					}
				};
				if (!forcedTarget.IsValid)
				{
					command_Action.Disable("CommandStopAttackFailNotForceAttacking".Translate());
				}
				command_Action.hotKey = KeyBindingDefOf.Misc5;
				yield return command_Action;
			}
		}
		if (CanToggleHoldFire)
		{
			yield return new Command_Toggle
			{
				defaultLabel = "CommandHoldFire".Translate(),
				defaultDesc = "CommandHoldFireDesc".Translate(),
				icon = ContentFinder<Texture2D>.Get("UI/Commands/HoldFire"),
				hotKey = KeyBindingDefOf.Misc6,
				toggleAction = delegate
				{
					holdFire = !holdFire;
					if (holdFire)
					{
						ResetForcedTarget();
					}
				},
				isActive = () => holdFire
			};
		}
		if (IsTargrtingWorld)
		{
			yield return new Command_Action
			{
				defaultLabel = "CMC_CommandTargetMapFromWorld".Translate(),
				defaultDesc = "CMC_CommandTarggetMapFromWorldDesc".Translate(),
				icon = ContentFinder<Texture2D>.Get("UI/UI_TargetMap"),
				action = delegate
				{
					IsTargrtingWorld = false;
					powerComp.powerOutputInt = -10000f;
					SoundDefOf.Tick_Low.PlayOneShotOnCamera();
				}
			};
		}
		else
		{
			yield return new Command_Action
			{
				defaultLabel = "CMC_CommandTargetWorldFromMap".Translate(),
				defaultDesc = "CMC_CommandTarggetWorldFromMapDesc".Translate(),
				icon = ContentFinder<Texture2D>.Get("UI/UI_TargetWorld"),
				action = delegate
				{
					IsTargrtingWorld = true;
					powerComp.powerOutputInt = -48000f;
					SoundDefOf.Tick_Low.PlayOneShotOnCamera();
				}
			};
		}
	}

	public override string GetInspectString()
	{
		StringBuilder stringBuilder = new StringBuilder();
		string inspectString = base.GetInspectString();
		if (!inspectString.NullOrEmpty())
		{
			stringBuilder.AppendLine(inspectString);
		}
		if (AttackVerb.verbProps.minRange > 0f)
		{
			stringBuilder.AppendLine("MinimumRange".Translate() + ": " + AttackVerb.verbProps.minRange.ToString("F0"));
		}
		if (base.Spawned && IsMortarOrProjectileFliesOverhead && base.Position.Roofed(base.Map))
		{
			stringBuilder.AppendLine("CannotFire".Translate() + ": " + "Roofed".Translate().CapitalizeFirst());
		}
		else if (base.Spawned && burstCooldownTicksLeft > 0 && BurstCooldownTime() > 5f)
		{
			stringBuilder.AppendLine("CanFireIn".Translate() + ": " + burstCooldownTicksLeft.ToStringSecondsFromTicks());
		}
		CompChangeableProjectile compChangeableProjectile = gun.TryGetComp<CompChangeableProjectile>();
		if (compChangeableProjectile != null)
		{
			if (compChangeableProjectile.Loaded)
			{
				stringBuilder.AppendLine("ShellLoaded".Translate(compChangeableProjectile.LoadedShell.LabelCap, compChangeableProjectile.LoadedShell));
			}
			else
			{
				stringBuilder.AppendLine("ShellNotLoaded".Translate());
			}
		}
		return stringBuilder.ToString().TrimEndNewlines();
	}

	public virtual bool IsValidTarget(Thing t)
	{
		if (t is Pawn pawn)
		{
			if (base.Faction == Faction.OfPlayer && pawn.IsPrisoner)
			{
				return false;
			}
			if (AttackVerb.ProjectileFliesOverhead())
			{
				RoofDef roofDef = base.Map.roofGrid.RoofAt(t.Position);
				if (roofDef != null && roofDef.isThickRoof)
				{
					return false;
				}
			}
			if (mannableComp == null)
			{
				return !GenAI.MachinesLike(base.Faction, pawn);
			}
			if (pawn.RaceProps.Animal && pawn.Faction == Faction.OfPlayer)
			{
				return false;
			}
		}
		return true;
	}

	public override void OrderAttack(LocalTargetInfo targ)
	{
		if (!targ.IsValid)
		{
			if (forcedTarget.IsValid)
			{
				ResetForcedTarget();
			}
			return;
		}
		if ((targ.Cell - base.Position).LengthHorizontal < AttackVerb.verbProps.EffectiveMinRange(targ, this))
		{
			Messages.Message("MessageTargetBelowMinimumRange".Translate(), this, MessageTypeDefOf.RejectInput, historical: false);
			return;
		}
		if ((targ.Cell - base.Position).LengthHorizontal > AttackVerb.verbProps.range)
		{
			Messages.Message("MessageTargetBeyondMaximumRange".Translate(), this, MessageTypeDefOf.RejectInput, historical: false);
			return;
		}
		if (forcedTarget != targ)
		{
			forcedTarget = targ;
			if (burstCooldownTicksLeft <= 0)
			{
				TryStartShootSomething(canBeginBurstImmediately: false);
			}
		}
		if (holdFire)
		{
			Messages.Message("MessageTurretWontFireBecauseHoldFire".Translate(def.label), this, MessageTypeDefOf.RejectInput, historical: false);
		}
	}

	public override void PostMake()
	{
		base.PostMake();
		burstCooldownTicksLeft = def.building.turretInitialCooldownTime.SecondsToTicks();
		MakeGun();
	}

	public void ResetCurrentTarget()
	{
		currentTargetInt = LocalTargetInfo.Invalid;
		burstWarmupTicksLeft = 0;
	}

	public void ResetForcedTarget()
	{
		forcedTarget = LocalTargetInfo.Invalid;
		burstWarmupTicksLeft = 0;
		if (burstCooldownTicksLeft <= 0)
		{
			TryStartShootSomething(canBeginBurstImmediately: false);
		}
	}

	public IAttackTargetSearcher TargSearcher()
	{
		if (mannableComp != null && mannableComp.MannedNow)
		{
			return mannableComp.ManningPawn;
		}
		return this;
	}

	protected override void Tick()
	{
		base.Tick();
		if (!IsTargrtingWorld)
		{
			if (forcedTarget.IsValid && !CanSetForcedTarget)
			{
				ResetForcedTarget();
			}
			if (forcedTarget.ThingDestroyed)
			{
				ResetForcedTarget();
			}
			if (Active && (mannableComp == null || mannableComp.MannedNow) && !base.IsStunned && base.Spawned)
			{
				turrettop.TurretTopTick();
				GunCompEq.verbTracker.VerbsTick();
				if (AttackVerb.state != VerbState.Bursting)
				{
					burstActivated = false;
					if (WarmingUp && turrettop.CurRotation == turrettop.DestRotation)
					{
						burstWarmupTicksLeft--;
						if (burstWarmupTicksLeft == 0)
						{
							BeginBurst();
						}
					}
					else
					{
						if (burstCooldownTicksLeft > 0)
						{
							burstCooldownTicksLeft--;
						}
						if (burstCooldownTicksLeft <= 0 && this.IsHashIntervalTick(10))
						{
							TryStartShootSomething(canBeginBurstImmediately: true);
						}
					}
				}
			}
			else
			{
				ResetCurrentTarget();
			}
		}
		else if (Active && (mannableComp == null || mannableComp.MannedNow) && !base.IsStunned && base.Spawned && GameComponent_CeleTech.Instance.ASEA_observedMap != null)
		{
			turrettop.TurretTopTick();
			if (burstCooldownTicksLeft <= 0 && turrettop.CurRotation == turrettop.DestRotation)
			{
				Map map = GameComponent_CeleTech.Instance.ASEA_observedMap.Map;
				if (map == null)
				{
					return;
				}
				if (map.attackTargetsCache.TargetsHostileToFaction(base.Faction).Count > 0)
				{
					TryStartShootSomething_WorldTarget(canBeginBurstImmediately: true);
				}
			}
			else
			{
				burstCooldownTicksLeft--;
			}
		}
		if (CanExtractShell)
		{
			CompChangeableProjectile compChangeableProjectile = gun.TryGetComp<CompChangeableProjectile>();
			if (!compChangeableProjectile.allowedShellsSettings.AllowedToAccept(compChangeableProjectile.LoadedShell))
			{
				ExtractShell();
			}
		}
		if (!CanToggleHoldFire)
		{
			holdFire = false;
		}
	}

	public void TryActivateBurst()
	{
		burstActivated = true;
		TryStartShootSomething(canBeginBurstImmediately: true);
	}

	public virtual LocalTargetInfo TryFindNewTarget()
	{
		IAttackTargetSearcher attackTargetSearcher = TargSearcher();
		Faction faction = attackTargetSearcher.Thing.Faction;
		float range = AttackVerb.verbProps.range;
		if (Rand.Value < 0.5f && AttackVerb.ProjectileFliesOverhead() && faction.HostileTo(Faction.OfPlayer) && base.Map.listerBuildings.allBuildingsColonist.Where(delegate(Building x)
		{
			float num = AttackVerb.verbProps.EffectiveMinRange(x, this);
			float num2 = x.Position.DistanceToSquared(base.Position);
			return num2 > num * num && num2 < range * range;
		}).TryRandomElement(out var result))
		{
			return result;
		}
		TargetScanFlags targetScanFlags = TargetScanFlags.NeedThreat | TargetScanFlags.NeedAutoTargetable;
		if (!AttackVerb.ProjectileFliesOverhead())
		{
			targetScanFlags |= TargetScanFlags.NeedLOSToAll;
			targetScanFlags |= TargetScanFlags.LOSBlockableByGas;
		}
		if (IsMortar)
		{
			targetScanFlags |= TargetScanFlags.NeedNotUnderThickRoof;
		}
		return (Thing)AttackTargetFinder.BestShootTargetFromCurrentPosition(attackTargetSearcher, targetScanFlags, IsValidTarget);
	}

	public void TryStartShootSomething(bool canBeginBurstImmediately)
	{
		if (progressBarEffecter != null)
		{
			progressBarEffecter.Cleanup();
			progressBarEffecter = null;
		}
		if (!base.Spawned || (holdFire && CanToggleHoldFire) || (AttackVerb.ProjectileFliesOverhead() && base.Map.roofGrid.Roofed(base.Position)) || !AttackVerb.Available())
		{
			ResetCurrentTarget();
			return;
		}
		bool isValid = currentTargetInt.IsValid;
		if (forcedTarget.IsValid)
		{
			currentTargetInt = forcedTarget;
		}
		else
		{
			currentTargetInt = TryFindNewTarget();
		}
		if (!isValid && currentTargetInt.IsValid && def.building.playTargetAcquiredSound)
		{
			SoundDefOf.TurretAcquireTarget.PlayOneShot(new TargetInfo(base.Position, base.Map));
		}
		if (!currentTargetInt.IsValid)
		{
			ResetCurrentTarget();
			return;
		}
		float randomInRange = def.building.turretBurstWarmupTime.RandomInRange;
		if (randomInRange > 0f)
		{
			burstWarmupTicksLeft = randomInRange.SecondsToTicks();
		}
		else if (canBeginBurstImmediately)
		{
			BeginBurst();
		}
		else
		{
			burstWarmupTicksLeft = 1;
		}
	}

	public void TryStartShootSomething_WorldTarget(bool canBeginBurstImmediately)
	{
		if (GameComponent_CeleTech.Instance.ASEA_observedMap.Map != null && base.Spawned && (!holdFire || !CanToggleHoldFire) && (!AttackVerb.ProjectileFliesOverhead() || !base.Map.roofGrid.Roofed(base.Position)) && AttackVerb.Available())
		{
			Verb_ShootWorld verb_ShootWorld = AttackVerb as Verb_ShootWorld;
			verb_ShootWorld.TryCastFireMission();
			BurstComplete();
			refuelableComp.ConsumeFuel(1f);
		}
	}

	public void UpdateGunVerbs()
	{
		List<Verb> allVerbs = gun.TryGetComp<CompEquippable>().AllVerbs;
		for (int i = 0; i < allVerbs.Count; i++)
		{
			Verb verb = allVerbs[i];
			verb.caster = this;
			verb.castCompleteCallback = BurstComplete;
		}
	}

	public override void SpawnSetup(Map map, bool respawningAfterLoad)
	{
		base.SpawnSetup(map, respawningAfterLoad);
		TurretExtension_CMC modExtension = def.GetModExtension<TurretExtension_CMC>();
		rotationVelocity = modExtension.rotationSpeed;
		IsTargrtingWorld = false;
		dormantComp = GetComp<CompCanBeDormant>();
		initiatableComp = GetComp<CompInitiatable>();
		powerComp = GetComp<CompPowerTrader>();
		mannableComp = GetComp<CompMannable>();
		interactableComp = GetComp<CompInteractable>();
		refuelableComp = GetComp<CompRefuelable>();
		powerCellComp = GetComp<CompMechPowerCell>();
		if (!respawningAfterLoad)
		{
			turrettop.SetRotationFromOrientation();
		}
	}

	protected virtual void BeginBurst()
	{
		AttackVerb.TryStartCastOn(CurrentTarget);
		OnAttackedTarget(CurrentTarget);
	}
}
