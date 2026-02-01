using System;
using System.Reflection;
using HarmonyLib;
using RimTalk.Data;
using RimTalk.Util;
using Verse;

namespace RimTalk.Compatibility;

[StaticConstructorOnStartup]
public static class CharacterEditorCompatibilityPatch
{
	private const string PersonaMarker = "RIMTALK_PERSONA:";

	private static volatile bool _patched;

	static CharacterEditorCompatibilityPatch()
	{
		Harmony harmony = new Harmony("cj.rimtalk.compat.charactereditor");
		TryPatch(harmony);
	}

	public static void TryPatch(Harmony harmony)
	{
		if (_patched)
		{
			return;
		}
		try
		{
			Type healthToolType = AccessTools.TypeByName("CharacterEditor.HealthTool");
			if (!(healthToolType == null))
			{
				MethodInfo getAllHediffsMethod = AccessTools.Method(healthToolType, "GetAllHediffsAsSeparatedString");
				MethodInfo setHediffsMethod = AccessTools.Method(healthToolType, "SetHediffsFromSeparatedString");
				if (getAllHediffsMethod == null || setHediffsMethod == null)
				{
					Logger.Warning("Character Editor found but methods missing, compatibility patch skipped");
					return;
				}
				harmony.Patch(getAllHediffsMethod, null, new HarmonyMethod(typeof(CharacterEditorCompatibilityPatch), "AppendPersonaData"));
				harmony.Patch(setHediffsMethod, null, new HarmonyMethod(typeof(CharacterEditorCompatibilityPatch), "RestorePersonaData"));
				_patched = true;
				Logger.Message("Character Editor compatibility enabled");
			}
		}
		catch (Exception ex)
		{
			Logger.Warning("Failed to apply Character Editor compatibility patch: " + ex.Message);
		}
	}

	public static void AppendPersonaData(Pawn p, ref string __result)
	{
		if (p == null)
		{
			return;
		}
		try
		{
			string personality = PersonaService.GetPersonality(p);
			if (!string.IsNullOrEmpty(personality))
			{
				float chattiness = PersonaService.GetTalkInitiationWeight(p);
				string encoded = Uri.EscapeDataString(personality);
				string personaData = "RIMTALK_PERSONA:" + encoded + "|" + chattiness;
				__result = (string.IsNullOrEmpty(__result) ? personaData : (__result + ":" + personaData));
			}
		}
		catch (Exception ex)
		{
			Logger.Warning("Failed to export persona for " + p.LabelShort + ": " + ex.Message);
		}
	}

	public static void RestorePersonaData(Pawn p, string s)
	{
		if (p == null || string.IsNullOrEmpty(s))
		{
			return;
		}
		try
		{
			int markerIndex = s.IndexOf("RIMTALK_PERSONA:", StringComparison.Ordinal);
			if (markerIndex < 0)
			{
				return;
			}
			int dataStart = markerIndex + "RIMTALK_PERSONA:".Length;
			int entryEnd = s.IndexOf(':', dataStart);
			if (entryEnd < 0)
			{
				entryEnd = s.Length;
			}
			string dataSection = s.Substring(dataStart, entryEnd - dataStart);
			int lastPipe = dataSection.LastIndexOf('|');
			if (lastPipe >= 0)
			{
				string encodedPersonality = dataSection.Substring(0, lastPipe);
				string chattinessStr = dataSection.Substring(lastPipe + 1);
				string personality = Uri.UnescapeDataString(encodedPersonality);
				if (!string.IsNullOrEmpty(personality) && float.TryParse(chattinessStr, out var chattiness))
				{
					PersonaService.SetPersonality(p, personality);
					PersonaService.SetTalkInitiationWeight(p, chattiness);
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Warning("Failed to restore persona for " + p.LabelShort + ": " + ex.Message);
		}
	}
}
