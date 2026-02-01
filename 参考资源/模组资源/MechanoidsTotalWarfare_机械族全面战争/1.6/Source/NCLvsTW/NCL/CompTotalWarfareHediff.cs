using RimWorld;
using Verse;

namespace NCL;

public class CompTotalWarfareHediff : ThingComp
{
	public bool applyed = false;

	private int CurrentDaySpecialHediffCount => Find.World.GetComponent<TipComponent>()?.TWCurrentDaySpecialHediffCount ?? 0;

	private bool ReachedDailySpecialHediffLimit => CurrentDaySpecialHediffCount >= TotalWarfareSettings.MaxSpecialHediffsPerDay;

	public CompProperties_TotalWarfareHediff Props => (CompProperties_TotalWarfareHediff)props;

	public override void PostSpawnSetup(bool respawningAfterLoad)
	{
		base.PostSpawnSetup(respawningAfterLoad);
		if (!TotalWarfareSettings.EnableAutoTrigger || TWSettings.ReinforceNotApply || !Find.World.GetComponent<TipComponent>().TWtriggered2 || applyed)
		{
			applyed = true;
			return;
		}
		Pawn pawn = parent as Pawn;
		if (pawn.def.defName == "TW_Mech_Doxa" || pawn.def.defName == "TW_Mech_Shell_Fortification" || pawn.def.defName == "Mech_BlackApocriton")
		{
			return;
		}
		BodyPartRecord brain = pawn.health.hediffSet.GetBrain();
		if (brain == null || (pawn.Faction != null && !pawn.Faction.HostileTo(Faction.OfPlayer)))
		{
			return;
		}
		TipComponent tipComp = Find.World.GetComponent<TipComponent>();
		HediffDef selectedDef = null;
		bool trySpecialHediff = false;
		if (tipComp != null && tipComp.TWCurrentDaySpecialHediffCount < TotalWarfareSettings.MaxSpecialHediffsPerDay)
		{
			trySpecialHediff = Rand.Chance(0.25f);
		}
		if (trySpecialHediff)
		{
			selectedDef = TWDefOf.TW_TacticalArtilleryCoordinationModule_AutoMortarEMP;
			if (pawn.health.hediffSet.GetFirstHediffOfDef(selectedDef) == null)
			{
				Hediff hediff = HediffMaker.MakeHediff(selectedDef, pawn, brain);
				pawn.health.AddHediff(hediff);
				applyed = true;
				tipComp.TWCurrentDaySpecialHediffCount++;
				return;
			}
		}
		selectedDef = ((pawn.equipment.Primary == null || !pawn.equipment.Primary.def.IsRangedWeapon) ? Props.TWhediffsMelee.RandomElement() : Props.TWhediffsRange.RandomElement());
		if (selectedDef != null && pawn.health.hediffSet.GetFirstHediffOfDef(selectedDef) == null)
		{
			Hediff hediff2 = HediffMaker.MakeHediff(selectedDef, pawn, brain);
			pawn.health.AddHediff(hediff2);
			applyed = true;
		}
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref applyed, "applyed", defaultValue: false);
	}
}
