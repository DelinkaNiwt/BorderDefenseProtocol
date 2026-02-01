using System;
using System.Text;
using Verse;

namespace CharacterEditor;

public static class CompatibilityTool
{
	public static string GetRJWTooltip(Pawn pawn)
	{
		string text = "";
		try
		{
			ThingComp rJWComp = pawn.GetRJWComp();
			Type aType = Reflect.GetAType("rjw", "Orientation");
			string name = Enum.GetName(aType, rJWComp.GetMemberValue<object>("orientation", null));
			text = "{PAWN}".Translate(pawn.Named("PAWN")).AdjustedFor(pawn).Resolve();
			text = text + " " + Label.IS + " " + name;
			StringBuilder memberValue = rJWComp.GetMemberValue<StringBuilder>("quirks", null);
			if (memberValue != null)
			{
				string memberValueAsString = rJWComp.GetMemberValueAsString<StringBuilder>("quirks", "");
				memberValueAsString = memberValueAsString.Replace("None, ", "").Replace("None", "");
				text = text + "\n" + memberValueAsString;
			}
		}
		catch
		{
		}
		return text;
	}

	public static void OpenRJWDialog(Pawn pawn)
	{
		Type aType = Reflect.GetAType("rjw", "Dialog_Sexcard");
		if (aType != null)
		{
			object obj = Activator.CreateInstance(aType, pawn);
			if (obj != null)
			{
				WindowTool.Open(obj as Window);
			}
		}
	}

	public static string GetPersonalitiesTooltip(Pawn pawn)
	{
		string result = "";
		try
		{
			ThingComp persoComp = pawn.GetPersoComp();
			result = (string)persoComp.CallMethod("GetDescription", null);
		}
		catch
		{
		}
		return result;
	}

	public static void OpenPersonalitiesDialog(Pawn pawn)
	{
		Type aType = Reflect.GetAType("SPM1.UI", "Dialog_PersonalityEditor");
		if (aType != null)
		{
			aType.CallMethod("OpenDialogFor", new object[1] { pawn });
		}
	}

	public static string GetPsyche(this Pawn pawn)
	{
		string text = "";
		if (pawn == null || !CEditor.IsPsychologyActive)
		{
			return text;
		}
		Type aType = Reflect.GetAType("Psychology", "PsycheCardUtility");
		if (aType != null)
		{
			text = (string)aType.CallMethod("GetPsychology", new object[1] { pawn });
		}
		if (Prefs.DevMode)
		{
			Log.Message("getting psychology of " + pawn.GetPawnName() + " =" + text);
		}
		return text.AsBase64(Encoding.UTF8);
	}

	public static void SetPsyche(this Pawn pawn, string data)
	{
		if (pawn == null || !CEditor.IsPsychologyActive)
		{
			return;
		}
		try
		{
			string text = data.Base64ToString(Encoding.UTF8);
			if (Prefs.DevMode)
			{
				Log.Message("setting psychology of " + pawn.GetPawnName() + " =" + text);
			}
			Type aType = Reflect.GetAType("Psychology", "PsycheCardUtility");
			if (aType != null)
			{
				aType.CallMethod("SetPsychology", new object[2] { pawn, text });
			}
		}
		catch (Exception e)
		{
			MessageTool.DebugException(e);
		}
	}

	public static string GetPersonality(this Pawn pawn)
	{
		string text = "";
		if (pawn == null || !CEditor.IsPersonalitiesActive)
		{
			return text;
		}
		try
		{
			Type aType = Reflect.GetAType("SPM1", "Extensions");
			if (aType != null)
			{
				text = (string)aType.CallMethod("ExtractPersonality", new object[1] { pawn });
			}
			if (Prefs.DevMode)
			{
				Log.Message("getting personality of " + pawn.GetPawnName() + " =" + text);
			}
			return text.AsBase64(Encoding.UTF8);
		}
		catch (Exception ex)
		{
			Log.Message("getting personality failed - this is not an issue of the editor! " + ex.Message + "\n" + ex.StackTrace);
		}
		return "";
	}

	public static void SetPersonality(this Pawn pawn, string data)
	{
		if (pawn == null || !CEditor.IsPersonalitiesActive)
		{
			return;
		}
		try
		{
			string text = data.Base64ToString(Encoding.UTF8);
			if (Prefs.DevMode)
			{
				Log.Message("setting personality of " + pawn.GetPawnName() + " =" + text);
			}
			Type aType = Reflect.GetAType("SPM1", "Extensions");
			if (aType != null)
			{
				aType.CallMethod("IntractPersonality", new object[2] { pawn, text });
			}
		}
		catch (Exception ex)
		{
			Log.Message("setting personality failed - this is not an issue of the editor! " + ex.Message + "\n" + ex.StackTrace);
		}
	}

	public static string GetFavoriteColorTooltip(Pawn pawn)
	{
		string result = "";
		try
		{
			string arg = string.Empty;
			if (pawn.Ideo != null && !pawn.Ideo.hidden)
			{
				arg = "OrIdeoColor".Translate(pawn.Named("PAWN"));
			}
			result = "FavoriteColorTooltip".Translate(pawn.Named("PAWN"), pawn.story.favoriteColor.label.Named("COLOR"), 0.6f.ToStringPercent().Named("PERCENTAGE"), arg.Named("ORIDEO")).Resolve();
		}
		catch
		{
		}
		return result;
	}

	public static void UpdateLifestage(Pawn p)
	{
		if (!CEditor.IsAgeMattersActive)
		{
			return;
		}
		HediffComp hediffComp = CompTool.GetHediffComp(p, CEditor.IsAgeMattersActive, "LifeStageHediffAssociation");
		if (hediffComp == null)
		{
			return;
		}
		try
		{
			hediffComp.CallMethod("UpdateHediffDependingOnLifeStage", null);
		}
		catch
		{
		}
	}
}
