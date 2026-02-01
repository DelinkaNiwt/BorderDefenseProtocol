using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using VanillaPsycastsExpanded.UI;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded;

[StaticConstructorOnStartup]
public class Hediff_PsycastAbilities : Hediff_Abilities
{
	private static readonly Texture2D PsySetNext = ContentFinder<Texture2D>.Get("UI/Gizmos/Psyset_Next");

	public float experience;

	public int maxLevelFromTitles;

	public int points;

	public List<PsycasterPathDef> previousUnlockedPaths = new List<PsycasterPathDef>();

	public Hediff_Psylink psylink;

	public List<PsySet> psysets = new List<PsySet>();

	public List<MeditationFocusDef> unlockedMeditationFoci = new List<MeditationFocusDef>();

	public List<PsycasterPathDef> unlockedPaths = new List<PsycasterPathDef>();

	private IChannelledPsycast currentlyChanneling;

	private HediffStage curStage;

	private List<IMinHeatGiver> minHeatGivers = new List<IMinHeatGiver>();

	private int psysetIndex;

	private int statPoints;

	public Ability CurrentlyChanneling
	{
		get
		{
			IChannelledPsycast channelledPsycast = currentlyChanneling;
			return (Ability)((channelledPsycast is Ability) ? channelledPsycast : null);
		}
	}

	public override HediffStage CurStage
	{
		get
		{
			if (curStage == null)
			{
				RecacheCurStage();
			}
			return curStage;
		}
	}

	public IEnumerable<Gizmo> GetPsySetGizmos()
	{
		if (psysets.Count > 0)
		{
			int nextIndex = psysetIndex + 1;
			if (nextIndex > psysets.Count)
			{
				nextIndex = 0;
			}
			yield return new Command_ActionWithFloat
			{
				defaultLabel = "VPE.PsySetNext".Translate(),
				defaultDesc = "VPE.PsySetDesc".Translate(PsySetLabel(psysetIndex), PsySetLabel(nextIndex)),
				icon = PsySetNext,
				action = delegate
				{
					psysetIndex = nextIndex;
				},
				Order = 10f,
				floatMenuGetter = GetPsySetFloatMenuOptions
			};
		}
	}

	private string PsySetLabel(int index)
	{
		if (index == psysets.Count)
		{
			return "VPE.All".Translate();
		}
		return psysets[index].Name;
	}

	private IEnumerable<FloatMenuOption> GetPsySetFloatMenuOptions()
	{
		for (int i = 0; i <= psysets.Count; i++)
		{
			int index = i;
			yield return new FloatMenuOption(PsySetLabel(index), delegate
			{
				psysetIndex = index;
			});
		}
	}

	public void InitializeFromPsylink(Hediff_Psylink psylink)
	{
		this.psylink = psylink;
		((Hediff_Level)this).level = psylink.level;
		points = ((Hediff_Level)this).level;
		if (((Hediff_Level)this).level <= 1)
		{
			points = 2;
		}
		RecacheCurStage();
	}

	private void RecacheCurStage()
	{
		minHeatGivers.RemoveAll((IMinHeatGiver giver) => giver == null || !giver.IsActive);
		curStage = new HediffStage
		{
			statOffsets = new List<StatModifier>
			{
				new StatModifier
				{
					stat = StatDefOf.PsychicEntropyMax,
					value = ((Hediff_Level)this).level * 5 + statPoints * 10
				},
				new StatModifier
				{
					stat = StatDefOf.PsychicEntropyRecoveryRate,
					value = (float)((Hediff_Level)this).level * 0.0125f + (float)statPoints * 0.05f
				},
				new StatModifier
				{
					stat = StatDefOf.PsychicSensitivity,
					value = (float)statPoints * 0.05f
				},
				new StatModifier
				{
					stat = VPE_DefOf.VPE_PsyfocusCostFactor,
					value = (float)statPoints * -0.01f
				},
				new StatModifier
				{
					stat = VPE_DefOf.VPE_PsychicEntropyMinimum,
					value = minHeatGivers.Sum((IMinHeatGiver giver) => (giver.MinHeat != 0) ? giver.MinHeat : 0)
				}
			},
			becomeVisible = false
		};
		if (PsycastsMod.Settings.changeFocusGain)
		{
			curStage.statOffsets.Add(new StatModifier
			{
				stat = StatDefOf.MeditationFocusGain,
				value = (float)statPoints * 0.1f
			});
		}
		if (((Hediff)this).pawn != null && ((Hediff)this).pawn.Spawned)
		{
			((Hediff)this).pawn.health.Notify_HediffChanged((Hediff)(object)this);
		}
	}

	public void UseAbility(float focus, float entropy)
	{
		((Hediff)this).pawn.psychicEntropy.TryAddEntropy(entropy);
		((Hediff)this).pawn.psychicEntropy.OffsetPsyfocusDirectly(0f - focus);
	}

	public void ChangeLevel(int levelOffset, bool sendLetter)
	{
		((Hediff_Level)(object)this).ChangeLevel(levelOffset);
		if (sendLetter && PawnUtility.ShouldSendNotificationAbout(((Hediff)this).pawn))
		{
			Find.LetterStack.ReceiveLetter("VPE.PsylinkGained".Translate(((Hediff)this).pawn.LabelShortCap), "VPE.PsylinkGained.Desc".Translate(((Hediff)this).pawn.LabelShortCap, ((Hediff)this).pawn.gender.GetPronoun().CapitalizeFirst(), ExperienceRequiredForLevel(((Hediff_Level)this).level + 1)), LetterDefOf.PositiveEvent, ((Hediff)this).pawn);
		}
	}

	public override void ChangeLevel(int levelOffset)
	{
		((Hediff_Abilities)this).ChangeLevel(levelOffset);
		points += levelOffset;
		RecacheCurStage();
		if (psylink == null)
		{
			psylink = ((Hediff)this).pawn.health.hediffSet.hediffs.OfType<Hediff_Psylink>().FirstOrDefault();
		}
		if (psylink == null)
		{
			((Hediff)this).pawn.ChangePsylinkLevel(((Hediff_Level)this).level, sendLetter: false);
			psylink = ((Hediff)this).pawn.health.hediffSet.hediffs.OfType<Hediff_Psylink>().First();
		}
		psylink.level = ((Hediff_Level)this).level;
	}

	public void Reset()
	{
		points = ((Hediff_Level)this).level;
		unlockedPaths.Clear();
		unlockedMeditationFoci.Clear();
		MeditationFocusTypeAvailabilityCache.ClearFor(((Hediff)this).pawn);
		statPoints = 0;
		CompAbilities comp = ((ThingWithComps)((Hediff)this).pawn).GetComp<CompAbilities>();
		if (comp != null)
		{
			comp.LearnedAbilities.RemoveAll((Ability a) => a.def.Psycast() != null);
		}
		RecacheCurStage();
	}

	public void GainExperience(float experienceGain, bool sendLetter = true)
	{
		if (((Hediff_Level)this).level < PsycastsMod.Settings.maxLevel)
		{
			experience += experienceGain;
			bool flag = false;
			while (((Hediff_Level)this).level < PsycastsMod.Settings.maxLevel && experience >= (float)ExperienceRequiredForLevel(((Hediff_Level)this).level + 1))
			{
				ChangeLevel(1, sendLetter && !flag);
				flag = true;
				experience -= ExperienceRequiredForLevel(((Hediff_Level)this).level);
			}
		}
	}

	public bool SufficientPsyfocusPresent(float focusRequired)
	{
		return ((Hediff)this).pawn.psychicEntropy.CurrentPsyfocus > focusRequired;
	}

	public override bool SatisfiesConditionForAbility(AbilityDef abilityDef)
	{
		if (!((Hediff_Abilities)this).SatisfiesConditionForAbility(abilityDef))
		{
			return abilityDef.requiredHediff?.minimumLevel <= psylink.level;
		}
		return true;
	}

	public void AddMinHeatGiver(IMinHeatGiver giver)
	{
		if (!minHeatGivers.Contains(giver))
		{
			minHeatGivers.Add(giver);
			RecacheCurStage();
		}
	}

	public void BeginChannelling(IChannelledPsycast psycast)
	{
		currentlyChanneling = psycast;
	}

	public override void ExposeData()
	{
		((Hediff_Level)this).ExposeData();
		Scribe_Values.Look(ref experience, "experience", 0f);
		Scribe_Values.Look(ref points, "points", 0);
		Scribe_Values.Look(ref statPoints, "statPoints", 0);
		Scribe_Values.Look(ref psysetIndex, "psysetIndex", 0);
		Scribe_Values.Look(ref maxLevelFromTitles, "maxLevelFromTitles", 0);
		Scribe_Collections.Look(ref previousUnlockedPaths, "previousUnlockedPaths", LookMode.Def);
		Scribe_Collections.Look(ref unlockedPaths, "unlockedPaths", LookMode.Def);
		Scribe_Collections.Look(ref unlockedMeditationFoci, "unlockedMeditationFoci", LookMode.Def);
		Scribe_Collections.Look(ref psysets, "psysets", LookMode.Deep);
		Scribe_Collections.Look(ref minHeatGivers, "minHeatGivers", LookMode.Reference);
		Scribe_References.Look(ref psylink, "psylink");
		Scribe_References.Look(ref currentlyChanneling, "currentlyChanneling");
		if (minHeatGivers == null)
		{
			minHeatGivers = new List<IMinHeatGiver>();
		}
		if (Scribe.mode == LoadSaveMode.PostLoadInit)
		{
			if (unlockedPaths == null)
			{
				unlockedPaths = new List<PsycasterPathDef>();
			}
			if (previousUnlockedPaths == null)
			{
				previousUnlockedPaths = new List<PsycasterPathDef>();
			}
			RecacheCurStage();
		}
	}

	public void SpentPoints(int count = 1)
	{
		points -= count;
	}

	public void ImproveStats(int count = 1)
	{
		statPoints += count;
		RecacheCurStage();
	}

	public void UnlockPath(PsycasterPathDef path)
	{
		unlockedPaths.Add(path);
	}

	public void UnlockMeditationFocus(MeditationFocusDef focus)
	{
		unlockedMeditationFoci.Add(focus);
		MeditationFocusTypeAvailabilityCache.ClearFor(((Hediff)this).pawn);
	}

	public bool ShouldShow(Ability ability)
	{
		if (psysetIndex != psysets.Count)
		{
			return psysets[psysetIndex].Abilities.Contains(ability.def);
		}
		return true;
	}

	public void RemovePsySet(PsySet set)
	{
		psysets.Remove(set);
		psysetIndex = Mathf.Clamp(psysetIndex, 0, psysets.Count);
	}

	public static int ExperienceRequiredForLevel(int level)
	{
		if (level <= 20)
		{
			if (level <= 1)
			{
				return 100;
			}
			return Mathf.RoundToInt((float)ExperienceRequiredForLevel(level - 1) * 1.15f);
		}
		if (level <= 30)
		{
			return Mathf.RoundToInt((float)ExperienceRequiredForLevel(level - 1) * 1.1f);
		}
		return Mathf.RoundToInt((float)ExperienceRequiredForLevel(level - 1) * 1.05f);
	}

	public override void GiveRandomAbilityAtLevel(int? forLevel = null)
	{
	}

	public override void Tick()
	{
		((Hediff)this).Tick();
		IChannelledPsycast channelledPsycast = currentlyChanneling;
		if (channelledPsycast != null && !channelledPsycast.IsActive)
		{
			currentlyChanneling = null;
		}
		if (minHeatGivers.RemoveAll((IMinHeatGiver giver) => giver == null || !giver.IsActive) > 0)
		{
			RecacheCurStage();
		}
	}
}
