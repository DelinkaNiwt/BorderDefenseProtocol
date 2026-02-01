using UnityEngine;
using Verse;

namespace NCLProjectiles;

public struct ProjectileFlightStage : IExposable
{
	public Vector3 origin;

	public Vector3 destination;

	public int startingTick;

	public float startingHeight;

	public float endingHeight;

	public float distance;

	public int duration;

	public void ExposeData()
	{
		Scribe_Values.Look(ref origin, "origin");
		Scribe_Values.Look(ref destination, "destination");
		Scribe_Values.Look(ref startingTick, "startingTick", 0);
		Scribe_Values.Look(ref startingHeight, "startingHeight", 0f);
		Scribe_Values.Look(ref endingHeight, "endingHeight", 0f);
		Scribe_Values.Look(ref distance, "distance", 0f);
		Scribe_Values.Look(ref duration, "duration", 0);
	}
}
