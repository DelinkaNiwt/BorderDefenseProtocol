using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AlienRace;

public class GeneralSettings
{
	public float maleGenderProbability = 0.5f;

	public bool immuneToAge;

	public bool canLayDown = true;

	public float minAgeForAdulthood = -1f;

	public List<ThingDef> validBeds = new List<ThingDef>();

	public static HashSet<ThingDef> lockedBeds = new HashSet<ThingDef>();

	public List<ChemicalSettings> chemicalSettings;

	public List<AlienChanceEntry<TraitWithDegree>> forcedRaceTraitEntries;

	public List<AlienChanceEntry<TraitWithDegree>> disallowedTraits;

	public IntRange additionalTraits = IntRange.Zero;

	public AlienPartGenerator alienPartGenerator = new AlienPartGenerator();

	public List<SkillGain> passions = new List<SkillGain>();

	public List<AlienChanceEntry<AbilityDef>> abilities = new List<AlienChanceEntry<AbilityDef>>();

	public List<FactionRelationSettings> factionRelations = new List<FactionRelationSettings>();

	public int maxDamageForSocialfight = int.MaxValue;

	public bool allowHumanBios;

	public bool immuneToXenophobia;

	public List<ThingDef> notXenophobistTowards = new List<ThingDef>();

	public bool humanRecipeImport;

	[LoadDefFromField("HAR_AlienCorpseCategory")]
	public ThingCategoryDef corpseCategory;

	public SimpleCurve lovinIntervalHoursFromAge;

	public List<int> growthAges = new List<int> { 7, 10, 13 };

	public SimpleCurve growthFactorByAge;

	public SimpleCurve ageSkillFactorCurve;

	public List<BackstoryCategoryFilter> childBackstoryFilter;

	public List<BackstoryCategoryFilter> adultBackstoryFilter;

	public List<BackstoryCategoryFilter> adultVatBackstoryFilter;

	public List<BackstoryCategoryFilter> newbornBackstoryFilter;

	public ReproductionSettings reproduction = new ReproductionSettings();

	public List<AlienChanceEntry<GeneDef>> raceGenes = new List<AlienChanceEntry<GeneDef>>();

	internal List<StatPartAgeOverride> ageStatOverrides = new List<StatPartAgeOverride>();

	[Unsaved(false)]
	public Dictionary<StatDef, StatPart_Age> ageStatOverride = new Dictionary<StatDef, StatPart_Age>();

	public List<MeditationFocusDef> meditationFocii = new List<MeditationFocusDef>();

	public int[] GrowthAges => growthAges?.ToArray();

	public bool CanUseBed(ThingDef bedDef)
	{
		if (!validBeds.Contains(bedDef))
		{
			if (validBeds.NullOrEmpty())
			{
				return !lockedBeds.Contains(bedDef);
			}
			return false;
		}
		return true;
	}

	public bool CanUseChemical(ChemicalDef chemical)
	{
		if (chemicalSettings.NullOrEmpty() || chemical == null)
		{
			return true;
		}
		foreach (ChemicalSettings cs in chemicalSettings)
		{
			if (cs.chemical == chemical && !cs.ingestible)
			{
				return false;
			}
		}
		return true;
	}
}
