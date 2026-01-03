using System;
using System.Collections.Generic;
using Verse;

namespace Milira;

public class CompProperties_DelayedPawnSpawnOnWakeup : CompProperties
{
	public List<PawnKindDef> spawnableMilian;

	public int delayTicks = 60;

	public SoundDef spawnSound;

	public EffecterDef spawnEffecter;

	public Type lordJob;

	public bool shouldJoinParentLord;

	public string activatedMessageKey;

	public FloatRange points;

	public IntRange pawnSpawnRadius = new IntRange(2, 2);

	public bool aggressive = true;

	public bool dropInPods;

	public float defendRadius = 21f;

	public MentalStateDef mentalState;

	public bool destroyAfterSpawn;

	public CompProperties_DelayedPawnSpawnOnWakeup()
	{
		compClass = typeof(CompDelayedPawnSpawnOnWakeup);
	}
}
