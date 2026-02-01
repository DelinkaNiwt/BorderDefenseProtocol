using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using RimTalk.API;
using RimTalk.Data;
using RimTalk.Source.Data;
using RimTalk.Util;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimTalk.Service;

public static class ContextBuilder
{
	private static readonly MethodInfo VisibleHediffsMethod = AccessTools.Method(typeof(HealthCardUtility), "VisibleHediffs");

	public static string GetRaceContext(Pawn pawn, PromptService.InfoLevel infoLevel)
	{
		ContextSettings contextSettings = Settings.Get().Context;
		if (!contextSettings.IncludeRace || !ModsConfig.BiotechActive || pawn.genes?.Xenotype == null)
		{
			return null;
		}
		return "Race: " + pawn.genes.XenotypeLabel;
	}

	public static string GetNotableGenesContext(Pawn pawn, PromptService.InfoLevel infoLevel)
	{
		ContextSettings contextSettings = Settings.Get().Context;
		if (!contextSettings.IncludeNotableGenes || !ModsConfig.BiotechActive || pawn.genes?.GenesListForReading == null)
		{
			return null;
		}
		IEnumerable<TaggedString> notableGenes = from g in pawn.genes.GenesListForReading
			where g.def.biostatMet != 0 || g.def.biostatCpx != 0
			select g.def.LabelCap;
		if (infoLevel == PromptService.InfoLevel.Short)
		{
			notableGenes = from g in (from g in pawn.genes.GenesListForReading
					where g.def.biostatMet != 0 || g.def.biostatCpx != 0
					orderby Mathf.Abs(g.def.biostatMet) + g.def.biostatCpx descending
					select g).Take(3)
				select g.def.LabelCap;
		}
		if (notableGenes.Any())
		{
			return "Notable Genes: " + string.Join(", ", notableGenes);
		}
		return null;
	}

	public static string GetIdeologyContext(Pawn pawn, PromptService.InfoLevel infoLevel)
	{
		ContextSettings contextSettings = Settings.Get().Context;
		if (!contextSettings.IncludeIdeology || !ModsConfig.IdeologyActive || pawn.ideo?.Ideo == null)
		{
			return null;
		}
		StringBuilder sb = new StringBuilder();
		Ideo ideo = pawn.ideo.Ideo;
		if (infoLevel == PromptService.InfoLevel.Short)
		{
			IEnumerable<string> memes = (from m in ideo.memes?.Where((MemeDef m) => m != null).Take(3)
				select m.LabelCap.Resolve() into label
				where !string.IsNullOrEmpty(label)
				select label);
			if (memes != null && memes.Any())
			{
				return "Memes: " + string.Join(", ", memes);
			}
			return null;
		}
		sb.Append("Ideology: " + ideo.name);
		IEnumerable<string> memes2 = (from m in ideo.memes?.Where((MemeDef m) => m != null)
			select m.LabelCap.Resolve() into label
			where !string.IsNullOrEmpty(label)
			select label);
		if (memes2 != null && memes2.Any())
		{
			sb.Append("\nMemes: " + string.Join(", ", memes2));
		}
		return sb.ToString();
	}

	public static string GetBackstoryContext(Pawn pawn, PromptService.InfoLevel infoLevel)
	{
		ContextSettings contextSettings = Settings.Get().Context;
		if (!contextSettings.IncludeBackstory)
		{
			return null;
		}
		StringBuilder sb = new StringBuilder();
		if (infoLevel == PromptService.InfoLevel.Short)
		{
			if (pawn.story?.Adulthood != null)
			{
				return "Background: " + pawn.story.Adulthood.TitleCapFor(pawn.gender);
			}
		}
		else
		{
			if (pawn.story?.Childhood != null)
			{
				sb.Append(ContextHelper.FormatBackstory("Childhood", pawn.story.Childhood, pawn, infoLevel));
			}
			if (pawn.story?.Adulthood != null)
			{
				if (sb.Length > 0)
				{
					sb.Append("\n");
				}
				sb.Append(ContextHelper.FormatBackstory("Adulthood", pawn.story.Adulthood, pawn, infoLevel));
			}
		}
		return (sb.Length > 0) ? sb.ToString() : null;
	}

	public static string GetTraitsContext(Pawn pawn, PromptService.InfoLevel infoLevel)
	{
		ContextSettings contextSettings = Settings.Get().Context;
		if (!contextSettings.IncludeTraits)
		{
			return null;
		}
		List<string> traits = new List<string>();
		IEnumerable<Trait> enumerable = pawn.story?.traits?.TraitsSorted;
		foreach (Trait trait in enumerable ?? Enumerable.Empty<Trait>())
		{
			TraitDegreeData degreeData = trait.def.degreeDatas.FirstOrDefault((TraitDegreeData d) => d.degree == trait.Degree);
			if (degreeData != null)
			{
				string traitText = ((infoLevel == PromptService.InfoLevel.Full) ? (degreeData.label + ":" + CommonUtil.Sanitize(degreeData.description, pawn)) : degreeData.label);
				traits.Add(traitText);
			}
		}
		if (infoLevel == PromptService.InfoLevel.Short && traits.Count > 3)
		{
			traits = traits.Take(3).ToList();
		}
		if (traits.Any())
		{
			string separator = ((infoLevel == PromptService.InfoLevel.Full) ? "\n" : ",");
			return "Traits: " + string.Join(separator, traits);
		}
		return null;
	}

	public static string GetSkillsContext(Pawn pawn, PromptService.InfoLevel infoLevel)
	{
		ContextSettings contextSettings = Settings.Get().Context;
		if (!contextSettings.IncludeSkills)
		{
			return null;
		}
		IEnumerable<string> skills = pawn.skills?.skills?.Select((SkillRecord s) => $"{s.def.label}: {s.Level}");
		if (skills != null && skills.Any())
		{
			return "Skills: " + string.Join(", ", skills);
		}
		return null;
	}

	public static string GetHealthContext(Pawn pawn, PromptService.InfoLevel infoLevel)
	{
		ContextSettings contextSettings = Settings.Get().Context;
		if (!contextSettings.IncludeHealth)
		{
			return null;
		}
		IEnumerable<Hediff> hediffs = (IEnumerable<Hediff>)VisibleHediffsMethod.Invoke(null, new object[2] { pawn, false });
		if (infoLevel == PromptService.InfoLevel.Short)
		{
			hediffs = (from h in hediffs
				orderby h.Visible ? 1 : 0 descending, h.Severity descending, h.ageTicks descending
				select h).Take(3);
		}
		string healthInfo = string.Join(",", from h in hediffs
			group h by h.def into g
			select g.Key.label + "(" + string.Join(",", g.Select((Hediff h) => h.Part?.Label ?? "")) + ")");
		if (!string.IsNullOrEmpty(healthInfo))
		{
			return "Health: " + healthInfo;
		}
		return null;
	}

	public static string GetMoodContext(Pawn pawn, PromptService.InfoLevel infoLevel)
	{
		ContextSettings contextSettings = Settings.Get().Context;
		if (!contextSettings.IncludeMood)
		{
			return null;
		}
		Need_Mood m = pawn.needs?.mood;
		if (m?.MoodString != null)
		{
			return (pawn.Downed && !pawn.IsBaby()) ? "Critical: Downed (in pain/distress)" : (pawn.InMentalState ? ("Mood: " + pawn.MentalState?.InspectLine + " (in mental break)") : $"Mood: {m.MoodString} ({(int)(m.CurLevelPercentage * 100f)}%)");
		}
		return null;
	}

	public static string GetThoughtsContext(Pawn pawn, PromptService.InfoLevel infoLevel)
	{
		ContextSettings contextSettings = Settings.Get().Context;
		if (!contextSettings.IncludeThoughts)
		{
			return null;
		}
		Dictionary<Thought, float> allThoughts = ContextHelper.GetThoughts(pawn);
		IEnumerable<string> thoughts = ((infoLevel == PromptService.InfoLevel.Short) ? (from t in allThoughts.Keys.Take(3)
			select CommonUtil.Sanitize(t.LabelCap)) : allThoughts.Keys.Select((Thought t) => CommonUtil.Sanitize(t.LabelCap)));
		if (thoughts.Any())
		{
			return "Memory: " + string.Join(", ", thoughts);
		}
		return null;
	}

	public static string GetPrisonerSlaveContext(Pawn pawn, PromptService.InfoLevel infoLevel)
	{
		ContextSettings contextSettings = Settings.Get().Context;
		if (!contextSettings.IncludePrisonerSlaveStatus || (!pawn.IsSlave && !pawn.IsPrisoner))
		{
			return null;
		}
		return pawn.GetPrisonerSlaveStatus();
	}

	public static string GetRelationsContext(Pawn pawn, PromptService.InfoLevel infoLevel)
	{
		ContextSettings contextSettings = Settings.Get().Context;
		if (!contextSettings.IncludeRelations)
		{
			return null;
		}
		return RelationsService.GetRelationsString(pawn);
	}

	public static string GetEquipmentContext(Pawn pawn, PromptService.InfoLevel infoLevel)
	{
		ContextSettings contextSettings = Settings.Get().Context;
		if (!contextSettings.IncludeEquipment)
		{
			return null;
		}
		List<string> equipment = new List<string>();
		if (pawn.equipment?.Primary != null)
		{
			equipment.Add("Weapon: " + pawn.equipment.Primary.LabelCap);
		}
		IEnumerable<string> apparelLabels = pawn.apparel?.WornApparel?.Select((Apparel a) => a.LabelCap);
		if (apparelLabels != null && apparelLabels.Any())
		{
			equipment.Add("Apparel: " + string.Join(", ", apparelLabels));
		}
		if (equipment.Any())
		{
			return "Equipment: " + string.Join(", ", equipment);
		}
		return null;
	}

	public static void BuildDialogueType(StringBuilder sb, TalkRequest talkRequest, List<Pawn> pawns, string shortName, Pawn mainPawn)
	{
		if (talkRequest.TalkType.IsFromUser())
		{
			sb.Append(pawns[1].LabelShort + "(" + pawns[1].GetRole() + ") said to " + shortName + ": '" + talkRequest.Prompt + "'. ");
			if (pawns[1].IsPlayer() && Settings.Get().PlayerDialogueMode == Settings.PlayerDialogueMode.Manual)
			{
				sb.Append("Generate dialogue starting after this. Do not generate any further lines for " + pawns[1].LabelShort);
			}
			else
			{
				sb.Append("Generate multi turn dialogues starting after this (do not repeat initial dialogue), beginning with " + shortName);
			}
			return;
		}
		if (pawns.Count == 1)
		{
			sb.Append(shortName + " short monologue");
		}
		else if (mainPawn.IsInCombat() || mainPawn.GetMapRole() == MapRole.Invading)
		{
			if (talkRequest.TalkType != TalkType.Urgent && !mainPawn.InMentalState)
			{
				talkRequest.Prompt = null;
			}
			talkRequest.TalkType = TalkType.Urgent;
			sb.Append((mainPawn.IsSlave || mainPawn.IsPrisoner) ? (shortName + " dialogue short (worry)") : (shortName + " dialogue short, urgent tone (" + mainPawn.GetMapRole().ToString().ToLower() + "/command)"));
		}
		else
		{
			sb.Append(shortName + " starts conversation, taking turns");
		}
		if (mainPawn.InMentalState)
		{
			sb.Append("\nbe dramatic (mental break)");
		}
		else if (mainPawn.Downed && !mainPawn.IsBaby())
		{
			sb.Append("\n(downed in pain. Short, strained dialogue)");
		}
		else
		{
			sb.Append("\n" + talkRequest.Prompt);
		}
	}

	public static void BuildLocationContext(StringBuilder sb, ContextSettings contextSettings, Pawn mainPawn)
	{
		if (contextSettings.IncludeLocationAndTemperature)
		{
			string locationStatus = ContextHelper.GetPawnLocationStatus(mainPawn);
			if (!string.IsNullOrEmpty(locationStatus))
			{
				int temperature = Mathf.RoundToInt(mainPawn.Position.GetTemperature(mainPawn.Map));
				Room room = mainPawn.GetRoom();
				string roomRole = ((room == null || room.PsychologicallyOutdoors) ? "" : (room.Role?.label ?? "Room"));
				string locationInfo = (string.IsNullOrEmpty(roomRole) ? $"{locationStatus};{temperature}C" : $"{locationStatus};{temperature}C;{roomRole}");
				locationInfo = ContextHookRegistry.ApplyPawnHooks(ContextCategories.Pawn.Location, mainPawn, locationInfo);
				sb.Append("\nLocation: " + locationInfo);
			}
		}
	}

	public static void BuildEnvironmentContext(StringBuilder sb, ContextSettings contextSettings, Pawn mainPawn)
	{
		if (contextSettings.IncludeTerrain)
		{
			TerrainDef terrain = mainPawn.Position.GetTerrain(mainPawn.Map);
			if (terrain != null)
			{
				string value = ContextHookRegistry.ApplyPawnHooks(ContextCategories.Pawn.Terrain, mainPawn, terrain.LabelCap);
				sb.Append("\nTerrain: " + value);
			}
		}
		if (contextSettings.IncludeBeauty)
		{
			List<IntVec3> nearbyCells = ContextHelper.GetNearbyCells(mainPawn);
			if (nearbyCells.Count > 0)
			{
				float beautySum = nearbyCells.Sum((IntVec3 c) => BeautyUtility.CellBeauty(c, mainPawn.Map));
				string value2 = ContextHookRegistry.ApplyPawnHooks(ContextCategories.Pawn.Beauty, mainPawn, Describer.Beauty(beautySum / (float)nearbyCells.Count));
				sb.Append("\nCellBeauty: " + value2);
			}
		}
		Room pawnRoom = mainPawn.GetRoom();
		if (contextSettings.IncludeCleanliness && pawnRoom != null && !pawnRoom.PsychologicallyOutdoors)
		{
			string value3 = ContextHookRegistry.ApplyPawnHooks(ContextCategories.Pawn.Cleanliness, mainPawn, Describer.Cleanliness(pawnRoom.GetStat(RoomStatDefOf.Cleanliness)));
			sb.Append("\nCleanliness: " + value3);
		}
		if (contextSettings.IncludeSurroundings)
		{
			string surroundingsText = ContextHelper.CollectNearbyContextText(mainPawn, 3);
			if (!string.IsNullOrEmpty(surroundingsText))
			{
				string value4 = ContextHookRegistry.ApplyPawnHooks(ContextCategories.Pawn.Surroundings, mainPawn, surroundingsText);
				sb.Append("\nSurroundings:\n");
				sb.Append(value4);
			}
		}
	}

	[Obsolete("Use CommonUtil.Sanitize instead. Kept for backward compatibility.")]
	public static string Sanitize(string text, Pawn pawn = null)
	{
		return CommonUtil.Sanitize(text, pawn);
	}
}
