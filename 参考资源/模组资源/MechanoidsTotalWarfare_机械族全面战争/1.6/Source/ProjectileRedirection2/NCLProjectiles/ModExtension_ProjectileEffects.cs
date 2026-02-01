using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace NCLProjectiles;

public class ModExtension_ProjectileEffects : DefModExtension
{
	private static readonly Dictionary<int, int> originIncrementers = new Dictionary<int, int>();

	public bool alignOriginWithDrawPos;

	public Vector3 originOffset = Vector3.zero;

	public List<Vector3> originOffsets;

	public float originDistance;

	public bool alignOriginOffsetWithDirection;

	public string originAttachment;

	public int originAttachmentIndex = -1;

	public bool randomizeOriginAttachment;

	public float rotationRate;

	public bool fixedRotation;

	public bool activeTracking;

	public string progressFunction;

	[Unsaved(false)]
	public Func<float, float> progress;

	public string heightFunction;

	[Unsaved(false)]
	public Func<float, float> height;

	public FloatRange heightFactorMagnitude = FloatRange.ZeroToOne;

	public bool useVariableHeightFactor;

	public string lateralOffsetFunction;

	[Unsaved(false)]
	public Func<float, float> lateralOffset;

	public FloatRange lateralOffsetMagnitude = FloatRange.ZeroToOne;

	public float lateralOffsetMirrorChance = 0.5f;

	public List<EffectDef> launchEffects;

	public List<EffectDef> effects;

	public List<EffectDef> impactEffects;

	public List<EffectDef> returnEffects;

	public List<ProjectileStageConfiguration> stages;

	[Unsaved(false)]
	public bool hasImpactEffects;

	public ModExtension_ProjectileEffects()
	{
		LongEventHandler.ExecuteWhenFinished(delegate
		{
			if (!progressFunction.NullOrEmpty())
			{
				progress = AnimationUtility.GetFunctionByName(progressFunction);
			}
			if (!heightFunction.NullOrEmpty())
			{
				height = AnimationUtility.GetFunctionByName(heightFunction, AnimationUtility.Sine);
			}
			if (!lateralOffsetFunction.NullOrEmpty())
			{
				lateralOffset = AnimationUtility.GetFunctionByName(lateralOffsetFunction);
			}
			if (stages != null)
			{
				foreach (ProjectileStageConfiguration stage in stages)
				{
					stage.Initialize();
				}
			}
			hasImpactEffects = !impactEffects.NullOrEmpty() || !returnEffects.NullOrEmpty();
		});
	}

	public Vector3 GetOriginOffsetFor(Thing thing)
	{
		if (thing == null || originOffsets.NullOrEmpty())
		{
			return originOffset;
		}
		int num = 0;
		if (originIncrementers.TryGetValue(thing.thingIDNumber, out var value))
		{
			num = value;
		}
		if (num >= originOffsets.Count)
		{
			num = 0;
		}
		Vector3 result = originOffsets[num++];
		originIncrementers[thing.thingIDNumber] = num;
		return result;
	}

	public ProjectileStageConfiguration GetStageAt(int tick = 0)
	{
		if (stages != null)
		{
			foreach (ProjectileStageConfiguration stage in stages)
			{
				if (stage.duration < 0)
				{
					return stage;
				}
				if (tick < stage.duration)
				{
					return stage;
				}
				tick -= stage.duration;
			}
		}
		return null;
	}

	public int GetFlightTimeOffset()
	{
		int num = 0;
		if (stages != null)
		{
			foreach (ProjectileStageConfiguration stage in stages)
			{
				if (stage.duration > -1)
				{
					num += stage.duration;
				}
			}
		}
		return num;
	}
}
