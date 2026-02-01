using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace VanillaPsycastsExpanded.Staticlord;

public class GameCondition_PsychicFlashstorm : GameCondition_Flashstorm
{
	private static readonly AccessTools.FieldRef<GameCondition_Flashstorm, int> nextLightningTicksRef = AccessTools.FieldRefAccess<GameCondition_Flashstorm, int>("nextLightningTicks");

	public int numStrikes;

	public int TicksBetweenStrikes => base.Duration / numStrikes;

	private Vector3 RandomLocation()
	{
		return centerLocation.ToVector3() + new Vector3(Vortex.Wrap(Mathf.Abs(Rand.Gaussian(0f, base.AreaRadius)), base.AreaRadius), 0f, 0f).RotatedBy(Rand.Range(0f, 360f));
	}

	public override void GameConditionTick()
	{
		base.GameConditionTick();
		if (nextLightningTicksRef(this) - Find.TickManager.TicksGame > TicksBetweenStrikes)
		{
			nextLightningTicksRef(this) = TicksBetweenStrikes + Find.TickManager.TicksGame;
		}
		for (int i = 0; i < 2; i++)
		{
			FleckMaker.ThrowSmoke(RandomLocation(), base.SingleMap, 4f);
		}
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref numStrikes, "numStrikes", 0);
	}
}
