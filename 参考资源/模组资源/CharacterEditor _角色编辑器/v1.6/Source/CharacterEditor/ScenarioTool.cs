using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace CharacterEditor;

internal static class ScenarioTool
{
	internal const string CO_AGE = "age";

	internal const string CO_GENDER = "gender";

	internal const string CO_PAWNNAME = "pawnName";

	internal const string CO_LOCATION = "location";

	internal const string CO_ANIMALKIND = "animalKind";

	internal const string CO_COUNT = "count";

	internal const string CO_PARTS = "parts";

	internal const string CO_STUFF = "stuff";

	internal const string CO_THINGDEF = "thingDef";

	internal const string CO_STYLEDEF = "styleDef";

	internal const string CO_QUALITY = "quality";

	internal static int CurrentTakenPawnCount => CEditor.InStartingScreen ? Find.GameInitData.startingPawnCount : CurrentPawnList.Count;

	internal static List<Pawn> CurrentPawnList => CEditor.InStartingScreen ? Find.GameInitData.startingAndOptionalPawns : PawnxTool.GetPawnList(Label.COLONISTS, onMap: true, Faction.OfPlayer);

	internal static List<ScenPart> ScenarioParts => Find.Scenario.GetMemberValue<List<ScenPart>>("parts", null);

	internal static void LoadCapsuleSetup(string filepath)
	{
		if (!FileIO.Exists(filepath))
		{
			return;
		}
		byte[] input = FileIO.ReadFile(filepath);
		string text = input.AsString(Encoding.UTF8);
		if (text.NullOrEmpty())
		{
			return;
		}
		string[] array = text.SplitNo("\n");
		if (array.NullOrEmpty())
		{
			MessageTool.Show("can't load scenario file. reason-> no data in file=" + filepath);
			return;
		}
		try
		{
			PawnxTool.DeleteAllPawns(Label.COLONISTS, onMap: true, Faction.OfPlayer);
		}
		catch
		{
		}
		Dictionary<Pawn, string> dictionary = new Dictionary<Pawn, string>();
		int pawnCount = 0;
		for (int i = 0; i < array.Length; i++)
		{
			try
			{
				switch (i)
				{
				case 0:
					SetScenarioParameterFromSeparatedString(array[i], out pawnCount);
					continue;
				case 1:
					SetScenaioPartsFromSeparatedString(array[i]);
					continue;
				}
				PresetPawn presetPawn = new PresetPawn();
				Pawn key = presetPawn.LoadPawn(-1, choosePlace: false, array[i]);
				dictionary.Add(key, presetPawn.dicParams.GetValue(PresetPawn.Param.P33_relations));
			}
			catch
			{
				MessageTool.Show("error while parsing scenario file. reason -> wrong or invalid data format");
			}
		}
		if (CEditor.InStartingScreen)
		{
			Find.GameInitData.startingPawnCount = pawnCount;
		}
		foreach (Pawn key2 in dictionary.Keys)
		{
			key2.SetRelationsFromSeparatedString(dictionary[key2]);
		}
	}

	internal static void SaveCapsuleSetup(string filepath)
	{
		if (filepath.NullOrEmpty())
		{
			return;
		}
		string allScenarioParameter = GetAllScenarioParameter();
		string allScenarioPartsAsSeparatedString = GetAllScenarioPartsAsSeparatedString();
		string text = "";
		string text2 = "";
		List<Pawn> currentPawnList = CurrentPawnList;
		foreach (Pawn item in currentPawnList)
		{
			PresetPawn presetPawn = new PresetPawn();
			string text3 = presetPawn.SavePawn(item, -1);
			text2 = text2 + item.GetPawnName() + ",";
			text = text + "\n" + text3;
		}
		text2 = text2.SubstringRemoveLast();
		allScenarioParameter += text2;
		string text4 = allScenarioParameter + allScenarioPartsAsSeparatedString + text;
		FileIO.WriteFile(filepath, text4.AsBytes(Encoding.UTF8));
	}

	internal static int GetTypeReplacer<T>(T sd) where T : ScenPart
	{
		if (sd.GetType() == typeof(ScenPart_StartingThing_Defined))
		{
			return 1;
		}
		if (sd.GetType() == typeof(ScenPart_ScatterThingsAnywhere))
		{
			return 2;
		}
		if (sd.GetType() == typeof(ScenPart_ScatterThingsNearPlayerStart))
		{
			return 3;
		}
		if (sd.GetType() == typeof(ScenPart_StartingAnimal))
		{
			return 4;
		}
		if (sd.GetType() == typeof(ScenPart_StartingThingStyle_Defined))
		{
			return 5;
		}
		if (sd.GetType() == typeof(ScenPart_ScatterThingsStyleAnywhere))
		{
			return 6;
		}
		if (sd.GetType() == typeof(ScenPart_StartingAnimalExtra))
		{
			return 7;
		}
		if (sd.GetType() == typeof(ScenPart_ScatterThingsNearPlayerExtra))
		{
			return 8;
		}
		return 0;
	}

	internal static string GetScenarioPartString<T>(T sd) where T : ScenPart
	{
		string text = "";
		Selected selectedScenarioPart = sd.GetSelectedScenarioPart();
		if (sd.IsScenarioAnimal())
		{
			text = text + GetTypeReplacer(sd) + "|";
			text += ((selectedScenarioPart != null && selectedScenarioPart.pkd != null && !selectedScenarioPart.pkd.defName.NullOrEmpty()) ? (selectedScenarioPart.pkd.defName + "|") : "|");
			text += ((selectedScenarioPart != null) ? (selectedScenarioPart.stackVal + "|") : "|");
			string text2 = text;
			object obj;
			if (selectedScenarioPart == null)
			{
				obj = "|";
			}
			else
			{
				int gender = (int)selectedScenarioPart.gender;
				obj = gender + "|";
			}
			text = text2 + (string)obj;
			text += ((selectedScenarioPart != null) ? (selectedScenarioPart.age + "|") : "|");
			return text + ((selectedScenarioPart != null && selectedScenarioPart.pawnName != null) ? (selectedScenarioPart.pawnName as NameSingle).Name : "");
		}
		text = text + GetTypeReplacer(sd) + "|";
		text += ((selectedScenarioPart != null && selectedScenarioPart.thingDef != null) ? (selectedScenarioPart.thingDef.SDefname() + "|") : "|");
		text += ((selectedScenarioPart != null && selectedScenarioPart.stuff != null) ? (selectedScenarioPart.stuff.SDefname() + "|") : "|");
		text += ((selectedScenarioPart != null && selectedScenarioPart.style != null) ? (selectedScenarioPart.style.SDefname() + "|") : "|");
		text += "|";
		text += ((selectedScenarioPart != null) ? (selectedScenarioPart.quality + "|") : "|");
		return text + ((selectedScenarioPart != null) ? selectedScenarioPart.stackVal.ToString() : "");
	}

	internal static string GetAllScenarioParameter()
	{
		string text = "";
		text = text + CurrentPawnList.FirstOrFallback().Faction.Name + "|";
		return text + (CEditor.InStartingScreen ? Find.GameInitData.startingPawnCount : CurrentPawnList.Count) + "|";
	}

	internal static string GetAllScenarioPartsAsSeparatedString()
	{
		List<ScenPart> scenarioParts = ScenarioParts;
		if (scenarioParts.NullOrEmpty())
		{
			return "";
		}
		string text = "\n";
		foreach (ScenPart item in scenarioParts)
		{
			text += GetScenarioPartString(item);
			text += ":";
		}
		return text.SubstringRemoveLast();
	}

	internal static void SetScenarioParameterFromSeparatedString(string s, out int pawnCount)
	{
		pawnCount = 0;
		if (!s.NullOrEmpty())
		{
			string[] array = s.SplitNo("|");
			Faction.OfPlayer.Name = array.GetStringValue(0);
			pawnCount = array.GetStringValue(1).AsInt32();
		}
	}

	internal static void SetScenaioPartsFromSeparatedString(string s)
	{
		if (s.NullOrEmpty())
		{
			return;
		}
		RemoveAllSupportedScenarioPartsFromList();
		List<ScenPart> scenarioParts = ScenarioParts;
		string[] array = s.SplitNo(":");
		string[] array2 = array;
		foreach (string text in array2)
		{
			if (text.NullOrEmpty())
			{
				continue;
			}
			string[] array3 = text.SplitNo("|");
			string stringValue = array3.GetStringValue(0);
			bool flag = stringValue == "4" || stringValue == "7";
			bool flag2 = stringValue == "1" || stringValue == "3" || stringValue == "5";
			bool flag3 = stringValue == "2" || stringValue == "6";
			bool flag4 = stringValue == "3" || stringValue == "8";
			if (flag)
			{
				Selected selected = Selected.ByThingDef(null);
				selected.pkd = DefTool.PawnKindDef(array3.GetStringValue(1));
				selected.stackVal = array3.GetStringValue(2).AsInt32();
				Enum.TryParse<Gender>(array3.GetStringValue(3), out var result);
				selected.gender = result;
				selected.age = array3.GetStringValue(4).AsInt32();
				selected.pawnName = new NameSingle(array3.GetStringValue(5));
				AddScenePart(CreateScenePart_Animal(selected));
			}
			else
			{
				Selected s2 = Selected.ByName(array3.GetStringValue(1), array3.GetStringValue(2), array3.GetStringValue(3), array3.GetStringValue(4).HexStringToColor(), array3.GetStringValue(5).AsInt32(), array3.GetStringValue(6).AsInt32());
				if (flag2)
				{
					AddScenePart(CreateScenePart_Defined(s2));
				}
				else if (flag3)
				{
					AddScenePart(CreateScenePart_Anywhere(s2));
				}
				else if (flag4)
				{
					AddScenePart(CreateScenePart_NearPlayer(s2));
				}
			}
		}
	}

	internal static void RemoveAllSupportedScenarioPartsFromList()
	{
		List<ScenPart> scenarioParts = ScenarioParts;
		if (scenarioParts.NullOrEmpty())
		{
			return;
		}
		for (int num = scenarioParts.Count - 1; num >= 0; num--)
		{
			if (scenarioParts[num].IsSupportedScenarioPart())
			{
				scenarioParts.Remove(scenarioParts[num]);
			}
		}
	}

	internal static Selected GetSelectedScenarioPart<T>(this T part) where T : ScenPart
	{
		Selected selected = Selected.ByThingDef(null);
		if (!part.IsSupportedScenarioPart())
		{
			return selected;
		}
		if (part.IsScenarioAnimal())
		{
			selected.age = part.GetMemberValue("age", 1);
			selected.pkd = part.GetMemberValue<PawnKindDef>("animalKind", null);
			selected.stackVal = part.GetMemberValue("count", 1);
			selected.gender = part.GetMemberValue("gender", Gender.None);
			selected.pawnName = part.GetMemberValue<Name>("pawnName", null);
			selected.location = part.GetMemberValue("location", default(IntVec3));
		}
		else
		{
			selected.thingDef = part.GetMemberValue<ThingDef>("thingDef", null);
			selected.stuff = part.GetMemberValue<ThingDef>("stuff", null);
			selected.style = part.GetMemberValue<ThingStyleDef>("styleDef", null);
			QualityCategory? memberValue = ((object)part).GetMemberValue("quality", (QualityCategory?)QualityCategory.Normal);
			selected.quality = (int)(memberValue.HasValue ? memberValue.Value : QualityCategory.Awful);
			selected.stackVal = part.GetMemberValue("count", 1);
			selected.location = part.GetMemberValue("location", default(IntVec3));
		}
		return selected;
	}

	internal static void SetScenarioParts(List<ScenPart> l)
	{
		Find.Scenario.SetMemberValue("parts", l);
	}

	internal static int GetScenarioPartCount<T>(this T part) where T : ScenPart
	{
		return part.GetMemberValue("count", 0);
	}

	internal static void SetScenarioPartCount<T>(this T part, int count) where T : ScenPart
	{
		if (part.IsSupportedScenarioPart())
		{
			part.SetMemberValue("count", count);
		}
	}

	internal static ScenPart_StartingThingStyle_Defined CreateScenePart_Defined(Selected s)
	{
		ScenPart_StartingThingStyle_Defined scenPart_StartingThingStyle_Defined = new ScenPart_StartingThingStyle_Defined();
		ScenPartDef def = (from xx in DefDatabase<ScenPartDef>.AllDefs
			where xx.defName == "StartingThing_Defined"
			orderby xx.defName
			select xx).First();
		scenPart_StartingThingStyle_Defined.def = def;
		scenPart_StartingThingStyle_Defined.SetMemberValue("thingDef", s.thingDef);
		scenPart_StartingThingStyle_Defined.SetMemberValue("stuff", s.stuff);
		scenPart_StartingThingStyle_Defined.SetMemberValue("styleDef", s.style);
		scenPart_StartingThingStyle_Defined.SetMemberValue("quality", (QualityCategory)s.quality);
		scenPart_StartingThingStyle_Defined.SetMemberValue("count", s.stackVal);
		scenPart_StartingThingStyle_Defined.location = s.location;
		return scenPart_StartingThingStyle_Defined;
	}

	internal static ScenPart_ScatterThingsStyleAnywhere CreateScenePart_Anywhere(Selected s)
	{
		ScenPart_ScatterThingsStyleAnywhere scenPart_ScatterThingsStyleAnywhere = new ScenPart_ScatterThingsStyleAnywhere();
		ScenPartDef def = (from xx in DefDatabase<ScenPartDef>.AllDefs
			where xx.defName == "ScatterThingsAnywhere"
			orderby xx.defName
			select xx).First();
		scenPart_ScatterThingsStyleAnywhere.def = def;
		scenPart_ScatterThingsStyleAnywhere.SetMemberValue("thingDef", s.thingDef);
		scenPart_ScatterThingsStyleAnywhere.SetMemberValue("stuff", s.stuff);
		scenPart_ScatterThingsStyleAnywhere.SetMemberValue("styleDef", s.style);
		scenPart_ScatterThingsStyleAnywhere.SetMemberValue("quality", (QualityCategory)s.quality);
		scenPart_ScatterThingsStyleAnywhere.SetMemberValue("count", s.stackVal);
		scenPart_ScatterThingsStyleAnywhere.location = s.location;
		return scenPart_ScatterThingsStyleAnywhere;
	}

	internal static ScenPart_ScatterThingsNearPlayerExtra CreateScenePart_NearPlayer(Selected s)
	{
		ScenPart_ScatterThingsNearPlayerExtra scenPart_ScatterThingsNearPlayerExtra = new ScenPart_ScatterThingsNearPlayerExtra();
		ScenPartDef def = (from xx in DefDatabase<ScenPartDef>.AllDefs
			where xx.defName == "ScatterThingsNearPlayerStart"
			orderby xx.defName
			select xx).First();
		scenPart_ScatterThingsNearPlayerExtra.def = def;
		scenPart_ScatterThingsNearPlayerExtra.SetMemberValue("thingDef", s.thingDef);
		scenPart_ScatterThingsNearPlayerExtra.SetMemberValue("stuff", s.stuff);
		scenPart_ScatterThingsNearPlayerExtra.SetMemberValue("styleDef", s.style);
		scenPart_ScatterThingsNearPlayerExtra.SetMemberValue("quality", (QualityCategory)s.quality);
		scenPart_ScatterThingsNearPlayerExtra.SetMemberValue("count", s.stackVal);
		scenPart_ScatterThingsNearPlayerExtra.location = s.location;
		return scenPart_ScatterThingsNearPlayerExtra;
	}

	internal static ScenPart_StartingAnimalExtra CreateScenePart_Animal(Selected s)
	{
		ScenPart_StartingAnimalExtra scenPart_StartingAnimalExtra = new ScenPart_StartingAnimalExtra();
		ScenPartDef def = (from xx in DefDatabase<ScenPartDef>.AllDefs
			where xx.defName == "StartingAnimal"
			orderby xx.defName
			select xx).First();
		scenPart_StartingAnimalExtra.def = def;
		scenPart_StartingAnimalExtra.SetMemberValue("animalKind", s.pkd);
		scenPart_StartingAnimalExtra.SetMemberValue("count", s.stackVal);
		scenPart_StartingAnimalExtra.SetMemberValue("gender", s.gender);
		scenPart_StartingAnimalExtra.SetMemberValue("age", s.age);
		scenPart_StartingAnimalExtra.SetMemberValue("pawnName", s.pawnName);
		scenPart_StartingAnimalExtra.location = s.location;
		return scenPart_StartingAnimalExtra;
	}

	internal static void AddScenePart<T>(T scenPart) where T : ScenPart
	{
		if (!scenPart.IsSupportedScenarioPart())
		{
			return;
		}
		List<ScenPart> list = ScenarioParts;
		if (list == null)
		{
			list = new List<ScenPart>();
		}
		try
		{
			list.Add(scenPart);
			SetScenarioParts(list);
		}
		catch
		{
			MessageTool.Show("couldn't add scenario parts", MessageTypeDefOf.RejectInput);
		}
	}

	internal static void MergeNonScenarioPart(Selected s, ref List<ScenPart> l, bool doMerge = true)
	{
		if (s.stackVal <= 0)
		{
			return;
		}
		bool flag = false;
		if (doMerge)
		{
			foreach (ScenPart item in l)
			{
				if (s.pkd != null)
				{
					PawnKindDef memberValue = item.GetMemberValue<PawnKindDef>("animalKind", null);
					if (memberValue == null || !(memberValue.defName == s.pkd.defName))
					{
						continue;
					}
					int memberValue2 = item.GetMemberValue("age", 0);
					if (memberValue2 == s.age)
					{
						Gender memberValue3 = item.GetMemberValue("gender", Gender.None);
						if (memberValue3 == s.gender)
						{
							int scenarioPartCount = item.GetScenarioPartCount();
							scenarioPartCount += s.stackVal;
							item.SetScenarioPartCount(scenarioPartCount);
							flag = true;
							break;
						}
					}
				}
				else
				{
					ThingDef memberValue4 = item.GetMemberValue<ThingDef>("thingDef", null);
					if (memberValue4 != null && !s.HasQuality && memberValue4.defName == s.thingDef.defName)
					{
						int scenarioPartCount2 = item.GetScenarioPartCount();
						scenarioPartCount2 += s.stackVal;
						item.SetScenarioPartCount(scenarioPartCount2);
						flag = true;
						break;
					}
				}
			}
		}
		if (!flag)
		{
			if (s.pkd != null)
			{
				l.Add(CreateScenePart_Animal(s));
			}
			else
			{
				l.Add(CreateScenePart_Defined(s));
			}
		}
	}

	internal static void AddScenarioPartMerged(Selected s, bool addToTaken)
	{
		if (s.stackVal <= 0)
		{
			return;
		}
		bool flag = false;
		foreach (ScenPart scenarioPart in ScenarioParts)
		{
			if (s.pkd != null)
			{
				PawnKindDef memberValue = scenarioPart.GetMemberValue<PawnKindDef>("animalKind", null);
				if (memberValue == null || !(memberValue.defName == s.pkd.defName))
				{
					continue;
				}
				int memberValue2 = scenarioPart.GetMemberValue("age", 0);
				if (memberValue2 == s.age)
				{
					Gender memberValue3 = scenarioPart.GetMemberValue("gender", Gender.None);
					if (memberValue3 == s.gender)
					{
						int scenarioPartCount = scenarioPart.GetScenarioPartCount();
						scenarioPartCount += s.stackVal;
						scenarioPart.SetScenarioPartCount(scenarioPartCount);
						flag = true;
						break;
					}
				}
			}
			else
			{
				ThingDef memberValue4 = scenarioPart.GetMemberValue<ThingDef>("thingDef", null);
				if (memberValue4 != null && !s.HasQuality && memberValue4.defName == s.thingDef.defName && ((addToTaken && scenarioPart.IsScenarioDefined()) || (addToTaken && scenarioPart.IsScenarioNearPlayer()) || (!addToTaken && scenarioPart.IsScatterAnywherePart())))
				{
					int scenarioPartCount2 = scenarioPart.GetScenarioPartCount();
					scenarioPartCount2 += s.stackVal;
					scenarioPart.SetScenarioPartCount(scenarioPartCount2);
					flag = true;
					break;
				}
			}
		}
		if (!flag)
		{
			if (s.pkd != null)
			{
				AddScenarioPartAnimal(s);
			}
			else if (addToTaken)
			{
				AddScenarioPartTaken(s);
			}
			else
			{
				AddScenarioPartMap(s);
			}
		}
	}

	private static void AddScenarioPartTaken(Selected s)
	{
		AddScenePart(CreateScenePart_Defined(s));
	}

	private static void AddScenarioPartMap(Selected s)
	{
		AddScenePart(CreateScenePart_Anywhere(s));
	}

	private static void AddScenarioPartTakenNear(Selected s)
	{
		AddScenePart(CreateScenePart_NearPlayer(s));
	}

	internal static void AddScenarioPartAnimal(Selected s)
	{
		AddScenePart(CreateScenePart_Animal(s));
	}

	internal static bool IsScenarioAnimal(this ScenPart part)
	{
		return part != null && (part.GetType() == typeof(ScenPart_StartingAnimal) || part.GetType() == typeof(ScenPart_StartingAnimalExtra));
	}

	internal static bool IsScatterAnywherePart(this ScenPart part)
	{
		return part != null && (part.GetType() == typeof(ScenPart_ScatterThingsAnywhere) || part.GetType() == typeof(ScenPart_ScatterThingsStyleAnywhere));
	}

	internal static bool IsScenarioDefined(this ScenPart part)
	{
		return part != null && (part.GetType() == typeof(ScenPart_StartingThing_Defined) || part.GetType() == typeof(ScenPart_StartingThingStyle_Defined));
	}

	internal static bool IsScenarioNearPlayer(this ScenPart part)
	{
		return part != null && (part.GetType() == typeof(ScenPart_ScatterThingsNearPlayerStart) || part.GetType() == typeof(ScenPart_ScatterThingsNearPlayerExtra));
	}

	internal static bool IsSupportedScenarioPart(this ScenPart part)
	{
		if (part == null)
		{
			return false;
		}
		return part.GetType() == typeof(ScenPart_StartingAnimal) || part.GetType() == typeof(ScenPart_StartingAnimalExtra) || part.GetType() == typeof(ScenPart_StartingThing_Defined) || part.GetType() == typeof(ScenPart_StartingThingStyle_Defined) || part.GetType() == typeof(ScenPart_ScatterThingsStyleAnywhere) || part.GetType() == typeof(ScenPart_ScatterThingsAnywhere) || part.GetType() == typeof(ScenPart_ScatterThingsNearPlayerStart) || part.GetType() == typeof(ScenPart_ScatterThingsNearPlayerExtra);
	}
}
