using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AlienRace;

public class AlienBackstoryDef : BackstoryDef
{
	public static HashSet<BackstoryDef> checkBodyType = new HashSet<BackstoryDef>();

	public List<AlienChanceEntry<TraitWithDegree>> forcedTraitsChance = new List<AlienChanceEntry<TraitWithDegree>>();

	public List<AlienChanceEntry<TraitWithDegree>> disallowedTraitsChance = new List<AlienChanceEntry<TraitWithDegree>>();

	public WorkTags workAllows = WorkTags.AllWork;

	public float maleCommonality = 100f;

	public float femaleCommonality = 100f;

	public BackstoryDef linkedBackstory;

	public RelationSettings relationSettings = new RelationSettings();

	public List<HediffDef> forcedHediffs = new List<HediffDef>();

	public List<SkillGain> passions = new List<SkillGain>();

	public IntRange bioAgeRange;

	public IntRange chronoAgeRange;

	public List<ThingDefCountRangeClass> forcedItems = new List<ThingDefCountRangeClass>();

	public bool CommonalityApproved(Gender g)
	{
		return (float)Rand.Range(0, 100) < ((g == Gender.Female) ? femaleCommonality : maleCommonality);
	}

	public bool Approved(Pawn p)
	{
		if (CommonalityApproved(p.gender) && (bioAgeRange == default(IntRange) || (bioAgeRange.min < p.ageTracker.AgeBiologicalYears && p.ageTracker.AgeBiologicalYears < bioAgeRange.max)))
		{
			if (!(chronoAgeRange == default(IntRange)))
			{
				if (chronoAgeRange.min < p.ageTracker.AgeChronologicalYears)
				{
					return p.ageTracker.AgeChronologicalYears < chronoAgeRange.max;
				}
				return false;
			}
			return true;
		}
		return false;
	}

	public override void ResolveReferences()
	{
		identifier = defName;
		base.ResolveReferences();
		workDisables = (((workAllows & WorkTags.AllWork) != WorkTags.None) ? workDisables : (~workAllows));
		if (bodyTypeGlobal == null && bodyTypeFemale == null && bodyTypeMale == null)
		{
			checkBodyType.Add(this);
			bodyTypeGlobal = DefDatabase<BodyTypeDef>.GetRandom();
		}
	}
}
