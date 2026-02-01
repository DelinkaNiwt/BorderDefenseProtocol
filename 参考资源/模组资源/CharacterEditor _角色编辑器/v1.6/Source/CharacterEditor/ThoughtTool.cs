using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace CharacterEditor;

internal static class ThoughtTool
{
	internal const string CO_CACHEDTHOUGHTS = "cachedThoughts";

	internal const string CO_REASON = "reason";

	internal const string CO_CURSTAGEINDEX = "curStageIndex";

	internal static bool IsTrulySocial(this ThoughtDef t)
	{
		return t != null && (t.IsSocial || (t.IsTypeOf<Thought_Memory>() && t.HasLabelPlaceholder()));
	}

	internal static bool IsClassOf<T>(this ThoughtDef t)
	{
		return t.thoughtClass == typeof(T);
	}

	internal static bool IsTypeOf<T>(this ThoughtDef t)
	{
		return t != null && (t.thoughtClass == typeof(T) || t.ThoughtClass == typeof(T) || (t.thoughtClass != null && (t.thoughtClass.BaseType == typeof(T) || (t.thoughtClass.BaseType != null && (t.thoughtClass.BaseType.BaseType == typeof(T) || (t.thoughtClass.BaseType.BaseType != null && t.thoughtClass.BaseType.BaseType.BaseType == typeof(T)))))));
	}

	internal static bool IsForWeapon(this ThoughtDef t)
	{
		return t.HasLabelPlaceholder("{WEAPON}");
	}

	internal static bool IsForTitle(this ThoughtDef t)
	{
		return t.IsTypeOf<Thought_MemoryRoyalTitle>();
	}

	internal static bool HasLabelPlaceholder(this ThoughtDef t, string ph = "{0}")
	{
		return t != null && !t.stages.NullOrEmpty() && t.stages[0].SLabel().Contains(ph);
	}

	internal static bool HasMemories(this Pawn pawn)
	{
		return pawn != null && pawn.needs != null && pawn.needs.mood != null && pawn.needs.mood.thoughts != null && pawn.needs.mood.thoughts.memories != null && pawn.needs.mood.thoughts.memories.Memories != null;
	}

	internal static Gender GetOppositeGender(this Pawn p)
	{
		return (p == null) ? Gender.Female : ((p.gender != Gender.Male) ? Gender.Male : Gender.Female);
	}

	internal static List<Thought_Situational> GetAllThoughtSituationals(this Pawn p)
	{
		return p.needs.mood.thoughts.situational.GetMemberValue<List<Thought_Situational>>("cachedThoughts", null);
	}

	internal static int FixStageValue(ThoughtDef t, int stage)
	{
		return (stage >= 0 && !t.stages.NullOrEmpty()) ? ((t.stages.CountAllowNull() <= stage) ? t.stages.IndexOf(t.stages.Last()) : stage) : 0;
	}

	internal static string GetAsSeparatedString(this Thought t)
	{
		if (t == null || t.def.IsNullOrEmpty())
		{
			return "";
		}
		string text = "";
		text = text + t.def.defName + "|";
		string reason;
		Pawn otherPawn = t.GetOtherPawn(out reason);
		text = ((otherPawn != null || reason.NullOrEmpty()) ? (text + otherPawn.GetPawnNameAsSeparatedString() + "|") : (text + reason + "|"));
		text = text + t.CurStageIndex + "|";
		text = ((!t.def.IsTypeOf<Thought_MemoryRoyalTitle>()) ? (text + "|") : (text + ((Thought_MemoryRoyalTitle)t).titleDef.defName + "|"));
		text = ((!t.def.IsTypeOf<Thought_Memory>()) ? (text + "1|") : (text + ((Thought_Memory)t).moodPowerFactor + "|"));
		float opinionOffset = t.GetOpinionOffset();
		text = text + ((opinionOffset == float.MinValue) ? 0f : opinionOffset) + "|";
		return (!t.def.IsTypeOf<Thought_Memory>()) ? (text + "0") : (text + ((Thought_Memory)t).age);
	}

	internal static string GetAllMemoriesAsSeparatedString(this Pawn p)
	{
		if (!p.HasMemoryTracker() || p.needs.mood.thoughts.memories.Memories.NullOrEmpty())
		{
			return "";
		}
		string text = "";
		foreach (Thought_Memory memory in p.needs.mood.thoughts.memories.Memories)
		{
			text += memory.GetAsSeparatedString();
			text += ":";
		}
		return text.SubstringRemoveLast();
	}

	internal static string GetAllSituationalMemoriesAsSeparatedString(this Pawn p)
	{
		if (!p.HasSituationalTracker() || p.GetAllThoughtSituationals().NullOrEmpty())
		{
			return "";
		}
		string text = "";
		foreach (Thought_Situational allThoughtSituational in p.GetAllThoughtSituationals())
		{
			text += allThoughtSituational.GetAsSeparatedString();
			text += ":";
		}
		return text.SubstringRemoveLast();
	}

	internal static void SetMemories(this Pawn p, string s)
	{
		if (!p.HasMemoryTracker())
		{
			return;
		}
		try
		{
			p.needs.mood.thoughts.memories.Memories.Clear();
			if (s.NullOrEmpty())
			{
				return;
			}
			string[] array = s.SplitNo(":");
			string[] array2 = array;
			foreach (string s2 in array2)
			{
				string[] array3 = s2.SplitNo("|");
				if (array3.Length == 7)
				{
					ThoughtDef thoughtDef = DefTool.ThoughtDef(array3[0]);
					if (thoughtDef != null)
					{
						Pawn otherPawnFromSeparatedString = PawnxTool.GetOtherPawnFromSeparatedString(array3[1]);
						p.AddThought(thoughtDef, otherPawnFromSeparatedString, array3[2].AsInt32(), array3[3], array3[4].AsFloat(), array3[5].AsFloat(), array3[6].AsInt32());
					}
				}
			}
		}
		catch (Exception e)
		{
			MessageTool.DebugException(e);
		}
	}

	internal static void SetSituationalMemories(this Pawn p, string s)
	{
		if (!p.HasSituationalTracker())
		{
			return;
		}
		try
		{
			p.needs.mood.thoughts.situational.Notify_SituationalThoughtsDirty();
			if (s.NullOrEmpty())
			{
				return;
			}
			string[] array = s.SplitNo(":");
			string[] array2 = array;
			foreach (string s2 in array2)
			{
				string[] array3 = s2.SplitNo("|");
				if (array3.Length == 7)
				{
					ThoughtDef thoughtDef = DefTool.ThoughtDef(array3[0]);
					if (thoughtDef != null)
					{
						Pawn otherPawnFromSeparatedString = PawnxTool.GetOtherPawnFromSeparatedString(array3[1]);
						p.AddThought(thoughtDef, otherPawnFromSeparatedString, array3[2].AsInt32(), array3[3], array3[4].AsFloat(), array3[5].AsFloat(), array3[6].AsInt32());
					}
				}
			}
		}
		catch (Exception e)
		{
			MessageTool.DebugException(e);
		}
	}

	internal static bool HasOtherPawnMember(this ThoughtDef t)
	{
		return t != null && (t.IsSocial || t.stackLimitForSameOtherPawn > 0 || t.IsTypeOf<Thought_MemorySocialCumulative>() || t.IsTypeOf<Thought_MemorySocial>() || t.IsTypeOf<Thought_SituationalSocial>() || t.IsTypeOf<Thought_PsychicHarmonizer>() || (t.IsTypeOf<Thought_Memory>() && t.GetThoughtLabel().Contains("{0}")) || t.IsTypeOf<Thought_BondedAnimalMaster>() || t.IsTypeOf<Thought_NotBondedAnimalMaster>() || t.SDefname().Equals("Jealous"));
	}

	internal static Pawn GetOtherPawn(this Thought t, out string reason)
	{
		Pawn result = null;
		reason = "";
		if (t == null)
		{
			return result;
		}
		if (t.def.IsTypeOf<Thought_Memory>())
		{
			return ((Thought_Memory)t).otherPawn;
		}
		if (t.def.IsTypeOf<Thought_SituationalSocial>())
		{
			return ((Thought_SituationalSocial)t).otherPawn;
		}
		if (t.def.IsTypeOf<Thought_BondedAnimalMaster>() || t.def.IsTypeOf<Thought_NotBondedAnimalMaster>())
		{
			reason = ((Thought_Situational)t).GetMemberValue("reason", "");
			List<DirectPawnRelation> directRelations = t.pawn.relations.DirectRelations;
			for (int i = 0; i < directRelations.Count; i++)
			{
				DirectPawnRelation directPawnRelation = directRelations[i];
				result = directPawnRelation.otherPawn;
				if (directPawnRelation.def == PawnRelationDefOf.Bond && !result.Dead && result.Spawned && result.Faction == Faction.OfPlayer && result.GetPawnName() == reason)
				{
					return result;
				}
			}
			return null;
		}
		if (t.def.SDefname().Equals("Jealous"))
		{
			reason = ((Thought_Situational)t).GetMemberValue("reason", "");
			return null;
		}
		if (t.def.IsTypeOf<ThoughtWorker_WantToSleepWithSpouseOrLover>() || t.def.IsTypeOf<Thought_OpinionOfMyLover>())
		{
			DirectPawnRelation directPawnRelation2 = LovePartnerRelationUtility.ExistingMostLikedLovePartnerRel(t.pawn, allowDead: false);
			if (directPawnRelation2 != null)
			{
				reason = directPawnRelation2.def.GetGenderSpecificLabel(directPawnRelation2.otherPawn);
				return directPawnRelation2.otherPawn;
			}
		}
		return result;
	}

	internal static float GetOpinionOffset(this Thought t)
	{
		float result = float.MinValue;
		if (t == null)
		{
			return result;
		}
		return t.def.IsTypeOf<Thought_MemorySocial>() ? ((Thought_MemorySocial)t).opinionOffset : ((!t.def.IsTypeOf<Thought_SituationalSocial>()) ? float.MinValue : ((t.CurStageIndex < 0) ? 0f : ((Thought_SituationalSocial)t).OpinionOffset()));
	}

	internal static float TryGetMoodOffset(this Thought t)
	{
		float result = 0f;
		if (t != null)
		{
			try
			{
				result = t.MoodOffset();
			}
			catch
			{
			}
		}
		return result;
	}

	internal static string GetThoughtLabel(this ThoughtDef t, int stage = 0, Pawn p = null)
	{
		if (t == null)
		{
			return "";
		}
		string text = null;
		if (!t.stages.NullOrEmpty())
		{
			try
			{
				text = text ?? t.stages[stage].labelSocial;
			}
			catch
			{
			}
			try
			{
				text = text ?? t.stages[stage].label;
			}
			catch
			{
			}
			try
			{
				text = text ?? t.stages[stage].untranslatedLabelSocial;
			}
			catch
			{
			}
			try
			{
				text = text ?? t.stages[stage].untranslatedLabel;
			}
			catch
			{
			}
		}
		text = text ?? t.label;
		text = text ?? t.defName;
		text = text.CapitalizeFirst();
		if (text.StartsWith("{"))
		{
			if (t.defName.StartsWith("HumanLeatherApparel"))
			{
				text = string.Format(text, "Apparel");
			}
			else if (t.defName == "JealousRage" || t.defName == "OnKill_GoodThought" || t.defName == "RelicAtRitual" || t.defName == "IdeoBuildingMissing" || t.defName == "IdeoBuildingDisrespected" || t.defName == "RitualDelayed" || t.defName == "OnKill_BadThought" || t.defName == "ObservedTerror" || t.defName == "KillThirst" || t.defName.StartsWith("BondedThought"))
			{
				text = t.defName;
			}
			else if (p != null && t.Worker != null)
			{
				text = t.Worker.PostProcessLabel(p, text);
			}
		}
		return text;
	}

	internal static string GetThoughtDescription(this ThoughtDef t, int stage = 0, Thought thought = null)
	{
		if (t == null)
		{
			return "";
		}
		string text = null;
		if (!t.stages.NullOrEmpty())
		{
			try
			{
				text = t.stages[stage].description;
			}
			catch
			{
			}
		}
		text = text ?? t.description;
		text = text ?? "";
		try
		{
			if (thought != null)
			{
				if (t.IsClassOf<Thought_IdeoRoleEmpty>())
				{
					Thought_IdeoRoleEmpty thought_IdeoRoleEmpty = (Thought_IdeoRoleEmpty)thought;
					text = text.Replace("{0}", thought_IdeoRoleEmpty.Role.ideo.memberName).Replace("{ROLE_labelIndef}", thought_IdeoRoleEmpty.Role.LabelCap);
				}
				else if (t.IsClassOf<Thought_MemoryRoyalTitle>())
				{
					Thought_MemoryRoyalTitle thought_MemoryRoyalTitle = (Thought_MemoryRoyalTitle)thought;
					string newValue = ((CEditor.API.Pawn.gender == Gender.Female) ? thought_MemoryRoyalTitle.titleDef.labelFemale : thought_MemoryRoyalTitle.titleDef.label);
					text = text.Replace("{TITLE_label}", newValue);
				}
				else if (t.IsClassOf<Thought_IdeoRoleLost>())
				{
					Thought_IdeoRoleLost thought_IdeoRoleLost = (Thought_IdeoRoleLost)thought;
					text = text.Replace("{0}", thought_IdeoRoleLost.Role.ideo.memberName).Replace("{ROLE_labelIndef}", thought_IdeoRoleLost.Role.LabelCap);
				}
				else if (t.IsClassOf<Thought_IdeoRoleApparelRequirementNotMet>())
				{
					Thought_IdeoRoleApparelRequirementNotMet thought_IdeoRoleApparelRequirementNotMet = (Thought_IdeoRoleApparelRequirementNotMet)thought;
					text = text.Replace("{0}", thought_IdeoRoleApparelRequirementNotMet.Role.ideo.memberName).Replace("{ROLE_labelIndef}", thought_IdeoRoleApparelRequirementNotMet.Role.LabelCap);
				}
				else if (t.IsClassOf<Thought_RelicAtRitual>())
				{
					Thought_RelicAtRitual thought_RelicAtRitual = (Thought_RelicAtRitual)thought;
					text = text.Replace("{RELICNAME_labelIndef}", thought_RelicAtRitual.relicName);
				}
				else if (t.IsClassOf<Thought_Situational_WearingDesiredApparel>())
				{
					Thought_Situational_WearingDesiredApparel thought_Situational_WearingDesiredApparel = (Thought_Situational_WearingDesiredApparel)thought;
					Precept_Apparel precept_Apparel = (Precept_Apparel)thought_Situational_WearingDesiredApparel.sourcePrecept;
					text = text.Replace("{APPAREL_indefinite}", precept_Apparel.apparelDef.LabelCap);
				}
			}
		}
		catch
		{
		}
		text = text ?? "";
		return text.CapitalizeFirst();
	}

	internal static int CountOfDefs<T>(List<Thought> l, out Thought example, string startsWdefname = null)
	{
		int num = 0;
		example = null;
		foreach (Thought item in l)
		{
			if (item.def.IsClassOf<T>() && (startsWdefname == null || item.def.defName.StartsWith(startsWdefname)))
			{
				example = item;
				num++;
			}
		}
		return num;
	}

	internal static string GetThoughtLabel(this Thought t)
	{
		if (t == null)
		{
			return "";
		}
		int stage = FixStageValue(t.def, t.CurStageIndex);
		return t.def.GetThoughtLabel(stage);
	}

	internal static string GetThoughtDescription(this Thought t)
	{
		if (t == null)
		{
			return "";
		}
		int stage = FixStageValue(t.def, t.CurStageIndex);
		return t.def.GetThoughtDescription(stage, t);
	}

	internal static List<Thought> GetThoughtsSorted(this Pawn p)
	{
		List<Thought> list = new List<Thought>();
		if (p.HasMemoryTracker() && p.HasSituationalTracker())
		{
			List<Thought_Memory> memories = p.needs.mood.thoughts.memories.Memories;
			List<Thought_Situational> allThoughtSituationals = p.GetAllThoughtSituationals();
			List<Thought> list2 = new List<Thought>();
			list2.AddRange(memories);
			list2.AddRange(allThoughtSituationals);
			List<Thought> collection = (from td in list2
				where td.GetOpinionOffset() == float.MinValue
				orderby td.TryGetMoodOffset()
				select td).ToList();
			List<Thought> collection2 = (from td in list2
				where td.GetOpinionOffset() != float.MinValue
				orderby td.GetOpinionOffset()
				select td).ToList();
			list.AddRange(collection);
			list.Reverse();
			list.AddRange(collection2);
		}
		return list;
	}

	internal static void AddThought(this Pawn pawn, ThoughtDef t, Pawn otherPawn = null, int stage = 0, string optDefName = null, float moodPowerFactor = 1f, float opinionOffset = 1f, int age = 0)
	{
		if (pawn != null && t != null)
		{
			if (t.IsTypeOf<Thought_Memory>())
			{
				pawn.AddThought_Memory(t, otherPawn, stage, optDefName, moodPowerFactor, opinionOffset, age);
			}
			else if (t.IsTypeOf<Thought_Situational>())
			{
				AddThoughtSituational(pawn, t, otherPawn, stage, optDefName, moodPowerFactor, opinionOffset);
			}
		}
	}

	private static void AddThought_Memory(this Pawn p, ThoughtDef t, Pawn otherPawn, int stage, string optDefName, float moodPowerFactor, float opinionOffset, int age)
	{
		IndividualThoughtToAdd individualThoughtToAdd = new IndividualThoughtToAdd(t, p, otherPawn, moodPowerFactor, opinionOffset);
		individualThoughtToAdd.thought.SetForcedStage(stage);
		if (t.IsTypeOf<Thought_Memory>())
		{
			individualThoughtToAdd.thought.age = age;
			individualThoughtToAdd.thought.moodPowerFactor = moodPowerFactor;
		}
		if (t.IsTypeOf<Thought_MemorySocial>())
		{
			((Thought_MemorySocial)individualThoughtToAdd.thought).opinionOffset = opinionOffset;
		}
		if (t.IsTypeOf<Thought_MemoryRoyalTitle>() && !optDefName.NullOrEmpty())
		{
			((Thought_MemoryRoyalTitle)individualThoughtToAdd.thought).titleDef = DefTool.GetDef<RoyalTitleDef>(optDefName);
		}
		if (t.IsTypeOf<Thought_PsychicHarmonizer>() && p.HasHealthTracker())
		{
			List<BodyPartRecord> listOfAllowedBodyPartRecords = p.GetListOfAllowedBodyPartRecords(HediffDefOf.PsychicHarmonizer);
			p.AddHediff2(random: false, HediffDefOf.PsychicHarmonizer, 1f, listOfAllowedBodyPartRecords.FirstOrDefault());
			foreach (Hediff hediff in p.health.hediffSet.hediffs)
			{
				if (hediff.def == HediffDefOf.PsychicHarmonizer)
				{
					((Thought_PsychicHarmonizer)individualThoughtToAdd.thought).harmonizer = hediff;
					break;
				}
			}
		}
		if (t.IsTypeOf<Thought_WeaponTrait>() && p.HasEquipmentTracker())
		{
			List<ThingWithComps> allEquipmentListForReading = p.equipment.AllEquipmentListForReading;
			((Thought_WeaponTrait)individualThoughtToAdd.thought).weapon = allEquipmentListForReading.FirstOrDefault();
		}
		if (!t.IsTypeOf<Thought_MemorySocial>() || otherPawn != null)
		{
			individualThoughtToAdd.Add();
		}
	}

	private static void AddThoughtSituational(Pawn p, ThoughtDef t, Pawn otherPawn, int stage, string optDefName, float moodPowerFactor, float opinionOffset)
	{
		Thought thought = ThoughtMaker.MakeThought(t);
		thought.pawn = p;
		int num = FixStageValue(t, stage);
		thought.SetMemberValue("curStageIndex", num);
		if (otherPawn != null)
		{
			if (t.IsTypeOf<Thought_SituationalSocial>())
			{
				((Thought_SituationalSocial)thought).otherPawn = otherPawn;
			}
			if (t.IsTypeOf<Thought_BondedAnimalMaster>() || t.IsTypeOf<Thought_NotBondedAnimalMaster>() || t.SDefname().Equals("Jealous"))
			{
				((Thought_Situational)thought).SetMemberValue("reason", otherPawn.LabelShort);
			}
		}
		try
		{
			List<Thought_Situational> memberValue = p.needs.mood.thoughts.situational.GetMemberValue<List<Thought_Situational>>("cachedThoughts", null);
			memberValue.CallMethod("Add", new object[1] { thought });
			p.needs.mood.thoughts.situational.SetMemberValue("cachedThoughts", memberValue);
		}
		catch (Exception ex)
		{
			RemoveThoughtSituational(p, thought);
			Log.Error(ex.Message + "\n" + ex.StackTrace);
			MessageTool.Show("failed to add thought. reason = this thought or thought stage cause an exception. action was rolled back.");
		}
	}

	internal static void ClearAllThoughts(this Pawn p)
	{
		if (p.HasMemoryTracker())
		{
			p.needs.mood.thoughts.memories.Memories.Clear();
		}
		if (p.HasSituationalTracker())
		{
			p.needs.mood.thoughts.situational.Notify_SituationalThoughtsDirty();
		}
	}

	internal static void RemoveThought(this Pawn p, Thought t)
	{
		if (p != null && t != null)
		{
			if (t.def.IsTypeOf<Thought_Memory>())
			{
				RemoveThoughtMemory(p, t);
			}
			else if (t.def.IsTypeOf<Thought_Situational>())
			{
				RemoveThoughtSituational(p, t);
			}
		}
	}

	private static void RemoveThoughtMemory(Pawn p, Thought t)
	{
		if (!p.HasMemoryTracker() || t == null)
		{
			return;
		}
		try
		{
			Thought_Memory th = t as Thought_Memory;
			p.needs.mood.thoughts.memories.RemoveMemory(th);
		}
		catch
		{
		}
	}

	private static void RemoveThoughtSituational(Pawn p, Thought t)
	{
		if (!p.HasSituationalTracker() || t == null)
		{
			return;
		}
		try
		{
			Thought_Situational item = t as Thought_Situational;
			List<Thought_Situational> memberValue = p.needs.mood.thoughts.situational.GetMemberValue<List<Thought_Situational>>("cachedThoughts", null);
			if (memberValue != null && memberValue.Contains(item))
			{
				memberValue.Remove(item);
				p.needs.mood.thoughts.situational.SetMemberValue("cachedThoughts", memberValue);
			}
		}
		catch
		{
		}
	}
}
