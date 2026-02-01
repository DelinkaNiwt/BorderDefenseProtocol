using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace CharacterEditor;

internal static class TraitTool
{
	private static int pid = 0;

	private static Dictionary<KeyValuePair<TraitDef, TraitDegreeData>, string> dicDesc = new Dictionary<KeyValuePair<TraitDef, TraitDegreeData>, string>();

	internal static Func<KeyValuePair<TraitDef, TraitDegreeData>, string> FTraitLabel = (KeyValuePair<TraitDef, TraitDegreeData> t) => (t.Value != null) ? (t.Value.LabelCap ?? t.Value.label ?? t.Key.label) : t.Key.LabelCap.RawText;

	internal static Func<KeyValuePair<TraitDef, TraitDegreeData>, string> FTraitTooltip = (KeyValuePair<TraitDef, TraitDegreeData> t) => dicDesc.GetValue(t);

	internal static Func<KeyValuePair<TraitDef, TraitDegreeData>, KeyValuePair<TraitDef, TraitDegreeData>, bool> FTraitComparator = (KeyValuePair<TraitDef, TraitDegreeData> t1, KeyValuePair<TraitDef, TraitDegreeData> t2) => t1.Key == t2.Key && t1.Value == t2.Value;

	internal static Func<GeneticTraitData, string> FGeneticTraitLabel = (GeneticTraitData gtd) => GetGeneticTraitLabel(gtd);

	internal static Func<List<KeyValuePair<TraitDef, TraitDegreeData>>, GeneticTraitData, string> FTraitLabel2 => (List<KeyValuePair<TraitDef, TraitDegreeData>> l, GeneticTraitData gtd) => FTraitLabel(l.Where(delegate(KeyValuePair<TraitDef, TraitDegreeData> pair)
	{
		KeyValuePair<TraitDef, TraitDegreeData> keyValuePair = pair;
		int result;
		if (keyValuePair.Key == gtd.def)
		{
			keyValuePair = pair;
			result = ((keyValuePair.Value.degree == gtd.degree) ? 1 : 0);
		}
		else
		{
			result = 0;
		}
		return (byte)result != 0;
	}).FirstOrFallback());

	internal static void UpdateDicTooltip(List<KeyValuePair<TraitDef, TraitDegreeData>> l)
	{
		dicDesc.Clear();
		pid = CEditor.API.Pawn.thingIDNumber;
		if (l.NullOrEmpty())
		{
			return;
		}
		foreach (KeyValuePair<TraitDef, TraitDegreeData> item in l)
		{
			if (item.Key == null)
			{
				continue;
			}
			string text = "";
			string text2 = ((item.Value != null) ? item.Value.description : item.Key.description);
			if (!text2.NullOrEmpty())
			{
				try
				{
					Trait trait = new Trait(item.Key, (item.Value != null) ? item.Value.degree : 0);
					text2 = trait.TipString(CEditor.API.Pawn);
				}
				catch
				{
				}
			}
			text += text2;
			text += "\n\n";
			text += item.Key.GetModName().Colorize(Color.gray);
			dicDesc.Add(item, text);
		}
	}

	internal static string GetGeneticTraitTooltip(GeneticTraitData gtd)
	{
		if (dicDesc.NullOrEmpty() || pid != CEditor.API.Pawn.thingIDNumber)
		{
			UpdateDicTooltip(ListOfTraitsKeyValuePair(null));
		}
		TraitDegreeData tdd = GetTraitDegreeDataFromGenetic(gtd);
		return FTraitTooltip(dicDesc.Keys.First((KeyValuePair<TraitDef, TraitDegreeData> x) => x.Key == gtd.def && x.Value == tdd));
	}

	internal static TraitDegreeData GetTraitDegreeDataFromGenetic(GeneticTraitData gtd)
	{
		if (gtd.def.degreeDatas.NullOrEmpty())
		{
			return null;
		}
		foreach (TraitDegreeData degreeData in gtd.def.degreeDatas)
		{
			if (degreeData.degree == gtd.degree)
			{
				return degreeData;
			}
		}
		return null;
	}

	internal static string GetGeneticTraitLabel(GeneticTraitData gtd)
	{
		if (gtd == null || gtd.def == null)
		{
			return "";
		}
		if (gtd.def.degreeDatas.NullOrEmpty())
		{
			return gtd.def.LabelCap;
		}
		foreach (TraitDegreeData degreeData in gtd.def.degreeDatas)
		{
			if (degreeData.degree == gtd.degree)
			{
				return degreeData.LabelCap;
			}
		}
		return gtd.def.label;
	}

	internal static HashSet<GeneticTraitData> ListAllAsGenticTraitData()
	{
		List<TraitDef> list = ListOfTraitDef(null);
		HashSet<GeneticTraitData> hashSet = new HashSet<GeneticTraitData>();
		foreach (TraitDef item in list)
		{
			if (item.degreeDatas.NullOrEmpty())
			{
				GeneticTraitData geneticTraitData = new GeneticTraitData();
				geneticTraitData.def = item;
				geneticTraitData.degree = 0;
				hashSet.Add(geneticTraitData);
				continue;
			}
			foreach (TraitDegreeData degreeData in item.degreeDatas)
			{
				GeneticTraitData geneticTraitData2 = new GeneticTraitData();
				geneticTraitData2.def = item;
				geneticTraitData2.degree = degreeData.degree;
				hashSet.Add(geneticTraitData2);
			}
		}
		return hashSet;
	}

	internal static string GetTraitAsSeparatedString(this Trait t)
	{
		if (t == null || t.def.IsNullOrEmpty())
		{
			return "";
		}
		string text = "";
		text = text + t.def.defName + "|";
		return text + t.Degree;
	}

	internal static string GetAllTraitsAsSeparatedString(this Pawn p)
	{
		if (!p.HasTraitTracker() || p.story.traits.allTraits.NullOrEmpty())
		{
			return "";
		}
		string text = "";
		foreach (Trait allTrait in p.story.traits.allTraits)
		{
			text += allTrait.GetTraitAsSeparatedString();
			text += ":";
		}
		return text.SubstringRemoveLast();
	}

	internal static void SetTraitsFromSeparatedString(this Pawn p, string s)
	{
		if (!p.HasTraitTracker() || s.NullOrEmpty())
		{
			return;
		}
		try
		{
			Dictionary<TraitDef, int> geneBasedTraitDefsToSkip = p.GetGeneBasedTraitDefsToSkip();
			string[] array = s.Split(new string[1] { ":" }, StringSplitOptions.None);
			string[] array2 = array;
			foreach (string text in array2)
			{
				string[] array3 = text.Split(new string[1] { "|" }, StringSplitOptions.None);
				if (array3.Length != 2)
				{
					continue;
				}
				TraitDef traitDef = DefTool.TraitDef(array3[0]);
				int num = array3[1].AsInt32();
				bool flag = false;
				foreach (TraitDef key in geneBasedTraitDefsToSkip.Keys)
				{
					if (traitDef.defName == key.defName && num == geneBasedTraitDefsToSkip[key])
					{
						geneBasedTraitDefsToSkip[key] = -100;
						flag = true;
						break;
					}
				}
				if (traitDef != null && !flag)
				{
					p.AddTrait(traitDef, traitDef.GetDegreeDataOrFirst(num));
				}
			}
			MeditationFocusTypeAvailabilityCache.ClearFor(p);
			StatsReportUtility.Reset();
		}
		catch (Exception e)
		{
			MessageTool.DebugException(e);
		}
	}

	internal static Dictionary<TraitDef, int> GetGeneBasedTraitDefsToSkip(this Pawn p)
	{
		Dictionary<TraitDef, int> dictionary = new Dictionary<TraitDef, int>();
		if (!p.HasGeneTracker())
		{
			return dictionary;
		}
		List<Gene> genesListForReading = p.genes.GenesListForReading;
		for (int i = 0; i < genesListForReading.Count; i++)
		{
			Gene gene = genesListForReading[i];
			if (gene.def.forcedTraits.NullOrEmpty())
			{
				continue;
			}
			foreach (GeneticTraitData forcedTrait in gene.def.forcedTraits)
			{
				if (!dictionary.ContainsKey(forcedTrait.def))
				{
					dictionary.Add(forcedTrait.def, forcedTrait.degree);
				}
			}
		}
		return dictionary;
	}

	internal static string GetTooltipForSkillpoints(this Pawn pawn, SkillRecord skillrecord)
	{
		if (pawn == null || skillrecord == null || skillrecord.def == null || !pawn.HasStoryTracker())
		{
			return "";
		}
		SkillDef def = skillrecord.def;
		string text = "Base lvl " + skillrecord.levelInt + "\n";
		try
		{
			foreach (Trait allTrait in pawn.story.traits.allTraits)
			{
				if (allTrait.CurrentData == null || allTrait.CurrentData.skillGains.NullOrEmpty())
				{
					continue;
				}
				foreach (SkillGain skillGain in allTrait.CurrentData.skillGains)
				{
					if (skillGain.skill.defName == def.defName)
					{
						int amount = skillGain.amount;
						if (amount != 0)
						{
							text = ((amount <= 0) ? (text ?? "") : (text + "+"));
							text = text + amount + " ";
							text += allTrait.LabelCap ?? allTrait.Label ?? allTrait.def.label;
							text += "\n";
						}
					}
				}
			}
			BackstoryDef backstory = pawn.story.GetBackstory(BackstorySlot.Adulthood);
			BackstoryDef backstory2 = pawn.story.GetBackstory(BackstorySlot.Childhood);
			if (backstory != null && !backstory.skillGains.NullOrEmpty())
			{
				foreach (SkillGain skillGain2 in backstory.skillGains)
				{
					if (skillGain2.skill.defName == def.defName)
					{
						int amount2 = skillGain2.amount;
						if (amount2 != 0)
						{
							text = ((amount2 <= 0) ? (text ?? "") : (text + "+"));
							text = text + amount2 + " ";
							text += backstory.TitleCapFor(pawn.gender);
							text += "\n";
						}
					}
				}
			}
			if (backstory2 != null && !backstory2.skillGains.NullOrEmpty())
			{
				foreach (SkillGain skillGain3 in backstory2.skillGains)
				{
					if (skillGain3.skill.defName == def.defName)
					{
						int amount3 = skillGain3.amount;
						if (amount3 != 0)
						{
							text = ((amount3 <= 0) ? (text ?? "") : (text + "+"));
							text = text + amount3 + " ";
							text += backstory2.TitleCapFor(pawn.gender);
							text += "\n";
						}
					}
				}
			}
		}
		catch
		{
		}
		if (skillrecord.Aptitude > 0)
		{
			text = text + "+" + skillrecord.Aptitude + " Genetic";
		}
		else if (skillrecord.Aptitude < 0)
		{
			text = text + skillrecord.Aptitude + " Genetic";
		}
		return text;
	}

	internal static int GetTraitOffsetForSkill(this Pawn pawn, SkillDef skill)
	{
		if (pawn == null || skill == null || !pawn.HasStoryTracker())
		{
			return 0;
		}
		int num = 0;
		foreach (Trait allTrait in pawn.story.traits.allTraits)
		{
			if (allTrait.CurrentData == null || allTrait.CurrentData.skillGains.NullOrEmpty())
			{
				continue;
			}
			foreach (SkillGain skillGain in allTrait.CurrentData.skillGains)
			{
				if (skillGain.skill.defName == skill.defName)
				{
					num += skillGain.amount;
				}
			}
		}
		return num;
	}

	internal static void AddTrait(this Pawn pawn, TraitDef traitDef, TraitDegreeData degreeData, bool random = false, bool doChangeSkillValue = false, Trait oldTraitToReplace = null)
	{
		if (pawn == null || pawn.story == null || (!random && traitDef == null))
		{
			return;
		}
		if (random)
		{
			traitDef = GetRandomTrait(out degreeData);
			if (pawn.HasTrait(traitDef, degreeData))
			{
				traitDef = GetRandomTrait(out degreeData);
			}
		}
		if (pawn.story.traits == null)
		{
			pawn.story.traits = new TraitSet(pawn);
		}
		if (pawn.story.traits.allTraits == null)
		{
			pawn.story.traits.allTraits = new List<Trait>();
		}
		Trait trait = new Trait(traitDef, degreeData?.degree ?? traitDef.degreeDatas.FirstOrDefault().degree);
		if (oldTraitToReplace != null)
		{
			if (pawn.skills != null && doChangeSkillValue)
			{
				TraitDegreeData currentData = oldTraitToReplace.CurrentData;
				if (currentData != null && !currentData.skillGains.NullOrEmpty())
				{
					foreach (SkillRecord skill in pawn.skills.skills)
					{
						foreach (SkillGain skillGain in currentData.skillGains)
						{
							if (skill.def.defName == skillGain.skill.defName)
							{
								skill.Level -= skillGain.amount;
							}
						}
					}
				}
			}
			pawn.story.traits.allTraits.Replace(oldTraitToReplace, trait);
		}
		else
		{
			pawn.story.traits.allTraits.Add(trait);
		}
		trait.pawn = pawn;
		pawn.Notify_DisabledWorkTypesChanged();
		if (pawn.skills != null)
		{
			pawn.skills.Notify_SkillDisablesChanged();
			if (doChangeSkillValue && degreeData != null && !degreeData.skillGains.NullOrEmpty())
			{
				foreach (SkillRecord skill2 in pawn.skills.skills)
				{
					foreach (SkillGain skillGain2 in degreeData.skillGains)
					{
						if (skill2.def.defName == skillGain2.skill.defName)
						{
							skill2.Level += skillGain2.amount;
						}
					}
				}
			}
		}
		if (!pawn.Dead && pawn.RaceProps.Humanlike && pawn.needs.mood != null)
		{
			pawn.needs.mood.thoughts.situational.Notify_SituationalThoughtsDirty();
		}
		MeditationFocusTypeAvailabilityCache.ClearFor(pawn);
		StatsReportUtility.Reset();
		CEditor.API.UpdateGraphics();
	}

	internal static void RemoveTrait(this Pawn pawn, Trait t)
	{
		if (t == null)
		{
			return;
		}
		if (t.CurrentData != null && !t.CurrentData.skillGains.NullOrEmpty())
		{
			foreach (SkillRecord skill in pawn.skills.skills)
			{
				foreach (SkillGain skillGain in t.CurrentData.skillGains)
				{
					if (skill.def.defName == skillGain.skill.defName)
					{
						skill.Level -= skillGain.amount;
					}
				}
			}
		}
		pawn.story.traits.allTraits.Remove(t);
		MeditationFocusTypeAvailabilityCache.ClearFor(pawn);
		StatsReportUtility.Reset();
		CEditor.API.UpdateGraphics();
	}

	internal static TraitDegreeData GetDegreeDataOrFirst(this TraitDef t, int degree)
	{
		if (t == null || t.degreeDatas.NullOrEmpty())
		{
			return null;
		}
		foreach (TraitDegreeData degreeData in t.degreeDatas)
		{
			if (degreeData.degree == degree)
			{
				return degreeData;
			}
		}
		return t.degreeDatas.FirstOrDefault();
	}

	internal static TraitDef GetRandomTrait(out TraitDegreeData degreeData)
	{
		TraitDef traitDef = DefDatabase<TraitDef>.AllDefs.RandomElement();
		degreeData = (traitDef.degreeDatas.NullOrEmpty() ? null : traitDef.degreeDatas.RandomElement());
		return traitDef;
	}

	internal static bool HasTrait(this Pawn pawn, TraitDef traitDef, TraitDegreeData degreeData)
	{
		if (pawn == null || pawn.story == null || traitDef == null || pawn.story.traits == null || pawn.story.traits.allTraits.NullOrEmpty())
		{
			return false;
		}
		int num = degreeData?.degree ?? 0;
		foreach (Trait allTrait in pawn.story.traits.allTraits)
		{
			if (allTrait.def.defName == traitDef.defName && allTrait.Degree == num)
			{
				return true;
			}
		}
		return false;
	}

	internal static List<TraitDef> ListOfTraitDef(string modname)
	{
		bool bAll = modname.NullOrEmpty();
		return (from td in DefDatabase<TraitDef>.AllDefs
			where td != null && !td.defName.NullOrEmpty() && (bAll || td.IsFromMod(modname))
			orderby td.defName
			select td).ToList();
	}

	internal static HashSet<StatModifier> ListOfTraitStatModifier(string modname, bool withNull)
	{
		List<TraitDef> list = ListOfTraitDef(modname);
		HashSet<StatModifier> hashSet = new HashSet<StatModifier>();
		List<string> list2 = new List<string>();
		if (withNull)
		{
			hashSet.Add(null);
		}
		if (!list.NullOrEmpty())
		{
			foreach (TraitDef item in list)
			{
				if (item == null || item.degreeDatas.NullOrEmpty())
				{
					continue;
				}
				foreach (TraitDegreeData degreeData in item.degreeDatas)
				{
					if (degreeData == null)
					{
						continue;
					}
					if (!degreeData.statOffsets.NullOrEmpty())
					{
						foreach (StatModifier statOffset in degreeData.statOffsets)
						{
							if (statOffset.stat != null && statOffset.stat.defName != null && !list2.Contains(statOffset.stat.defName))
							{
								list2.Add(statOffset.stat.defName);
								hashSet.Add(statOffset);
							}
						}
					}
					if (degreeData.statFactors.NullOrEmpty())
					{
						continue;
					}
					foreach (StatModifier statFactor in degreeData.statFactors)
					{
						if (statFactor.stat != null && statFactor.stat.defName != null && !list2.Contains(statFactor.stat.defName))
						{
							list2.Add(statFactor.stat.defName);
							hashSet.Add(statFactor);
						}
					}
				}
			}
		}
		return hashSet;
	}

	internal static List<KeyValuePair<TraitDef, TraitDegreeData>> ListOfTraitsKeyValuePair(string modname, StatModifier sm = null, string category = null)
	{
		List<TraitDef> list = ListOfTraitDef(modname);
		List<KeyValuePair<TraitDef, TraitDegreeData>> list2 = new List<KeyValuePair<TraitDef, TraitDegreeData>>();
		foreach (TraitDef item in list)
		{
			if (item.degreeDatas.NullOrEmpty())
			{
				list2.Add(new KeyValuePair<TraitDef, TraitDegreeData>(item, null));
				continue;
			}
			foreach (TraitDegreeData degreeData in item.degreeDatas)
			{
				list2.Add(new KeyValuePair<TraitDef, TraitDegreeData>(item, degreeData));
			}
		}
		List<KeyValuePair<TraitDef, TraitDegreeData>> list3 = list2.OrderBy(delegate(KeyValuePair<TraitDef, TraitDegreeData> td)
		{
			KeyValuePair<TraitDef, TraitDegreeData> keyValuePair = td;
			string text;
			if (keyValuePair.Value == null)
			{
				keyValuePair = td;
				text = keyValuePair.Key.LabelCap.RawText;
			}
			else
			{
				keyValuePair = td;
				text = keyValuePair.Value.LabelCap;
				if (text == null)
				{
					keyValuePair = td;
					text = keyValuePair.Value.label;
					if (text == null)
					{
						keyValuePair = td;
						text = keyValuePair.Key.label;
					}
				}
			}
			return text;
		}).ToList();
		if (sm != null)
		{
			List<KeyValuePair<TraitDef, TraitDegreeData>> list4 = new List<KeyValuePair<TraitDef, TraitDegreeData>>();
			foreach (KeyValuePair<TraitDef, TraitDegreeData> item2 in list3)
			{
				if (item2.Key.degreeDatas.NullOrEmpty())
				{
					continue;
				}
				foreach (TraitDegreeData degreeData2 in item2.Key.degreeDatas)
				{
					if (degreeData2 != null)
					{
						if (!degreeData2.statOffsets.NullOrEmpty() && degreeData2.statOffsets.Contains(sm))
						{
							list4.Add(item2);
							break;
						}
						if (!degreeData2.statFactors.NullOrEmpty() && degreeData2.statFactors.Contains(sm))
						{
							list4.Add(item2);
							break;
						}
					}
				}
			}
			list3 = list4;
		}
		if (category != null)
		{
			List<KeyValuePair<TraitDef, TraitDegreeData>> list5 = new List<KeyValuePair<TraitDef, TraitDegreeData>>();
			foreach (KeyValuePair<TraitDef, TraitDegreeData> item3 in list3)
			{
				if (item3.Key.degreeDatas.NullOrEmpty())
				{
					continue;
				}
				foreach (TraitDegreeData degreeData3 in item3.Key.degreeDatas)
				{
					if (degreeData3 == null)
					{
						continue;
					}
					if (category == Label.STAT)
					{
						if (!degreeData3.statFactors.NullOrEmpty() || !degreeData3.statOffsets.NullOrEmpty())
						{
							list5.Add(item3);
							break;
						}
					}
					else if (category == Label.MENTAL)
					{
						if (degreeData3.mentalBreakInspirationGainChance != 0f || degreeData3.forcedMentalStateMtbDays != -1f || degreeData3.forcedMentalState != null || degreeData3.randomMentalState != null || !degreeData3.disallowedMentalStates.NullOrEmpty() || !degreeData3.theOnlyAllowedMentalBreaks.NullOrEmpty())
						{
							list5.Add(item3);
							break;
						}
					}
					else if (category == Label.THOUGHTS)
					{
						if (!degreeData3.disallowedThoughts.NullOrEmpty() || !degreeData3.disallowedThoughtsFromIngestion.NullOrEmpty() || !degreeData3.extraThoughtsFromIngestion.NullOrEmpty())
						{
							list5.Add(item3);
							break;
						}
					}
					else if (category == Label.INSPIRATIONS)
					{
						if (degreeData3.mentalBreakInspirationGainChance != 0f || !degreeData3.disallowedInspirations.NullOrEmpty() || !degreeData3.mentalBreakInspirationGainSet.NullOrEmpty())
						{
							list5.Add(item3);
							break;
						}
					}
					else if (category == Label.FOCUS)
					{
						if (!degreeData3.allowedMeditationFocusTypes.NullOrEmpty() || !degreeData3.disallowedMeditationFocusTypes.NullOrEmpty())
						{
							list5.Add(item3);
							break;
						}
					}
					else if (category == Label.SKILLGAINS)
					{
						if (!degreeData3.skillGains.NullOrEmpty())
						{
							list5.Add(item3);
							break;
						}
					}
					else if (category == Label.ABILITIES)
					{
						if (!degreeData3.abilities.NullOrEmpty())
						{
							list5.Add(item3);
							break;
						}
					}
					else if (category == Label.NEEDS)
					{
						if (!degreeData3.enablesNeeds.NullOrEmpty())
						{
							list5.Add(item3);
							break;
						}
					}
					else if (category == Label.INGESTIBLEMOD && !degreeData3.ingestibleModifiers.NullOrEmpty())
					{
						list5.Add(item3);
						break;
					}
				}
			}
			list3 = list5;
		}
		return list3;
	}
}
