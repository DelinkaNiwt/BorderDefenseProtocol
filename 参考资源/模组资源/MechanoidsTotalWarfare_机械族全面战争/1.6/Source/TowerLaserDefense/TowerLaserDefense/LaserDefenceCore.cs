using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace TowerLaserDefense;

[StaticConstructorOnStartup]
public class LaserDefenceCore : IExposable
{
	public class MainComponent : GameComponent
	{
		public MainComponent(Game game)
		{
		}

		public override void GameComponentTick()
		{
			if (Find.TickManager.TicksGame % 1000 == 0)
			{
				CleanupAllInstances();
			}
		}
	}

	private bool _detectionEnabled = true;

	private FleckCreationData emberFleck = new FleckCreationData
	{
		def = DefDatabase<FleckDef>.GetNamed("NCL_Fleck_BurnerUsedEmber")
	};

	private int _cleanCounter;

	private Effecter _cooldownEffecter;

	public static readonly List<LaserDefenceCore> Instances = new List<LaserDefenceCore>();

	public LaserDefenceProperties properties;

	public ILaserDefenceParent parent;

	private float _aimingAngle;

	private int _randomRot;

	private int _noRot;

	private CompPowerTrader _power;

	private List<LockedTargetData> _lockedTargets = new List<LockedTargetData>();

	private List<Thing> _finishedTargets = new List<Thing>();

	private int _destroyedCount = 0;

	private int _coolDownTicksLeft = 0;

	private CompProperties_Stunnable StunComp => Parent?.def?.GetCompProperties<CompProperties_Stunnable>();

	public bool DetectionEnabled
	{
		get
		{
			return _detectionEnabled;
		}
		set
		{
			if (_detectionEnabled != value)
			{
				_detectionEnabled = value;
				if (!value)
				{
					_lockedTargets.Clear();
				}
			}
		}
	}

	private bool IsStunned
	{
		get
		{
			if (Parent is Pawn pawn)
			{
				return pawn.stances.stunner.Stunned;
			}
			if (Parent is Building)
			{
				return Parent.TryGetComp<CompStunnable>()?.StunHandler.Stunned ?? false;
			}
			return false;
		}
	}

	private bool RequiresPower => properties.requiresPower && Parent.TryGetComp<CompPowerTrader>() != null;

	public Thing Parent => parent.Thing;

	public CompPowerTrader Power
	{
		get
		{
			if (Parent is Building building)
			{
				return _power ?? (_power = building.GetComp<CompPowerTrader>());
			}
			return null;
		}
	}

	public bool IsStunnedByEMP
	{
		get
		{
			if (Parent is Building)
			{
				return Parent.TryGetComp<CompStunnable>()?.StunHandler.StunFromEMP ?? false;
			}
			return false;
		}
	}

	public void ToggleDetection()
	{
		DetectionEnabled = !DetectionEnabled;
	}

	public static void CleanupAllInstances()
	{
		Instances.RemoveAll((LaserDefenceCore core) => core?.Parent == null || core.Parent.Destroyed || !core.Parent.Spawned);
	}

	public bool HasEnoughPowerToFire()
	{
		if (!RequiresPower || !properties.enablePowerConsumption)
		{
			return true;
		}
		if (Power == null || !Power.PowerOn || Power.PowerNet == null)
		{
			return false;
		}
		float powerConsumptionPerShot = properties.powerConsumptionPerShot;
		return Power.PowerNet.CurrentEnergyGainRate() >= powerConsumptionPerShot || GetTotalStoredEnergy(Power.PowerNet) >= powerConsumptionPerShot;
	}

	private float GetTotalStoredEnergy(PowerNet net)
	{
		float num = 0f;
		foreach (CompPowerBattery batteryComp in net.batteryComps)
		{
			num += batteryComp.StoredEnergy;
		}
		return num;
	}

	private void ConsumePowerFromNet(PowerNet net, float amount)
	{
		foreach (CompPowerBattery batteryComp in net.batteryComps)
		{
			if (amount <= 0f)
			{
				break;
			}
			if (batteryComp.StoredEnergy > 0f)
			{
				float num = Mathf.Min(amount, batteryComp.StoredEnergy);
				batteryComp.DrawPower(num);
				amount -= num;
			}
		}
		if (amount > 0f)
		{
			Power.PowerOutput -= amount;
		}
	}

	public LaserDefenceCore(ILaserDefenceParent parent, LaserDefenceProperties properties)
	{
		this.parent = parent;
		this.properties = properties;
	}

	public bool CanSeeTarget(Thing target)
	{
		if (target == null || target.Destroyed || !target.Spawned || Parent == null || Parent.Map == null)
		{
			return false;
		}
		if (Parent is Pawn pawn && pawn.stances.stunner.Stunned)
		{
			return false;
		}
		if (target is Projectile { Launcher: var launcher } projectile)
		{
			if (launcher?.Faction != null && !launcher.HostileTo(Parent))
			{
				return false;
			}
			if ((properties.ignoreAirProjectiles && projectile.def.projectile.flyOverhead) || (properties.ignoreGroundProjectiles && !projectile.def.projectile.flyOverhead))
			{
				return false;
			}
		}
		if (RequiresPower && Parent is Building building)
		{
			CompPowerTrader comp = building.GetComp<CompPowerTrader>();
			if (comp == null || !comp.PowerOn)
			{
				return false;
			}
		}
		return target.Spawned && target.Map == Parent.Map && (float)(target.PositionHeld - Parent.PositionHeld).LengthHorizontalSquared <= properties.range * properties.range && (!properties.needSight || GenSight.LineOfSight(Parent.Position, target.Position, Parent.Map));
	}

	public bool CanLockTarget(Thing target)
	{
		if (_coolDownTicksLeft > 0)
		{
			return false;
		}
		if (_lockedTargets.Count >= properties.interceptCount)
		{
			return false;
		}
		if (!CanSeeTarget(target))
		{
			return false;
		}
		foreach (LaserDefenceCore instance in Instances)
		{
			if (Enumerable.Any(instance._lockedTargets, (LockedTargetData data) => data.target.GetUniqueLoadID() == target.GetUniqueLoadID()))
			{
				return false;
			}
		}
		return true;
	}

	public bool TryLockTarget(Thing target)
	{
		try
		{
			if (target == null || _lockedTargets == null || properties == null)
			{
				return false;
			}
			if (!CanLockTarget(target))
			{
				return false;
			}
			string targetID = target.GetUniqueLoadID();
			if (_lockedTargets.Any((LockedTargetData x) => x.target?.GetUniqueLoadID() == targetID))
			{
				return false;
			}
			_lockedTargets.Add(new LockedTargetData(target));
			return true;
		}
		catch (Exception arg)
		{
			Log.Error($"Error in TryLockTarget: {arg}");
			return false;
		}
	}

	private void TryRemoveTarget(Thing target)
	{
		if (target == null)
		{
			return;
		}
		List<LockedTargetData> list = _lockedTargets.Where((LockedTargetData data) => data.target == target).ToList();
		foreach (LockedTargetData item in list)
		{
			int num = _lockedTargets.IndexOf(item);
			if (num >= 0 && num < _lockedTargets.Count)
			{
				_lockedTargets.RemoveAt(num);
			}
			else
			{
				Log.Warning("尝试移除无效索引的目标: " + target.LabelCap);
			}
		}
	}

	public void Tick()
	{
		if (!DetectionEnabled)
		{
			_randomRot = 0;
			_noRot = 0;
			return;
		}
		try
		{
			if (++_cleanCounter >= 250 || _cleanCounter < 0)
			{
				_cleanCounter = 0;
				_lockedTargets.RemoveAll((LockedTargetData data) => data.target == null || data.target.Destroyed || !data.target.Spawned || data.target.Map != Parent.Map);
			}
			if (Parent == null || Parent.Destroyed || properties == null || _lockedTargets == null)
			{
				return;
			}
			if (_coolDownTicksLeft > 0)
			{
				_coolDownTicksLeft--;
				_lockedTargets.Clear();
				if (properties.enableCooldownEffect && _cooldownEffecter != null)
				{
					_cooldownEffecter.EffectTick(new TargetInfo(Parent.PositionHeld, Parent.Map), new TargetInfo(Parent.PositionHeld, Parent.Map));
				}
				if (_coolDownTicksLeft == 0 && _cooldownEffecter != null)
				{
					_cooldownEffecter.Cleanup();
					_cooldownEffecter = null;
				}
				return;
			}
			if (_cooldownEffecter != null)
			{
				_cooldownEffecter.Cleanup();
				_cooldownEffecter = null;
			}
			if (!IsStunned)
			{
				if (RequiresPower)
				{
					CompPowerTrader power = Power;
					if (power == null || !power.PowerOn)
					{
						goto IL_01bf;
					}
				}
				List<LockedTargetData> list = new List<LockedTargetData>(_lockedTargets);
				int num = 0;
				foreach (LockedTargetData item in list)
				{
					if (num >= Mathf.Min(properties.maxTargetsPerTick, list.Count))
					{
						break;
					}
					num++;
					if (item.target == null || item.target.Destroyed || !item.target.Spawned || item.target.Map != Parent.Map)
					{
						TryRemoveTarget(item.target);
						continue;
					}
					float num2 = (item.target.PositionHeld - Parent.PositionHeld).LengthHorizontalSquared;
					if (num2 > properties.range * properties.range)
					{
						TryRemoveTarget(item.target);
						continue;
					}
					if (properties.needSight && !GenSight.LineOfSight(Parent.Position, item.target.Position, Parent.Map))
					{
						TryRemoveTarget(item.target);
						continue;
					}
					item.time++;
					if (item.time < properties.interceptTime)
					{
						continue;
					}
					if (DestroyTarget(item.target))
					{
						_destroyedCount++;
						if (_destroyedCount >= properties.coolDownAfterShots)
						{
							_coolDownTicksLeft = properties.coolDownTicks;
							_destroyedCount = 0;
							if (properties.enableCooldownEffect)
							{
								TriggerCooldownEffect();
							}
							break;
						}
					}
					TryRemoveTarget(item.target);
				}
				GunRotate();
				return;
			}
			goto IL_01bf;
			IL_01bf:
			_lockedTargets.Clear();
		}
		catch (Exception arg)
		{
			Log.Error($"LaserDefenceCore.Tick error: {arg}");
		}
	}

	private void TriggerCooldownEffect()
	{
		_cooldownEffecter?.Cleanup();
		EffecterDef named = DefDatabase<EffecterDef>.GetNamed("BlastMechBandShockwave");
		if (named != null)
		{
			_cooldownEffecter = named.Spawn();
			_cooldownEffecter.Trigger(new TargetInfo(Parent.PositionHeld, Parent.Map), new TargetInfo(Parent.PositionHeld, Parent.Map));
		}
		else
		{
			Log.Warning("无法找到 'BlastMechBandShockwave' EffecterDef");
		}
	}

	private int GetStunTicksLeft()
	{
		return Parent.TryGetComp<CompStunnable>()?.StunHandler.StunTicksLeft ?? 0;
	}

	private void GunRotate()
	{
		Vector3 vector = new Vector3(0f, 0f, 0f);
		foreach (LockedTargetData lockedTarget in _lockedTargets)
		{
			vector += (lockedTarget.target.DrawPos - Parent.TrueCenter()).normalized;
		}
		if (vector != Vector3.zero)
		{
			_aimingAngle = vector.AngleFlat();
			_randomRot = 0;
			_noRot = Rand.Range(30, 60);
			return;
		}
		if (_noRot > 0)
		{
			_noRot--;
			return;
		}
		if (_randomRot == 0)
		{
			_randomRot = Rand.Range(-75, 75);
		}
		if (_randomRot > 0)
		{
			_randomRot--;
			_aimingAngle -= 1f;
			if (_randomRot == 0)
			{
				_noRot = Rand.Range(30, 60);
			}
		}
		else
		{
			_randomRot++;
			_aimingAngle += 1f;
			if (_randomRot == 0)
			{
				_noRot = Rand.Range(30, 60);
			}
		}
	}

	public void DrawAt(Vector3 drawPos)
	{
		drawPos.y = 0f;
		if (properties.enableLaserLine)
		{
			foreach (LockedTargetData lockedTarget in _lockedTargets)
			{
				Material mat = MaterialPool.MatFrom("Motes/LaserLine", ShaderDatabase.TransparentPostLight, new Color(1f, 1f, 1f, (float)lockedTarget.time * 0.8f / (float)properties.interceptTime));
				Vector3 drawPos2 = lockedTarget.target.DrawPos;
				drawPos2.y = 0f;
				GenDraw.DrawLineBetween(drawPos, drawPos2, AltitudeLayer.BuildingBelowTop.AltitudeFor(), mat, 0.7f);
			}
		}
		if (properties.graphicData != null)
		{
			drawPos.y = AltitudeLayer.BuildingOnTop.AltitudeFor();
			properties.graphicData.GraphicColoredFor(parent.Thing).Draw(drawPos, Rot4.North, parent.Thing, _aimingAngle);
		}
	}

	private bool DestroyTarget(Thing target)
	{
		try
		{
			if (target == null || target.Destroyed || !target.Spawned)
			{
				return false;
			}
			if (Parent == null || Parent.Destroyed || Parent.Map == null)
			{
				return false;
			}
			bool flag = properties.enableSpecialBulletExplosion && target is Projectile projectile && projectile.def.defName == "Bullet_HellsphereCannonGun";
			Vector3 start = parent.Thing.TrueCenter();
			if (Math.Abs(properties.laserOffsetX) > 0.01f || Math.Abs(properties.laserOffsetY) > 0.01f)
			{
				start.x += properties.laserOffsetX;
				start.z += properties.laserOffsetY;
			}
			if (properties.enableConnectingLineFleck && !string.IsNullOrEmpty(properties.connectingLineFleck))
			{
				FleckDef namedSilentFail = DefDatabase<FleckDef>.GetNamedSilentFail(properties.connectingLineFleck);
				if (namedSilentFail != null)
				{
					FleckMaker.ConnectingLine(start, target.DrawPos, namedSilentFail, Parent.Map, properties.connectinglaserWidth);
				}
			}
			if (!flag)
			{
				if (properties.randomImpactFlecks != null && properties.randomImpactFlecks.Count > 0)
				{
					string defName = properties.randomImpactFlecks[Rand.Range(0, properties.randomImpactFlecks.Count)];
					FleckDef namedSilentFail2 = DefDatabase<FleckDef>.GetNamedSilentFail(defName);
					if (namedSilentFail2 != null)
					{
						FleckMaker.Static(target.DrawPos, Parent.Map, namedSilentFail2, properties.impactScale);
					}
				}
				if (!string.IsNullOrEmpty(properties.secondaryImpactFleck))
				{
					FleckDef namedSilentFail3 = DefDatabase<FleckDef>.GetNamedSilentFail(properties.secondaryImpactFleck);
					if (namedSilentFail3 != null)
					{
						FleckMaker.Static(target.DrawPos, Parent.Map, namedSilentFail3, properties.secondaryImpactScale);
					}
				}
				if (!string.IsNullOrEmpty(properties.tertiaryImpactFleck))
				{
					FleckDef namedSilentFail4 = DefDatabase<FleckDef>.GetNamedSilentFail(properties.tertiaryImpactFleck);
					if (namedSilentFail4 != null)
					{
						FleckMaker.Static(target.DrawPos, Parent.Map, namedSilentFail4, properties.tertiaryImpactScale);
					}
				}
				if (properties.enableSmokeEffect)
				{
					FleckMaker.ThrowSmoke(target.DrawPos, Parent.Map, Mathf.Clamp(properties.smokeSize, 0.5f, 5f));
				}
				if (properties.enableFireGlowEffect)
				{
					FleckMaker.ThrowFireGlow(target.DrawPos, Parent.Map, Mathf.Clamp(properties.smokeSize, 0.5f, 5f));
				}
			}
			if (properties.interceptSound != null)
			{
				properties.interceptSound.PlayOneShot(new TargetInfo(target.Position, Parent.Map));
			}
			if (RequiresPower && properties.enablePowerConsumption && Power != null && Power.PowerNet != null)
			{
				ConsumePowerFromNet(Power.PowerNet, properties.powerConsumptionPerShot);
			}
			if (target.Spawned && !target.Destroyed)
			{
				if (flag)
				{
					TriggerBulletExplosion(target as Projectile);
				}
				else
				{
					target.Destroy(DestroyMode.KillFinalize);
				}
				return true;
			}
			return false;
		}
		catch (Exception arg)
		{
			Log.Error($"摧毁目标时出错: {arg}");
			return false;
		}
	}

	private void TriggerBulletExplosion(Projectile bullet)
	{
		if (bullet == null || bullet.Destroyed || !bullet.Spawned || bullet.Map == null)
		{
			return;
		}
		try
		{
			IntVec3 position = bullet.Position;
			Map map = bullet.Map;
			GenExplosion.DoExplosion(position, map, 4.9f, DamageDefOf.Vaporize, bullet.Launcher, 800, 1f);
			FleckMaker.ThrowLightningGlow(position.ToVector3(), map, 1.5f);
			FleckMaker.ThrowMicroSparks(position.ToVector3(), map);
			bullet.Destroy();
		}
		catch (Exception arg)
		{
			Log.Error($"触发子弹爆炸时出错: {arg}");
		}
	}

	private void TrySetLauncherViaReflection(Projectile projectile, Thing launcher)
	{
		if (launcher == null)
		{
			return;
		}
		try
		{
			FieldInfo field = typeof(Projectile).GetField("launcher", BindingFlags.Instance | BindingFlags.NonPublic);
			if (field != null)
			{
				field.SetValue(projectile, launcher);
				return;
			}
			FieldInfo[] array = (from f in typeof(Projectile).GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
				where f.FieldType == typeof(Thing) && f.Name.ToLower().Contains("launcher")
				select f).ToArray();
			if (array.Length != 0)
			{
				array[0].SetValue(projectile, launcher);
			}
			else
			{
				Log.Warning("无法设置地狱火炮子弹的发射器");
			}
		}
		catch (Exception arg)
		{
			Log.Error($"设置发射器时出错: {arg}");
		}
	}

	public void ExposeData()
	{
		Scribe_Values.Look(ref _detectionEnabled, "detectionEnabled", defaultValue: true);
		Scribe_Collections.Look(ref _lockedTargets, "_lockedTargets", LookMode.Deep);
		Scribe_Values.Look(ref _destroyedCount, "_destroyedCount", 0);
		Scribe_Values.Look(ref _coolDownTicksLeft, "_coolDownTicksLeft", 0);
		Scribe_Values.Look(ref _cleanCounter, "_cleanCounter", 0);
		if (Scribe.mode == LoadSaveMode.PostLoadInit && _coolDownTicksLeft <= 0 && _cooldownEffecter != null)
		{
			_cooldownEffecter.Cleanup();
			_cooldownEffecter = null;
		}
	}
}
