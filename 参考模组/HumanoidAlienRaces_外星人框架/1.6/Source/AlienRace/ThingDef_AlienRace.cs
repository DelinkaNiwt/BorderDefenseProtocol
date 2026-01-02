using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AlienRace.ExtendedGraphics;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace AlienRace;

public class ThingDef_AlienRace : ThingDef
{
	public class AlienSettings
	{
		public GeneralSettings generalSettings = new GeneralSettings();

		public GraphicPaths graphicPaths = new GraphicPaths();

		public Dictionary<Type, StyleSettings> styleSettings = new Dictionary<Type, StyleSettings>();

		public ThoughtSettings thoughtSettings = new ThoughtSettings();

		public RelationSettings relationSettings = new RelationSettings();

		public RaceRestrictionSettings raceRestriction = new RaceRestrictionSettings();

		public CompatibilityInfo compatibility = new CompatibilityInfo();
	}

	public AlienSettings alienRace;

	public override void ResolveReferences()
	{
		if (!HasComp<AlienPartGenerator.AlienComp>())
		{
			comps.Add(new CompProperties(typeof(AlienPartGenerator.AlienComp)));
		}
		base.ResolveReferences();
		if (alienRace.generalSettings.alienPartGenerator.customHeadDrawSize.Equals(Vector2.zero))
		{
			alienRace.generalSettings.alienPartGenerator.customHeadDrawSize = alienRace.generalSettings.alienPartGenerator.customDrawSize;
		}
		if (alienRace.generalSettings.alienPartGenerator.customPortraitHeadDrawSize.Equals(Vector2.zero))
		{
			alienRace.generalSettings.alienPartGenerator.customPortraitHeadDrawSize = alienRace.generalSettings.alienPartGenerator.customPortraitDrawSize;
		}
		if (alienRace.generalSettings.alienPartGenerator.headFemaleOffset.Equals(Vector2.negativeInfinity))
		{
			alienRace.generalSettings.alienPartGenerator.headFemaleOffset = alienRace.generalSettings.alienPartGenerator.headOffset;
		}
		AlienPartGenerator alienPartGenerator = alienRace.generalSettings.alienPartGenerator;
		if (alienPartGenerator.headFemaleOffsetDirectional == null)
		{
			alienPartGenerator.headFemaleOffsetDirectional = alienRace.generalSettings.alienPartGenerator.headOffsetDirectional;
		}
		alienRace.generalSettings.alienPartGenerator.alienProps = this;
		foreach (Type type in typeof(StyleItemDef).AllSubclassesNonAbstract())
		{
			if (!alienRace.styleSettings.ContainsKey(type))
			{
				alienRace.styleSettings.Add(type, new StyleSettings());
			}
		}
		foreach (AlienPartGenerator.BodyAddon bodyAddon in alienRace.generalSettings.alienPartGenerator.bodyAddons)
		{
			AlienPartGenerator.DirectionalOffset offsets = bodyAddon.offsets;
			if (offsets.west == null)
			{
				offsets.west = bodyAddon.offsets.east;
			}
		}
		if (alienRace.generalSettings.minAgeForAdulthood < 0f)
		{
			alienRace.generalSettings.minAgeForAdulthood = (float)AccessTools.Field(typeof(PawnBioAndNameGenerator), "MinAgeForAdulthood").GetValue(null);
		}
		foreach (StatPartAgeOverride spao in alienRace.generalSettings.ageStatOverrides)
		{
			alienRace.generalSettings.ageStatOverride[spao.stat] = spao.overridePart;
		}
		for (int i = 0; i < race.lifeStageAges.Count; i++)
		{
			LifeStageAge lsa = race.lifeStageAges[i];
			LifeStageAgeAlien lsaa = lsa as LifeStageAgeAlien;
			if (lsaa == null)
			{
				lsaa = new LifeStageAgeAlien
				{
					def = lsa.def,
					minAge = lsa.minAge,
					soundAmbience = lsa.soundAmbience,
					soundAngry = lsa.soundAngry,
					soundCall = lsa.soundCall,
					soundDeath = lsa.soundDeath,
					soundWounded = lsa.soundWounded
				};
				race.lifeStageAges[i] = lsaa;
			}
			if (lsaa.customDrawSize.Equals(Vector2.zero))
			{
				lsaa.customDrawSize = alienRace.generalSettings.alienPartGenerator.customDrawSize;
			}
			if (lsaa.customPortraitDrawSize.Equals(Vector2.zero))
			{
				lsaa.customPortraitDrawSize = alienRace.generalSettings.alienPartGenerator.customPortraitDrawSize;
			}
			if (lsaa.customHeadDrawSize.Equals(Vector2.zero))
			{
				lsaa.customHeadDrawSize = alienRace.generalSettings.alienPartGenerator.customHeadDrawSize;
			}
			if (lsaa.customPortraitHeadDrawSize.Equals(Vector2.zero))
			{
				lsaa.customPortraitHeadDrawSize = alienRace.generalSettings.alienPartGenerator.customPortraitHeadDrawSize;
			}
			if (lsaa.customFemaleDrawSize.Equals(Vector2.zero))
			{
				lsaa.customFemaleDrawSize = alienRace.generalSettings.alienPartGenerator.customFemaleDrawSize;
			}
			if (lsaa.customFemalePortraitDrawSize.Equals(Vector2.zero))
			{
				lsaa.customFemalePortraitDrawSize = alienRace.generalSettings.alienPartGenerator.customFemalePortraitDrawSize;
			}
			if (lsaa.customFemaleHeadDrawSize.Equals(Vector2.zero))
			{
				lsaa.customFemaleHeadDrawSize = alienRace.generalSettings.alienPartGenerator.customFemaleHeadDrawSize;
			}
			if (lsaa.customFemalePortraitHeadDrawSize.Equals(Vector2.zero))
			{
				lsaa.customFemalePortraitHeadDrawSize = alienRace.generalSettings.alienPartGenerator.customFemalePortraitHeadDrawSize;
			}
			if (lsaa.headOffset.Equals(Vector2.zero))
			{
				lsaa.headOffset = alienRace.generalSettings.alienPartGenerator.headOffset;
			}
			if (lsaa.headFemaleOffset.Equals(Vector2.negativeInfinity))
			{
				lsaa.headFemaleOffset = alienRace.generalSettings.alienPartGenerator.headFemaleOffset;
			}
			LifeStageAgeAlien lifeStageAgeAlien = lsaa;
			if (lifeStageAgeAlien.headOffsetDirectional == null)
			{
				lifeStageAgeAlien.headOffsetDirectional = alienRace.generalSettings.alienPartGenerator.headOffsetDirectional;
			}
			AlienPartGenerator.DirectionalOffset offsets = lsaa.headOffsetDirectional;
			if (offsets.west == null)
			{
				offsets.west = lsaa.headOffsetDirectional.east;
			}
			lifeStageAgeAlien = lsaa;
			if (lifeStageAgeAlien.headFemaleOffsetDirectional == null)
			{
				lifeStageAgeAlien.headFemaleOffsetDirectional = alienRace.generalSettings.alienPartGenerator.headFemaleOffsetDirectional;
			}
			offsets = lsaa.headFemaleOffsetDirectional;
			if (offsets.west == null)
			{
				offsets.west = lsaa.headFemaleOffsetDirectional.east;
			}
		}
		if (alienRace.graphicPaths.head.path == "Things/Pawn/Humanlike/Heads/" && !alienRace.graphicPaths.head.GetSubGraphics().Any())
		{
			foreach (HeadTypeDef headType in DefDatabase<HeadTypeDef>.AllDefs)
			{
				AlienPartGenerator.ExtendedConditionGraphic headtypeGraphic = new AlienPartGenerator.ExtendedConditionGraphic
				{
					conditions = new List<Condition>(1)
					{
						new ConditionHeadType
						{
							headType = headType
						}
					},
					path = headType.graphicPath
				};
				alienRace.graphicPaths.head.extendedGraphics.Add(headtypeGraphic);
			}
		}
		if (alienRace.graphicPaths.skeleton.path == "Things/Pawn/Humanlike/HumanoidDessicated" && !alienRace.graphicPaths.skeleton.GetSubGraphics().Any())
		{
			alienRace.graphicPaths.skeleton.path = string.Empty;
			foreach (BodyTypeDef bodyType in alienRace.generalSettings.alienPartGenerator.bodyTypes)
			{
				alienRace.graphicPaths.skeleton.extendedGraphics.Add(new AlienPartGenerator.ExtendedConditionGraphic
				{
					conditions = new List<Condition>(1)
					{
						new ConditionBodyType
						{
							bodyType = bodyType
						}
					},
					path = bodyType.bodyDessicatedGraphicPath
				});
			}
		}
		RecursiveAttributeCheck(typeof(AlienSettings), Traverse.Create(alienRace), defName);
		foreach (ThingDef bedDef in alienRace.generalSettings.validBeds)
		{
			GeneralSettings.lockedBeds.Add(bedDef);
		}
		foreach (ThoughtDef thoughtDef in alienRace.thoughtSettings.restrictedThoughts)
		{
			if (!ThoughtSettings.thoughtRestrictionDict.ContainsKey(thoughtDef))
			{
				ThoughtSettings.thoughtRestrictionDict.Add(thoughtDef, new List<ThingDef_AlienRace>());
			}
			ThoughtSettings.thoughtRestrictionDict[thoughtDef].Add(this);
		}
		foreach (ThingDef thingDef in alienRace.raceRestriction.apparelList)
		{
			RaceRestrictionSettings.apparelRestricted.Add(thingDef);
			alienRace.raceRestriction.whiteApparelList.Add(thingDef);
		}
		foreach (ThingDef thingDef2 in alienRace.raceRestriction.weaponList)
		{
			RaceRestrictionSettings.weaponRestricted.Add(thingDef2);
			alienRace.raceRestriction.whiteWeaponList.Add(thingDef2);
		}
		foreach (BuildableDef thingDef3 in alienRace.raceRestriction.buildingList)
		{
			RaceRestrictionSettings.buildingRestricted.Add(thingDef3);
			alienRace.raceRestriction.whiteBuildingList.Add(thingDef3);
		}
		foreach (BuildableDef thingDef4 in alienRace.raceRestriction.hiddenBuildingList)
		{
			RaceRestrictionSettings.buildingsHidden.Add(thingDef4);
		}
		foreach (RecipeDef recipeDef in alienRace.raceRestriction.recipeList)
		{
			RaceRestrictionSettings.recipeRestricted.Add(recipeDef);
			alienRace.raceRestriction.whiteRecipeList.Add(recipeDef);
		}
		foreach (ThingDef thingDef5 in alienRace.raceRestriction.plantList)
		{
			RaceRestrictionSettings.plantRestricted.Add(thingDef5);
			alienRace.raceRestriction.whitePlantList.Add(thingDef5);
		}
		foreach (TraitDef traitDef in alienRace.raceRestriction.traitList)
		{
			RaceRestrictionSettings.traitRestricted.Add(traitDef);
			alienRace.raceRestriction.whiteTraitList.Add(traitDef);
		}
		foreach (ThingDef thingDef6 in alienRace.raceRestriction.foodList)
		{
			RaceRestrictionSettings.foodRestricted.Add(thingDef6);
			alienRace.raceRestriction.whiteFoodList.Add(thingDef6);
		}
		foreach (ThingDef thingDef7 in alienRace.raceRestriction.petList)
		{
			RaceRestrictionSettings.petRestricted.Add(thingDef7);
			alienRace.raceRestriction.whitePetList.Add(thingDef7);
		}
		foreach (ResearchProjectDef projectDef in alienRace.raceRestriction.researchList.SelectMany((ResearchProjectRestrictions rl) => rl?.projects))
		{
			if (!RaceRestrictionSettings.researchRestrictionDict.ContainsKey(projectDef))
			{
				RaceRestrictionSettings.researchRestrictionDict.Add(projectDef, new List<ThingDef_AlienRace>());
			}
			RaceRestrictionSettings.researchRestrictionDict[projectDef].Add(this);
		}
		foreach (GeneDef geneDef in alienRace.raceRestriction.geneList)
		{
			RaceRestrictionSettings.geneRestricted.Add(geneDef);
			alienRace.raceRestriction.whiteGeneList.Add(geneDef);
		}
		foreach (GeneDef geneDef2 in alienRace.raceRestriction.geneListEndo)
		{
			RaceRestrictionSettings.geneRestrictedEndo.Add(geneDef2);
			alienRace.raceRestriction.whiteGeneListEndo.Add(geneDef2);
		}
		foreach (GeneDef geneDef3 in alienRace.raceRestriction.geneListXeno)
		{
			RaceRestrictionSettings.geneRestrictedXeno.Add(geneDef3);
			alienRace.raceRestriction.whiteGeneListXeno.Add(geneDef3);
		}
		foreach (XenotypeDef xenotypeDef in alienRace.raceRestriction.xenotypeList)
		{
			RaceRestrictionSettings.xenotypeRestricted.Add(xenotypeDef);
			alienRace.raceRestriction.whiteXenotypeList.Add(xenotypeDef);
		}
		foreach (ThingDef thingDef8 in alienRace.raceRestriction.reproductionList)
		{
			RaceRestrictionSettings.reproductionRestricted.Add(thingDef8);
			alienRace.raceRestriction.whiteReproductionList.Add(thingDef8);
		}
		if (race.hasCorpse && alienRace.generalSettings.corpseCategory != ThingCategoryDefOf.CorpsesHumanlike)
		{
			ThingCategoryDefOf.CorpsesHumanlike.childThingDefs.Remove(race.corpseDef);
			if (alienRace.generalSettings.corpseCategory != null)
			{
				race.corpseDef.thingCategories = new List<ThingCategoryDef>(1) { alienRace.generalSettings.corpseCategory };
				alienRace.generalSettings.corpseCategory.childThingDefs.Add(race.corpseDef);
				alienRace.generalSettings.corpseCategory.ResolveReferences();
			}
			ThingCategoryDefOf.CorpsesHumanlike.ResolveReferences();
		}
		alienRace.generalSettings.alienPartGenerator.GenerateMeshsAndMeshPools();
		if (!alienRace.generalSettings.humanRecipeImport || this == ThingDefOf.Human)
		{
			return;
		}
		(recipes ?? (recipes = new List<RecipeDef>())).AddRange(ThingDefOf.Human.recipes.Where((RecipeDef rd) => !rd.targetsBodyPart || rd.appliedOnFixedBodyParts.NullOrEmpty() || rd.appliedOnFixedBodyParts.Any((BodyPartDef bpd) => race.body.AllParts.Any((BodyPartRecord bpr) => bpr.def == bpd))));
		DefDatabase<RecipeDef>.AllDefsListForReading.ForEach(delegate(RecipeDef rd)
		{
			List<ThingDef> recipeUsers = rd.recipeUsers;
			if (recipeUsers != null && recipeUsers.Contains(ThingDefOf.Human))
			{
				rd.recipeUsers.Add(this);
			}
			ThingFilter defaultIngredientFilter = rd.defaultIngredientFilter;
			if (defaultIngredientFilter != null && !defaultIngredientFilter.Allows(ThingDefOf.Meat_Human))
			{
				rd.defaultIngredientFilter.SetAllow(race.meatDef, allow: false);
			}
		});
		recipes.RemoveDuplicates();
		void RecursiveAttributeCheck(Type type2, Traverse instance, string debug)
		{
			if (type2 == typeof(ThingDef_AlienRace))
			{
				return;
			}
			try
			{
				debug += ".";
				string debugBackup = debug;
				FieldInfo[] fields = type2.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				foreach (FieldInfo field in fields)
				{
					debug = debugBackup;
					debug = debug + "(" + field.FieldType.FullName + ") " + field.Name;
					Traverse instanceNew = instance.Field(field.Name);
					if (typeof(IList).IsAssignableFrom(field.FieldType))
					{
						object value = instanceNew.GetValue();
						if (value != null)
						{
							foreach (object o in (IList)value)
							{
								if (o.GetType().Assembly == typeof(ThingDef_AlienRace).Assembly)
								{
									RecursiveAttributeCheck(o.GetType(), Traverse.Create(o), debug);
								}
							}
						}
					}
					if (field.FieldType.Assembly == typeof(ThingDef_AlienRace).Assembly)
					{
						RecursiveAttributeCheck(field.FieldType, instanceNew, debug);
					}
					LoadDefFromField attribute = field.GetCustomAttribute<LoadDefFromField>();
					if (attribute != null && instanceNew.GetValue() == null)
					{
						instanceNew.SetValue((attribute.defName == "this") ? this : attribute.GetDef(field.FieldType));
					}
				}
			}
			catch (InvalidOperationException arg)
			{
				Log.Error($"RecursiveAttribute Error: {debug}\n{arg}");
			}
		}
	}
}
