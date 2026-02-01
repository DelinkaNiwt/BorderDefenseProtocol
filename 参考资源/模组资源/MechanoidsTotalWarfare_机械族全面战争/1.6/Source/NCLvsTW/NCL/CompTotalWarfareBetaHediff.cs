using RimWorld;
using Verse;

namespace NCL;

public class CompTotalWarfareBetaHediff : ThingComp
{
	public bool applyed = false;

	private int CurrentDaySpecialHediffCount => Find.World.GetComponent<TipComponent>()?.TWCurrentDaySpecialHediffCountA ?? 0;

	private int CurrentDayAirstrikeCount => Find.World.GetComponent<TipComponent>()?.TWCurrentDayAirstrikeCount ?? 0;

	private bool ReachedDailySpecialHediffLimit => CurrentDaySpecialHediffCount >= TotalWarfareSettings.MaxSpecialHediffsPerDayA;

	private bool ReachedDailyAirstrikeLimit => CurrentDayAirstrikeCount >= TotalWarfareSettings.MaxAirstrikeHediffsPerDay;

	public CompProperties_TotalWarfareBetaHediff Props => (CompProperties_TotalWarfareBetaHediff)props;

	public override void PostSpawnSetup(bool respawningAfterLoad)
	{
		base.PostSpawnSetup(respawningAfterLoad);
		if (!TotalWarfareSettings.EnableMechEnhancement || TWSettings.ReinforceNotApply || !Find.World.GetComponent<TipComponent>().TWtriggered1 || applyed)
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
		float airstrikeThreshold = TotalWarfareSettings.AirstrikeWealthThreshold;
		if (tipComp != null && tipComp.num >= airstrikeThreshold && tipComp.TWCurrentDayAirstrikeCount < TotalWarfareSettings.MaxAirstrikeHediffsPerDay && Rand.Chance(0.1f))
		{
			HediffDef airstrikeDef = DefDatabase<HediffDef>.GetNamed("TW_TacticalArtilleryCoordinationModule_Airstrike_Signal_Transmitter", errorOnFail: false);
			if (airstrikeDef != null && pawn.health.hediffSet.GetFirstHediffOfDef(airstrikeDef) == null)
			{
				Hediff hediff = HediffMaker.MakeHediff(airstrikeDef, pawn, brain);
				pawn.health.AddHediff(hediff);
				applyed = true;
				tipComp.TWCurrentDayAirstrikeCount++;
				return;
			}
		}
		HediffDef selectedDef = null;
		bool trySpecialHediff = false;
		if (tipComp != null && tipComp.TWCurrentDaySpecialHediffCount < TotalWarfareSettings.MaxSpecialHediffsPerDayA)
		{
			trySpecialHediff = Rand.Chance(0.25f);
		}
		if (trySpecialHediff)
		{
			selectedDef = TWDefOf.TW_TacticalArtilleryCoordinationModule_AutoMortar;
			if (pawn.health.hediffSet.GetFirstHediffOfDef(selectedDef) == null)
			{
				Hediff hediff2 = HediffMaker.MakeHediff(selectedDef, pawn, brain);
				pawn.health.AddHediff(hediff2);
				applyed = true;
				tipComp.TWCurrentDaySpecialHediffCount++;
				return;
			}
		}
		selectedDef = ((pawn.equipment.Primary == null || !pawn.equipment.Primary.def.IsRangedWeapon) ? Props.TWhediffsMelee.RandomElement() : Props.TWhediffsRange.RandomElement());
		if (selectedDef != null && pawn.health.hediffSet.GetFirstHediffOfDef(selectedDef) == null)
		{
			Hediff hediff3 = HediffMaker.MakeHediff(selectedDef, pawn, brain);
			pawn.health.AddHediff(hediff3);
			applyed = true;
		}
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref applyed, "applyed", defaultValue: false);
	}
}
