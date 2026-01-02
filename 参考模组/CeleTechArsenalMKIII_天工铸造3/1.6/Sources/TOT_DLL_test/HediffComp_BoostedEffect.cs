using RimWorld;
using UnityEngine;
using Verse;

namespace TOT_DLL_test;

public class HediffComp_BoostedEffect : HediffComp
{
	public Mote CMC_Mote;

	public HediffCompProperties_BoostedEffect Props => (HediffCompProperties_BoostedEffect)props;

	public override void CompExposeData()
	{
		base.CompExposeData();
		Scribe_Deep.Look(ref CMC_Mote, "CMC_Mote");
	}

	public override void CompPostTick(ref float severityAdjustment)
	{
		Pawn pawn = parent.pawn;
		base.CompPostTick(ref severityAdjustment);
		bool flag = !pawn.InBed() && pawn.Awake() && !pawn.Downed;
		if (CMC_Mote.DestroyedOrNull())
		{
			ThingDef cMC_Mote_ChipBoosted = CMC_Def.CMC_Mote_ChipBoosted;
			CMC_Mote = MoteMaker.MakeAttachedOverlay(offset: new Vector3(0f, 0f, -0.05f), thing: parent.pawn, moteDef: cMC_Mote_ChipBoosted, scale: 2.3f, solidTimeOverride: 1f);
			CMC_Mote.exactRotation = 0f;
		}
		if (flag)
		{
			CMC_Mote.Maintain();
		}
	}
}
