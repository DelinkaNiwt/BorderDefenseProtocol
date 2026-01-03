using RimWorld;
using Verse;

namespace TOT_DLL_test;

public class Comp_BodyshapeAjuster : ThingComp
{
	private BodyTypeDef BodyShape;

	private bool ChangedBS = false;

	public CompProperties_BodyShapeAjuster Props => (CompProperties_BodyShapeAjuster)props;

	public override void Notify_Equipped(Pawn pawn)
	{
		base.Notify_Equipped(pawn);
		if (pawn.story.bodyType == BodyTypeDefOf.Hulk || pawn.story.bodyType == BodyTypeDefOf.Fat)
		{
			BodyShape = pawn.story.bodyType;
			ChangedBS = true;
			if (pawn.gender == Gender.Male)
			{
				pawn.story.bodyType = BodyTypeDefOf.Male;
			}
			else
			{
				pawn.story.bodyType = BodyTypeDefOf.Female;
			}
		}
	}

	public override void Notify_Unequipped(Pawn pawn)
	{
		base.Notify_Unequipped(pawn);
		if (ChangedBS)
		{
			pawn.story.bodyType = BodyShape;
			ChangedBS = false;
		}
	}

	public void ExposeData()
	{
		Scribe_Values.Look(ref BodyShape, "original bodytype", BodyTypeDefOf.Thin);
		Scribe_Values.Look(ref ChangedBS, "if bodytype changed", defaultValue: false);
	}
}
