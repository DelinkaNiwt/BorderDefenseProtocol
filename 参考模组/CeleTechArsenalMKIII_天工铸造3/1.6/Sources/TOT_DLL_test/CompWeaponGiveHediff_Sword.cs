using RimWorld;
using Verse;

namespace TOT_DLL_test;

public class CompWeaponGiveHediff_Sword : CompBiocodable
{
	public HediffDef named = DefDatabase<HediffDef>.GetNamed("NSwordBinded");

	public int Killcount = 0;

	public int TickToCheck;

	public new CompProperties_WeaponGiveHediff_Sword Props => props as CompProperties_WeaponGiveHediff_Sword;

	public override void Notify_Equipped(Pawn pawn)
	{
		if (Biocodable && Props.biocodeOnEquip)
		{
			CodeFor(pawn);
		}
	}

	public override void CodeFor(Pawn pawn)
	{
		if (Biocodable)
		{
			biocoded = true;
			codedPawn = pawn;
			codedPawnLabel = pawn.Name.ToStringFull;
			if (named != null)
			{
				Hediff hediff = HediffMaker.MakeHediff(named, pawn);
				pawn.health.AddHediff(hediff);
			}
			OnCodedFor(pawn);
		}
	}

	public override void UnCode()
	{
		biocoded = false;
		Pawn pawn = base.CodedPawn;
		Hediff hediff = HediffMaker.MakeHediff(named, pawn);
		pawn.health.RemoveHediff(hediff);
		codedPawn = null;
		codedPawnLabel = null;
	}
}
