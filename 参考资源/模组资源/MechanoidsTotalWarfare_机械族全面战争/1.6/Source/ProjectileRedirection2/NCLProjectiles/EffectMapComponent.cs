using System;
using System.Collections.Generic;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace NCLProjectiles;

public class EffectMapComponent : MapComponent
{
	public static EffectMapComponent cachedInstance;

	public static readonly Dictionary<int, EffectMapComponent> CachedInstances = new Dictionary<int, EffectMapComponent>();

	private static readonly HashSet<string> InvalidEffectDefs = new HashSet<string>();

	private static readonly int EffectPriorityCount = Enum.GetValues(typeof(EffectPriority)).Length;

	public readonly List<VisualEffect> Effects = new List<VisualEffect>(1000);

	public readonly int[] EffectCount = new int[EffectPriorityCount];

	private readonly int[] effectCountIncrementer = new int[EffectPriorityCount];

	private readonly List<WeaponWithAttachments> tickingWeapons = new List<WeaponWithAttachments>();

	public EffectMapComponent(Map map)
		: base(map)
	{
		CachedInstances[map.uniqueID] = this;
		cachedInstance = null;
	}

	public override void MapRemoved()
	{
		base.MapRemoved();
		CachedInstances.Remove(map.uniqueID);
	}

	public override void FinalizeInit()
	{
		base.FinalizeInit();
		FindTickingWeaponsOnMap();
	}

	public override void MapComponentUpdate()
	{
		if (WorldRendererUtility.DrawingMap && Find.CurrentMap == map && !WorldComponent_GravshipController.CutsceneInProgress)
		{
			Draw();
		}
	}

	public override void MapComponentTick()
	{
		EffectTick();
		WeaponTick();
	}

	private void EffectTick()
	{
		ResetEffectIncrementer();
		int num = 0;
		while (num < Effects.Count)
		{
			VisualEffect visualEffect = Effects[num];
			if (visualEffect.Tick())
			{
				num++;
				effectCountIncrementer[(int)visualEffect.def.priority]++;
			}
			else
			{
				Effects.RemoveAt(num);
				visualEffect.def.endSound?.PlayOneShot(SoundInfo.InMap(new TargetInfo(visualEffect.IntPosition, map)));
			}
		}
		UpdateEffectCounts();
	}

	private void ResetEffectIncrementer()
	{
		for (int i = 0; i < EffectPriorityCount; i++)
		{
			effectCountIncrementer[i] = 0;
		}
	}

	private void UpdateEffectCounts()
	{
		for (int i = 0; i < EffectPriorityCount; i++)
		{
			EffectCount[i] = effectCountIncrementer[i];
		}
	}

	private void Draw()
	{
		try
		{
			FogGrid fogGrid = map.fogGrid;
			CellRect currentViewRect = Find.CameraDriver.CurrentViewRect;
			currentViewRect.ClipInsideMap(map);
			currentViewRect = currentViewRect.ExpandedBy(1);
			CellIndices cellIndices = map.cellIndices;
			for (int i = 0; i < Effects.Count; i++)
			{
				VisualEffect visualEffect = Effects[i];
				if (visualEffect.IsInViewOf(ref currentViewRect, fogGrid, cellIndices))
				{
					try
					{
						visualEffect.Draw();
					}
					catch (Exception arg)
					{
						Log.Error($"(NCL Projectiles) Error trying to draw visual effect at {visualEffect.Position}: {arg}");
					}
				}
			}
		}
		catch (Exception arg2)
		{
			Log.Error($"(NCL Projectiles) Error trying to draw visual effects: {arg2}");
		}
	}

	public void AddEffect(VisualEffect effect)
	{
		Effects.Add(effect);
	}

	public void CreateEffect(EffectContext context)
	{
		if (context.def.subtractParentElapsed && context.parentDuration >= context.def.duration.max)
		{
			return;
		}
		switch (context.def.type)
		{
		case EffectType.Muzzle:
			CreateFlash(ref context);
			return;
		case EffectType.Trail:
			CreateTrail(ref context);
			return;
		case EffectType.TrailOrbiter:
			CreateTrailOrbiter(ref context);
			return;
		case EffectType.LinePulse:
			CreateLinePulse(ref context);
			return;
		case EffectType.Particle:
			CreateParticle(ref context);
			return;
		case EffectType.Deformer:
			CreateDeformer(ref context);
			return;
		case EffectType.Animated:
			CreateAnimated(ref context);
			return;
		case EffectType.Flicker:
			CreateFlicker(ref context);
			return;
		case EffectType.Drifter:
			CreateDrifter(ref context);
			return;
		case EffectType.Pather:
			CreatePather(ref context);
			return;
		case EffectType.Orbiter:
			CreateOrbiter(ref context);
			return;
		case EffectType.Scatterer:
			CreateScatterer(ref context);
			return;
		case EffectType.Flipper:
			CreateFlipper(ref context);
			return;
		case EffectType.Composite:
			CreateComposite(ref context);
			return;
		case EffectType.Spawner:
			CreateSpawner(ref context);
			return;
		case EffectType.Sequencer:
			CreateSequencer(ref context);
			return;
		}
		if (InvalidEffectDefs.Add(context.def.defName))
		{
			Log.Warning($"(NCL Projectiles) Could not create effect {context.def.LabelCap} with type {context.def.type}, skipping future attempts");
		}
	}

	private void CreateComposite(ref EffectContext context)
	{
		EffectDef def = context.def;
		if (def == null || def.subeffects.NullOrEmpty())
		{
			return;
		}
		int num = 0;
		for (int i = 0; i < def.count; i++)
		{
			Vector3 vector = Vector3.zero;
			if (def.applyDriftToPosition)
			{
				float randomInRange = def.drawDriftDistance.RandomInRange;
				if (randomInRange != 0f)
				{
					vector = EffectUtility.CalculateDriftOffset(randomInRange);
				}
			}
			foreach (EffectDef subeffect in def.subeffects)
			{
				EffectContext context2 = context.CreateSubEffectContext(subeffect);
				context2.delayOffset = context.delayOffset + num;
				if (def.applyDriftToPosition)
				{
					context2.position += vector;
				}
				if (def.applyDriftToOrigin)
				{
					context2.origin += vector;
				}
				if (def.applyDriftToDestination)
				{
					context2.destination += vector;
				}
				CreateEffect(context2);
			}
			int num2 = num;
			EffectDef effectDef = def;
			num = num2 + (effectDef.delayStep.HasValue ? effectDef.delayStep.GetValueOrDefault().RandomInRange : 0);
		}
	}

	private void CreateFlash(ref EffectContext context)
	{
		EffectDef def = context.def;
		if (def != null && def.HasMaterial && context.def.CheckInterval(context))
		{
			AddEffect(new VisualEffect_Flash(this, context));
		}
	}

	private void CreateTrail(ref EffectContext context)
	{
		if (context.def != null && context.def.HasMaterial && context.destination != Vector3.zero && context.origin != context.destination && context.def.CheckInterval(context))
		{
			AddEffect(new VisualEffect_Line(this, context));
		}
	}

	private void CreateTrailOrbiter(ref EffectContext context)
	{
		if (context.def != null && context.def.count > 0 && !context.def.subeffects.NullOrEmpty() && context.def.CheckInterval(context))
		{
			AddEffect(new VisualEffect_TrailerOrbiter(this, context));
		}
	}

	private void CreateLinePulse(ref EffectContext context)
	{
		if (context.def != null && context.def.HasMaterial && context.destination != Vector3.zero && context.origin != context.destination && context.def.CheckInterval(context))
		{
			AddEffect(new VisualEffect_LinePulse(this, context));
		}
	}

	private void CreateParticle(ref EffectContext context)
	{
		if (context.def != null && context.def.HasMaterial && context.def.CheckInterval(context))
		{
			for (int i = 0; i < context.def.count; i++)
			{
				AddEffect(new VisualEffect_Particle(this, context));
			}
		}
	}

	private void CreateDeformer(ref EffectContext context)
	{
		if (context.def != null && context.def.HasMaterial && context.def.CheckInterval(context))
		{
			AddEffect(new VisualEffect_ParticleDeformer(this, context));
		}
	}

	private void CreateAnimated(ref EffectContext context)
	{
		if (context.def != null && context.def.HasMaterial && context.def.CheckInterval(context))
		{
			AddEffect(new VisualEffect_ParticleAnimated(this, context));
		}
	}

	private void CreateFlicker(ref EffectContext context)
	{
		if (context.def != null && context.def.HasMaterial && context.def.CheckInterval(context))
		{
			AddEffect(new VisualEffect_ParticleFlicker(this, context));
		}
	}

	private void CreateDrifter(ref EffectContext context)
	{
		if (context.def == null || !context.def.HasMaterial || !context.def.CheckInterval(context))
		{
			return;
		}
		if (context.def.count > 1)
		{
			float num = (context.def.inheritRotation ? context.angle : 0f);
			for (int i = 0; i < context.def.count; i++)
			{
				float y = ((!context.def.useEvenDriftSpread) ? (num + context.def.driftOffset.RandomInRange) : (num + Mathf.Lerp(context.def.driftOffset.min, context.def.driftOffset.max, (float)i / (float)context.def.count)));
				Quaternion quaternion = Quaternion.Euler(0f, y, 0f);
				float num2 = num + context.def.rotationOffset.RandomInRange;
				Quaternion rotation = Quaternion.Euler(0f, num2, 0f);
				float num3 = context.def.distance.RandomInRange;
				if (context.def.scaleDistanceWithParent)
				{
					num3 *= context.parentScale;
				}
				Vector3 destination = context.position + quaternion * new Vector3(0f, 0f, num3);
				EffectContext context2 = context.CreateSubEffectContext(context.def);
				context2.destination = destination;
				if (context.def.startingDistance != FloatRange.Zero)
				{
					num3 = context.def.startingDistance.RandomInRange;
					if (context.def.scaleDistanceWithParent)
					{
						num3 *= context.parentScale;
					}
					context2.position = context.position + quaternion * new Vector3(0f, 0f, num3);
				}
				if (context.def.inheritRotationFromPath)
				{
					context2.rotation = rotation;
					context2.angle = num2;
				}
				else
				{
					context2.rotation = Quaternion.Euler(0f, num, 0f);
					context2.angle = num;
				}
				AddEffect(new VisualEffect_ParticleDrifter(this, context2));
			}
			return;
		}
		float angle = (context.def.inheritRotation ? context.angle : 0f) + context.def.rotationOffset.RandomInRange;
		float num4 = context.def.distance.RandomInRange;
		Quaternion quaternion2 = Quaternion.AngleAxis(angle, Vector3.up);
		if (context.def.scaleDistanceWithParent)
		{
			num4 *= context.parentScale;
		}
		Vector3 destination2 = context.position + quaternion2 * (Vector3.forward * num4);
		EffectContext context3 = context.CreateSubEffectContext(context.def);
		context3.destination = destination2;
		if (context.def.startingDistance != FloatRange.Zero)
		{
			num4 = context.def.startingDistance.RandomInRange;
			if (context.def.scaleDistanceWithParent)
			{
				num4 *= context.parentScale;
			}
			context3.position = context.position + quaternion2 * (Vector3.forward * num4);
		}
		AddEffect(new VisualEffect_ParticleDrifter(this, context3));
	}

	private void CreatePather(ref EffectContext context)
	{
		if (context.def != null && context.def.CheckInterval(context))
		{
			float y = (context.def.inheritRotation ? context.angle : 0f);
			if (context.def.destinationDrawOffset != Vector3.zero)
			{
				EffectContext context2 = context.CreateSubEffectContext(context.def);
				Vector3 vector = (context2.destination - context2.origin).Yto0();
				context2.destination += Quaternion.Euler(0f, y, 0f) * context.def.destinationDrawOffset;
				context2.rotation = ((vector == Vector3.zero) ? Quaternion.identity : Quaternion.LookRotation(vector));
				context2.angle = context2.rotation.eulerAngles.y;
				AddEffect(new VisualEffect_ParticlePather(this, context2));
			}
			else
			{
				AddEffect(new VisualEffect_ParticlePather(this, context));
			}
		}
	}

	private void CreateOrbiter(ref EffectContext context)
	{
		if (context.def != null && context.def.HasMaterial && context.def.CheckInterval(context))
		{
			float num = (context.def.inheritRotation ? context.angle : 0f) + context.def.orbitOffset.RandomInRange;
			float num2 = ((context.def.count > 1) ? (360f / (float)context.def.count) : 0f);
			for (int i = 0; i < context.def.count; i++)
			{
				float orbitAngle = (num + num2 * (float)i) % 360f;
				EffectContext context2 = context.CreateSubEffectContext(context.def);
				context2.orbitAngle = orbitAngle;
				AddEffect(new VisualEffect_ParticleOrbiter(this, context2));
			}
		}
	}

	private void CreateScatterer(ref EffectContext context)
	{
		if (context.def != null && !context.def.subeffects.NullOrEmpty())
		{
			AddEffect(new VisualEffect_ParticleScatterer(this, context));
		}
	}

	private void CreateFlipper(ref EffectContext context)
	{
		if (context.def != null)
		{
			AddEffect(new VisualEffect_ParticleFlipper(this, context));
		}
	}

	private void CreateSpawner(ref EffectContext context)
	{
		if (context.def != null && context.def.CheckInterval(context))
		{
			AddEffect(new VisualEffect_Spawner(this, context));
		}
	}

	private void CreateSequencer(ref EffectContext context)
	{
		if (context.def != null && context.def.CheckInterval(context))
		{
			AddEffect(new VisualEffect_Sequencer(this, context));
		}
	}

	private void FindTickingWeaponsOnMap()
	{
		tickingWeapons.Clear();
		foreach (Pawn item in map.mapPawns.AllPawnsSpawned)
		{
			if (item.equipment?.Primary is WeaponWithAttachments { TickWeaponWhileEquipped: not false } weaponWithAttachments)
			{
				tickingWeapons.Add(weaponWithAttachments);
			}
		}
	}

	private void WeaponTick()
	{
		for (int i = 0; i < tickingWeapons.Count; i++)
		{
			tickingWeapons[i].EquippedTick();
		}
	}

	public void RegisterTickingWeapon(WeaponWithAttachments weapon)
	{
		if (!tickingWeapons.Contains(weapon))
		{
			tickingWeapons.Add(weapon);
		}
	}

	public void DeregisterTickingWeapon(WeaponWithAttachments weapon)
	{
		tickingWeapons.Remove(weapon);
	}
}
