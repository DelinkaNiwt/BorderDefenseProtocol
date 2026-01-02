using Verse;

namespace Milira;

public class CompMilianApparelChange : ThingComp
{
	private Pawn wearer;

	public CompProperties_MilianApparelChange Props_EquipMilian => (CompProperties_MilianApparelChange)props;

	public CompMilianHairSwitch compMilianHairSwitch => wearer.TryGetComp<CompMilianHairSwitch>();

	public override void Notify_Equipped(Pawn pawn)
	{
		wearer = pawn;
		if (compMilianHairSwitch != null)
		{
			compMilianHairSwitch.DrawHairBool();
		}
	}

	public override void Notify_Unequipped(Pawn pawn)
	{
		if (compMilianHairSwitch != null)
		{
			compMilianHairSwitch.DrawHairBool();
		}
		wearer = null;
	}

	public override void PostDestroy(DestroyMode mode, Map previousMap)
	{
		if (wearer != null)
		{
			if (compMilianHairSwitch != null)
			{
				compMilianHairSwitch.DrawHairBool();
			}
			wearer = null;
		}
		base.PostDestroy(mode, previousMap);
	}

	public override void PostExposeData()
	{
		Scribe_References.Look(ref wearer, "wearer");
		base.PostExposeData();
	}
}
