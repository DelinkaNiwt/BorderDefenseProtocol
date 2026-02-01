using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace CharacterEditor;

internal static class HealthTool
{
	internal static bool bIsOverridden;

	internal const string HB_All = "All";

	internal const string HB_AllImplants = "AllImplants";

	internal const string HB_AllAddictions = "AllAddictions";

	internal const string HB_AllDiseases = "AllDiseases";

	internal const string HB_AllInjuries = "AllInjuries";

	internal const string HB_AllTime = "AllTime";

	internal const string HB_Arm = "Arm";

	internal const string HB_Brain = "Brain";

	internal const string HB_Clavicle = "Clavicle";

	internal const string HB_Ear = "Ear";

	internal const string HB_Eye = "Eye";

	internal const string HB_Femur = "Femur";

	internal const string HB_Finger = "Finger";

	internal const string HB_Foot = "Foot";

	internal const string HB_Hand = "Hand";

	internal const string HB_Head = "Head";

	internal const string HB_Heart = "Heart";

	internal const string HB_Humerus = "Humerus";

	internal const string HB_Jaw = "Jaw";

	internal const string HB_Kidney = "Kidney";

	internal const string HB_Leg = "Leg";

	internal const string HB_Liver = "Liver";

	internal const string HB_Lung = "Lung";

	internal const string HB_Neck = "Neck";

	internal const string HB_Nose = "Nose";

	internal const string HB_Pelvis = "Pelvis";

	internal const string HB_Radius = "Radius";

	internal const string HB_Shoulder = "Shoulder";

	internal const string HB_Skull = "Skull";

	internal const string HB_Spine = "Spine";

	internal const string HB_Sternum = "Sternum";

	internal const string HB_Stomach = "Stomach";

	internal const string HB_Tibia = "Tibia";

	internal const string HB_Toe = "Toe";

	internal const string HB_Torso = "Torso";

	internal const string HB_UtilitySlot = "UtilitySlot";

	internal const string HB_Utility = "Utility";

	internal const string HB_WholeBody = "WholeBody";

	internal const string H_Abasia = "Abasia";

	internal const string H_AestheticShaper = "AestheticShaper";

	internal const string H_AlcoholTolerance = "AlcoholTolerance";

	internal const string H_Alzheimers = "Alzheimers";

	internal const string H_Anesthetic = "Anesthetic";

	internal const string H_Asthma = "Asthma";

	internal const string H_BadBack = "BadBack";

	internal const string H_Blindness = "Blindness";

	internal const string H_BloodLoss = "BloodLoss";

	internal const string H_Carcinoma = "Carcinoma";

	internal const string H_Cataract = "Cataract";

	internal const string H_CatatonicBreakdown = "CatatonicBreakdown";

	internal const string H_ChemicalDamageModerate = "ChemicalDamageModerate";

	internal const string H_ChemicalDamageSevere = "ChemicalDamageSevere";

	internal const string H_Circadian = "Circadian";

	internal const string H_Cirrhosis = "Cirrhosis";

	internal const string H_Coagulator = "Coagulator";

	internal const string H_Cochlear = "Cochlear";

	internal const string H_CryptosleepSickness = "CryptosleepSickness";

	internal const string H_Dementia = "Dementia";

	internal const string H_Denture = "Denture";

	internal const string H_DrugOverdose = "DrugOverdose";

	internal const string H_ElbowBlade = "ElbowBlade";

	internal const string H_Fangs = "Fangs";

	internal const string H_FibrousMechanites = "FibrousMechanites";

	internal const string H_FieldHand = "FieldHand";

	internal const string H_Flu = "Flu";

	internal const string H_FoodPoisoning = "FoodPoisoning";

	internal const string H_Force = "_Force";

	internal const string H_Frail = "Frail";

	internal const string H_GastroAnalyzer = "GastroAnalyzer";

	internal const string H_GutWorms = "GutWorms";

	internal const string H_HandTalon = "HandTalon";

	internal const string H_Hangover = "Hangover";

	internal const string H_TortureCrown = "TortureCrown";

	internal const string H_BlindFold = "Blindfold";

	internal const string H_HealingEnhancer = "HealingEnhancer";

	internal const string H_Hear = "Hearing";

	internal const string H_HearingLoss = "HearingLoss";

	internal const string H_High = "High";

	internal const string H_HungerMaker = "HungerMaker";

	internal const string H_Immunoenhancer = "Immunoenhancer";

	internal const string H_Joyfuzz = "Joyfuzz";

	internal const string H_Joywire = "Joywire";

	internal const string H_KneeSpike = "KneeSpike";

	internal const string H_LearningAssistant = "LearningAssistant";

	internal const string H_LoveEnhancer = "LoveEnhancer";

	internal const string H_Malaria = "Malaria";

	internal const string H_Malnutrition = "Malnutrition";

	internal const string H_Mindscrew = "Mindscrew";

	internal const string H_Command = "Command";

	internal const string H_MuscleParasites = "MuscleParasites";

	internal const string H_Neurocalcularor = "Neurocalculator";

	internal const string H_NoPain = "NoPain";

	internal const string H_Painstopper = "Painstopper";

	internal const string H_Plague = "Plague";

	internal const string H_PowerClaw = "PowerClaw";

	internal const string H_Pregnant = "Pregnant";

	internal const string H_PregnantHuman = "PregnantHuman";

	internal const string H_Psychic = "Psychic";

	internal const string H_PsychicShock = "PsychicShock";

	internal const string H_PsychicEntropy = "PsychicEntropy";

	internal const string H_NeuralHealRecoveryGain = "NeuralHealRecoveryGain";

	internal const string H_WorkDrive = "WorkDrive";

	internal const string H_PreachHealth = "PreachHealth";

	internal const string H_BerserkTrance = "BerserkTrance";

	internal const string H_GlucosoidRush = "GlucosoidRush";

	internal const string H_ImmunityDrive = "ImmunityDrive";

	internal const string H_WorkFocus = "WorkFocus";

	internal const string H_NeuralSupercharge = "NeuralSupercharge";

	internal const string H_ResurrectionPsychosis = "ResurrectionPsychosis";

	internal const string H_ResurrectionSickness = "ResurrectionSickness";

	internal const string H_SensoryMechanites = "SensoryMechanites";

	internal const string H_Skingland = "skinGland";

	internal const string H_SleepingSickness = "SleepingSickness";

	internal const string H_Smelling = "Smelling";

	internal const string H_SpeedBoost = "SpeedBoost";

	internal const string H_tolerance = "tolerance";

	internal const string H_ToxicBuildup = "ToxicBuildup";

	internal const string H_TraumaSavant = "TraumaSavant";

	internal const string H_VenomTalon = "VenomTalon";

	internal const string H_WoodenFoot = "WoodenFoot";

	internal const string H_WoundInfection = "WoundInfection";

	internal static Dictionary<string, Func<HediffDef, bool>> AllBodyPartChecks
	{
		get
		{
			Dictionary<string, Func<HediffDef, bool>> dictionary = new Dictionary<string, Func<HediffDef, bool>>();
			dictionary.Add("All", IsForAllParts);
			dictionary.Add("AllImplants", IsForAllParts);
			dictionary.Add("AllAddictions", IsForAllParts);
			dictionary.Add("AllDiseases", IsForAllParts);
			dictionary.Add("AllInjuries", IsForAllParts);
			dictionary.Add("AllTime", IsForAllParts);
			dictionary.Add("Arm", IsForArm);
			dictionary.Add("Brain", IsForBrain);
			dictionary.Add("Clavicle", IsForClavicle);
			dictionary.Add("Ear", IsForEar);
			dictionary.Add("Eye", IsForEye);
			dictionary.Add("Femur", IsForFemur);
			dictionary.Add("Finger", IsForFinger);
			dictionary.Add("Foot", IsForFoot);
			dictionary.Add("Hand", IsForHand);
			dictionary.Add("Head", IsForHead);
			dictionary.Add("Heart", IsForHeart);
			dictionary.Add("Humerus", IsForHumerus);
			dictionary.Add("Jaw", IsForJaw);
			dictionary.Add("Kidney", IsForKidney);
			dictionary.Add("Leg", IsForLeg);
			dictionary.Add("Liver", IsForLiver);
			dictionary.Add("Lung", IsForLung);
			dictionary.Add("Nose", IsForNose);
			dictionary.Add("Pelvis", IsForPelvis);
			dictionary.Add("Radius", IsForRadius);
			dictionary.Add("Shoulder", IsForShoulder);
			dictionary.Add("Skull", IsForSkull);
			dictionary.Add("Spine", IsForSpine);
			dictionary.Add("Sternum", IsForSternum);
			dictionary.Add("Stomach", IsForStomach);
			dictionary.Add("Tibia", IsForTibia);
			dictionary.Add("Toe", IsForToe);
			dictionary.Add("Torso", IsForTorso);
			dictionary.Add("UtilitySlot", IsForUtilitySlot);
			dictionary.Add("WholeBody", IsForWholeBody);
			return dictionary;
		}
	}

	internal static string GetHediffAsSeparatedString(this Hediff h)
	{
		if (h == null || h.def.IsNullOrEmpty())
		{
			return "";
		}
		string text = "";
		text = text + h.def.defName + "|";
		text = text + h.Severity + "|";
		text = text + ((h.Part != null) ? h.Part.def.defName : "") + "|";
		text = text + ((h.Part != null) ? h.Part.Index.ToString() : "") + "|";
		text = text + (h.IsPermanent() ? "1" : "0") + "|";
		text = text + h.GetLevel() + "|";
		text = text + h.GetPainValue() + "|";
		text = text + h.GetDuration() + "|";
		return text + h.GetOtherPawn().GetPawnNameAsSeparatedString();
	}

	internal static string GetAllHediffsAsSeparatedString(this Pawn p)
	{
		if (!p.HasHealthTracker() || p.health.hediffSet.hediffs.NullOrEmpty())
		{
			return "";
		}
		string text = "";
		foreach (Hediff hediff in p.health.hediffSet.hediffs)
		{
			text += hediff.GetHediffAsSeparatedString();
			text += ":";
		}
		return text.SubstringRemoveLast();
	}

	internal static void SetHediffsFromSeparatedString(this Pawn p, string s)
	{
		if (!p.HasHealthTracker() || s.NullOrEmpty())
		{
			return;
		}
		try
		{
			string[] array = s.Split(new string[1] { ":" }, StringSplitOptions.None);
			string[] array2 = array;
			foreach (string text in array2)
			{
				string[] array3 = text.Split(new string[1] { "|" }, StringSplitOptions.None);
				if (array3.Length >= 5)
				{
					int level = ((array3.Length > 5) ? array3[5].AsInt32() : (-1));
					int pain = ((array3.Length > 6) ? array3[6].AsInt32() : (-1));
					int duration = ((array3.Length > 7) ? array3[7].AsInt32() : (-1));
					string otherPawnName = ((array3.Length > 8) ? array3[8] : "");
					p.AddHediffByName(array3[0], array3[1].AsFloat(), array3[2], array3[3].AsInt32(), array3[4].AsBool(), level, pain, duration, otherPawnName);
				}
			}
		}
		catch (Exception e)
		{
			MessageTool.DebugException(e);
		}
	}

	internal static string GetFullLabel(this Hediff h)
	{
		if (h == null)
		{
			return "";
		}
		try
		{
			string label = h.Label;
			label = label ?? "";
			string text = ((h.Part == null) ? "" : ((h.Part.Label == null) ? "" : (h.Part.Label + " ")));
			text = text ?? "";
			return text + label;
		}
		catch
		{
			return h.def.label;
		}
	}

	internal static bool IsAdjustableSeverity(HediffDef hediff)
	{
		bool result = false;
		if (hediff != null)
		{
			result = (hediff.injuryProps != null && hediff.hediffClass != typeof(Hediff_MissingPart)) || hediff.IsAddiction || hediff.maxSeverity > 1f || ((hediff.addedPartProps == null && !(hediff.hediffClass == typeof(Hediff_MissingPart)) && !hediff.countsAsAddedPartOrImplant) ? true : false);
			if (hediff.hediffClass == typeof(Hediff_Psylink))
			{
				result = false;
			}
		}
		return result;
	}

	internal static float GetMaxSeverity(HediffDef hediff)
	{
		float num = 0f;
		if (hediff == null)
		{
			return 0f;
		}
		try
		{
			num = ((hediff.lethalSeverity >= 0f) ? hediff.lethalSeverity : hediff.maxSeverity);
			if (num > 99f)
			{
				num = ((hediff.stages.CountAllowNull() <= 0) ? 99f : ((hediff.IsHediffWithEffect() || hediff.IsHediffPsylink()) ? 99f : 1f));
			}
		}
		catch
		{
		}
		return num;
	}

	internal static bool NeedBodyPart(HediffDef hediff)
	{
		if (hediff == null)
		{
			return false;
		}
		if (hediff.defName.Contains("_Force") && hediff.hediffClass == typeof(HediffWithComps))
		{
			return false;
		}
		if (hediff.hediffClass == typeof(Hediff_AddedPart) || hediff.hediffClass == typeof(Hediff_Injury) || hediff.hediffClass == typeof(HediffWithComps) || hediff.hediffClass == typeof(Hediff_MissingPart) || hediff.hediffClass == typeof(Hediff_Implant))
		{
			return true;
		}
		return false;
	}

	internal static BodyPartRecord GetBodyPart(Pawn pawn, Hediff h)
	{
		if (pawn != null && h != null && h.Part != null && h.Part.def != null && pawn.RaceProps.body.AllParts != null)
		{
			try
			{
				foreach (BodyPartRecord allPart in pawn.RaceProps.body.AllParts)
				{
					if (allPart.def.defName == h.Part.def.defName && allPart.Index == h.Part.Index)
					{
						return allPart;
					}
				}
			}
			catch (Exception ex)
			{
				Log.Error("GetBodyPart: " + ex.Message);
			}
		}
		return null;
	}

	internal static BodyPartRecord GetBodyPartByDefName(Pawn pawn, string defName, int index)
	{
		if (pawn != null && !string.IsNullOrEmpty(defName))
		{
			foreach (BodyPartRecord allPart in pawn.RaceProps.body.AllParts)
			{
				if (allPart.def.defName == defName && allPart.Index == index)
				{
					return allPart;
				}
			}
		}
		return null;
	}

	internal static bool IsFor(HediffDef h, string bodyparttype)
	{
		return AllBodyPartChecks[bodyparttype](h);
	}

	internal static bool IsForAllParts(this HediffDef h)
	{
		return h.hediffClass == typeof(Hediff_MissingPart) || h.defName == "ChemicalDamageModerate" || h.defName == "ChemicalDamageSevere" || h.defName == "MuscleParasites" || h.defName == "FibrousMechanites" || h.defName == "SensoryMechanites" || h.defName == "Carcinoma" || h.defName == "WoundInfection";
	}

	internal static bool IsForArm(this HediffDef h)
	{
		return h.defName.Contains("Arm") || h.defName == "PowerClaw" || h.defName == "ElbowBlade";
	}

	internal static bool IsForBrain(this HediffDef h)
	{
		return h.defName.Contains("Brain") || (h.defName.StartsWith("Psychic") && h.defName != "PsychicEntropy") || (h.HasComp(typeof(HediffComp_Disappears)) && h.defName.Contains("_Psychic")) || h.defName.StartsWith("Circadian") || h.defName == "Dementia" || h.defName == "Alzheimers" || h.defName == "ResurrectionPsychosis" || h.defName == "TraumaSavant" || h.defName == "Joywire" || h.defName == "Painstopper" || h.defName == "Neurocalculator" || h.defName == "LearningAssistant" || h.defName == "Mindscrew" || h.defName == "Joyfuzz" || h.defName == "Abasia" || h.defName == "NoPain" || h.defName == "HungerMaker" || h.defName == "SpeedBoost" || h.defName.EndsWith("Command") || h.IsHediffPsylink() || h.HasComp(typeof(HediffComp_EntropyLink)) || h.HasComp(typeof(HediffComp_Link));
	}

	internal static bool IsForClavicle(this HediffDef h)
	{
		return h.defName.Contains("Clavicle");
	}

	internal static bool IsForEar(this HediffDef h)
	{
		return h.defName.Contains("Ear") || h.defName.Contains("Hearing") || h.defName.Contains("Cochlear") || h.defName == "HearingLoss";
	}

	internal static bool IsForEye(this HediffDef h)
	{
		return h.defName.Contains("Eye") || h.defName == "Cataract" || h.defName == "Blindness";
	}

	internal static bool IsForFemur(this HediffDef h)
	{
		return h.defName.Contains("Femur");
	}

	internal static bool IsForFinger(this HediffDef h)
	{
		return h.defName.Contains("Finger") || h.defName == "VenomTalon";
	}

	internal static bool IsForFoot(this HediffDef h)
	{
		return h.defName.Contains("Foot");
	}

	internal static bool IsForHand(this HediffDef h)
	{
		return h.defName.Contains("Hand");
	}

	internal static bool IsForHead(this HediffDef h)
	{
		return h.defName.Contains("Head") || h.defName == "Hangover" || h.defName == "TortureCrown" || h.defName == "Blindfold";
	}

	internal static bool IsForHeart(this HediffDef h)
	{
		return h.defName.Contains("Heart");
	}

	internal static bool IsForHumerus(this HediffDef h)
	{
		return h.defName.Contains("Humerus");
	}

	internal static bool IsForJaw(this HediffDef h)
	{
		return h.defName.Contains("Jaw") || h.defName.StartsWith("Denture") || h.defName.Contains("Fangs");
	}

	internal static bool IsForKidney(this HediffDef h)
	{
		return h.defName.Contains("Kidney") || h.defName == "Immunoenhancer";
	}

	internal static bool IsForLeg(this HediffDef h)
	{
		return h.defName.Contains("Leg") || h.defName == "KneeSpike";
	}

	internal static bool IsForLiver(this HediffDef h)
	{
		return h.defName.Contains("Liver") || h.defName == "Cirrhosis" || h.defName == "AlcoholTolerance";
	}

	internal static bool IsForLung(this HediffDef h)
	{
		return h.defName.Contains("Lung") || h.defName == "Asthma";
	}

	internal static bool IsForNose(this HediffDef h)
	{
		return h.defName.Contains("Nose") || h.defName.Contains("Smelling") || h.defName == "GastroAnalyzer";
	}

	internal static bool IsForPelvis(this HediffDef h)
	{
		return h.defName.Contains("Pelvis");
	}

	internal static bool IsForRadius(this HediffDef h)
	{
		return h.defName.Contains("Radius");
	}

	internal static bool IsForShoulder(this HediffDef h)
	{
		return h.defName.Contains("Shoulder");
	}

	internal static bool IsForSkull(this HediffDef h)
	{
		return h.defName.Contains("Skull");
	}

	internal static bool IsForSpine(this HediffDef h)
	{
		return h.defName.Contains("Spine") || h.defName == "BadBack";
	}

	internal static bool IsForSternum(this HediffDef h)
	{
		return h.defName.Contains("Sternum");
	}

	internal static bool IsForStomach(this HediffDef h)
	{
		return h.defName.Contains("Stomach") || h.defName == "GutWorms";
	}

	internal static bool IsForTibia(this HediffDef h)
	{
		return h.defName.Contains("Tibia");
	}

	internal static bool IsForToe(this HediffDef h)
	{
		return h.defName.Contains("Toe");
	}

	internal static bool IsForTorso(this HediffDef h)
	{
		return h.defName.Contains("Torso") || h.defName.EndsWith("skinGland") || h.defName == "Coagulator" || h.defName == "HealingEnhancer" || h.defName == "AestheticShaper" || h.defName == "LoveEnhancer";
	}

	internal static bool IsForUtilitySlot(this HediffDef h)
	{
		return h.defName.Contains("Utility");
	}

	internal static bool IsForWholeBody(this HediffDef h)
	{
		return h.IsAddiction || h.defName.ToLower().Contains("tolerance") || h.defName.EndsWith("High") || h.defName == "PsychicEntropy" || h.defName == "PsychicShock" || h.defName == "NeuralHealRecoveryGain" || h.defName == "NeuralSupercharge" || h.defName == "WorkDrive" || h.defName == "ImmunityDrive" || h.defName == "WorkFocus" || h.defName == "PreachHealth" || h.defName == "BerserkTrance" || h.defName == "GlucosoidRush" || h.defName == "CatatonicBreakdown" || h.defName == "BloodLoss" || h.defName.EndsWith("Flu") || h.defName.EndsWith("Plague") || h.defName == "Malaria" || h.defName == "SleepingSickness" || h.defName == "Anesthetic" || h.defName == "Frail" || h.defName == "CryptosleepSickness" || h.defName == "FoodPoisoning" || h.defName == "ToxicBuildup" || h.defName == "Pregnant" || h.defName == "PregnantHuman" || h.defName == "DrugOverdose" || h.defName == "ResurrectionSickness" || h.defName == "Malnutrition";
	}

	internal static void AddHediffByName(this Pawn p, string defName, float severity, string bodyPartDefName, int bodyPartIndex, bool permanent, int level, int pain, int duration, string otherPawnName)
	{
		HediffDef hediff = DefTool.HediffDef(defName);
		Pawn otherPawnFromSeparatedString = PawnxTool.GetOtherPawnFromSeparatedString(otherPawnName);
		BodyPartRecord bodyPartByDefName = GetBodyPartByDefName(p, bodyPartDefName, bodyPartIndex);
		p.AddHediff2(random: false, hediff, severity, bodyPartByDefName, permanent, level, pain, duration, otherPawnFromSeparatedString);
	}

	internal static void AddHediff2(this Pawn pawn, bool random, HediffDef hediff = null, float severity = 1f, BodyPartRecord bpr = null, bool isPermanent = false, int level = -1, int pain = -1, int duration = -1, Pawn otherPawn = null)
	{
		if (pawn == null)
		{
			return;
		}
		if (random)
		{
			if (hediff == null)
			{
				hediff = DefDatabase<HediffDef>.AllDefs.Where((HediffDef td) => td.defName != null).ToList().RandomElement();
			}
			if (IsAdjustableSeverity(hediff))
			{
				float maxSeverity = GetMaxSeverity(hediff);
				int maxValue = (int)(maxSeverity * 100f);
				int minValue = (int)(hediff.minSeverity * 100f);
				severity = (float)CEditor.zufallswert.Next(minValue, maxValue) / 100f;
				if (severity > maxSeverity)
				{
					severity = maxSeverity;
				}
			}
			if (hediff.injuryProps != null)
			{
				isPermanent = CEditor.zufallswert.Next(0, 5) == 0;
			}
			if (hediff.IsHediffWithLevel())
			{
				level = CEditor.zufallswert.Next((int)hediff.minSeverity, (int)hediff.maxSeverity);
			}
			if (hediff.IsHediffWithComps())
			{
				pain = (int)ConvertSliderToPainCategory(CEditor.zufallswert.Next(0, 3));
				duration = CEditor.zufallswert.Next(0, 220000);
			}
			if (hediff.IsHediffWithOtherPawn())
			{
				otherPawn = Find.WorldPawns.AllPawnsAlive.RandomElement();
			}
			List<BodyPartRecord> listOfAllowedBodyPartRecords = pawn.GetListOfAllowedBodyPartRecords(hediff);
			if (!listOfAllowedBodyPartRecords.NullOrEmpty())
			{
				bpr = listOfAllowedBodyPartRecords.RandomElement();
			}
		}
		if (hediff == null)
		{
			return;
		}
		try
		{
			if (bpr != null || hediff.IsHediffWithParents())
			{
				if (!pawn.health.hediffSet.PartIsMissing(bpr) || hediff.IsHediffWithParents())
				{
					Hediff hediff2 = HediffMaker.MakeHediff(hediff, pawn, bpr);
					hediff2.Severity = severity;
					hediff2.SetLevel(level);
					hediff2.SetPermanent(isPermanent);
					hediff2.SetPainValue(pain);
					hediff2.SetDuration(duration);
					hediff2.SetOtherPawn(otherPawn);
					if (hediff2.GetType() == typeof(Hediff_Psylink))
					{
						pawn.CheckAddPsylink((int)Math.Round(severity));
					}
					else
					{
						pawn.health.AddHediff(hediff2, bpr);
						pawn.health.Notify_HediffChanged(hediff2);
						if (hediff2.def.IsHediffPsylink())
						{
							pawn.psychicEntropy?.Notify_GainedPsylink();
							pawn.psychicEntropy?.PsychicEntropyTrackerTickInterval(0);
						}
					}
				}
			}
			else
			{
				List<BodyPartRecord> listOfAllowedBodyPartRecords2 = pawn.GetListOfAllowedBodyPartRecords(hediff);
				if (hediff.IsForWholeBody() || listOfAllowedBodyPartRecords2.CountAllowNull() > 1)
				{
					HealthUtility.AdjustSeverity(pawn, hediff, severity);
				}
				Hediff hediff3 = pawn.TryGetHediffByDefName(hediff.defName);
				if (hediff3 != null)
				{
					hediff3.Severity = severity;
					hediff3.SetLevel(level);
					hediff3.SetPermanent(isPermanent);
					hediff3.SetPainValue(pain);
					hediff3.SetDuration(duration);
					hediff3.SetOtherPawn(otherPawn);
				}
			}
			pawn.health.summaryHealth.Notify_HealthChanged();
		}
		catch
		{
		}
		pawn.needs?.AddOrRemoveNeedsAsAppropriate();
		CEditor.API.UpdateGraphics();
	}

	internal static Hediff TryGetHediffByDefName(this Pawn p, string defName)
	{
		Hediff result = null;
		if (p.HasHealthTracker())
		{
			foreach (Hediff hediff in p.health.hediffSet.hediffs)
			{
				if (hediff.def.defName == defName)
				{
					result = hediff;
					break;
				}
			}
		}
		return result;
	}

	internal static int GetLevel(this Hediff h)
	{
		int result = -1;
		if (h != null && h.def.IsHediffWithLevel())
		{
			result = ((Hediff_Level)h).level;
		}
		return result;
	}

	internal static void SetLevel(this Hediff h, int val)
	{
		if (h != null && val >= 0 && h.def.IsHediffWithLevel())
		{
			((Hediff_Level)h).level = val;
			h.Severity = val;
		}
	}

	internal static void SetPermanent(this Hediff h, bool permanent)
	{
		if (h != null && h.def.injuryProps != null)
		{
			HediffComp_GetsPermanent hediffComp_GetsPermanent = h.TryGetComp<HediffComp_GetsPermanent>();
			if (hediffComp_GetsPermanent != null)
			{
				hediffComp_GetsPermanent.IsPermanent = permanent;
			}
		}
	}

	internal static PainCategory ConvertSliderToPainCategory(int val)
	{
		if (val <= 0)
		{
			return PainCategory.Painless;
		}
		return val switch
		{
			1 => PainCategory.LowPain, 
			2 => PainCategory.MediumPain, 
			3 => PainCategory.HighPain, 
			_ => PainCategory.HighPain, 
		};
	}

	internal static int ConvertPainCategoryToSliderVal(PainCategory val)
	{
		return val switch
		{
			PainCategory.Painless => 0, 
			PainCategory.LowPain => 1, 
			PainCategory.MediumPain => 2, 
			PainCategory.HighPain => 3, 
			_ => 3, 
		};
	}

	internal static int GetPainValue(this Hediff h)
	{
		int result = -1;
		if (h != null && h.def.injuryProps != null)
		{
			HediffComp_GetsPermanent hediffComp_GetsPermanent = h.TryGetComp<HediffComp_GetsPermanent>();
			if (hediffComp_GetsPermanent != null)
			{
				result = (int)hediffComp_GetsPermanent.PainCategory;
			}
		}
		return result;
	}

	internal static void SetPainValue(this Hediff h, int val)
	{
		if (h != null && val >= 0 && h.def.injuryProps != null)
		{
			h.TryGetComp<HediffComp_GetsPermanent>()?.SetPainCategory((PainCategory)val);
		}
	}

	internal static int GetDuration(this Hediff h)
	{
		int result = -1;
		if (h != null)
		{
			HediffComp_Disappears hediffComp_Disappears = h.TryGetComp<HediffComp_Disappears>();
			if (hediffComp_Disappears != null)
			{
				result = hediffComp_Disappears.ticksToDisappear;
			}
		}
		return result;
	}

	internal static void SetDuration(this Hediff h, int val)
	{
		if (h != null && val >= 0)
		{
			HediffComp_Disappears hediffComp_Disappears = h.TryGetComp<HediffComp_Disappears>();
			if (hediffComp_Disappears != null)
			{
				hediffComp_Disappears.ticksToDisappear = val;
				hediffComp_Disappears.Props.showRemainingTime = true;
			}
		}
	}

	internal static Pawn GetOtherPawn(this Hediff h)
	{
		Pawn result = null;
		if (h != null)
		{
			if (h.def.IsHediffWithTarget())
			{
				HediffWithTarget hediffWithTarget = h as HediffWithTarget;
				if (hediffWithTarget.target is Pawn)
				{
					result = (Pawn)hediffWithTarget.target;
				}
			}
			else if (h.def.IsHediffLink())
			{
				HediffComp_Link hediffComp_Link = h.TryGetComp<HediffComp_Link>();
				if (hediffComp_Link != null)
				{
					result = hediffComp_Link.OtherPawn;
				}
			}
		}
		return result;
	}

	internal static void SetOtherPawn(this Hediff h, Pawn p)
	{
		if (h == null || p == null)
		{
			return;
		}
		if (h.def.IsHediffWithTarget())
		{
			HediffWithTarget hediffWithTarget = h as HediffWithTarget;
			hediffWithTarget.target = p;
		}
		if (h.def.IsHediffWithParents())
		{
			HediffWithParents hediffWithParents = h as HediffWithParents;
			hediffWithParents.SetParents(h.pawn, p, PregnancyUtility.GetInheritedGeneSet(p, h.pawn));
		}
		else if (h.def.IsHediffLink())
		{
			HediffComp_Link hediffComp_Link = h.TryGetComp<HediffComp_Link>();
			if (hediffComp_Link != null)
			{
				hediffComp_Link.other = p;
			}
		}
	}

	internal static bool IsHediffWithOtherPawn(this HediffDef h)
	{
		return h != null && (h.IsHediffWithTarget() || h.IsHediffLink() || h.IsHediffWithParents());
	}

	internal static bool IsHediffLink(this HediffDef h)
	{
		return h != null && (h.HasComp(typeof(HediffComp_Link)) || h.HasComp(typeof(HediffComp_EntropyLink)));
	}

	internal static bool IsHediffWithTarget(this HediffDef h)
	{
		return h != null && h.hediffClass != null && (h.hediffClass == typeof(HediffWithTarget) || h.hediffClass.BaseType == typeof(HediffWithTarget));
	}

	internal static bool IsHediffWithParents(this HediffDef h)
	{
		return h != null && h.hediffClass != null && (h.hediffClass == typeof(HediffWithParents) || h.hediffClass.BaseType == typeof(HediffWithParents));
	}

	internal static bool IsHediffWithComps(this HediffDef h)
	{
		return h != null && h.hediffClass != null && (h.hediffClass == typeof(HediffWithComps) || h.hediffClass.BaseType == typeof(HediffWithComps));
	}

	internal static bool IsHediffWithLevel(this HediffDef h)
	{
		return h != null && h.hediffClass != null && (h.hediffClass == typeof(Hediff_Level) || h.hediffClass.BaseType == typeof(Hediff_Level) || (h.hediffClass.BaseType != null && h.hediffClass.BaseType.BaseType == typeof(Hediff_Level)));
	}

	internal static bool IsHediffPsylink(this HediffDef h)
	{
		return h != null && h.hediffClass != null && (h.hediffClass == typeof(Hediff_Psylink) || h.hediffClass.ToString().EndsWith("Hediff_PsycastAbilities"));
	}

	internal static bool IsHediffPsycastAbilities(this HediffDef h)
	{
		return h != null && h.hediffClass != null && h.hediffClass.ToString().EndsWith("Hediff_PsycastAbilities");
	}

	internal static bool IsHediffWithEffect(this HediffDef h)
	{
		return h != null && h.hediffClass != null && (h.hediffClass == typeof(Hediff_DeathrestEffect) || h.hediffClass.BaseType == typeof(Hediff_DeathrestEffect));
	}

	internal static bool IsImplant(this HediffDef h)
	{
		return h != null && h.hediffClass != null && (h.hediffClass == typeof(Hediff_Implant) || h.hediffClass.BaseType == typeof(Hediff_Implant));
	}

	internal static bool IsAddedPart(this HediffDef h)
	{
		return h != null && h.hediffClass != null && (h.hediffClass == typeof(Hediff_AddedPart) || h.hediffClass.BaseType == typeof(Hediff_AddedPart));
	}

	internal static bool IsHigh(this HediffDef h)
	{
		return h != null && h.hediffClass != null && (h.hediffClass == typeof(Hediff_High) || h.hediffClass.BaseType == typeof(Hediff_High));
	}

	internal static void RemoveHediff(this Pawn pawn, Hediff hediff)
	{
		if (pawn != null && hediff != null)
		{
			pawn.health.hediffSet.hediffs.Remove(hediff);
			pawn.health.summaryHealth.Notify_HealthChanged();
			pawn.health.Notify_HediffChanged(hediff);
			pawn.needs.AddOrRemoveNeedsAsAppropriate();
			StatsReportUtility.Reset();
		}
	}

	internal static void ResurrectAndHeal(this Pawn pawn)
	{
		if (pawn.Dead)
		{
			ResurrectionUtility.TryResurrect(pawn);
		}
		try
		{
			List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
			Dictionary<Hediff, bool> dictionary = new Dictionary<Hediff, bool>();
			using (List<Hediff>.Enumerator enumerator = hediffs.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (enumerator.Current.Part != null && enumerator.Current.def.hediffClass == typeof(Hediff_AddedPart))
					{
						dictionary.Add(enumerator.Current, value: false);
					}
					else
					{
						dictionary.Add(enumerator.Current, value: true);
					}
				}
			}
			foreach (Hediff key in dictionary.Keys)
			{
				try
				{
					if (dictionary[key])
					{
						pawn.RemoveHediff(key);
					}
				}
				catch
				{
					if (pawn.Dead)
					{
						ResurrectionUtility.TryResurrect(pawn);
					}
				}
			}
		}
		catch
		{
			MessageTool.Show("failed to heal");
		}
	}

	internal static void Medicate(this Pawn pawn)
	{
		if (pawn.HasHealthTracker())
		{
			Medicine medicine = null;
			MedicalCareCategory medicalCareCategory = ((pawn.playerSettings != null) ? pawn.playerSettings.medCare : MedicalCareCategory.Best);
			switch ((medicalCareCategory == MedicalCareCategory.NoCare || medicalCareCategory == MedicalCareCategory.NoMeds) ? MedicalCareCategory.HerbalOrWorse : medicalCareCategory)
			{
			case MedicalCareCategory.HerbalOrWorse:
				medicine = (Medicine)ThingMaker.MakeThing(ThingDefOf.MedicineHerbal);
				break;
			case MedicalCareCategory.NormalOrWorse:
				medicine = (Medicine)ThingMaker.MakeThing(ThingDefOf.MedicineIndustrial);
				break;
			case MedicalCareCategory.Best:
				medicine = (Medicine)ThingMaker.MakeThing(ThingDefOf.MedicineUltratech);
				break;
			}
			TendUtility.DoTend(null, pawn, medicine);
		}
	}

	internal static void Hurt(this Pawn pawn)
	{
		int num = CEditor.zufallswert.Next(1, 3);
		for (int i = 0; i < num; i++)
		{
			HediffDef hediff = DefDatabase<HediffDef>.AllDefs.Where((HediffDef td) => td.defName != null && td.isBad && td.injuryProps != null).ToList().RandomElement();
			pawn.AddHediff2(random: true, hediff);
		}
	}

	internal static void Addictize(this Pawn pawn)
	{
		HediffDef hediffDef = null;
		for (int i = 0; i < 10; i++)
		{
			hediffDef = DefDatabase<HediffDef>.AllDefs.Where((HediffDef td) => td.defName != null && td.IsAddiction).ToList().RandomElement();
			if (!pawn.health.hediffSet.HasHediff(hediffDef))
			{
				break;
			}
		}
		if (hediffDef != null)
		{
			pawn.AddHediff2(random: true, hediffDef);
		}
	}

	internal static void Deaddictize(this Pawn pawn)
	{
		foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
		{
			if (hediff.def != null && hediff.def.IsAddiction)
			{
				pawn.RemoveHediff(hediff);
				break;
			}
		}
	}

	internal static void DamageUntilDeath(this Pawn pawn)
	{
		if (!pawn.Dead)
		{
			if (pawn.Downed)
			{
				HealthUtility.DamageUntilDead(pawn);
			}
			else
			{
				HealthUtility.DamageUntilDowned(pawn);
			}
		}
	}

	internal static void Anaesthetize(this Pawn pawn)
	{
		HealthUtility.TryAnesthetize(pawn);
	}

	internal static void ShowDebugInfo(this Hediff h)
	{
		if (h == null || !Prefs.DevMode)
		{
			return;
		}
		try
		{
			if (h is HediffWithComps)
			{
				foreach (HediffComp comp in (h as HediffWithComps).comps)
				{
					if (comp != null)
					{
						MessageTool.Show("compType=" + comp.GetType());
					}
				}
			}
			MessageTool.Show("tags=" + h.def.tags.ListToString());
			MessageTool.Show(h.def.defName + "  Class=" + h.def.hediffClass?.ToString() + "  Bclass=" + h.def.hediffClass.BaseType);
		}
		catch
		{
		}
	}

	internal static List<HediffDef> ListOfHediffDef(string modname, Pawn p, BodyPartRecord bpr, string filter, bool wholeBody)
	{
		bool bAll1 = modname.NullOrEmpty();
		List<HediffDef> list = (from td in DefDatabase<HediffDef>.AllDefs
			where !td.label.NullOrEmpty() && (bAll1 || td.IsFromMod(modname))
			orderby td.label
			select td).ToList();
		if (filter == Label.HB_ALLIMPLANTS)
		{
			list = list.Where((HediffDef td) => td.IsImplant() || td.IsHediffWithLevel()).ToList();
		}
		else if (filter == Label.HB_ALLADDICTIONS)
		{
			list = list.Where((HediffDef td) => td.IsAddiction || td.IsHigh() || td.HasComp(typeof(HediffComp_DrugEffectFactor)) || td.HasComp(typeof(HediffComp_Effecter))).ToList();
		}
		else if (filter == Label.HB_ALLDISEASES)
		{
			list = list.Where((HediffDef td) => td.isBad && td.defName != "NoPain" && td.defName != "SpeedBoost").ToList();
		}
		else if (filter == Label.HB_ALLINJURIES)
		{
			list = list.Where((HediffDef td) => td.injuryProps != null).ToList();
		}
		else if (filter == Label.HB_ALLTIME)
		{
			list = list.Where((HediffDef td) => td.HasComp(typeof(HediffComp_Disappears))).ToList();
		}
		else if (filter == Label.HB_WITHLEVEL)
		{
			list = list.Where((HediffDef td) => td.IsHediffWithLevel()).ToList();
		}
		if (bpr != null)
		{
			list = list.Where((HediffDef td) => p.GetListOfAllowedBodyPartRecords(td).Contains(bpr)).ToList();
		}
		else if (wholeBody)
		{
			list = list.Where((HediffDef td) => td.IsForWholeBody()).ToList();
		}
		return list;
	}

	internal static List<BodyPartRecord> GetListOfAllowedBodyPartRecords(this Pawn p, HediffDef h)
	{
		if (p == null || h == null)
		{
			return null;
		}
		List<BodyPartRecord> l = new List<BodyPartRecord>();
		if (!h.descriptionHyperlinks.NullOrEmpty())
		{
			foreach (DefHyperlink descriptionHyperlink in h.descriptionHyperlinks)
			{
				ResolveHyperlink(descriptionHyperlink, p, ref l, firstPass: true);
			}
		}
		if (l.NullOrEmpty() && !h.tags.NullOrEmpty())
		{
			List<BodyPartRecord> list = new List<BodyPartRecord>();
			foreach (string tag in h.tags)
			{
				list = p.GetListOfBodyPartRecordsByName(tag, h, tag == "All");
				if (!list.NullOrEmpty())
				{
					l.AddRange(list);
				}
			}
		}
		if (l.NullOrEmpty())
		{
			foreach (string key in AllBodyPartChecks.Keys)
			{
				if (AllBodyPartChecks[key](h))
				{
					l = p.GetListOfBodyPartRecordsByName(key, h, key == "All");
					break;
				}
			}
		}
		if (l.NullOrEmpty())
		{
			l = p.GetListOfBodyPartRecordsByName(null, h, all: true);
			bool flag = h.modContentPack == null || !h.modContentPack.IsCoreMod;
			if (h.injuryProps == null && !h.HasComp(typeof(HediffComp_TendDuration)) && (h.IsHediffWithComps() || flag))
			{
				l.Insert(0, null);
			}
		}
		return l;
	}

	internal static void ResolveHyperlink(DefHyperlink defHyper, Pawn p, ref List<BodyPartRecord> l, bool firstPass)
	{
		if (defHyper.def is RecipeDef)
		{
			RecipeDef recipeDef = (RecipeDef)defHyper.def;
			if (!recipeDef.appliedOnFixedBodyParts.NullOrEmpty())
			{
				foreach (BodyPartRecord allPart in p.RaceProps.body.AllParts)
				{
					foreach (BodyPartDef appliedOnFixedBodyPart in recipeDef.appliedOnFixedBodyParts)
					{
						if (allPart.def.defName == appliedOnFixedBodyPart.defName && !l.Contains(allPart))
						{
							l.Add(allPart);
						}
					}
				}
				return;
			}
			if (!recipeDef.appliedOnFixedBodyPartGroups.NullOrEmpty())
			{
				foreach (BodyPartRecord allPart2 in p.RaceProps.body.AllParts)
				{
					foreach (BodyPartGroupDef appliedOnFixedBodyPartGroup in recipeDef.appliedOnFixedBodyPartGroups)
					{
						if (allPart2.def.defName == appliedOnFixedBodyPartGroup.defName && !l.Contains(allPart2))
						{
							l.Add(allPart2);
						}
					}
				}
				return;
			}
			if (!firstPass || recipeDef.descriptionHyperlinks.NullOrEmpty())
			{
				return;
			}
			{
				foreach (DefHyperlink descriptionHyperlink in recipeDef.descriptionHyperlinks)
				{
					ResolveHyperlink(descriptionHyperlink, p, ref l, firstPass: false);
				}
				return;
			}
		}
		if (!(defHyper.def is ThingDef))
		{
			return;
		}
		ThingDef thingDef = (ThingDef)defHyper.def;
		CompProperties compByType = thingDef.GetCompByType(typeof(CompProperties_UseEffectInstallImplant));
		if (compByType != null)
		{
			CompProperties_UseEffectInstallImplant compProperties_UseEffectInstallImplant = compByType as CompProperties_UseEffectInstallImplant;
			if (compProperties_UseEffectInstallImplant.bodyPart == null)
			{
				return;
			}
			{
				foreach (BodyPartRecord allPart3 in p.RaceProps.body.AllParts)
				{
					if (allPart3.def.defName == compProperties_UseEffectInstallImplant.bodyPart.defName && !l.Contains(allPart3))
					{
						l.Add(allPart3);
					}
				}
				return;
			}
		}
		if (!firstPass || thingDef.descriptionHyperlinks.NullOrEmpty())
		{
			return;
		}
		foreach (DefHyperlink descriptionHyperlink2 in thingDef.descriptionHyperlinks)
		{
			ResolveHyperlink(descriptionHyperlink2, p, ref l, firstPass: false);
		}
	}

	internal static List<BodyPartRecord> GetListOfBodyPartRecordsByName(this Pawn p, string defName, HediffDef h, bool all = false)
	{
		List<BodyPartRecord> list = new List<BodyPartRecord>();
		if (p != null && h != null)
		{
			if (defName == "WholeBody")
			{
				list.Add(null);
			}
			else
			{
				foreach (BodyPartRecord allPart in p.RaceProps.body.AllParts)
				{
					if (all)
					{
						if (!list.Contains(allPart))
						{
							list.Add(allPart);
						}
					}
					else if (!h.hediffGivers.NullOrEmpty())
					{
						foreach (HediffGiver hediffGiver in h.hediffGivers)
						{
							if (hediffGiver.partsToAffect.NullOrEmpty())
							{
								if (allPart.def.defName == defName)
								{
									if (!list.Contains(allPart))
									{
										list.Add(allPart);
									}
									break;
								}
							}
							else if (hediffGiver.partsToAffect.Contains(allPart.def))
							{
								if (!list.Contains(allPart))
								{
									list.Add(allPart);
								}
								break;
							}
						}
					}
					else if (allPart.def.defName == defName && !list.Contains(allPart))
					{
						list.Add(allPart);
					}
				}
			}
		}
		return list;
	}
}
