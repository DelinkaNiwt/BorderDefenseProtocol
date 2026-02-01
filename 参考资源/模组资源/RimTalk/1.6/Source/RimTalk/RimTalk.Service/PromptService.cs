using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using RimTalk.API;
using RimTalk.Data;
using RimTalk.Util;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace RimTalk.Service;

public static class PromptService
{
	public enum InfoLevel
	{
		Short,
		Normal,
		Full
	}

	public static string BuildContext(List<Pawn> pawns)
	{
		StringBuilder context = new StringBuilder();
		for (int i = 0; i < pawns.Count; i++)
		{
			Pawn pawn = pawns[i];
			if (!pawn.IsPlayer())
			{
				InfoLevel infoLevel = ((!Settings.Get().Context.EnableContextOptimization && i == 0) ? InfoLevel.Normal : InfoLevel.Short);
				string pawnContext = CreatePawnContext(pawn, infoLevel);
				pawnContext = CommonUtil.StripFormattingTags(pawnContext);
				Cache.Get(pawn).Context = pawnContext;
				context.AppendLine($"[P{i + 1}]").AppendLine(pawnContext);
			}
		}
		return context.ToString().TrimEnd();
	}

	public static string CreatePawnBackstory(Pawn pawn, InfoLevel infoLevel = InfoLevel.Normal)
	{
		StringBuilder sb = new StringBuilder();
		string name = pawn.LabelShort;
		string title = ((pawn.story?.title == null) ? "" : ("(" + pawn.story.title + ")"));
		string genderAndAge = Regex.Replace(pawn.MainDesc(writeFaction: false), "\\(\\d+\\)", "").Trim();
		sb.AppendLine(name + " " + title + " (" + genderAndAge + ")");
		string role = pawn.GetRole(includeFaction: true);
		if (role != null)
		{
			sb.AppendLine("Role: " + role);
		}
		AppendWithHook(sb, pawn, ContextCategories.Pawn.Race, ContextBuilder.GetRaceContext(pawn, infoLevel));
		if (infoLevel != InfoLevel.Short && !pawn.IsVisitor() && !pawn.IsEnemy())
		{
			AppendWithHook(sb, pawn, ContextCategories.Pawn.Genes, ContextBuilder.GetNotableGenesContext(pawn, infoLevel));
		}
		AppendWithHook(sb, pawn, ContextCategories.Pawn.Ideology, ContextBuilder.GetIdeologyContext(pawn, infoLevel));
		if ((pawn.IsEnemy() || pawn.IsVisitor()) && !pawn.IsQuestLodger())
		{
			return sb.ToString();
		}
		AppendWithHook(sb, pawn, ContextCategories.Pawn.Backstory, ContextBuilder.GetBackstoryContext(pawn, infoLevel));
		AppendWithHook(sb, pawn, ContextCategories.Pawn.Traits, ContextBuilder.GetTraitsContext(pawn, infoLevel));
		if (infoLevel != InfoLevel.Short)
		{
			AppendWithHook(sb, pawn, ContextCategories.Pawn.Skills, ContextBuilder.GetSkillsContext(pawn, infoLevel));
		}
		return sb.ToString();
	}

	public static string CreatePawnContext(Pawn pawn, InfoLevel infoLevel = InfoLevel.Normal)
	{
		StringBuilder sb = new StringBuilder();
		sb.Append(CreatePawnBackstory(pawn, infoLevel));
		AppendWithHook(sb, pawn, ContextCategories.Pawn.Health, ContextBuilder.GetHealthContext(pawn, infoLevel));
		string personality = Cache.Get(pawn).Personality;
		if (personality != null)
		{
			sb.AppendLine("Personality: " + personality);
		}
		if (pawn.IsEnemy())
		{
			return sb.ToString();
		}
		AppendWithHook(sb, pawn, ContextCategories.Pawn.Mood, ContextBuilder.GetMoodContext(pawn, infoLevel));
		AppendWithHook(sb, pawn, ContextCategories.Pawn.Thoughts, ContextBuilder.GetThoughtsContext(pawn, infoLevel));
		AppendWithHook(sb, pawn, ContextCategories.Pawn.CaptiveStatus, ContextBuilder.GetPrisonerSlaveContext(pawn, infoLevel));
		if (pawn.IsVisitor())
		{
			Lord lord = pawn.GetLord() ?? pawn.CurJob?.lord;
			if (lord?.LordJob != null)
			{
				string cleanName = lord.LordJob.GetType().Name.Replace("LordJob_", "");
				sb.AppendLine("Activity: " + cleanName);
			}
		}
		AppendWithHook(sb, pawn, ContextCategories.Pawn.Social, ContextBuilder.GetRelationsContext(pawn, infoLevel));
		if (infoLevel != InfoLevel.Short)
		{
			AppendWithHook(sb, pawn, ContextCategories.Pawn.Equipment, ContextBuilder.GetEquipmentContext(pawn, infoLevel));
		}
		return sb.ToString();
	}

	public static void DecoratePrompt(TalkRequest talkRequest, List<Pawn> pawns, string status)
	{
		ContextSettings contextSettings = Settings.Get().Context;
		StringBuilder sb = new StringBuilder();
		CommonUtil.InGameData gameData = CommonUtil.GetInGameData();
		Pawn mainPawn = pawns[0];
		string shortName = mainPawn.LabelShort ?? "";
		ContextBuilder.BuildDialogueType(sb, talkRequest, pawns, shortName, mainPawn);
		sb.Append("\n" + status);
		if (contextSettings.IncludeTime)
		{
			sb.Append("\nTime: " + ApplyEnvironmentWithHook(mainPawn.Map, ContextCategories.Environment.Time, gameData.Hour12HString));
		}
		if (contextSettings.IncludeDate)
		{
			sb.Append("\nToday: " + ApplyEnvironmentWithHook(mainPawn.Map, ContextCategories.Environment.Date, gameData.DateString));
		}
		if (contextSettings.IncludeSeason)
		{
			sb.Append("\nSeason: " + ApplyEnvironmentWithHook(mainPawn.Map, ContextCategories.Environment.Season, gameData.SeasonString));
		}
		if (contextSettings.IncludeWeather)
		{
			sb.Append("\nWeather: " + ApplyEnvironmentWithHook(mainPawn.Map, ContextCategories.Environment.Weather, gameData.WeatherString));
		}
		ContextBuilder.BuildLocationContext(sb, contextSettings, mainPawn);
		ContextBuilder.BuildEnvironmentContext(sb, contextSettings, mainPawn);
		if (contextSettings.IncludeWealth)
		{
			sb.Append("\nWealth: " + ApplyEnvironmentWithHook(mainPawn.Map, ContextCategories.Environment.Wealth, Describer.Wealth(mainPawn.Map.wealthWatcher.WealthTotal)));
		}
		if (AIService.IsFirstInstruction())
		{
			sb.Append("\nin " + Constant.Lang);
		}
		talkRequest.Prompt = sb.ToString();
	}

	private static void AppendIfNotEmpty(StringBuilder sb, string text)
	{
		if (!string.IsNullOrEmpty(text))
		{
			sb.AppendLine(text);
		}
	}

	private static void AppendWithHook(StringBuilder sb, Pawn pawn, ContextCategory category, string text)
	{
		if (ContextHookRegistry.HasAnyInjections)
		{
			foreach (var item in ContextHookRegistry.GetInjectedSectionsAt(category))
			{
				ContextHookRegistry.InjectPosition pos = item.Position;
				Delegate provider = item.Provider;
				if (pos == ContextHookRegistry.InjectPosition.Before && provider is Func<Pawn, string> p)
				{
					AppendIfNotEmpty(sb, p(pawn));
				}
			}
		}
		string hooked = ContextHookRegistry.ApplyPawnHooks(category, pawn, text ?? "");
		AppendIfNotEmpty(sb, hooked);
		if (!ContextHookRegistry.HasAnyInjections)
		{
			return;
		}
		foreach (var item2 in ContextHookRegistry.GetInjectedSectionsAt(category))
		{
			ContextHookRegistry.InjectPosition pos2 = item2.Position;
			Delegate provider2 = item2.Provider;
			if (pos2 == ContextHookRegistry.InjectPosition.After && provider2 is Func<Pawn, string> p2)
			{
				AppendIfNotEmpty(sb, p2(pawn));
			}
		}
	}

	private static string ApplyEnvironmentWithHook(Map map, ContextCategory category, string text)
	{
		StringBuilder sb = new StringBuilder();
		if (ContextHookRegistry.HasAnyInjections)
		{
			foreach (var item in ContextHookRegistry.GetInjectedSectionsAt(category))
			{
				ContextHookRegistry.InjectPosition pos = item.Position;
				Delegate provider = item.Provider;
				if (pos == ContextHookRegistry.InjectPosition.Before && provider is Func<Map, string> p)
				{
					AppendIfNotEmpty(sb, p(map));
				}
			}
		}
		string hooked = ContextHookRegistry.ApplyEnvironmentHooks(category, map, text ?? "");
		AppendIfNotEmpty(sb, hooked);
		if (ContextHookRegistry.HasAnyInjections)
		{
			foreach (var item2 in ContextHookRegistry.GetInjectedSectionsAt(category))
			{
				ContextHookRegistry.InjectPosition pos2 = item2.Position;
				Delegate provider2 = item2.Provider;
				if (pos2 == ContextHookRegistry.InjectPosition.After && provider2 is Func<Map, string> p2)
				{
					AppendIfNotEmpty(sb, p2(map));
				}
			}
		}
		return sb.ToString().TrimEnd();
	}
}
