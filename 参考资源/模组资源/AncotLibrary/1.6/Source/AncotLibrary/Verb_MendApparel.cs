using RimWorld;
using Verse;

namespace AncotLibrary;

public class Verb_MendApparel : Verb
{
	public VerbProperties_MendApparel verbProps_mend => (VerbProperties_MendApparel)verbProps;

	protected override bool TryCastShot()
	{
		return MendApparel(base.EquipmentSource.TryGetComp<CompApparelReloadable>());
	}

	public bool MendApparel(CompApparelReloadable reloadable)
	{
		Pawn pawn = caster as Pawn;
		if (reloadable == null || !reloadable.CanBeUsed(out var _) || pawn == null || pawn.apparel.WornApparel.NullOrEmpty())
		{
			return false;
		}
		Apparel apparel = FindApparelWithLowestHitPoint(pawn, verbProps_mend.percentThreshold);
		if (apparel != null)
		{
			apparel.HitPoints += verbProps_mend.hitPointPerUse;
			if (apparel.HitPoints > apparel.MaxHitPoints)
			{
				apparel.HitPoints = apparel.MaxHitPoints;
			}
			reloadable.UsedOnce();
			return true;
		}
		Messages.Message("Ancot.NoApparelToMend".Translate(pawn.LabelShortCap), null, MessageTypeDefOf.NeutralEvent, historical: false);
		return false;
	}

	public Apparel FindApparelWithLowestHitPoint(Pawn pawn, float maxPrecent)
	{
		Apparel result = null;
		float num = maxPrecent;
		foreach (Apparel item in pawn.apparel.WornApparel)
		{
			if (item.HitPoints > 0)
			{
				float num2 = (float)item.HitPoints / (float)item.MaxHitPoints;
				if (num2 < num)
				{
					num = num2;
					result = item;
				}
			}
		}
		return result;
	}
}
