using RimWorld;
using UnityEngine;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded;

public class MoteCastBubble : MoteBubble
{
	private float durationSecs;

	protected override bool EndOfLife => base.AgeSecs >= durationSecs;

	public override float Alpha => 1f;

	public void Setup(Pawn pawn, Ability ability)
	{
		SetupMoteBubble(ability.def.icon, null);
		Attach(pawn);
		durationSecs = Mathf.Max(3f, ability.GetCastTimeForPawn().TicksToSeconds());
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref durationSecs, "durationSecs", 0f);
	}
}
