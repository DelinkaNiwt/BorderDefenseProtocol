using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AlienRace;

public class ReproductionSettings
{
	public PawnKindDef childKindDef;

	public SimpleCurve maleFertilityAgeFactor = new SimpleCurve(new _003C_003Ez__ReadOnlyArray<CurvePoint>(new CurvePoint[4]
	{
		new CurvePoint(14f, 0f),
		new CurvePoint(18f, 1f),
		new CurvePoint(50f, 1f),
		new CurvePoint(90f, 0f)
	}));

	public SimpleCurve femaleFertilityAgeFactor = new SimpleCurve(new _003C_003Ez__ReadOnlyArray<CurvePoint>(new CurvePoint[7]
	{
		new CurvePoint(14f, 0f),
		new CurvePoint(20f, 1f),
		new CurvePoint(28f, 1f),
		new CurvePoint(35f, 0.5f),
		new CurvePoint(40f, 0.1f),
		new CurvePoint(45f, 0.02f),
		new CurvePoint(50f, 0f)
	}));

	public List<HybridSpecificSettings> hybridSpecific = new List<HybridSpecificSettings>();

	public GenderPossibility fertilizingGender;

	public GenderPossibility gestatingGender = GenderPossibility.Female;

	public static bool ApplicableGender(Pawn pawn, bool gestating)
	{
		ReproductionSettings reproduction = (pawn.def as ThingDef_AlienRace)?.alienRace.generalSettings.reproduction ?? new ReproductionSettings();
		return ApplicableGender(pawn.gender, reproduction, gestating);
	}

	public static bool ApplicableGender(Gender gender, ReproductionSettings reproduction, bool gestating)
	{
		if (gestating)
		{
			if (reproduction.gestatingGender.IsGenderApplicable(gender))
			{
				return true;
			}
		}
		else if (reproduction.fertilizingGender.IsGenderApplicable(gender))
		{
			return true;
		}
		return false;
	}

	public static bool GenderReproductionCheck(Pawn pawn, Pawn partnerPawn)
	{
		ReproductionSettings pawnReproduction = (pawn.def as ThingDef_AlienRace)?.alienRace.generalSettings.reproduction ?? new ReproductionSettings();
		ReproductionSettings partnerReproduction = (partnerPawn.def as ThingDef_AlienRace)?.alienRace.generalSettings.reproduction ?? new ReproductionSettings();
		if (!ApplicableGender(pawn.gender, pawnReproduction, gestating: false) || !ApplicableGender(partnerPawn.gender, partnerReproduction, gestating: true))
		{
			if (ApplicableGender(pawn.gender, pawnReproduction, gestating: true))
			{
				return ApplicableGender(partnerPawn.gender, partnerReproduction, gestating: false);
			}
			return false;
		}
		return true;
	}
}
