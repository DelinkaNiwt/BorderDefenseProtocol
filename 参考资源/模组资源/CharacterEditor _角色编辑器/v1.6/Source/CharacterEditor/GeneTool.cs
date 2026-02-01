using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace CharacterEditor;

internal static class GeneTool
{
	internal enum FreeList
	{
		CategoryFactors,
		CategoryOffsets,
		StatFactors,
		StatOffsets,
		Aptitudes,
		Capacities,
		Abilities,
		Immunities,
		Protections,
		DamageFactors,
		ForcedTraits,
		SuppressedTraits,
		DisabledNeeds,
		ForcedHeadTypes,
		DisabledWorkTags,
		ConditionalStats,
		All
	}

	private static bool bAllCategories = false;

	private static StatCategoryDef selected_StatFactor_CatDef = null;

	private static StatCategoryDef selected_StatOffset_CatDef = null;

	internal static HashSet<StatCategoryDef> lCategoryDef_Factors;

	internal static HashSet<StatCategoryDef> lCategoryDef_Offsets;

	internal static HashSet<ConditionalStatAffecter> lFreeConditionalStats;

	internal static HashSet<StatDef> lFreeStatDefFactors;

	internal static HashSet<StatDef> lFreeStatDefOffsets;

	internal static HashSet<SkillDef> lFreeAptitudes;

	internal static HashSet<PawnCapacityDef> lFreeCapacities;

	internal static HashSet<AbilityDef> lFreeAbilities;

	internal static HashSet<NeedDef> lFreeNeeds;

	internal static HashSet<HeadTypeDef> lFreeForcedHeadTypes;

	internal static HashSet<HediffDef> lFreeImmunities;

	internal static HashSet<HediffDef> lFreeProtections;

	internal static HashSet<DamageDef> lFreeDamageFactors;

	internal static HashSet<GeneticTraitData> lFreeForcedTraits;

	internal static HashSet<GeneticTraitData> lFreeSuppressedTraits;

	internal static HashSet<WorkTags> lFreeWorkTags;

	internal static List<StatModifier> lCopyStatFactors = new List<StatModifier>();

	internal static List<StatModifier> lCopyStatOffsets = new List<StatModifier>();

	internal static List<Aptitude> lCopyAptitude = new List<Aptitude>();

	internal static List<DamageFactor> lCopyDamageFactors = new List<DamageFactor>();

	internal static List<PawnCapacityModifier> lCopyCapacities = new List<PawnCapacityModifier>();

	internal static List<AbilityDef> lCopyAbilities = new List<AbilityDef>();

	internal static List<GeneticTraitData> lCopyForcedTraits = new List<GeneticTraitData>();

	internal static List<GeneticTraitData> lCopySuppressedTraits = new List<GeneticTraitData>();

	internal static List<HediffDef> lCopyImmunities = new List<HediffDef>();

	internal static List<HediffDef> lCopyProtections = new List<HediffDef>();

	internal static List<NeedDef> lCopyDisabledNeeds = new List<NeedDef>();

	internal static List<HeadTypeDef> lCopyForcedHeadTypes = new List<HeadTypeDef>();

	internal static List<string> lCopyCustomEffectDescriptions = new List<string>();

	internal static List<string> lCopyExclusionTags = new List<string>();

	internal static List<string> lCopyHairTags = new List<string>();

	internal static List<string> lCopyBeardTags = new List<string>();

	internal static List<float> lCopyGizmoThres = new List<float>();

	internal static WorkTags lCopyDisabledWorkTags = WorkTags.None;

	internal static List<GeneDef> cachedGenes = new List<GeneDef>();

	internal static StatCategoryDef StatFactorCategory
	{
		get
		{
			return selected_StatFactor_CatDef;
		}
		set
		{
			selected_StatFactor_CatDef = value;
			SelectedGene.UpdateFreeLists(FreeList.CategoryFactors);
			SelectedGene.UpdateFreeLists(FreeList.StatFactors);
		}
	}

	internal static StatCategoryDef StatOffsetCategory
	{
		get
		{
			return selected_StatOffset_CatDef;
		}
		set
		{
			selected_StatOffset_CatDef = value;
			SelectedGene.UpdateFreeLists(FreeList.CategoryOffsets);
			SelectedGene.UpdateFreeLists(FreeList.StatOffsets);
		}
	}

	internal static bool UseAllCategories
	{
		get
		{
			return bAllCategories;
		}
		set
		{
			bAllCategories = value;
			SelectedGene.UpdateFreeLists(FreeList.All);
		}
	}

	internal static GeneDef SelectedGene { get; set; }

	internal static Func<DamageDef, string> DamageLabel => (DamageDef def) => "DamageType".Translate(def.label).CapitalizeFirst();

	internal static GeneCategoryDef BodySizeCategory => DefTool.GetDef<GeneCategoryDef>("SZSpecial");

	internal static HashSet<GeneDef> ListBodySizeGenes => DefTool.ListBy((GeneDef x) => !x.labelShortAdj.NullOrEmpty() && x.labelShortAdj.StartsWith("bodysize"));

	internal static void UpdateFreeLists(this GeneDef g, FreeList f)
	{
		if (f == FreeList.All || f == FreeList.CategoryFactors)
		{
			lCategoryDef_Factors = ListOfStatCategoryDef_ForStatFactor(CEditor.API.Pawn.RaceProps.IsMechanoid, UseAllCategories);
		}
		if (f == FreeList.All || f == FreeList.CategoryOffsets)
		{
			lCategoryDef_Offsets = ListOfStatCategoryDef_ForStatOffset(CEditor.API.Pawn.RaceProps.IsMechanoid, UseAllCategories);
		}
		if (g != null)
		{
			g.ResolveReferences();
			if (f == FreeList.All || f == FreeList.ConditionalStats)
			{
				lFreeConditionalStats = ListConditionalAffectors();
			}
			if (f == FreeList.All || f == FreeList.StatFactors)
			{
				lFreeStatDefFactors = g.statFactors.ListOfFreeCustom(DefTool.StatDefs_Selection((selected_StatFactor_CatDef == null) ? lCategoryDef_Factors : new HashSet<StatCategoryDef> { selected_StatFactor_CatDef }), DefTool.CompareStatModifier, DefTool.CompareStatCategoryNot, selected_StatFactor_CatDef);
			}
			if (f == FreeList.All || f == FreeList.StatOffsets)
			{
				lFreeStatDefOffsets = g.statOffsets.ListOfFreeCustom(DefTool.StatDefs_Selection((selected_StatOffset_CatDef == null) ? lCategoryDef_Offsets : new HashSet<StatCategoryDef> { selected_StatOffset_CatDef }), DefTool.CompareStatModifier, DefTool.CompareStatCategoryNot, selected_StatOffset_CatDef);
			}
			if (f == FreeList.All || f == FreeList.Aptitudes)
			{
				lFreeAptitudes = g.aptitudes.ListOfFree(DefTool.CompareAptitude);
			}
			if (f == FreeList.All || f == FreeList.DamageFactors)
			{
				lFreeDamageFactors = g.damageFactors.ListOfFree(DefTool.CompareDamageFactor);
			}
			if (f == FreeList.All || f == FreeList.Capacities)
			{
				lFreeCapacities = g.capMods.ListOfFree(DefTool.ComparePawnCapacityModifier);
			}
			if (f == FreeList.All || f == FreeList.Abilities)
			{
				lFreeAbilities = g.abilities.ListOfFreeDef();
			}
			if (f == FreeList.All || f == FreeList.Immunities)
			{
				lFreeImmunities = g.makeImmuneTo.ListOfFreeDef();
			}
			if (f == FreeList.All || f == FreeList.Protections)
			{
				lFreeProtections = g.hediffGiversCannotGive.ListOfFreeDef();
			}
			if (f == FreeList.All || f == FreeList.DamageFactors)
			{
				lFreeDamageFactors = g.damageFactors.ListOfFree(DefTool.CompareDamageFactor);
			}
			if (f == FreeList.All || f == FreeList.ForcedTraits)
			{
				lFreeForcedTraits = g.forcedTraits.ListOfFreeNonDef(TraitTool.ListAllAsGenticTraitData(), DefTool.CompareGeneticTraitData);
			}
			if (f == FreeList.All || f == FreeList.SuppressedTraits)
			{
				lFreeSuppressedTraits = g.suppressedTraits.ListOfFreeNonDef(TraitTool.ListAllAsGenticTraitData(), DefTool.CompareGeneticTraitData);
			}
			if (f == FreeList.All || f == FreeList.DisabledNeeds)
			{
				lFreeNeeds = g.disablesNeeds.ListOfFreeDef();
			}
			if (f == FreeList.All || f == FreeList.ForcedHeadTypes)
			{
				lFreeForcedHeadTypes = g.forcedHeadTypes.ListOfFreeDef();
			}
			if (f == FreeList.All || f == FreeList.DisabledWorkTags)
			{
				lFreeWorkTags = ListOfFreeDisabledWorkTags(g);
			}
		}
	}

	internal static void CopyList<T>(this List<T> lsource, ref List<T> ltarget)
	{
		ltarget = lsource?.ToList().ListFullCopy();
	}

	internal static void CopyHashSet<T>(this HashSet<T> lsource, ref HashSet<T> ltarget)
	{
		ltarget = lsource?.ToList().ListFullCopy().ToHashSet();
	}

	internal static void CopyStatOffsets(this GeneDef g)
	{
		g.statOffsets.CopyList(ref lCopyStatOffsets);
	}

	internal static void CopyStatFactors(this GeneDef g)
	{
		g.statFactors.CopyList(ref lCopyStatFactors);
	}

	internal static void CopyAptitudes(this GeneDef g)
	{
		g.aptitudes.CopyList(ref lCopyAptitude);
	}

	internal static void CopyDamageFactors(this GeneDef g)
	{
		g.damageFactors.CopyList(ref lCopyDamageFactors);
	}

	internal static void CopyCapacities(this GeneDef g)
	{
		g.capMods.CopyList(ref lCopyCapacities);
	}

	internal static void CopyAbilities(this GeneDef g)
	{
		g.abilities.CopyList(ref lCopyAbilities);
	}

	internal static void CopyForcedTraits(this GeneDef g)
	{
		g.forcedTraits.CopyList(ref lCopyForcedTraits);
	}

	internal static void CopySuppressedTraits(this GeneDef g)
	{
		g.suppressedTraits.CopyList(ref lCopySuppressedTraits);
	}

	internal static void CopyProtections(this GeneDef g)
	{
		g.hediffGiversCannotGive.CopyList(ref lCopyProtections);
	}

	internal static void CopyImmunities(this GeneDef g)
	{
		g.makeImmuneTo.CopyList(ref lCopyImmunities);
	}

	internal static void CopyDisabledNeeds(this GeneDef g)
	{
		g.disablesNeeds.CopyList(ref lCopyDisabledNeeds);
	}

	internal static void CopyForcedHeadTypes(this GeneDef g)
	{
		g.forcedHeadTypes.CopyList(ref lCopyForcedHeadTypes);
	}

	internal static void CopyCustomEffectDescriptions(this GeneDef g)
	{
		g.customEffectDescriptions.CopyList(ref lCopyCustomEffectDescriptions);
	}

	internal static void CopyExclusionTags(this GeneDef g)
	{
		g.exclusionTags.CopyList(ref lCopyExclusionTags);
	}

	internal static void CopyHairTags(this GeneDef g)
	{
		if (g.hairTagFilter != null)
		{
			g.hairTagFilter.tags.CopyList(ref lCopyHairTags);
		}
	}

	internal static void CopyBeardTags(this GeneDef g)
	{
		if (g.beardTagFilter != null)
		{
			g.beardTagFilter.tags.CopyList(ref lCopyBeardTags);
		}
	}

	internal static void CopyGizmoThres(this GeneDef g)
	{
		g.resourceGizmoThresholds.CopyList(ref lCopyGizmoThres);
	}

	internal static void CopyDisabledWorkTags(this GeneDef g)
	{
		lCopyDisabledWorkTags = g?.disabledWorkTags ?? WorkTags.None;
	}

	internal static void PasteListNonDefMulti<T1, T, T2, T3>(this List<T1> ltarget, List<T1> lsource, Func<T1, T, bool> comparator, Action<List<T1>, T1, T, T2, T3> valueSetter, Func<T1, T> defGetter, Func<T1, T2> valGetter1, Func<T1, T3> valGetter2) where T : Def
	{
		if (lsource.NullOrEmpty())
		{
			return;
		}
		foreach (T1 item in lsource)
		{
			ltarget.SetMulti(comparator, valueSetter, defGetter(item), valGetter1(item), valGetter2(item));
		}
	}

	internal static void PasteListNonDef<T1, T, T2>(this List<T1> ltarget, List<T1> lsource, Func<T1, T, bool> comparator, Action<List<T1>, T1, T, T2> valueSetter, Func<T1, T> defGetter, Func<T1, T2> valGetter) where T : Def
	{
		if (lsource.NullOrEmpty())
		{
			return;
		}
		foreach (T1 item in lsource)
		{
			ltarget.Set(comparator, valueSetter, defGetter(item), valGetter(item));
		}
	}

	internal static void PasteListNonDef2<T1, T, T2>(this List<T1> ltarget, List<T1> lsource, Func<T1, T1, bool> comparator, Action<List<T1>, T1, T, T2> valueSetter, Func<T1, T> defGetter, Func<T1, T2> valGetter) where T : Def
	{
		if (lsource.NullOrEmpty())
		{
			return;
		}
		foreach (T1 item in lsource)
		{
			ltarget.Set(comparator, valueSetter, item, defGetter(item), valGetter(item));
		}
	}

	internal static void PasteList<T>(this List<T> ltarget, List<T> lsource) where T : Def
	{
		if (lsource.NullOrEmpty())
		{
			return;
		}
		foreach (T item in lsource)
		{
			ltarget.AddDef(item);
		}
	}

	internal static void PasteList(this List<string> ltarget, List<string> lsource)
	{
		if (lsource.NullOrEmpty())
		{
			return;
		}
		foreach (string item in lsource)
		{
			ltarget.Add(item);
		}
	}

	internal static void PasteList(this List<float> ltarget, List<float> lsource)
	{
		if (lsource.NullOrEmpty())
		{
			return;
		}
		foreach (float item in lsource)
		{
			ltarget.Add(item);
		}
	}

	internal static void PasteDisabledWorkTags(this GeneDef g)
	{
		g.disabledWorkTags = lCopyDisabledWorkTags;
		g.UpdateFreeLists(FreeList.DisabledWorkTags);
	}

	internal static void PasteDisabledNeeds(this GeneDef g)
	{
		if (g.disablesNeeds == null)
		{
			g.disablesNeeds = new List<NeedDef>();
		}
		g.disablesNeeds.PasteList(lCopyDisabledNeeds);
		g.UpdateFreeLists(FreeList.DisabledNeeds);
	}

	internal static void PasteForcedHeadTypes(this GeneDef g)
	{
		if (g.forcedHeadTypes == null)
		{
			g.forcedHeadTypes = new List<HeadTypeDef>();
		}
		g.forcedHeadTypes.PasteList(lCopyForcedHeadTypes);
		g.UpdateFreeLists(FreeList.ForcedHeadTypes);
	}

	internal static void PasteCustomEffectDescriptions(this GeneDef g)
	{
		if (g.customEffectDescriptions == null)
		{
			g.customEffectDescriptions = new List<string>();
		}
		g.customEffectDescriptions.PasteList(lCopyCustomEffectDescriptions);
	}

	internal static void PasteExclusionTags(this GeneDef g)
	{
		if (g.exclusionTags == null)
		{
			g.exclusionTags = new List<string>();
		}
		g.exclusionTags.PasteList(lCopyExclusionTags);
	}

	internal static void PasteGizmoThres(this GeneDef g)
	{
		if (g.resourceGizmoThresholds == null)
		{
			g.resourceGizmoThresholds = new List<float>();
		}
		g.resourceGizmoThresholds.PasteList(lCopyGizmoThres);
	}

	internal static void PasteHairTags(this GeneDef g)
	{
		if (g.hairTagFilter == null)
		{
			g.hairTagFilter = new TagFilter();
		}
		g.hairTagFilter.tags.PasteList(lCopyHairTags);
	}

	internal static void PasteBeardTags(this GeneDef g)
	{
		if (g.beardTagFilter == null)
		{
			g.beardTagFilter = new TagFilter();
		}
		g.beardTagFilter.tags.PasteList(lCopyBeardTags);
	}

	internal static void PasteProtections(this GeneDef g)
	{
		if (g.hediffGiversCannotGive == null)
		{
			g.hediffGiversCannotGive = new List<HediffDef>();
		}
		g.hediffGiversCannotGive.PasteList(lCopyProtections);
		g.UpdateFreeLists(FreeList.Protections);
	}

	internal static void PasteImmunities(this GeneDef g)
	{
		if (g.makeImmuneTo == null)
		{
			g.makeImmuneTo = new List<HediffDef>();
		}
		g.makeImmuneTo.PasteList(lCopyImmunities);
		g.UpdateFreeLists(FreeList.Immunities);
	}

	internal static void PasteAbilities(this GeneDef g)
	{
		if (g.abilities == null)
		{
			g.abilities = new List<AbilityDef>();
		}
		g.abilities.PasteList(lCopyAbilities);
		g.UpdateFreeLists(FreeList.Abilities);
	}

	internal static void PasteForcedTraits(this GeneDef g)
	{
		if (g.forcedTraits == null)
		{
			g.forcedTraits = new List<GeneticTraitData>();
		}
		g.forcedTraits.PasteListNonDef2(lCopyForcedTraits, DefTool.CompareGeneticTraitData, DefTool.SetGeneticTraitData, DefTool.DefGetterGeneticTraitData, DefTool.ValGetterGeneticTraitData);
		g.UpdateFreeLists(FreeList.ForcedTraits);
	}

	internal static void PasteSuppressedTraits(this GeneDef g)
	{
		if (g.suppressedTraits == null)
		{
			g.suppressedTraits = new List<GeneticTraitData>();
		}
		g.suppressedTraits.PasteListNonDef2(lCopySuppressedTraits, DefTool.CompareGeneticTraitData, DefTool.SetGeneticTraitData, DefTool.DefGetterGeneticTraitData, DefTool.ValGetterGeneticTraitData);
		g.UpdateFreeLists(FreeList.SuppressedTraits);
	}

	internal static void PasteAptitude(this GeneDef g)
	{
		if (g.aptitudes == null)
		{
			g.aptitudes = new List<Aptitude>();
		}
		g.aptitudes.PasteListNonDef(lCopyAptitude, DefTool.CompareAptitude, DefTool.SetAptitude, DefTool.DefGetterAptitude, DefTool.ValGetterAptitude);
		g.UpdateFreeLists(FreeList.Aptitudes);
	}

	internal static void PasteDamageFactors(this GeneDef g)
	{
		if (g.damageFactors == null)
		{
			g.damageFactors = new List<DamageFactor>();
		}
		g.damageFactors.PasteListNonDef(lCopyDamageFactors, DefTool.CompareDamageFactor, DefTool.SetDamageFactor, DefTool.DefGetterDamageFactor, DefTool.ValGetterDamageFactor);
		g.UpdateFreeLists(FreeList.DamageFactors);
	}

	internal static void PasteStatFactors(this GeneDef g)
	{
		if (g.statFactors == null)
		{
			g.statFactors = new List<StatModifier>();
		}
		g.statFactors.PasteListNonDef(lCopyStatFactors, DefTool.CompareStatModifier, DefTool.SetStatModifier, DefTool.DefGetterStatModifier, DefTool.ValGetterStatModifier);
		g.UpdateFreeLists(FreeList.StatFactors);
	}

	internal static void PasteStatOffsets(this GeneDef g)
	{
		if (g.statOffsets == null)
		{
			g.statOffsets = new List<StatModifier>();
		}
		g.statOffsets.PasteListNonDef(lCopyStatOffsets, DefTool.CompareStatModifier, DefTool.SetStatModifier, DefTool.DefGetterStatModifier, DefTool.ValGetterStatModifier);
		g.UpdateFreeLists(FreeList.StatOffsets);
	}

	internal static void PasteCapacities(this GeneDef g)
	{
		if (g.capMods == null)
		{
			g.capMods = new List<PawnCapacityModifier>();
		}
		g.capMods.PasteListNonDefMulti(lCopyCapacities, DefTool.ComparePawnCapacityModifier, DefTool.SetPawnCapacityModifier, DefTool.DefGetterPawnCapacityModifier, DefTool.ValGetterPCMoffset, DefTool.ValGetterPCMfactor);
		g.UpdateFreeLists(FreeList.Capacities);
	}

	internal static HashSet<ConditionalStatAffecter> ListConditionalAffectors()
	{
		HashSet<ConditionalStatAffecter> hashSet = new HashSet<ConditionalStatAffecter>();
		hashSet.Add(new ConditionalStatAffecter_Child());
		hashSet.Add(new ConditionalStatAffecter_Clothed());
		hashSet.Add(new ConditionalStatAffecter_Unclothed());
		hashSet.Add(new ConditionalStatAffecter_InSunlight());
		return hashSet;
	}

	internal static HashSet<T> ListOfFreeCustom<T1, T, T2>(this ICollection<T1> otherList, ICollection<T> startList, Func<T1, T, bool> comparator, Func<T, T2, bool> comparator2, T2 otherDef) where T : Def
	{
		if (!otherList.EnumerableNullOrEmpty())
		{
			bool flag = otherDef != null;
			for (int i = 0; i < startList.Count; i++)
			{
				T val = startList.ElementAt(i);
				if (otherList.FindBy(comparator, val) != null)
				{
					startList.Remove(val);
					i--;
				}
				else if (flag && comparator2(val, otherDef))
				{
					startList.Remove(val);
					i--;
				}
			}
		}
		return startList.ToHashSet();
	}

	internal static HashSet<T> ListOfFree<T1, T>(this List<T1> otherList, Func<T1, T, bool> comparator) where T : Def
	{
		HashSet<T> hashSet = DefTool.ListBy((T x) => !x.defName.NullOrEmpty());
		if (!otherList.NullOrEmpty())
		{
			for (int num = 0; num < hashSet.Count; num++)
			{
				T val = hashSet.At(num);
				if (otherList.FindBy(comparator, val) != null)
				{
					hashSet.Remove(val);
					num--;
				}
			}
		}
		return hashSet;
	}

	internal static HashSet<T> ListOfFreeNonDef<T>(this ICollection<T> otherList, ICollection<T> startList, Func<T, T, bool> comparator)
	{
		if (!otherList.EnumerableNullOrEmpty())
		{
			for (int i = 0; i < startList.Count; i++)
			{
				T val = startList.ElementAt(i);
				if (otherList.FindBy<T>(comparator, val) != null)
				{
					startList.Remove(val);
					i--;
				}
			}
		}
		return startList.ToHashSet();
	}

	internal static HashSet<T> ListOfFreeDef<T>(this List<T> otherList) where T : Def
	{
		HashSet<T> hashSet = DefTool.ListBy((T x) => !x.defName.NullOrEmpty());
		if (!otherList.NullOrEmpty())
		{
			for (int num = 0; num < hashSet.Count; num++)
			{
				T item = hashSet.At(num);
				if (otherList.Contains(item))
				{
					hashSet.Remove(item);
					num--;
				}
			}
		}
		return hashSet;
	}

	internal static HashSet<WorkTags> ListOfFreeDisabledWorkTags(GeneDef g)
	{
		if (g == null)
		{
			return new HashSet<WorkTags>();
		}
		Array values = Enum.GetValues(typeof(WorkTags));
		HashSet<WorkTags> hashSet = new HashSet<WorkTags>();
		foreach (WorkTags item in values)
		{
			if (item != (g.disabledWorkTags & item))
			{
				hashSet.Add(item);
			}
		}
		return hashSet;
	}

	internal static HashSet<StatCategoryDef> ListOfStatCategoryDef_ForStatOffset(bool isMechanoid, bool all)
	{
		HashSet<StatCategoryDef> lskip = List_StatCategories_ToSkip(!isMechanoid, all);
		HashSet<StatCategoryDef> hashSet = DefTool.StatCategoryDefs_Selection(lskip);
		hashSet.RemoveCategoriesWithNoStatDef();
		return hashSet;
	}

	internal static HashSet<StatCategoryDef> ListOfStatCategoryDef_ForStatFactor(bool isMechanoid, bool all)
	{
		HashSet<StatCategoryDef> lskip = List_StatCategories_ToSkip(!isMechanoid, all);
		HashSet<StatCategoryDef> hashSet = DefTool.StatCategoryDefs_Selection(lskip);
		hashSet.RemoveCategoriesWithNoStatDef();
		return hashSet;
	}

	internal static HashSet<StatCategoryDef> List_StatCategories_ToSkip(bool skipMechnoid, bool allowAll)
	{
		HashSet<StatCategoryDef> hashSet = new HashSet<StatCategoryDef>();
		if (allowAll)
		{
			return hashSet;
		}
		hashSet.Add(StatCategoryDefOf.StuffStatOffsets);
		hashSet.Add(StatCategoryDefOf.EquippedStatOffsets);
		hashSet.Add(StatCategoryDefOf.StuffStatFactors);
		hashSet.Add(StatCategoryDefOf.BasicsNonPawn);
		hashSet.Add(StatCategoryDefOf.Building);
		hashSet.Add(StatCategoryDefOf.Terrain);
		hashSet.Add(StatCategoryDefOf.Apparel);
		hashSet.Add(StatCategoryDefOf.StuffStatFactors);
		hashSet.Add(StatCategoryDefOf.Weapon_Ranged);
		hashSet.Add(StatCategoryDefOf.Weapon_Melee);
		hashSet.Add(StatCategoryDefOf.Ability);
		hashSet.Add(StatCategoryDefOf.BasicsNonPawnImportant);
		hashSet.Add(StatCategoryDefOf.Meditation);
		hashSet.Add(DefTool.GetDef<StatCategoryDef>("Mechanitor"));
		if (skipMechnoid)
		{
			hashSet.Add(StatCategoryDefOf.Mechanoid);
		}
		return hashSet;
	}

	internal static List<string> GetAllExclusionTags()
	{
		SortedDictionary<string, int> sortedDictionary = new SortedDictionary<string, int>();
		foreach (GeneDef allDef in DefDatabase<GeneDef>.AllDefs)
		{
			sortedDictionary.AddFromList(allDef.exclusionTags, 0);
		}
		return sortedDictionary.Keys.ToList();
	}

	internal static List<string> GetAllHairTags()
	{
		SortedDictionary<string, int> sortedDictionary = new SortedDictionary<string, int>();
		foreach (GeneDef allDef in DefDatabase<GeneDef>.AllDefs)
		{
			if (allDef.hairTagFilter != null)
			{
				sortedDictionary.AddFromList(allDef.hairTagFilter.tags, 0);
			}
		}
		return sortedDictionary.Keys.ToList();
	}

	internal static List<string> GetAllBeardTags()
	{
		SortedDictionary<string, int> sortedDictionary = new SortedDictionary<string, int>();
		foreach (GeneDef allDef in DefDatabase<GeneDef>.AllDefs)
		{
			if (allDef.beardTagFilter != null)
			{
				sortedDictionary.AddFromList(allDef.beardTagFilter.tags, 0);
			}
		}
		return sortedDictionary.Keys.ToList();
	}

	internal static void SetSuppressedTrait(this GeneDef g, GeneticTraitData gtd, TraitDef t, int degree)
	{
		DefTool.Set(ref g.suppressedTraits, DefTool.CompareGeneticTraitData, DefTool.SetGeneticTraitData, gtd, t, degree);
		g.UpdateFreeLists(FreeList.SuppressedTraits);
	}

	internal static void SetForcedTrait(this GeneDef g, GeneticTraitData gtd, TraitDef t, int degree)
	{
		DefTool.Set(ref g.forcedTraits, DefTool.CompareGeneticTraitData, DefTool.SetGeneticTraitData, gtd, t, degree);
		g.UpdateFreeLists(FreeList.ForcedTraits);
	}

	internal static void SetDisabledNeed(this GeneDef g, NeedDef n)
	{
		if (g.disablesNeeds == null)
		{
			g.disablesNeeds = new List<NeedDef>();
		}
		g.disablesNeeds.AddDef(n);
		g.UpdateFreeLists(FreeList.DisabledNeeds);
		g.DoGeneActionForAllPawns(RecalcNeeds);
	}

	internal static void SetForcedHeadType(this GeneDef g, HeadTypeDef h)
	{
		if (g.forcedHeadTypes == null)
		{
			g.forcedHeadTypes = new List<HeadTypeDef>();
		}
		g.forcedHeadTypes.AddDef(h);
		g.UpdateFreeLists(FreeList.ForcedHeadTypes);
	}

	internal static void SetProtection(this GeneDef g, HediffDef h)
	{
		if (g.hediffGiversCannotGive == null)
		{
			g.hediffGiversCannotGive = new List<HediffDef>();
		}
		g.hediffGiversCannotGive.AddDef(h);
		g.UpdateFreeLists(FreeList.Protections);
	}

	internal static void SetImmunity(this GeneDef g, HediffDef h)
	{
		if (g.makeImmuneTo == null)
		{
			g.makeImmuneTo = new List<HediffDef>();
		}
		g.makeImmuneTo.AddDef(h);
		g.UpdateFreeLists(FreeList.Immunities);
	}

	internal static void SetAbility(this GeneDef g, AbilityDef a)
	{
		if (g.abilities == null)
		{
			g.abilities = new List<AbilityDef>();
		}
		g.abilities.AddDef(a);
		g.UpdateFreeLists(FreeList.Abilities);
	}

	internal static void SetCapacity(this GeneDef g, PawnCapacityDef c, float offset, float factor)
	{
		DefTool.SetMulti(ref g.capMods, DefTool.ComparePawnCapacityModifier, DefTool.SetPawnCapacityModifier, c, offset, factor);
		g.UpdateFreeLists(FreeList.Capacities);
	}

	internal static void SetAptitude(this GeneDef g, SkillDef s, int level)
	{
		DefTool.Set(ref g.aptitudes, DefTool.CompareAptitude, DefTool.SetAptitude, s, level);
		g.UpdateFreeLists(FreeList.Aptitudes);
	}

	internal static void SetDamageFactor(this GeneDef g, DamageDef d, float factor)
	{
		DefTool.Set(ref g.damageFactors, DefTool.CompareDamageFactor, DefTool.SetDamageFactor, d, factor);
		g.UpdateFreeLists(FreeList.DamageFactors);
	}

	internal static void SetStatOffset(this GeneDef g, StatDef s, float offset)
	{
		DefTool.Set(ref g.statOffsets, DefTool.CompareStatModifier, DefTool.SetStatModifier, s, offset);
		g.UpdateFreeLists(FreeList.StatOffsets);
	}

	internal static void SetStatFactor(this GeneDef g, StatDef s, float factor)
	{
		DefTool.Set(ref g.statFactors, DefTool.CompareStatModifier, DefTool.SetStatModifier, s, factor);
		g.UpdateFreeLists(FreeList.StatFactors);
	}

	internal static void SetPassionMod(this GeneDef g, SkillDef skill, PassionMod.PassionModType type)
	{
		if (g != null)
		{
			if (g.passionMod == null)
			{
				g.passionMod = new PassionMod();
			}
			g.passionMod.skill = skill;
			g.passionMod.modType = type;
			if (skill == null)
			{
				g.passionMod = null;
			}
			g.ResolveReferences();
			if (skill != null)
			{
				g.DoGeneActionForAllPawns(RecalcPassion);
			}
		}
	}

	internal static void SetCausesNeed(this GeneDef g, NeedDef need)
	{
		if (g != null)
		{
			g.ResolveReferences();
			g.DoGeneActionForAllPawns(RecalcNeeds);
		}
	}

	internal static void SetChemicalDef(this GeneDef g, ChemicalDef chemical)
	{
		if (g != null)
		{
			g.chemical = chemical;
			g.ResolveReferences();
			g.DoGeneActionForAllPawns(RecalcNeeds);
		}
	}

	internal static void SetForcedHairDef(this GeneDef g, HairDef hair)
	{
		if (g != null)
		{
			g.forcedHair = hair;
			g.ResolveReferences();
			g.DoGeneActionForAllPawns(RecalcForcedHair);
		}
	}

	internal static void SetDisabledWorkTags(this GeneDef g, WorkTags workTag)
	{
		if (g == null || workTag == WorkTags.None)
		{
			return;
		}
		List<WorkTags> list = g.disabledWorkTags.GetAllSelectedItems<WorkTags>().ToList();
		if (!list.Contains(workTag))
		{
			list.Add(workTag);
		}
		list.Remove(WorkTags.None);
		int num = 0;
		foreach (WorkTags item in list)
		{
			num = (int)(num + item);
		}
		g.disabledWorkTags = (WorkTags)Enum.Parse(typeof(WorkTags), num.ToString());
		g.UpdateFreeLists(FreeList.DisabledWorkTags);
		g.DoGeneActionForAllPawns(RecalcWork);
	}

	internal static void RemoveStatOffset(this GeneDef g, StatDef def)
	{
		StatModifier statModifier = g.statOffsets.FindBy(DefTool.CompareStatModifier, def);
		if (statModifier != null)
		{
			g.statOffsets.Remove(statModifier);
			g.UpdateFreeLists(FreeList.StatOffsets);
		}
	}

	internal static void RemoveStatFactor(this GeneDef g, StatDef def)
	{
		StatModifier statModifier = g.statFactors.FindBy(DefTool.CompareStatModifier, def);
		if (statModifier != null)
		{
			g.statFactors.Remove(statModifier);
			g.UpdateFreeLists(FreeList.StatFactors);
		}
	}

	internal static void RemoveAptitude(this GeneDef g, SkillDef def)
	{
		Aptitude aptitude = g.aptitudes.FindBy(DefTool.CompareAptitude, def);
		if (aptitude != null)
		{
			g.aptitudes.Remove(aptitude);
			g.UpdateFreeLists(FreeList.Aptitudes);
		}
	}

	internal static void RemoveDamageFactor(this GeneDef g, DamageDef def)
	{
		DamageFactor damageFactor = g.damageFactors.FindBy(DefTool.CompareDamageFactor, def);
		if (damageFactor != null)
		{
			g.damageFactors.Remove(damageFactor);
			g.UpdateFreeLists(FreeList.DamageFactors);
		}
	}

	internal static void RemoveCapacity(this GeneDef g, PawnCapacityDef def)
	{
		PawnCapacityModifier pawnCapacityModifier = g.capMods.FindBy(DefTool.ComparePawnCapacityModifier, def);
		if (pawnCapacityModifier != null)
		{
			g.capMods.Remove(pawnCapacityModifier);
			g.UpdateFreeLists(FreeList.Capacities);
		}
	}

	internal static void RemoveAbility(this GeneDef g, AbilityDef def)
	{
		AbilityDef abilityDef = g.abilities.FindByDef(def);
		if (abilityDef != null)
		{
			g.abilities.Remove(abilityDef);
			g.UpdateFreeLists(FreeList.Abilities);
		}
	}

	internal static void RemoveForcedTrait(this GeneDef g, GeneticTraitData gtd)
	{
		GeneticTraitData geneticTraitData = ((ICollection<GeneticTraitData>)g.forcedTraits).FindBy<GeneticTraitData>(DefTool.CompareGeneticTraitData, gtd);
		if (geneticTraitData != null)
		{
			g.forcedTraits.Remove(geneticTraitData);
			g.UpdateFreeLists(FreeList.ForcedTraits);
		}
	}

	internal static void RemoveSuppressedTrait(this GeneDef g, GeneticTraitData gtd)
	{
		GeneticTraitData geneticTraitData = ((ICollection<GeneticTraitData>)g.suppressedTraits).FindBy<GeneticTraitData>(DefTool.CompareGeneticTraitData, gtd);
		if (geneticTraitData != null)
		{
			g.suppressedTraits.Remove(geneticTraitData);
			g.UpdateFreeLists(FreeList.SuppressedTraits);
		}
	}

	internal static void RemoveProtection(this GeneDef g, HediffDef def)
	{
		HediffDef hediffDef = g.hediffGiversCannotGive.FindByDef(def);
		if (hediffDef != null)
		{
			g.hediffGiversCannotGive.Remove(hediffDef);
			g.UpdateFreeLists(FreeList.Protections);
		}
	}

	internal static void RemoveImmunity(this GeneDef g, HediffDef def)
	{
		HediffDef hediffDef = g.makeImmuneTo.FindByDef(def);
		if (hediffDef != null)
		{
			g.makeImmuneTo.Remove(hediffDef);
			g.UpdateFreeLists(FreeList.Immunities);
		}
	}

	internal static void RemoveDisabledNeed(this GeneDef g, NeedDef def)
	{
		NeedDef needDef = g.disablesNeeds.FindByDef(def);
		if (needDef != null)
		{
			g.disablesNeeds.Remove(needDef);
			g.UpdateFreeLists(FreeList.DisabledNeeds);
			g.DoGeneActionForAllPawns(RecalcNeeds);
		}
	}

	internal static void RemoveForcedHeadType(this GeneDef g, HeadTypeDef def)
	{
		HeadTypeDef headTypeDef = g.forcedHeadTypes.FindByDef(def);
		if (headTypeDef != null)
		{
			g.forcedHeadTypes.Remove(headTypeDef);
			g.UpdateFreeLists(FreeList.ForcedHeadTypes);
		}
	}

	internal static void RemoveDisabledWorkTags(this GeneDef g, WorkTags workTag)
	{
		if (g == null || workTag == WorkTags.None)
		{
			return;
		}
		List<WorkTags> list = g.disabledWorkTags.GetAllSelectedItems<WorkTags>().ToList();
		if (list.Contains(workTag))
		{
			list.Remove(workTag);
		}
		int num = 0;
		foreach (WorkTags item in list)
		{
			num = (int)(num + item);
		}
		g.disabledWorkTags = (WorkTags)Enum.Parse(typeof(WorkTags), num.ToString());
		g.UpdateFreeLists(FreeList.DisabledWorkTags);
		g.DoGeneActionForAllPawns(RecalcWork);
	}

	internal static void DoAllGeneActions(this GeneDef g)
	{
		g.DoGeneActionForAllPawns(RecalcNeeds);
		g.DoGeneActionForAllPawns(RecalcWork);
		g.DoGeneActionForAllPawns(RecalcForcedHair);
	}

	internal static void RecalcForcedHair(Pawn p, GeneDef g)
	{
		p.SetHair(g.forcedHair);
	}

	internal static void RecalcWork(Pawn p, GeneDef g)
	{
		p.Recalculate_WorkTypes();
	}

	internal static void RecalcNeeds(Pawn p, GeneDef g)
	{
		p.needs?.AddOrRemoveNeedsAsAppropriate();
	}

	internal static void RecalcPassion(Pawn p, GeneDef g)
	{
		SkillRecord skill = p.skills.GetSkill(g.passionMod.skill);
		skill.passion = g.passionMod.NewPassionFor(skill);
	}

	internal static HashSet<GeneticBodyType?> ListOfGeneticBodyType()
	{
		HashSet<GeneticBodyType> hashSet = EnumTool.GetAllEnumsOfType<GeneticBodyType>().ToHashSet();
		HashSet<GeneticBodyType?> hashSet2 = new HashSet<GeneticBodyType?>();
		hashSet2.Add(null);
		foreach (GeneticBodyType item in hashSet)
		{
			hashSet2.Add(item);
		}
		return hashSet2;
	}

	internal static bool IsBodySizeGene(this GeneDef g)
	{
		return g != null && !g.defName.NullOrEmpty() && g.defName.StartsWith("SZBodySize_");
	}

	internal static LifeStageDef GetLifeStageDef(this GeneDef g)
	{
		return g.IsBodySizeGene() ? DefTool.GetDef<LifeStageDef>("SZHumanSize_" + g.defName.SubstringFrom("SZBodySize_")) : null;
	}

	internal static XenotypeDef GetXenoTypeDef(this Pawn pawn)
	{
		if (!pawn.HasGeneTracker())
		{
			return null;
		}
		return pawn.genes.Xenotype;
	}

	internal static string GetXenoTypeDefName(this Pawn pawn)
	{
		XenotypeDef xenoTypeDef = pawn.GetXenoTypeDef();
		return (xenoTypeDef == null) ? "" : xenoTypeDef.defName;
	}

	internal static string GetXenoCustomName(this Pawn pawn)
	{
		if (!pawn.HasGeneTracker())
		{
			return "";
		}
		return pawn.genes.xenotypeName ?? "";
	}

	internal static string GetXenogeneAsSeparatedString(this Pawn p)
	{
		if (!p.HasGeneTracker())
		{
			return "";
		}
		string text = "";
		foreach (Gene xenogene in p.genes.Xenogenes)
		{
			text = text + xenogene.def.defName + ":";
		}
		return text.SubstringRemoveLast();
	}

	internal static string GetEndogeneAsSeparatedString(this Pawn p)
	{
		if (!p.HasGeneTracker())
		{
			return "";
		}
		string text = "";
		foreach (Gene endogene in p.genes.Endogenes)
		{
			text = text + endogene.def.defName + ":";
		}
		return text.SubstringRemoveLast();
	}

	internal static void SetXenogeneFromSeparatedString(this Pawn p, string s)
	{
		if (!p.HasGeneTracker())
		{
			return;
		}
		try
		{
			string[] array = s.Split(new string[1] { ":" }, StringSplitOptions.None);
			string[] array2 = array;
			foreach (string defName in array2)
			{
				GeneDef geneDef = DefTool.GeneDef(defName);
				if (geneDef != null)
				{
					p.genes.AddGene(geneDef, xenogene: true);
				}
			}
		}
		catch (Exception e)
		{
			MessageTool.DebugException(e);
		}
	}

	internal static void SetEndogeneFromSeparatedString(this Pawn p, string s)
	{
		if (!p.HasGeneTracker())
		{
			return;
		}
		try
		{
			string[] array = s.Split(new string[1] { ":" }, StringSplitOptions.None);
			string[] array2 = array;
			foreach (string defName in array2)
			{
				GeneDef geneDef = DefTool.GeneDef(defName);
				if (geneDef != null)
				{
					p.genes.AddGene(geneDef, xenogene: false);
				}
			}
		}
		catch (Exception e)
		{
			MessageTool.DebugException(e);
		}
	}

	internal static void ClearXenogenes(this Pawn p)
	{
		if (p.HasGeneTracker())
		{
			for (int num = p.genes.Xenogenes.Count - 1; num >= 0; num--)
			{
				p.genes.RemoveGene(p.genes.Xenogenes[num]);
			}
		}
	}

	internal static void ClearEndogenes(this Pawn p)
	{
		if (p.HasGeneTracker())
		{
			for (int num = p.genes.Endogenes.Count - 1; num >= 0; num--)
			{
				p.genes.RemoveGene(p.genes.Endogenes[num]);
			}
		}
	}

	internal static List<Gene> GetHairGenes(this Pawn p)
	{
		return (!p.HasGeneTracker()) ? new List<Gene>() : (from td in p.genes.GenesListForReading
			where td.IsHairGene()
			orderby !td.Overridden && td.Active descending
			select td).ToList();
	}

	internal static List<Gene> GetSkinGenes(this Pawn p)
	{
		return (!p.HasGeneTracker()) ? new List<Gene>() : (from td in p.genes.GenesListForReading
			where td.IsSkinGene()
			orderby !td.Overridden && td.Active descending
			select td).ToList();
	}

	internal static List<GeneDef> GetAllSkinGenes()
	{
		return DefTool.ListBy((GeneDef x) => x.IsSkinGene()).ToList();
	}

	internal static List<GeneDef> GetAllHairGenes()
	{
		return DefTool.ListBy((GeneDef x) => x.IsHairGene()).ToList();
	}

	internal static bool IsSkinGene(this GeneDef g)
	{
		return g != null && (g.endogeneCategory == EndogeneCategory.Melanin || (!g.defName.NullOrEmpty() && g.defName.StartsWith("Skin_")));
	}

	internal static bool IsHairGene(this GeneDef g)
	{
		return g != null && g.endogeneCategory == EndogeneCategory.HairColor;
	}

	internal static bool IsSkinGene(this Gene g)
	{
		return g.def.IsSkinGene();
	}

	internal static bool IsHairGene(this Gene g)
	{
		return g.def.IsHairGene();
	}

	internal static void ClearGenes(this Pawn p, bool xeno, bool keepHairAndSkin)
	{
		if (keepHairAndSkin)
		{
			List<Gene> list = (xeno ? p.genes.Xenogenes : p.genes.Endogenes);
			bool flag = false;
			bool flag2 = false;
			for (int num = list.Count - 1; num >= 0; num--)
			{
				Gene gene = list[num];
				bool flag3 = gene.IsHairGene();
				bool flag4 = gene.IsSkinGene();
				if (flag4 && !flag)
				{
					flag = true;
				}
				else if (flag3 && !flag2)
				{
					flag2 = true;
				}
				else if ((!flag3 && !flag4) || (flag && flag4) || (flag2 && flag3))
				{
					p.genes.RemoveGene(gene);
				}
			}
		}
		else if (xeno)
		{
			p.ClearXenogenes();
		}
		else
		{
			p.ClearEndogenes();
		}
	}

	internal static void PreGeneChange_HeadAndBodyTest(this Pawn p, out HeadTypeDef oldHead, out BodyTypeDef oldBody)
	{
		oldHead = p.story?.headType;
		oldBody = p.story?.bodyType;
	}

	internal static void PostGeneChange_HeadAndBodyTest(this Pawn p, HeadTypeDef oldHead, BodyTypeDef oldBody)
	{
		HeadTypeDef item = p.story?.headType;
		BodyTypeDef item2 = p.story?.bodyType;
		HashSet<HeadTypeDef> headDefList = p.GetHeadDefList();
		if (!headDefList.Contains(item))
		{
			if (headDefList.Contains(oldHead))
			{
				p.SetHeadTypeDef(oldHead);
			}
			else
			{
				p.SetHeadTypeDef(headDefList.RandomElement());
			}
		}
		List<BodyTypeDef> bodyDefList = p.GetBodyDefList();
		if (!bodyDefList.Contains(item2))
		{
			if (bodyDefList.Contains(oldBody))
			{
				p.SetBody(oldBody);
			}
			else
			{
				p.SetBody(bodyDefList.RandomElement());
			}
		}
	}

	internal static void PresetXenoType(this Pawn pawn, string defName, string name, bool clearXeno = true, bool clearEndo = true)
	{
		if (!pawn.HasGeneTracker())
		{
			return;
		}
		try
		{
			if (clearXeno)
			{
				pawn.ClearXenogenes();
			}
			if (clearEndo)
			{
				pawn.ClearEndogenes();
			}
			pawn.genes.Reset();
			XenotypeDef xenotypeDef = (defName.NullOrEmpty() ? null : DefTool.XenotypeDef(defName));
			name = (name.NullOrEmpty() ? null : name);
			pawn.genes.SetXenotypeDirect(xenotypeDef);
			pawn.genes.xenotypeName = name;
			pawn.genes.iconDef = null;
			if (xenotypeDef != null && xenotypeDef != XenotypeDefOf.Baseliner)
			{
				return;
			}
			if (Prefs.DevMode)
			{
				Log.Message("loading icon for custom xenotype " + name);
			}
			List<CustomXenotype> allCustomXenotypes = GetAllCustomXenotypes();
			foreach (CustomXenotype item in allCustomXenotypes)
			{
				if (item.name == name)
				{
					pawn.genes.iconDef = item.iconDef;
					break;
				}
			}
		}
		catch (Exception e)
		{
			MessageTool.DebugException(e);
		}
	}

	internal static List<CustomXenotype> GetAllCustomXenotypes()
	{
		List<CustomXenotype> lcustomxeontypes = new List<CustomXenotype>();
		try
		{
			foreach (FileInfo item in GenFilePaths.AllCustomXenotypeFiles.OrderBy((FileInfo f) => f.LastWriteTime))
			{
				string filePath = GenFilePaths.AbsFilePathForXenotype(Path.GetFileNameWithoutExtension(item.Name));
				PreLoadUtility.CheckVersionAndLoad(filePath, ScribeMetaHeaderUtility.ScribeHeaderMode.Xenotype, delegate
				{
					if (GameDataSaveLoader.TryLoadXenotype(filePath, out var xenotype))
					{
						lcustomxeontypes.Add(xenotype);
					}
				}, skipOnMismatch: true);
			}
		}
		catch (Exception ex)
		{
			Log.Error(ex.Message + "\n" + ex.StackTrace);
		}
		return lcustomxeontypes;
	}

	internal static void SetGenesFromList(this Pawn p, List<GeneDef> l, bool asXeno, bool keepOld = false)
	{
		if (!keepOld)
		{
			if (asXeno)
			{
				p.ClearXenogenes();
			}
			else
			{
				p.ClearEndogenes();
			}
		}
		foreach (GeneDef item in l)
		{
			if (item != null)
			{
				p.genes.AddGene(item, asXeno);
			}
		}
	}

	internal static void OverrideAllConflictingGenes(this Pawn_GeneTracker genes, Gene gene)
	{
		if (!ModLister.BiotechInstalled)
		{
			return;
		}
		gene.OverrideBy(null);
		foreach (Gene item in genes.GenesListForReading)
		{
			if (item != gene && item.def.ConflictsWith(gene.def))
			{
				item.OverrideBy(gene);
			}
		}
	}

	internal static void DoGeneActionForAllPawns(this GeneDef geneDef, Action<Pawn, GeneDef> action)
	{
		List<Pawn> list = Find.CurrentMap?.mapPawns?.AllPawns;
		if (list.NullOrEmpty())
		{
			return;
		}
		foreach (Pawn item in list)
		{
			if (item.HasGeneTracker())
			{
				Gene gene = item.genes.GenesListForReading.FirstOrFallback((Gene g) => g.def == geneDef);
				if (gene != null)
				{
					action(item, geneDef);
				}
			}
		}
	}

	internal static Gene RemoveGeneKeepFirst(this Pawn p, Gene gene)
	{
		List<Gene> list = (gene.IsHairGene() ? p.GetHairGenes() : (gene.IsSkinGene() ? p.GetSkinGenes() : p.genes.GenesListForReading.Where((Gene x) => x.def.displayCategory == gene.def.displayCategory).ToList()));
		if (!list.NullOrEmpty())
		{
			Gene gene2 = list.First();
			Gene gene3 = ((gene2.def == gene.def) ? list.At(list.NextOrPrevIndex(0, next: true, random: false)) : null);
			Gene gene4 = gene3 ?? gene2;
			p.genes.RemoveGene(gene);
			p.genes.CallMethod("Notify_GenesChanged", new object[1] { gene.def });
			p.MakeGeneFirst(gene4);
			return gene4;
		}
		return null;
	}

	internal static void AddGeneAsFirst(this Pawn p, GeneDef geneDef, bool xeno)
	{
		foreach (Gene endogene in p.genes.Endogenes)
		{
			if (endogene.def == geneDef)
			{
				MessageTool.Show("", MessageTypeDefOf.RejectInput);
				return;
			}
		}
		Gene g = p.genes.AddGene(geneDef, xeno);
		p.MakeGeneFirst(g);
	}

	internal static void MakeGeneFirst(this Pawn p, Gene g)
	{
		p.genes.GenesListForReading.Remove(g);
		p.genes.GenesListForReading.Insert(0, g);
		p.genes.OverrideAllConflictingGenes(g);
		if (g.IsHairGene())
		{
			p.SetHairColor(primary: true, g.def.hairColorOverride ?? g.def.IconColor);
		}
		else if (g.IsSkinGene())
		{
			p.SetSkinColor(primary: true, g.def.skinColorOverride ?? g.def.skinColorBase ?? g.def.IconColor);
		}
		if (g.def.forcedHair != null)
		{
			p.SetHair(g.def.forcedHair);
		}
		if (g.IsBodySizeGene())
		{
			p.SetDirty();
		}
	}

	internal static GeneDef ClosestColorGene(Color color, bool hair)
	{
		List<GeneDef> list = (hair ? GetAllHairGenes() : GetAllSkinGenes());
		Dictionary<GeneDef, Color> dictionary = new Dictionary<GeneDef, Color>();
		foreach (GeneDef item in list)
		{
			dictionary.Add(item, item.IconColor);
		}
		GeneDef geneDef = null;
		GeneDef geneDef2 = null;
		GeneDef geneDef3 = null;
		GeneDef geneDef4 = null;
		GeneDef geneDef5 = null;
		GeneDef geneDef6 = null;
		GeneDef geneDef7 = null;
		double num = 0.10000000149011612;
		double num2 = 0.20000000298023224;
		double num3 = 0.30000001192092896;
		double num4 = 0.4000000059604645;
		double num5 = 0.5;
		double num6 = 0.6000000238418579;
		double num7 = 0.699999988079071;
		foreach (GeneDef key in dictionary.Keys)
		{
			Color color2 = dictionary[key];
			double num8 = Math.Pow(Convert.ToDouble(color2.r) - (double)color.r, 2.0);
			double num9 = Math.Pow(Convert.ToDouble(color2.g) - (double)color.g, 2.0);
			double num10 = Math.Pow(Convert.ToDouble(color2.b) - (double)color.b, 2.0);
			double num11 = Math.Sqrt(num10 + num9 + num8);
			if (num11 == 0.0)
			{
				Color color3 = color2;
				geneDef = key;
				break;
			}
			if (num11 < num)
			{
				num = num11;
				Color color3 = color2;
				geneDef = key;
			}
			else if (num11 < num2)
			{
				num2 = num11;
				Color color4 = color2;
				geneDef2 = key;
			}
			else if (num11 < num3)
			{
				num3 = num11;
				Color color5 = color2;
				geneDef3 = key;
			}
			else if (num11 < num4)
			{
				num4 = num11;
				Color color6 = color2;
				geneDef4 = key;
			}
			else if (num11 < num5)
			{
				num5 = num11;
				Color color7 = color2;
				geneDef5 = key;
			}
			else if (num11 < num6)
			{
				num6 = num11;
				Color color8 = color2;
				geneDef6 = key;
			}
			else if (num11 < num7)
			{
				num7 = num11;
				Color color9 = color2;
				geneDef7 = key;
			}
		}
		return geneDef ?? geneDef2 ?? geneDef3 ?? geneDef4 ?? geneDef5 ?? geneDef6 ?? geneDef7 ?? list.First();
	}

	internal static void SetPawnXenotype(this Pawn p, XenotypeDef def, bool toXenogene)
	{
		if (p.HasGeneTracker() && def != null)
		{
			p.genes.SetXenotypeDirect(def);
			p.genes.iconDef = null;
			if (CEditor.InStartingScreen)
			{
				int index = StartingPawnUtility.PawnIndex(p);
				PawnGenerationRequest generationRequest = StartingPawnUtility.GetGenerationRequest(index);
				generationRequest.ForcedXenotype = def;
				generationRequest.ForcedCustomXenotype = null;
				StartingPawnUtility.SetGenerationRequest(index, generationRequest);
			}
			p.SetGenesFromList(def.genes, toXenogene, Event.current.control);
			CEditor.API.UpdateGraphics();
		}
	}

	internal static void SetPawnXenotype(this Pawn p, CustomXenotype c, bool toXenogene)
	{
		if (p.HasGeneTracker() && c != null)
		{
			p.genes.SetXenotypeDirect(XenotypeDefOf.Baseliner);
			p.genes.xenotypeName = c.name;
			p.genes.iconDef = c.iconDef;
			if (!Current.Game.customXenotypeDatabase.customXenotypes.Contains(c))
			{
				Current.Game.customXenotypeDatabase.customXenotypes.Add(c);
			}
			if (CEditor.InStartingScreen)
			{
				int index = StartingPawnUtility.PawnIndex(p);
				PawnGenerationRequest generationRequest = StartingPawnUtility.GetGenerationRequest(index);
				generationRequest.ForcedXenotype = null;
				generationRequest.ForcedCustomXenotype = c;
				StartingPawnUtility.SetGenerationRequest(index, generationRequest);
			}
			p.SetGenesFromList(c.genes, toXenogene, Event.current.control);
			CEditor.API.UpdateGraphics();
		}
	}

	internal static string PrintIfXenotypeIsPrefered(Pawn p)
	{
		if (ModsConfig.IdeologyActive && p.HasIdeoTracker())
		{
			return Label.XENOTYPEISPREFERED + " " + (p.Ideo.IsPreferredXenotype(p) ? "Yes".Translate() : "No".Translate());
		}
		return "";
	}

	private static void Remove(GeneDef def)
	{
		Type typeFromHandle = typeof(DefDatabase<GeneDef>);
		typeFromHandle.CallMethod("Remove", new object[1] { def });
	}

	internal static void UpdateGeneCache()
	{
		Type aType = Reflect.GetAType("RimWorld", "GeneUtility");
		if (aType != null)
		{
			aType.SetMemberValue("cachedGeneDefsInOrder", null);
		}
		List<GeneDef> genesInOrder = GeneUtility.GenesInOrder;
	}

	internal static void EnDisableBodySizeGenes()
	{
		bool isBodysizeActive = CEditor.IsBodysizeActive;
		HashSet<GeneDef> listBodySizeGenes = ListBodySizeGenes;
		GeneCategoryDef bodySizeCategory = BodySizeCategory;
		if (cachedGenes.NullOrEmpty() && !listBodySizeGenes.NullOrEmpty())
		{
			foreach (GeneDef item in listBodySizeGenes)
			{
				cachedGenes.Add(item);
			}
		}
		if (isBodysizeActive)
		{
			if (listBodySizeGenes.NullOrEmpty())
			{
				foreach (GeneDef cachedGene in cachedGenes)
				{
					DefDatabase<GeneDef>.Add(cachedGene);
					cachedGene.displayCategory = bodySizeCategory;
					cachedGene.ResolveReferences();
					cachedGene.PostLoad();
				}
				bodySizeCategory?.ResolveReferences();
				UpdateGeneCache();
				Log.Message("restoring bodysize genes done");
			}
			else
			{
				Log.Message("bodysizes genes are active");
			}
		}
		else if (!listBodySizeGenes.NullOrEmpty())
		{
			foreach (GeneDef item2 in listBodySizeGenes)
			{
				Remove(item2);
			}
			bodySizeCategory?.ResolveReferences();
			UpdateGeneCache();
			Log.Message("removing bodysize genes done");
		}
		else
		{
			Log.Message("bodysize genes are inactive");
		}
	}
}
