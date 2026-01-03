using RimWorld;
using Verse;

namespace AncotLibrary;

public class HediffComp_RemoveIfShieldDropped : HediffComp
{
	public Apparel wornApparel;

	public HediffCompProperties_RemoveIfShieldDropped Props => (HediffCompProperties_RemoveIfShieldDropped)props;

	public override bool CompShouldRemove => !parent.pawn.apparel.Wearing(wornApparel);

	public override void CompExposeData()
	{
		base.CompExposeData();
		Scribe_References.Look(ref wornApparel, "wornApparel");
	}
}
