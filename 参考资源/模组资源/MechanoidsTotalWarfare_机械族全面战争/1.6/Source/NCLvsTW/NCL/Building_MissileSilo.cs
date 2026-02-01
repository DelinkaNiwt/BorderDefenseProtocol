using System;
using System.Collections.Generic;
using System.Reflection;
using NyarsModPackTwo;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace NCL;

public class Building_MissileSilo : Building
{
	public enum LaunchCondition
	{
		HighAngleProjectilesOnly,
		AnyEnemyThreats
	}

	private enum LidState
	{
		Closed,
		Opening,
		Open,
		Closing
	}

	public class Dialog_FloatSlider : Window
	{
		private readonly Action<float> callback;

		private readonly string label;

		private readonly float min;

		private readonly float max;

		private float value;

		public override Vector2 InitialSize => new Vector2(400f, 150f);

		public Dialog_FloatSlider(string label, float min, float max, Action<float> callback, float startingValue)
		{
			this.label = label;
			this.min = min;
			this.max = max;
			this.callback = callback;
			value = startingValue;
			forcePause = true;
			absorbInputAroundWindow = true;
			closeOnClickedOutside = true;
		}

		public override void DoWindowContents(Rect inRect)
		{
			Text.Font = GameFont.Small;
			Rect labelRect = new Rect(inRect.x, inRect.y, inRect.width, 30f);
			Widgets.Label(labelRect, label);
			Rect sliderRect = new Rect(inRect.x + 10f, inRect.y + 40f, inRect.width - 20f, 30f);
			value = Widgets.HorizontalSlider(sliderRect, value, min, max, middleAlignment: false, "NCL.CurrentValue".Translate(value.ToString("F1")), "NCL.MinValue".Translate(min.ToString("F1")), "NCL.MaxValue".Translate(max.ToString("F1")), 0.1f);
			Rect buttonRect = new Rect(inRect.width / 2f - 50f, inRect.y + 80f, 100f, 30f);
			if (Widgets.ButtonText(buttonRect, "OK"))
			{
				callback?.Invoke(value);
				Close();
			}
		}
	}

	public LaunchCondition launchCondition = LaunchCondition.AnyEnemyThreats;

	private const int MissileCount = 24;

	private const int LaunchPositionsX = 4;

	private const int LaunchPositionsZ = 2;

	private const float LaunchOffset = 1.5f;

	private const int LaunchDistance = 500;

	public const int PowerPerShot = 100;

	private CompPowerTrader powerComp;

	public float launchInterval = 3f;

	public const int SteelPerShot = 20;

	private CompSteelResource steelComp;

	private Graphic lidGraphic;

	private LidState lidState = LidState.Closed;

	private float lidOffset = -1.1f;

	private float targetLidOffset = -1.1f;

	private const float LidMoveSpeed = 0.025f;

	private Graphic _lidGraphic;

	private int nextMissileIndex = 0;

	private float launchTimer = 0f;

	private Graphic LidGraphic
	{
		get
		{
			if (_lidGraphic == null && base.Spawned && Current.ProgramState == ProgramState.Playing)
			{
				_lidGraphic = GraphicDatabase.Get<Graphic_Single>("Buildings/NCL_MissileSilo_Top", ShaderDatabase.Cutout, def.graphicData.drawSize, Color.white);
			}
			return _lidGraphic;
		}
	}

	public override void PostMake()
	{
		base.PostMake();
		steelComp = GetComp<CompSteelResource>();
	}

	public override void SpawnSetup(Map map, bool respawningAfterLoad)
	{
		base.SpawnSetup(map, respawningAfterLoad);
		steelComp = GetComp<CompSteelResource>();
		powerComp = GetComp<CompPowerTrader>();
	}

	public bool HasEnoughPowerToFire()
	{
		if (powerComp == null)
		{
			return true;
		}
		if (!powerComp.PowerOn)
		{
			return false;
		}
		PowerNet powerNet = powerComp.PowerNet;
		if (powerNet == null)
		{
			return false;
		}
		return GetTotalStoredEnergy(powerNet) >= 100f;
	}

	private float PowerNetCurrentEnergyGainRate(PowerNet net)
	{
		return net.CurrentEnergyGainRate() / CompPower.WattsToWattDaysPerTick;
	}

	private float GetTotalStoredEnergy(PowerNet net)
	{
		float total = 0f;
		if (net?.batteryComps != null)
		{
			foreach (CompPowerBattery battery in net.batteryComps)
			{
				total += battery.StoredEnergy;
			}
		}
		return total;
	}

	private void ConsumePowerFromNet(float amount)
	{
		if (powerComp == null || powerComp.PowerNet == null)
		{
			return;
		}
		PowerNet net = powerComp.PowerNet;
		if (net.batteryComps == null)
		{
			return;
		}
		foreach (CompPowerBattery battery in net.batteryComps)
		{
			if (amount <= 0f)
			{
				break;
			}
			if (battery.StoredEnergy > 0f)
			{
				float consume = Mathf.Min(amount, battery.StoredEnergy);
				battery.DrawPower(consume);
				amount -= consume;
			}
		}
	}

	protected override void Tick()
	{
		base.Tick();
		if (lidState == LidState.Opening || lidState == LidState.Closing)
		{
			UpdateLidPosition();
		}
		if (lidState == LidState.Open)
		{
			launchTimer += 1f / 60f;
			bool hasEnoughSteel = steelComp != null && steelComp.HasEnoughResources(20);
			bool hasEnoughPower = HasEnoughPowerToFire();
			if (launchTimer >= launchInterval && HasEnemyTargets() && hasEnoughSteel && hasEnoughPower)
			{
				LaunchMissile();
				launchTimer = 0f;
			}
		}
	}

	private void UpdateLidPosition()
	{
		lidOffset = Mathf.Lerp(lidOffset, targetLidOffset, 0.025f);
		if (Mathf.Abs(lidOffset - targetLidOffset) < 0.01f)
		{
			lidOffset = targetLidOffset;
			if (lidState == LidState.Opening)
			{
				lidState = LidState.Open;
			}
			else if (lidState == LidState.Closing)
			{
				lidState = LidState.Closed;
			}
			if (base.Spawned)
			{
				base.Map.mapDrawer.MapMeshDirty(base.Position, MapMeshFlagDefOf.Things, regenAdjacentCells: true, regenAdjacentSections: false);
			}
		}
	}

	protected override void DrawAt(Vector3 drawLoc, bool flip = false)
	{
		base.DrawAt(drawLoc, flip);
		Graphic lidGraphic = LidGraphic;
		if (lidGraphic != null)
		{
			Vector3 lidPos = drawLoc;
			lidPos.y = AltitudeLayer.Filth.AltitudeFor();
			lidPos.z += lidOffset;
			lidGraphic.Draw(lidPos, base.Rotation, this);
		}
	}

	private bool HasEnemyTargets()
	{
		if (!base.Spawned || base.Map == null || base.Faction == null)
		{
			return false;
		}
		return launchCondition switch
		{
			LaunchCondition.HighAngleProjectilesOnly => HasHostileAirProjectiles(), 
			LaunchCondition.AnyEnemyThreats => HasHostileAirProjectiles() || HasEnemyPawns(), 
			_ => false, 
		};
	}

	private bool HasEnemyPawns()
	{
		foreach (Pawn pawn in base.Map.mapPawns.AllPawnsSpawned)
		{
			if (IsHostilePawn(pawn))
			{
				return true;
			}
		}
		return false;
	}

	private bool IsHostilePawn(Pawn pawn)
	{
		bool isPrisonerOrDowned = pawn.IsPrisoner || pawn.Downed;
		return pawn != null && pawn.Spawned && pawn.Faction != null && base.Faction != null && base.Faction.HostileTo(pawn.Faction) && !isPrisonerOrDowned;
	}

	private bool HasHostileAirProjectiles()
	{
		List<Thing> projectiles = base.Map.listerThings.ThingsInGroup(ThingRequestGroup.Projectile);
		foreach (Thing thing in projectiles)
		{
			if (thing is Projectile projectile && IsHostileAirProjectile(projectile))
			{
				return true;
			}
		}
		return false;
	}

	private bool IsHostileAirProjectile(Projectile projectile)
	{
		if (projectile.def.projectile == null || !projectile.def.projectile.flyOverhead)
		{
			return false;
		}
		Thing launcher = projectile.Launcher;
		if (launcher == null || launcher.Faction == null || base.Faction == null)
		{
			return false;
		}
		return base.Faction.HostileTo(launcher.Faction);
	}

	private Vector3 GetNextLaunchPosition()
	{
		int xIndex = nextMissileIndex % 4;
		int zIndex = nextMissileIndex / 4 % 2;
		float horizontalOffsetFactor = 1.2f;
		float verticalOffsetFactor = 1f;
		float verticalShift = -1.5f;
		Vector3 offset = new Vector3(((float)xIndex - 2f + 0.5f) * horizontalOffsetFactor, 0f, ((float)zIndex - 1f + 0.5f) * verticalOffsetFactor + verticalShift);
		nextMissileIndex = (nextMissileIndex + 1) % 24;
		return DrawPos + offset;
	}

	private LocalTargetInfo GetInitialTarget(Vector3 spawnPos)
	{
		IntVec3 targetCell = new IntVec3(spawnPos.ToIntVec3().x, spawnPos.ToIntVec3().y, Mathf.Min(spawnPos.ToIntVec3().z + 500, base.Map.Size.z - 1));
		return new LocalTargetInfo(targetCell);
	}

	private void LaunchMissile()
	{
		try
		{
			if (steelComp == null || !steelComp.ConsumeResources(20))
			{
				Messages.Message("Not enough steel to launch missile", this, MessageTypeDefOf.RejectInput);
				return;
			}
			ConsumePowerFromNet(100f);
			if (!base.Spawned || base.Map == null)
			{
				return;
			}
			Vector3 launchPos = GetNextLaunchPosition();
			IntVec3 spawnCell = launchPos.ToIntVec3();
			if (!spawnCell.InBounds(base.Map))
			{
				return;
			}
			ThingDef missileDef = ThingDef.Named("Nyar_IronRain_Rocket");
			if (missileDef == null)
			{
				return;
			}
			Thing missile = ThingMaker.MakeThing(missileDef);
			if (missile == null)
			{
				return;
			}
			missile.Position = spawnCell;
			GenSpawn.Spawn(missile, spawnCell, base.Map);
			if (missile is Bullet_TracingEnemies tracingMissile)
			{
				tracingMissile.trackingPosNow = spawnCell.ToVector3Shifted();
				InitializeMissileFields(tracingMissile, missileDef);
			}
			if (missile is Projectile projectile)
			{
				projectile.Launch(this, spawnCell.ToVector3Shifted(), GetInitialTarget(launchPos), LocalTargetInfo.Invalid, ProjectileHitFlags.All);
				FleckMaker.ThrowSmoke(launchPos, base.Map, 1.5f);
				FleckMaker.ThrowLightningGlow(launchPos, base.Map, 1.2f);
				SoundDef.Named("MissileLauncher_Fire").PlayOneShot(new TargetInfo(base.Position, base.Map));
				if (!Prefs.DevMode)
				{
				}
			}
		}
		catch (Exception arg)
		{
			Log.Error($"Error launching missile: {arg}");
		}
	}

	private void InitializeMissileFields(Bullet_TracingEnemies missile, ThingDef missileDef)
	{
		try
		{
			Type missileType = typeof(Bullet_TracingEnemies);
			FieldInfo flyingAngleField = missileType.GetField("flyingAngle", BindingFlags.Instance | BindingFlags.Public);
			if (flyingAngleField != null)
			{
				flyingAngleField.SetValue(missile, 0f);
			}
			FieldInfo trackingPosField = missileType.GetField("trackingPosNow", BindingFlags.Instance | BindingFlags.Public);
			if (trackingPosField != null)
			{
				Vector3 correctedPos = missile.Position.ToVector3Shifted();
				correctedPos.y = missileDef.Altitude;
				trackingPosField.SetValue(missile, correctedPos);
			}
			FieldInfo flyingTimeField = missileType.GetField("_flyingTime", BindingFlags.Instance | BindingFlags.NonPublic);
			if (flyingTimeField != null)
			{
				flyingTimeField.SetValue(missile, 0);
			}
			FieldInfo propsField = missileType.GetField("_props", BindingFlags.Instance | BindingFlags.NonPublic);
			if (propsField != null && propsField.GetValue(missile) == null)
			{
				propsField.SetValue(missile, missileDef.GetModExtension<ModExtension_BulletProperties>());
			}
			FieldInfo cacheField = missileType.GetField("_localTargetCache", BindingFlags.Instance | BindingFlags.NonPublic);
			if (cacheField != null && cacheField.GetValue(missile) == null)
			{
				cacheField.SetValue(missile, new List<Thing>());
			}
			FieldInfo interceptParamsField = missileType.GetField("_interceptParams", BindingFlags.Instance | BindingFlags.NonPublic);
			if (interceptParamsField != null && interceptParamsField.GetValue(missile) == null)
			{
				interceptParamsField.SetValue(missile, new object[2]);
			}
		}
		catch (Exception)
		{
		}
	}

	private void ToggleSilo()
	{
		if (lidState == LidState.Closed || lidState == LidState.Closing)
		{
			lidState = LidState.Opening;
			targetLidOffset = 1.3f;
			launchTimer = 0f;
		}
		else if (lidState == LidState.Open || lidState == LidState.Opening)
		{
			lidState = LidState.Closing;
			targetLidOffset = -1.1f;
		}
	}

	private string GetLaunchConditionDescription()
	{
		return launchCondition switch
		{
			LaunchCondition.HighAngleProjectilesOnly => "NCL.ConditionDesc.HighAngleOnly".Translate(), 
			LaunchCondition.AnyEnemyThreats => "NCL.ConditionDesc.AnyEnemyThreat".Translate(), 
			_ => string.Empty, 
		};
	}

	private void LaunchConditionSelector()
	{
		List<FloatMenuOption> options = new List<FloatMenuOption>();
		options.Add(new FloatMenuOption("NCL.Condition.HighAngleOnly".Translate(), delegate
		{
			launchCondition = LaunchCondition.HighAngleProjectilesOnly;
		}));
		options.Add(new FloatMenuOption("NCL.Condition.AnyEnemyThreat".Translate(), delegate
		{
			launchCondition = LaunchCondition.AnyEnemyThreats;
		}));
		Find.WindowStack.Add(new FloatMenu(options));
	}

	public override IEnumerable<Gizmo> GetGizmos()
	{
		foreach (Gizmo gizmo in base.GetGizmos())
		{
			yield return gizmo;
		}
		if (steelComp != null)
		{
			yield return new SteelResourceGizmo(steelComp);
		}
		yield return new Command_Action
		{
			icon = ContentFinder<Texture2D>.Get("ModIcon/NowIsTheTime"),
			defaultLabel = ((lidState == LidState.Closed || lidState == LidState.Closing) ? "NCL.ActivateSilo".Translate() : "NCL.DeactivateSilo".Translate()),
			defaultDesc = ((lidState == LidState.Closed || lidState == LidState.Closing) ? "NCL.ActivateSiloDesc".Translate() : "NCL.DeactivateSiloDesc".Translate()),
			action = ToggleSilo
		};
		yield return new Command_Action
		{
			icon = ContentFinder<Texture2D>.Get("ModIcon/Adjustment"),
			defaultLabel = "NCL.LaunchCondition".Translate(),
			defaultDesc = GetLaunchConditionDescription(),
			action = LaunchConditionSelector,
			disabledReason = "NCL.MustBeDisabledWhenActive".Translate()
		};
		yield return new Command_Action
		{
			icon = ContentFinder<Texture2D>.Get("ModIcon/FrequencyChange"),
			defaultLabel = "NCL.LaunchInterval".Translate(launchInterval.ToString("F1")),
			defaultDesc = "NCL.LaunchIntervalDesc".Translate(),
			action = delegate
			{
				Find.WindowStack.Add(new Dialog_FloatSlider("NCL.SetLaunchInterval".Translate(), 0.1f, 3f, delegate(float val)
				{
					launchInterval = val;
				}, launchInterval));
			}
		};
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref launchCondition, "launchCondition", LaunchCondition.AnyEnemyThreats);
		Scribe_Values.Look(ref lidState, "lidState", LidState.Closed);
		Scribe_Values.Look(ref lidOffset, "lidOffset", -1.5f);
		Scribe_Values.Look(ref targetLidOffset, "targetLidOffset", -1.5f);
		Scribe_Values.Look(ref nextMissileIndex, "nextMissileIndex", 0);
		Scribe_Values.Look(ref launchTimer, "launchTimer", 0f);
		Scribe_Values.Look(ref launchInterval, "launchInterval", 3f);
	}

	public override void DrawExtraSelectionOverlays()
	{
		base.DrawExtraSelectionOverlays();
		if (DebugSettings.godMode && base.Spawned)
		{
			for (int i = 0; i < 24; i++)
			{
				Vector3 pos = GetNextLaunchPosition();
				GenDraw.DrawCircleOutline(pos, 0.5f, SimpleColor.Red);
				GenDraw.DrawLineBetween(pos, GetInitialTarget(pos).Cell.ToVector3Shifted(), SimpleColor.Green);
			}
		}
	}
}
