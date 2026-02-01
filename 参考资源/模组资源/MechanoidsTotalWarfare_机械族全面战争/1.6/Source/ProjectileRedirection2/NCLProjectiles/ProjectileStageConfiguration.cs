using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace NCLProjectiles;

public class ProjectileStageConfiguration
{
	public string label;

	public ProjectileStageType type = ProjectileStageType.Terminal;

	public int duration = -1;

	public float speed = 5f;

	public bool overrideInitialAngle;

	public float angle;

	public bool activeTracking;

	public string progressFunction;

	public Func<float, float> progress;

	public Vector3 positionOffset = Vector3.zero;

	public string positionFunction;

	public Func<float, float> position;

	public bool alignPositionWithDestination = true;

	public float initialHeight;

	public float heightOffset;

	public string heightFunction;

	public Func<float, float> height;

	public float arcFactor;

	public string arcFunction;

	public Func<float, float> arc;

	public List<EffectDef> startEffects;

	public List<EffectDef> endEffects;

	public SoundDef startSound;

	public float TilesPerTick => speed / 100f;

	public void Initialize()
	{
		progress = AnimationUtility.GetFunctionByName(progressFunction, AnimationUtility.Linear);
		position = AnimationUtility.GetFunctionByName(positionFunction, AnimationUtility.Linear);
		height = AnimationUtility.GetFunctionByName(heightFunction, AnimationUtility.Linear);
		arc = AnimationUtility.GetFunctionByName(arcFunction, AnimationUtility.Sine);
	}
}
