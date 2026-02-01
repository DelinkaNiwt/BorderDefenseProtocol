using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace CharacterEditor;

internal static class DefTool
{
	private delegate void GiveShortHash(Def def, Type defType, HashSet<ushort> takenHashes);

	private static readonly GiveShortHash giveShortHash = AccessTools.MethodDelegate<GiveShortHash>(AccessTools.Method(typeof(ShortHashGiver), "GiveShortHash"));

	internal static readonly AccessTools.FieldRef<Dictionary<Type, HashSet<ushort>>> takenShortHashes = AccessTools.StaticFieldRefAccess<Dictionary<Type, HashSet<ushort>>>(AccessTools.Field(typeof(ShortHashGiver), "takenHashesPerDeftype"));

	internal static Func<ThingCategoryDef, bool> CONDITION_NO_CORPSE => (ThingCategoryDef t) => !t.defName.ToLower().Contains("corpse");

	internal static Func<StatCategoryDef, string> CategoryLabel => (StatCategoryDef cat) => cat.SLabel_PlusDefName();

	internal static Func<StatModifier, StatDef> DefGetterStatModifier => (StatModifier sm) => sm.stat;

	internal static Func<DamageFactor, DamageDef> DefGetterDamageFacotr => (DamageFactor df) => df.damageDef;

	internal static Func<GeneticTraitData, TraitDef> DefGetterGeneticTraitData => (GeneticTraitData gtd) => gtd.def;

	internal static Func<Aptitude, SkillDef> DefGetterAptitude => (Aptitude a) => a.skill;

	internal static Func<ThingDefCountClass, ThingDef> DefGetterThingDefCountClass => (ThingDefCountClass t) => t.thingDef;

	internal static Func<DamageFactor, DamageDef> DefGetterDamageFactor => (DamageFactor d) => d.damageDef;

	internal static Func<PawnCapacityModifier, PawnCapacityDef> DefGetterPawnCapacityModifier => (PawnCapacityModifier pcm) => pcm.capacity;

	internal static Func<StatModifier, float> ValGetterStatModifier => (StatModifier sm) => sm.value;

	internal static Func<DamageFactor, float> ValGetterDamageFacotr => (DamageFactor df) => df.factor;

	internal static Func<GeneticTraitData, int> ValGetterGeneticTraitData => (GeneticTraitData gtd) => gtd.degree;

	internal static Func<Aptitude, int> ValGetterAptitude => (Aptitude a) => a.level;

	internal static Func<ThingDefCountClass, int> ValGetterThingDefCountClass => (ThingDefCountClass t) => t.count;

	internal static Func<DamageFactor, float> ValGetterDamageFactor => (DamageFactor d) => d.factor;

	internal static Func<PawnCapacityModifier, float> ValGetterPCMoffset => (PawnCapacityModifier pcm) => pcm.offset;

	internal static Func<PawnCapacityModifier, float> ValGetterPCMfactor => (PawnCapacityModifier pcm) => pcm.postFactor;

	internal static Func<DamageFactor, DamageDef, bool> CompareDamageFactor => (DamageFactor a, DamageDef b) => a.damageDef.defName == b.defName;

	internal static Func<StatModifier, StatDef, bool> CompareStatModifier => (StatModifier a, StatDef b) => a.stat.defName == b.defName;

	internal static Func<ThingDefCountClass, ThingDef, bool> CompareThingDefCountClass => (ThingDefCountClass a, ThingDef b) => a.thingDef.defName == b.defName;

	internal static Func<Aptitude, SkillDef, bool> CompareAptitude => (Aptitude a, SkillDef b) => a.skill.defName == b.defName;

	internal static Func<GeneticTraitData, GeneticTraitData, bool> CompareGeneticTraitData => (GeneticTraitData a, GeneticTraitData b) => b != null && a.def.defName == b.def.defName && a.degree == b.degree;

	internal static Func<PawnCapacityModifier, PawnCapacityDef, bool> ComparePawnCapacityModifier => (PawnCapacityModifier a, PawnCapacityDef b) => a.capacity.defName == b.defName;

	internal static Func<StatDef, StatCategoryDef, bool> CompareStatCategoryNot => (StatDef a, StatCategoryDef b) => a.category != b;

	internal static KeyBindingDef TryCreateKeyBinding(string defName, string keyCode, string label, string desc)
	{
		KeyBindingDef keyBindingDef = null;
		try
		{
			if (!keyCode.NullOrEmpty())
			{
				keyBindingDef = new KeyBindingDef();
				keyBindingDef.category = KeyBindingCategoryDefOf.MainTabs;
				keyBindingDef.defaultKeyCodeA = keyCode.AsKeyCode();
				keyBindingDef.defaultKeyCodeB = KeyCode.None;
				keyBindingDef.defName = defName;
				keyBindingDef.description = desc;
				keyBindingDef.label = label;
				keyBindingDef.ResolveReferences();
				keyBindingDef.PostLoad();
				GiveShortHashTo(keyBindingDef, typeof(KeyBindingDef));
				DefDatabase<Verse.KeyBindingDef>.Add(keyBindingDef);
				KeyPrefs.KeyPrefsData.keyPrefs.Add(keyBindingDef, new KeyBindingData(keyBindingDef.defaultKeyCodeA, keyBindingDef.defaultKeyCodeB));
			}
		}
		catch (Exception ex)
		{
			Log.Error(ex.StackTrace);
		}
		return keyBindingDef;
	}

	internal static MainButtonDef GetCreateMainButton(string defName, string label, string desc, Type typeClass, ModContentPack pack, string keyCode, bool isVisible)
	{
		MainButtonDef mainButtonDef = MainButtonDef(defName);
		if (mainButtonDef == null)
		{
			KeyBindingDef hotKey = TryCreateKeyBinding(defName, keyCode, label, desc);
			mainButtonDef = new MainButtonDef();
			mainButtonDef.buttonVisible = isVisible;
			mainButtonDef.defName = defName;
			mainButtonDef.description = desc;
			mainButtonDef.label = label;
			mainButtonDef.tabWindowClass = typeClass;
			mainButtonDef.order = MainButtonDefOf.Menu.order - 1;
			mainButtonDef.hotKey = hotKey;
			mainButtonDef.validWithoutMap = false;
			mainButtonDef.modContentPack = pack;
			mainButtonDef.iconPath = "beditoricon";
			mainButtonDef.minimized = true;
			mainButtonDef.ResolveReferences();
			mainButtonDef.PostLoad();
			DefDatabase<RimWorld.MainButtonDef>.Add(mainButtonDef);
		}
		return mainButtonDef;
	}

	internal static void GiveShortHashTo(Def def, Type defType)
	{
		giveShortHash(def, defType, takenShortHashes()[defType]);
	}

	internal static T GetDef<T>(string defName) where T : Def
	{
		return (!defName.NullOrEmpty()) ? DefDatabase<T>.GetNamed(defName, errorOnFail: false) : null;
	}

	internal static BackstoryDef BackstoryDef(string defName)
	{
		return GetDef<BackstoryDef>(defName);
	}

	internal static MainButtonDef MainButtonDef(string defName)
	{
		return GetDef<MainButtonDef>(defName);
	}

	internal static PawnKindDef PawnKindDef(string defName)
	{
		return GetDef<PawnKindDef>(defName);
	}

	internal static PawnKindDef PawnKindDef(string defName, PawnKindDef fallback)
	{
		return PawnKindDef(defName) ?? fallback;
	}

	internal static PawnRelationDef PawnRelationDef(string defName)
	{
		return GetDef<PawnRelationDef>(defName);
	}

	internal static BodyTypeDef BodyTypeDef(string defName)
	{
		return GetDef<BodyTypeDef>(defName);
	}

	internal static ColorDef ColorDef(string defName)
	{
		return GetDef<ColorDef>(defName);
	}

	internal static BeardDef BeardDef(string defName)
	{
		return GetDef<BeardDef>(defName);
	}

	internal static TattooDef TattooDef(string defName)
	{
		return GetDef<TattooDef>(defName);
	}

	internal static AbilityDef AbilityDef(string defName)
	{
		return GetDef<AbilityDef>(defName);
	}

	internal static GeneDef GeneDef(string defName)
	{
		return GetDef<GeneDef>(defName);
	}

	internal static XenotypeDef XenotypeDef(string defName)
	{
		return GetDef<XenotypeDef>(defName);
	}

	internal static HairDef HairDef(string defName)
	{
		return GetDef<HairDef>(defName);
	}

	internal static HediffDef HediffDef(string defName)
	{
		return GetDef<HediffDef>(defName);
	}

	internal static RecordDef RecordDef(string defName)
	{
		return GetDef<RecordDef>(defName);
	}

	internal static SkillDef SkillDef(string defName)
	{
		return GetDef<SkillDef>(defName);
	}

	internal static PawnCapacityDef PawnCapacityDef(string defName)
	{
		return GetDef<PawnCapacityDef>(defName);
	}

	internal static ScenPartDef ScenPartDef(string defName)
	{
		return GetDef<ScenPartDef>(defName);
	}

	internal static ThingDef ThingDef(string defName)
	{
		return GetDef<ThingDef>(defName);
	}

	internal static ThingStyleDef ThingStyleDef(string defName)
	{
		return GetDef<ThingStyleDef>(defName);
	}

	internal static ThingDef ThingDef(string defName, ThingDef fallback)
	{
		return ThingDef(defName) ?? fallback;
	}

	internal static NeedDef NeedDef(string defName)
	{
		return GetDef<NeedDef>(defName);
	}

	internal static ThoughtDef ThoughtDef(string defName)
	{
		return GetDef<ThoughtDef>(defName);
	}

	internal static TraitDef TraitDef(string defName)
	{
		return GetDef<TraitDef>(defName);
	}

	internal static WorkTypeDef WorkTypeDef(string defName)
	{
		return GetDef<WorkTypeDef>(defName);
	}

	internal static StatDef StatDef(string defName)
	{
		return GetDef<StatDef>(defName);
	}

	internal static JobDef JobDef(string defName)
	{
		return GetDef<JobDef>(defName);
	}

	internal static SoundDef SoundDef(string defName)
	{
		return GetDef<SoundDef>(defName);
	}

	internal static StuffCategoryDef StuffCategoryDef(string defName)
	{
		return GetDef<StuffCategoryDef>(defName);
	}

	internal static BodyPartGroupDef BodyPartGroupDef(string defName)
	{
		return GetDef<BodyPartGroupDef>(defName);
	}

	internal static ApparelLayerDef ApparelLayerDef(string defName)
	{
		return GetDef<ApparelLayerDef>(defName);
	}

	internal static DesignatorDropdownGroupDef DesignatorDropdownGroupDef(string defName)
	{
		return GetDef<DesignatorDropdownGroupDef>(defName);
	}

	internal static DesignationCategoryDef DesignationCategoryDef(string defName)
	{
		return GetDef<DesignationCategoryDef>(defName);
	}

	internal static KeyBindingDef KeyBindingDef(string defName)
	{
		return GetDef<KeyBindingDef>(defName);
	}

	internal static bool IsFromMod(this Def d, string modname)
	{
		return d != null && d.modContentPack != null && d.modContentPack.Name == modname;
	}

	internal static bool IsFromCoreMod(this Def d)
	{
		return d != null && d.modContentPack != null && d.modContentPack.IsCoreMod;
	}

	internal static string GetModName(this Def d)
	{
		return (d != null && d.modContentPack != null) ? d.modContentPack.Name : "";
	}

	internal static bool IsNullOrEmpty(this Def d)
	{
		return d?.defName.NullOrEmpty() ?? true;
	}

	internal static List<T> ListAll<T>() where T : Def
	{
		return DefDatabase<T>.AllDefs.OrderBy((T x) => x.label).ToList();
	}

	internal static HashSet<string> ListModnamesWithNull<T>(Func<T, bool> condition = null) where T : Def
	{
		HashSet<string> hashSet = new HashSet<string>();
		hashSet.Add(null);
		hashSet.AddRange((condition == null) ? ListModnames<T>() : ListModnames(condition));
		return hashSet;
	}

	internal static HashSet<string> ListModnames<T>(Func<T, bool> condition) where T : Def
	{
		return (from x in DefDatabase<T>.AllDefs
			where x.modContentPack != null && condition(x)
			orderby x.modContentPack.Name
			select x.modContentPack.Name).ToHashSet();
	}

	internal static HashSet<string> ListModnames<T>() where T : Def
	{
		return (from x in DefDatabase<T>.AllDefs
			where x.modContentPack != null
			orderby x.modContentPack.Name
			select x.modContentPack.Name).ToHashSet();
	}

	internal static HashSet<T> ListByModWithNull<T>(string name, Func<T, bool> condition = null) where T : Def
	{
		HashSet<T> hashSet = new HashSet<T>();
		hashSet.Add(null);
		hashSet.AddRange((condition == null) ? ListByMod<T>(name) : ListByMod(name, condition));
		return hashSet;
	}

	internal static HashSet<T> ListByMod<T>(string name, Func<T, bool> condition) where T : Def
	{
		return (from x in DefDatabase<T>.AllDefs
			where !x.IsNullOrEmpty() && (name.NullOrEmpty() || x.IsFromMod(name)) && condition(x)
			orderby x.label
			select x).ToHashSet();
	}

	internal static HashSet<T> ListByMod<T>(string name) where T : Def
	{
		return (from x in DefDatabase<T>.AllDefs
			where !x.IsNullOrEmpty() && (name.NullOrEmpty() || x.IsFromMod(name))
			orderby x.label
			select x).ToHashSet();
	}

	internal static HashSet<T> ListBy<T>(Func<T, bool> condition) where T : Def
	{
		return (from x in DefDatabase<T>.AllDefs
			where condition(x)
			orderby x.label
			select x).ToHashSet();
	}

	internal static HashSet<T> AllDefsWithName<T>(Func<T, bool> condition = null) where T : Def
	{
		return (from x in DefDatabase<T>.AllDefs
			where !x.defName.NullOrEmpty() && (condition == null || condition(x))
			orderby x.defName
			select x).ToHashSet();
	}

	internal static HashSet<T> AllDefsWithLabel<T>(Func<T, bool> condition = null) where T : Def
	{
		return (from x in DefDatabase<T>.AllDefs
			where !x.defName.NullOrEmpty() && !x.label.NullOrEmpty() && (condition == null || condition(x))
			orderby x.label
			select x).ToHashSet();
	}

	internal static HashSet<T> AllDefsWithNameWithNull<T>(Func<T, bool> condition = null) where T : Def
	{
		HashSet<T> hashSet = new HashSet<T>();
		hashSet.Add(null);
		hashSet.AddRange(AllDefsWithName(condition));
		return hashSet;
	}

	internal static HashSet<T> AllDefsWithLabelWithNull<T>(Func<T, bool> condition = null) where T : Def
	{
		HashSet<T> hashSet = new HashSet<T>();
		hashSet.Add(null);
		hashSet.AddRange(AllDefsWithLabel(condition));
		return hashSet;
	}

	internal static T GetNext<T>(this HashSet<T> list, T current)
	{
		int index = list.IndexOf(current);
		index = list.NextOrPrevIndex(index, next: true, random: false);
		return list.ElementAt(index);
	}

	internal static T GetPrev<T>(this HashSet<T> list, T current)
	{
		int index = list.IndexOf(current);
		index = list.NextOrPrevIndex(index, next: false, random: false);
		return list.ElementAt(index);
	}

	internal static Func<ThingDef, bool> CONDITION_IS_ITEM(string modname, ThingCategoryDef tc, ThingCategory tc2)
	{
		return (ThingDef t) => !t.label.NullOrEmpty() && !t.IsBlueprint && !t.IsCorpse && !t.IsFrame && !t.isFrameInt && !t.IsMinified() && t.projectile == null && t.mote == null && (modname.NullOrEmpty() || t.IsFromMod(modname)) && (tc == null || (!t.thingCategories.NullOrEmpty() && t.thingCategories.Contains(tc))) && (tc2 == ThingCategory.None || (tc2 == ThingCategory.Mineable && (t.IsMineableMineral() || t.IsMineableRock())) || (tc2 == ThingCategory.Animal && t.race != null && t.race.Animal) || (tc2 == ThingCategory.Mechanoid && t.race != null && t.race.IsMechanoid) || (tc2 == ThingCategory.HumanOrAlien && t.race != null && !t.race.IsMechanoid && !t.race.Animal) || t.category.ToString() == tc2.ToString());
	}

	internal static Func<StatDef, bool> CONDITION_STATDEFS_APPAREL(List<StatCategoryDef> lnok)
	{
		return (StatDef t) => !lnok.Contains(t.category) || t.category == StatCategoryDefOf.Apparel;
	}

	internal static Func<StatDef, bool> CONDITION_STATDEFS_WEAPON(List<StatCategoryDef> lnok)
	{
		return (StatDef t) => !lnok.Contains(t.category) || t.category == StatCategoryDefOf.Weapon;
	}

	internal static Func<StatDef, bool> CONDITION_STATDEFS_GENE(List<StatCategoryDef> lnok)
	{
		return (StatDef t) => !lnok.Contains(t.category);
	}

	internal static string SLabel(this ThoughtStage t)
	{
		return t.label.NullOrEmpty() ? "" : t.label;
	}

	internal static string SLabel_PlusDefName(this Def d)
	{
		string text = d.SLabel();
		text = ((text == Label.NONE) ? Label.ALL : text);
		return text + ((d != null) ? (" [" + d.defName + "]") : "");
	}

	internal static string SLabel(this Def d)
	{
		return (d == null) ? Label.NONE : ((!d.LabelCap.NullOrEmpty()) ? d.LabelCap.ToString() : (d.label.NullOrEmpty() ? "" : d.label));
	}

	internal static string SDefname(this Def d)
	{
		return (d == null || d.defName.NullOrEmpty()) ? "" : d.defName;
	}

	internal static string STooltip<T>(this T def)
	{
		if (def == null)
		{
			return "";
		}
		string text = "";
		if (typeof(T) == typeof(StatDef))
		{
			StatDef statDef = def as StatDef;
			text = ((statDef.label != null) ? statDef.label.CapitalizeFirst() : "");
			text += "\n";
			text += ((statDef.category != null && statDef.category.label != null) ? statDef.category.label.Colorize(Color.yellow) : "");
			text += ((statDef.category != null && statDef.category.defName != null) ? (" [" + statDef.category.defName + "]").Colorize(Color.gray) : "");
			text += "\n\n";
			text += (statDef.description.NullOrEmpty() ? "" : statDef.description);
		}
		else if (typeof(T) == typeof(GeneDef))
		{
			GeneDef geneDef = def as GeneDef;
			text += geneDef.DescriptionFull;
			text += "\n\n";
			text += geneDef.Modname().Colorize(Color.gray);
		}
		else if (typeof(T) == typeof(Gene))
		{
			Gene gene = def as Gene;
			text += gene.def.DescriptionFull;
			text += "\n\n";
			text += gene.def.Modname().Colorize(Color.gray);
		}
		else if (typeof(T) == typeof(HediffDef))
		{
			HediffDef hediffDef = def as HediffDef;
			text += hediffDef.Description;
			text += "\n\n";
			text += hediffDef.Modname().Colorize(Color.gray);
		}
		else if (typeof(T) == typeof(XenotypeDef))
		{
			XenotypeDef xenotypeDef = def as XenotypeDef;
			text += xenotypeDef.descriptionShort ?? xenotypeDef.description;
			text += "\n\n";
			text += xenotypeDef.Modname().Colorize(Color.gray);
		}
		else if (typeof(T) == typeof(GeneticTraitData))
		{
			GeneticTraitData gtd = def as GeneticTraitData;
			text = TraitTool.GetGeneticTraitTooltip(gtd);
		}
		else if (typeof(T) == typeof(Ability))
		{
			Ability ability = def as Ability;
			text += ability.def.GetTooltip();
			text += "\n\n";
			text += ability.def.Modname().Colorize(Color.gray);
		}
		else if (typeof(T) == typeof(AbilityDef))
		{
			AbilityDef abilityDef = def as AbilityDef;
			text += abilityDef.GetTooltip();
			text += "\n\n";
			text += abilityDef.Modname().Colorize(Color.gray);
		}
		else if (typeof(T) == typeof(DamageDef))
		{
			DamageDef damageDef = def as DamageDef;
			text += (damageDef.description.NullOrEmpty() ? "" : (damageDef.description + "\n\n"));
			text = text + damageDef.SDefname() + "\n";
			text += damageDef.Modname().Colorize(Color.gray);
		}
		else if (typeof(T) == typeof(ResearchProjectDef))
		{
			ResearchProjectDef researchProjectDef = def as ResearchProjectDef;
			text = researchProjectDef.GetTip();
		}
		else if (IsDef<T>())
		{
			Def def2 = def as Def;
			text += (def2.description.NullOrEmpty() ? "" : (def2.description + "\n\n"));
			text += def2.Modname().Colorize(Color.gray);
		}
		return text;
	}

	internal static bool IsDef<T>()
	{
		return typeof(T) == typeof(Def) || typeof(T).BaseType == typeof(Def) || (typeof(T).BaseType != null && typeof(T).BaseType.BaseType == typeof(Def));
	}

	internal static string Modname(this Def t)
	{
		return (t != null && t.modContentPack != null && t.modContentPack.Name != null) ? t.modContentPack.Name : "";
	}

	internal static HashSet<StatDef> StatDefs_Selection(HashSet<StatCategoryDef> lselectedCat)
	{
		return ListBy((StatDef x) => !x.defName.NullOrEmpty() && (lselectedCat.NullOrEmpty() || lselectedCat.Contains(x.category) || x.category == null));
	}

	internal static HashSet<StatCategoryDef> StatCategoryDefs_Selection(HashSet<StatCategoryDef> lskip)
	{
		return ListByModWithNull(null, (StatCategoryDef x) => !x.defName.NullOrEmpty() && !lskip.Contains(x));
	}

	internal static void RemoveCategoriesWithNoStatDef(this HashSet<StatCategoryDef> lcat)
	{
		HashSet<StatDef> hashSet = StatDefs_Selection(lcat);
		for (int num = lcat.Count - 1; num >= 0; num--)
		{
			StatCategoryDef statCategoryDef = lcat.At(num);
			bool flag = false;
			foreach (StatDef item in hashSet)
			{
				if (item.category == statCategoryDef)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				lcat.Remove(statCategoryDef);
			}
		}
	}

	internal static bool DefNameComparator<T>(T d1, T d2) where T : Def
	{
		return d1?.defName == d2?.defName;
	}

	internal static void RandomSearchedDef<T>(ICollection<T> l, ref T def) where T : Def
	{
		def = l.RandomElement();
		SZWidgets.sFind = def.SLabel();
	}

	internal static Texture2D GetTIcon<T>(this T def, Selected s = null)
	{
		Texture2D texture2D = null;
		if (typeof(T) == typeof(ThingDef))
		{
			texture2D = (def as ThingDef).uiIcon;
		}
		else if (typeof(T) == typeof(Ability))
		{
			texture2D = (def as Ability).def.uiIcon;
		}
		else if (typeof(T) == typeof(AbilityDef))
		{
			texture2D = (def as AbilityDef).uiIcon;
		}
		else if (typeof(T) == typeof(HairDef))
		{
			texture2D = (def as HairDef).Icon;
		}
		else if (typeof(T) == typeof(BeardDef))
		{
			texture2D = (def as BeardDef).Icon;
		}
		else if (typeof(T) == typeof(GeneDef))
		{
			texture2D = (def as GeneDef).Icon;
		}
		else if (typeof(T) == typeof(XenotypeDef))
		{
			texture2D = (def as XenotypeDef).Icon;
		}
		else if (typeof(T) == typeof(TattooDef))
		{
			texture2D = (def as TattooDef).Icon;
		}
		else if (typeof(T) == typeof(ScenPart))
		{
			s = (def as ScenPart).GetSelectedScenarioPart();
			texture2D = SZWidgets.IconForStyleCustom(s, s.style);
		}
		else if (typeof(T) == typeof(ThingStyleDef))
		{
			texture2D = SZWidgets.IconForStyleCustom(s, def as ThingStyleDef);
		}
		else if (typeof(T) == typeof(ThingCategoryDef))
		{
			texture2D = (def as ThingCategoryDef).icon;
		}
		else if (typeof(T) == typeof(CustomXenotype))
		{
			texture2D = (def as CustomXenotype).IconDef?.Icon;
		}
		else if (typeof(T) == typeof(Gene))
		{
			texture2D = (def as Gene).def.Icon;
		}
		return (texture2D == BaseContent.BadTex) ? null : texture2D;
	}

	internal static Color GetTColor<T>(this T def, ThingDef stuff = null)
	{
		if (typeof(T) == typeof(GeneDef))
		{
			return (def as GeneDef).IconColor;
		}
		if (typeof(T) == typeof(XenotypeDef))
		{
			return RimWorld.XenotypeDef.IconColor;
		}
		if (typeof(T) == typeof(ThingDef))
		{
			ThingDef thingDef = def as ThingDef;
			if (stuff != null)
			{
				return thingDef.GetColor(stuff);
			}
			return thingDef.uiIconColor;
		}
		if (typeof(T) == typeof(Selected))
		{
			Selected selected = def as Selected;
			return selected.thingDef.GetTColor(selected.stuff);
		}
		if (typeof(T) == typeof(CustomXenotype))
		{
			CustomXenotype customXenotype = def as CustomXenotype;
			return customXenotype.inheritable ? RimWorld.XenotypeDef.IconColor : ColorTool.colSkyBlue;
		}
		return Color.white;
	}

	internal static T FindByDef<T>(this ICollection<T> l, T def) where T : Def
	{
		return (!l.EnumerableNullOrEmpty()) ? l.Where((T s) => s.defName == def.defName).FirstOrFallback() : null;
	}

	internal static T FindBy<T, TDef>(this ICollection<T> l, Func<T, TDef, bool> comparator, TDef def) where TDef : Def
	{
		return (!l.EnumerableNullOrEmpty()) ? l.Where((T s) => comparator(s, def)).FirstOrFallback() : default(T);
	}

	internal static T FindBy<T>(this ICollection<T> l, Func<T, T, bool> comparator, T obj)
	{
		return (!l.EnumerableNullOrEmpty()) ? l.Where((T s) => comparator(s, obj)).FirstOrFallback() : default(T);
	}

	internal static void SetMulti<T1, T, T2, T3>(ref List<T1> l, Func<T1, T, bool> comparator, Action<List<T1>, T1, T, T2, T3> valueSetter, T def, T2 value1, T3 value2) where T : Def
	{
		if (def != null)
		{
			if (l == null)
			{
				l = new List<T1>();
			}
			T1 arg = l.FindBy(comparator, def);
			valueSetter(l, arg, def, value1, value2);
		}
	}

	internal static void SetMulti<T1, T, T2, T3>(this List<T1> l, Func<T1, T, bool> comparator, Action<List<T1>, T1, T, T2, T3> valueSetter, T def, T2 value1, T3 value2) where T : Def
	{
		if (def != null)
		{
			if (l == null)
			{
				l = new List<T1>();
			}
			T1 arg = l.FindBy(comparator, def);
			valueSetter(l, arg, def, value1, value2);
		}
	}

	internal static void Set<T1, T, T2>(ref List<T1> l, Func<T1, T, bool> comparator, Action<List<T1>, T1, T, T2> valueSetter, T def, T2 value) where T : Def
	{
		if (def != null)
		{
			if (l == null)
			{
				l = new List<T1>();
			}
			T1 arg = l.FindBy(comparator, def);
			valueSetter(l, arg, def, value);
		}
	}

	internal static void Set<T1, T, T2>(this List<T1> l, Func<T1, T, bool> comparator, Action<List<T1>, T1, T, T2> valueSetter, T def, T2 value) where T : Def
	{
		if (def != null)
		{
			if (l == null)
			{
				l = new List<T1>();
			}
			T1 arg = l.FindBy(comparator, def);
			valueSetter(l, arg, def, value);
		}
	}

	internal static void Set<T1, T, T2>(ref List<T1> l, Func<T1, T1, bool> comparator, Action<List<T1>, T1, T, T2> valueSetter, T1 obj, T def, T2 value)
	{
		if (def != null)
		{
			if (l == null)
			{
				l = new List<T1>();
			}
			T1 arg = ((ICollection<T1>)l).FindBy<T1>(comparator, obj);
			valueSetter(l, arg, def, value);
		}
	}

	internal static void Set<T1, T, T2>(this List<T1> l, Func<T1, T1, bool> comparator, Action<List<T1>, T1, T, T2> valueSetter, T1 obj, T def, T2 value)
	{
		if (def != null)
		{
			if (l == null)
			{
				l = new List<T1>();
			}
			T1 arg = ((ICollection<T1>)l).FindBy<T1>(comparator, obj);
			valueSetter(l, arg, def, value);
		}
	}

	internal static void AddDef<T>(this List<T> l, T def) where T : Def
	{
		if (l == null)
		{
			l = new List<T>();
		}
		if (!l.Contains(def))
		{
			l.Add(def);
		}
	}

	internal static void SetStatModifier(List<StatModifier> l, StatModifier sm, StatDef def, float value)
	{
		if (sm != null)
		{
			sm.value = value;
			return;
		}
		sm = new StatModifier();
		sm.stat = def;
		sm.value = value;
		l.Add(sm);
	}

	internal static void SetAptitude(List<Aptitude> l, Aptitude a, SkillDef def, int value)
	{
		if (a != null)
		{
			a.level = value;
			return;
		}
		a = new Aptitude();
		a.skill = def;
		a.level = value;
		l.Add(a);
	}

	internal static void SetThingDefCountClass(List<ThingDefCountClass> l, ThingDefCountClass c, ThingDef def, int value)
	{
		if (c != null)
		{
			c.count = value;
			return;
		}
		c = new ThingDefCountClass();
		c.thingDef = def;
		c.count = value;
		l.Add(c);
	}

	internal static void SetGeneticTraitData(List<GeneticTraitData> l, GeneticTraitData gtd, TraitDef def, int value)
	{
		if (gtd != null)
		{
			gtd.degree = value;
			return;
		}
		gtd = new GeneticTraitData();
		gtd.def = def;
		gtd.degree = value;
		l.Add(gtd);
	}

	internal static void SetDamageFactor(List<DamageFactor> l, DamageFactor df, DamageDef def, float value)
	{
		if (df != null)
		{
			df.factor = value;
			return;
		}
		df = new DamageFactor();
		df.damageDef = def;
		df.factor = value;
		l.Add(df);
	}

	internal static void SetPawnCapacityModifier(List<PawnCapacityModifier> l, PawnCapacityModifier pcm, PawnCapacityDef def, float offset, float factor)
	{
		if (pcm != null)
		{
			pcm.offset = offset;
			pcm.postFactor = factor;
			return;
		}
		pcm = new PawnCapacityModifier();
		pcm.capacity = def;
		pcm.offset = offset;
		pcm.postFactor = factor;
		l.Add(pcm);
	}
}
