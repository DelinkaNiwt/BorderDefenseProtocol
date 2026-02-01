using RimWorld;
using Verse;

namespace NCL;

public class CompTotalWarfareAlphaHediff : ThingComp
{
	public bool applyed = false;

	public CompProperties_TotalWarfareAlphaHediff Props => (CompProperties_TotalWarfareAlphaHediff)props;

	public override void PostSpawnSetup(bool respawningAfterLoad)
	{
		base.PostSpawnSetup(respawningAfterLoad);
		if (!TotalWarfareSettings.EnableMechEnhancement || TWSettings.ReinforceNotApply || !Find.World.GetComponent<TipComponent>().TWtriggered3 || applyed)
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
		if (brain != null && (pawn.Faction == null || pawn.Faction.HostileTo(Faction.OfPlayer)))
		{
			HediffDef def = ((pawn.equipment.Primary == null || !pawn.equipment.Primary.def.IsRangedWeapon) ? Props.TWhediffsMelee.RandomElement() : Props.TWhediffsRange.RandomElement());
			if (Rand.Chance(0.1f))
			{
				def = TWDefOf.TW_TacticalArtilleryCoordinationModule_AutoMortar;
			}
			object obj = pawn.health?.hediffSet?.GetFirstHediffOfDef(def);
			if (obj == null)
			{
				Hediff hediff = HediffMaker.MakeHediff(def, pawn);
				pawn.health.AddHediff(hediff, brain);
				applyed = true;
			}
		}
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref applyed, "applyed", defaultValue: false);
	}
}
