using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace AlienRace;

public class RaceRestrictionSettings
{
	public bool onlyUseRaceRestrictedApparel;

	public List<ThingDef> apparelList = new List<ThingDef>();

	public List<ThingDef> whiteApparelList = new List<ThingDef>();

	public List<ThingDef> blackApparelList = new List<ThingDef>();

	public static HashSet<ThingDef> apparelRestricted = new HashSet<ThingDef>();

	public List<ResearchProjectRestrictions> researchList = new List<ResearchProjectRestrictions>();

	public static Dictionary<ResearchProjectDef, List<ThingDef_AlienRace>> researchRestrictionDict = new Dictionary<ResearchProjectDef, List<ThingDef_AlienRace>>();

	public bool onlyUseRaceRestrictedWeapons;

	public List<ThingDef> weaponList = new List<ThingDef>();

	public List<ThingDef> whiteWeaponList = new List<ThingDef>();

	public List<ThingDef> blackWeaponList = new List<ThingDef>();

	public static HashSet<ThingDef> weaponRestricted = new HashSet<ThingDef>();

	public bool onlyBuildRaceRestrictedBuildings;

	public List<BuildableDef> buildingList = new List<BuildableDef>();

	public List<BuildableDef> whiteBuildingList = new List<BuildableDef>();

	public List<BuildableDef> blackBuildingList = new List<BuildableDef>();

	public List<BuildableDef> hiddenBuildingList = new List<BuildableDef>();

	public static readonly HashSet<BuildableDef> buildingRestricted = new HashSet<BuildableDef>();

	public static readonly HashSet<BuildableDef> buildingsHidden = new HashSet<BuildableDef>();

	public static readonly HashSet<BuildableDef> buildingsRestrictedWithCurrentColony = new HashSet<BuildableDef>();

	public bool onlyDoRaceRestrictedRecipes;

	public List<RecipeDef> recipeList = new List<RecipeDef>();

	public List<RecipeDef> whiteRecipeList = new List<RecipeDef>();

	public List<RecipeDef> blackRecipeList = new List<RecipeDef>();

	public static readonly HashSet<RecipeDef> recipeRestricted = new HashSet<RecipeDef>();

	public bool onlyDoRaceRestrictedPlants;

	public List<ThingDef> plantList = new List<ThingDef>();

	public List<ThingDef> whitePlantList = new List<ThingDef>();

	public List<ThingDef> blackPlantList = new List<ThingDef>();

	public static HashSet<ThingDef> plantRestricted = new HashSet<ThingDef>();

	public bool onlyGetRaceRestrictedTraits;

	public List<TraitDef> traitList = new List<TraitDef>();

	public List<TraitDef> whiteTraitList = new List<TraitDef>();

	public List<TraitDef> blackTraitList = new List<TraitDef>();

	public static HashSet<TraitDef> traitRestricted = new HashSet<TraitDef>();

	public bool onlyEatRaceRestrictedFood;

	public List<ThingDef> foodList = new List<ThingDef>();

	public List<ThingDef> whiteFoodList = new List<ThingDef>();

	public List<ThingDef> blackFoodList = new List<ThingDef>();

	public static HashSet<ThingDef> foodRestricted = new HashSet<ThingDef>();

	public bool onlyTameRaceRestrictedPets;

	public List<ThingDef> petList = new List<ThingDef>();

	public List<ThingDef> whitePetList = new List<ThingDef>();

	public List<ThingDef> blackPetList = new List<ThingDef>();

	public static HashSet<ThingDef> petRestricted = new HashSet<ThingDef>();

	public List<ConceptDef> conceptList = new List<ConceptDef>();

	public List<WorkGiverDef> workGiverList = new List<WorkGiverDef>();

	public bool onlyHaveRaceRestrictedGenes;

	public List<GeneDef> geneList = new List<GeneDef>();

	public List<GeneDef> whiteGeneList = new List<GeneDef>();

	public List<string> whiteGeneTags = new List<string>();

	public List<GeneDef> blackGeneList = new List<GeneDef>();

	public List<string> blackGeneTags = new List<string>();

	public List<EndogeneCategory> blackEndoCategories = new List<EndogeneCategory>();

	public static HashSet<GeneDef> geneRestricted = new HashSet<GeneDef>();

	public bool onlyHaveRaceRestrictedGenesXeno;

	public List<GeneDef> geneListXeno = new List<GeneDef>();

	public List<GeneDef> whiteGeneListXeno = new List<GeneDef>();

	public List<string> whiteGeneTagsXeno = new List<string>();

	public List<GeneDef> blackGeneListXeno = new List<GeneDef>();

	public List<string> blackGeneTagsXeno = new List<string>();

	public List<EndogeneCategory> blackEndoCategoriesXeno = new List<EndogeneCategory>();

	public static HashSet<GeneDef> geneRestrictedXeno = new HashSet<GeneDef>();

	public bool onlyHaveRaceRestrictedGenesEndo;

	public List<GeneDef> geneListEndo = new List<GeneDef>();

	public List<GeneDef> whiteGeneListEndo = new List<GeneDef>();

	public List<string> whiteGeneTagsEndo = new List<string>();

	public List<GeneDef> blackGeneListEndo = new List<GeneDef>();

	public List<string> blackGeneTagsEndo = new List<string>();

	public List<EndogeneCategory> blackEndoCategoriesEndo = new List<EndogeneCategory>();

	public static HashSet<GeneDef> geneRestrictedEndo = new HashSet<GeneDef>();

	public bool onlyUseRaceRestrictedXenotypes;

	public List<XenotypeDef> xenotypeList = new List<XenotypeDef>();

	public List<XenotypeDef> whiteXenotypeList = new List<XenotypeDef>();

	public List<XenotypeDef> blackXenotypeList = new List<XenotypeDef>();

	public static HashSet<XenotypeDef> xenotypeRestricted = new HashSet<XenotypeDef>();

	public bool canReproduce = true;

	public bool canReproduceWithSelf = true;

	public bool onlyReproduceWithRestrictedRaces;

	public List<ThingDef> reproductionList = new List<ThingDef>();

	public List<ThingDef> whiteReproductionList = new List<ThingDef>();

	public List<ThingDef> blackReproductionList = new List<ThingDef>();

	public static HashSet<ThingDef> reproductionRestricted = new HashSet<ThingDef>();

	public static bool CanWear(ThingDef apparel, ThingDef race)
	{
		RaceRestrictionSettings raceRestriction = (race as ThingDef_AlienRace)?.alienRace.raceRestriction;
		bool result = true;
		if (apparelRestricted.Contains(apparel) || (raceRestriction != null && raceRestriction.onlyUseRaceRestrictedApparel))
		{
			result = raceRestriction?.whiteApparelList.Contains(apparel) ?? false;
		}
		if (result)
		{
			return !(raceRestriction?.blackApparelList.Contains(apparel) ?? false);
		}
		return false;
	}

	public static bool CanResearch(IEnumerable<ThingDef> races, ResearchProjectDef project)
	{
		if (researchRestrictionDict.ContainsKey(project))
		{
			return races.Any((ThingDef ar) => researchRestrictionDict[project].Contains(ar));
		}
		return true;
	}

	public static bool CanEquip(ThingDef weapon, ThingDef race)
	{
		RaceRestrictionSettings raceRestriction = (race as ThingDef_AlienRace)?.alienRace.raceRestriction;
		bool result = true;
		if (weaponRestricted.Contains(weapon) || (raceRestriction != null && raceRestriction.onlyUseRaceRestrictedWeapons))
		{
			result = raceRestriction?.whiteWeaponList.Contains(weapon) ?? false;
		}
		if (result)
		{
			return !(raceRestriction?.blackWeaponList.Contains(weapon) ?? false);
		}
		return false;
	}

	public static bool CanColonyBuild(BuildableDef building)
	{
		return !buildingsRestrictedWithCurrentColony.Contains(building);
	}

	public static bool CanBuild(BuildableDef building, ThingDef race)
	{
		RaceRestrictionSettings raceRestriction = (race as ThingDef_AlienRace)?.alienRace.raceRestriction;
		bool result = true;
		if (buildingRestricted.Contains(building) || (raceRestriction != null && raceRestriction.onlyBuildRaceRestrictedBuildings))
		{
			result = raceRestriction?.whiteBuildingList.Contains(building) ?? false;
		}
		if (result)
		{
			return !(raceRestriction?.blackBuildingList.Contains(building) ?? false);
		}
		return false;
	}

	public static bool CanDoRecipe(RecipeDef recipe, ThingDef race)
	{
		RaceRestrictionSettings raceRestriction = (race as ThingDef_AlienRace)?.alienRace.raceRestriction;
		bool result = true;
		if (recipeRestricted.Contains(recipe) || (raceRestriction != null && raceRestriction.onlyDoRaceRestrictedRecipes))
		{
			result = raceRestriction?.whiteRecipeList.Contains(recipe) ?? false;
		}
		if (result)
		{
			return !(raceRestriction?.blackRecipeList.Contains(recipe) ?? false);
		}
		return false;
	}

	public static bool CanPlant(ThingDef plant, ThingDef race)
	{
		RaceRestrictionSettings raceRestriction = (race as ThingDef_AlienRace)?.alienRace.raceRestriction;
		bool result = true;
		if (plantRestricted.Contains(plant) || (raceRestriction != null && raceRestriction.onlyDoRaceRestrictedPlants))
		{
			result = raceRestriction?.whitePlantList.Contains(plant) ?? false;
		}
		if (result)
		{
			return !(raceRestriction?.blackPlantList.Contains(plant) ?? false);
		}
		return false;
	}

	public static bool CanGetTrait(TraitDef trait, Pawn pawn, int degree = 0)
	{
		List<AlienChanceEntry<TraitWithDegree>> disallowedTraits = new List<AlienChanceEntry<TraitWithDegree>>();
		foreach (BackstoryDef backstory in pawn.story.AllBackstories)
		{
			if (backstory is AlienBackstoryDef alienBackstory && !alienBackstory.disallowedTraitsChance.NullOrEmpty())
			{
				disallowedTraits.AddRange(alienBackstory.disallowedTraitsChance);
			}
		}
		return CanGetTrait(trait, pawn.def, degree, disallowedTraits);
	}

	public static bool CanGetTrait(TraitDef trait, ThingDef race, int degree = 0, List<AlienChanceEntry<TraitWithDegree>> disallowedTraits = null)
	{
		ThingDef_AlienRace.AlienSettings alienProps = (race as ThingDef_AlienRace)?.alienRace;
		RaceRestrictionSettings raceRestriction = alienProps?.raceRestriction;
		bool result = true;
		if (traitRestricted.Contains(trait) || (raceRestriction != null && raceRestriction.onlyGetRaceRestrictedTraits))
		{
			result &= raceRestriction?.whiteTraitList.Contains(trait) ?? false;
		}
		if (disallowedTraits == null)
		{
			disallowedTraits = new List<AlienChanceEntry<TraitWithDegree>>();
		}
		if (alienProps != null && !alienProps.generalSettings.disallowedTraits.NullOrEmpty())
		{
			disallowedTraits.AddRange(alienProps.generalSettings.disallowedTraits);
		}
		if (!disallowedTraits.NullOrEmpty())
		{
			result &= disallowedTraits.All((AlienChanceEntry<TraitWithDegree> ace) => ace.Select(null).All((TraitWithDegree traitEntry) => traitEntry.def != trait || degree != traitEntry.degree));
		}
		if (result)
		{
			return !(raceRestriction?.blackTraitList.Contains(trait) ?? false);
		}
		return false;
	}

	public static bool CanEat(ThingDef food, ThingDef race)
	{
		RaceRestrictionSettings raceRestriction = (race as ThingDef_AlienRace)?.alienRace.raceRestriction;
		bool result = true;
		if (foodRestricted.Contains(food) || (raceRestriction != null && raceRestriction.onlyEatRaceRestrictedFood))
		{
			result = raceRestriction?.whiteFoodList.Contains(food) ?? false;
		}
		result &= !(raceRestriction?.blackFoodList.Contains(food) ?? false);
		ChemicalDef chemical = food.GetCompProperties<CompProperties_Drug>()?.chemical;
		if (result)
		{
			if (chemical != null)
			{
				return (race as ThingDef_AlienRace)?.alienRace.generalSettings.CanUseChemical(chemical) ?? true;
			}
			return true;
		}
		return false;
	}

	public static bool CanTame(ThingDef pet, ThingDef race)
	{
		RaceRestrictionSettings raceRestriction = (race as ThingDef_AlienRace)?.alienRace.raceRestriction;
		bool result = true;
		if (petRestricted.Contains(pet) || (raceRestriction != null && raceRestriction.onlyTameRaceRestrictedPets))
		{
			result = raceRestriction?.whitePetList.Contains(pet) ?? false;
		}
		if (result)
		{
			return !(raceRestriction?.blackPetList.Contains(pet) ?? false);
		}
		return false;
	}

	public static bool CanHaveGene(GeneDef gene, ThingDef race, bool xeno)
	{
		RaceRestrictionSettings raceRestriction = (race as ThingDef_AlienRace)?.alienRace.raceRestriction;
		bool result = true;
		if (!geneRestricted.Contains(gene))
		{
			RaceRestrictionSettings raceRestrictionSettings = raceRestriction;
			if (raceRestrictionSettings == null || !raceRestrictionSettings.onlyHaveRaceRestrictedGenes)
			{
				goto IL_0082;
			}
		}
		RaceRestrictionSettings raceRestrictionSettings2 = raceRestriction;
		result = (raceRestrictionSettings2 != null && raceRestrictionSettings2.whiteGeneList.Contains(gene)) || (gene.exclusionTags?.Any((string t) => raceRestriction?.whiteGeneTags.Any(t.StartsWith) ?? false) ?? false);
		goto IL_0082;
		IL_00e7:
		bool num = result;
		RaceRestrictionSettings raceRestrictionSettings3 = raceRestriction;
		int num2;
		if (raceRestrictionSettings3 == null || !raceRestrictionSettings3.blackGeneListXeno.Contains(gene))
		{
			List<string> exclusionTags = gene.exclusionTags;
			if (exclusionTags == null || !exclusionTags.Any((string t) => raceRestriction?.blackGeneTagsXeno.Any(t.StartsWith) ?? false))
			{
				num2 = ((!(raceRestriction?.blackEndoCategoriesXeno.Contains(gene.endogeneCategory) ?? false)) ? 1 : 0);
				goto IL_0145;
			}
		}
		num2 = 0;
		goto IL_0145;
		IL_0209:
		int num3;
		int num4;
		result = (byte)(num3 & num4) != 0;
		goto IL_020b;
		IL_0145:
		result = (byte)((num ? 1u : 0u) & (uint)num2) != 0;
		goto IL_020b;
		IL_020b:
		if (gene.chemical != null)
		{
			result &= (race as ThingDef_AlienRace)?.alienRace.generalSettings.CanUseChemical(gene.chemical) ?? true;
		}
		if (result)
		{
			RaceRestrictionSettings raceRestrictionSettings4 = raceRestriction;
			if (raceRestrictionSettings4 == null || !raceRestrictionSettings4.blackGeneList.Contains(gene))
			{
				List<string> exclusionTags2 = gene.exclusionTags;
				if (exclusionTags2 == null || !exclusionTags2.Any((string t) => raceRestriction?.blackGeneTags.Any(t.StartsWith) ?? false))
				{
					return !(raceRestriction?.blackEndoCategories.Contains(gene.endogeneCategory) ?? false);
				}
			}
		}
		return false;
		IL_0082:
		if (xeno)
		{
			if (!geneRestrictedXeno.Contains(gene))
			{
				RaceRestrictionSettings raceRestrictionSettings5 = raceRestriction;
				if (raceRestrictionSettings5 == null || !raceRestrictionSettings5.onlyHaveRaceRestrictedGenesXeno)
				{
					goto IL_00e7;
				}
			}
			bool num5 = result;
			RaceRestrictionSettings raceRestrictionSettings6 = raceRestriction;
			result = num5 & ((raceRestrictionSettings6 != null && raceRestrictionSettings6.whiteGeneListXeno.Contains(gene)) || (gene.exclusionTags?.Any((string t) => raceRestriction?.whiteGeneTagsXeno.Any(t.StartsWith) ?? false) ?? false));
			goto IL_00e7;
		}
		if (!geneRestrictedEndo.Contains(gene))
		{
			RaceRestrictionSettings raceRestrictionSettings7 = raceRestriction;
			if (raceRestrictionSettings7 == null || !raceRestrictionSettings7.onlyHaveRaceRestrictedGenesEndo)
			{
				goto IL_01ab;
			}
		}
		bool num6 = result;
		RaceRestrictionSettings raceRestrictionSettings8 = raceRestriction;
		result = num6 & ((raceRestrictionSettings8 != null && raceRestrictionSettings8.whiteGeneListEndo.Contains(gene)) || (gene.exclusionTags?.Any((string t) => raceRestriction?.whiteGeneTagsEndo.Any(t.StartsWith) ?? false) ?? false));
		goto IL_01ab;
		IL_01ab:
		num3 = (result ? 1 : 0);
		RaceRestrictionSettings raceRestrictionSettings9 = raceRestriction;
		if (raceRestrictionSettings9 == null || !raceRestrictionSettings9.blackGeneListEndo.Contains(gene))
		{
			List<string> exclusionTags3 = gene.exclusionTags;
			if (exclusionTags3 == null || !exclusionTags3.Any((string t) => raceRestriction?.blackGeneTagsEndo.Any(t.StartsWith) ?? false))
			{
				num4 = ((!(raceRestriction?.blackEndoCategoriesEndo.Contains(gene.endogeneCategory) ?? false)) ? 1 : 0);
				goto IL_0209;
			}
		}
		num4 = 0;
		goto IL_0209;
	}

	public static HashSet<XenotypeDef> FilterXenotypes(IEnumerable<XenotypeDef> xenotypes, ThingDef race, out HashSet<XenotypeDef> removedXenotypes)
	{
		HashSet<XenotypeDef> xenotypeDefs = new HashSet<XenotypeDef>();
		removedXenotypes = new HashSet<XenotypeDef>();
		foreach (XenotypeDef xenotypeDef in xenotypes)
		{
			if (CanUseXenotype(xenotypeDef, race))
			{
				xenotypeDefs.Add(xenotypeDef);
			}
			else
			{
				removedXenotypes.Add(xenotypeDef);
			}
		}
		return xenotypeDefs;
	}

	public static bool CanUseXenotype(XenotypeDef xenotype, ThingDef race)
	{
		RaceRestrictionSettings raceRestriction = (race as ThingDef_AlienRace)?.alienRace.raceRestriction;
		bool result = true;
		if (xenotypeRestricted.Contains(xenotype) || (raceRestriction != null && raceRestriction.onlyUseRaceRestrictedXenotypes))
		{
			result = raceRestriction?.whiteXenotypeList.Contains(xenotype) ?? false;
		}
		if (result)
		{
			return !(raceRestriction?.blackXenotypeList.Contains(xenotype) ?? false);
		}
		return false;
	}

	public static bool CanReproduce(Pawn pawn, Pawn partnerPawn)
	{
		if (ReproductionSettings.GenderReproductionCheck(pawn, partnerPawn))
		{
			return CanReproduce(pawn.def, partnerPawn.def);
		}
		return false;
	}

	public static bool CanReproduce(ThingDef race, ThingDef partnerRace)
	{
		if (CanReproduceWith(race, partnerRace))
		{
			return CanReproduceWith(partnerRace, race);
		}
		return false;
	}

	private static bool CanReproduceWith(ThingDef race, ThingDef partnerRace)
	{
		RaceRestrictionSettings raceRestriction = (race as ThingDef_AlienRace)?.alienRace.raceRestriction;
		if (raceRestriction != null && !raceRestriction.canReproduce)
		{
			return false;
		}
		if (race == partnerRace)
		{
			return raceRestriction?.canReproduceWithSelf ?? true;
		}
		bool result = true;
		if (reproductionRestricted.Contains(partnerRace) || (raceRestriction != null && raceRestriction.onlyReproduceWithRestrictedRaces))
		{
			result = raceRestriction?.whiteReproductionList.Contains(partnerRace) ?? false;
		}
		if (result)
		{
			return !(raceRestriction?.blackReproductionList.Contains(partnerRace) ?? false);
		}
		return false;
	}
}
