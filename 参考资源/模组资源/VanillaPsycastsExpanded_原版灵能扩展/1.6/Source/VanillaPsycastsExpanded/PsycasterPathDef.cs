using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded;

public class PsycasterPathDef : Def
{
	public static AbilityDef Blank;

	public static int TotalPoints;

	[Unsaved(false)]
	public List<AbilityDef> abilities;

	[Unsaved(false)]
	public AbilityDef[][] abilityLevelsInOrder;

	public string altBackground;

	[Unsaved(false)]
	public Texture2D altBackgroundImage;

	public string background;

	public Color backgroundColor;

	[Unsaved(false)]
	public Texture2D backgroundImage;

	[Unsaved(false)]
	public bool HasAbilities;

	public int height;

	[MustTranslate]
	public string lockedReason;

	[Unsaved(false)]
	public int MaxLevel;

	public int order;

	public List<BackstoryCategoryAndSlot> requiredBackstoriesAny;

	public MeditationFocusDef requiredFocus;

	public GeneDef requiredGene;

	public MemeDef requiredMeme;

	public bool requiredMechanitor;

	public bool ignoreLockRestrictionsForNeurotrainers = true;

	public bool ensureLockRequirement;

	public string tab;

	public string tooltip;

	public int width;

	public virtual bool CanPawnUnlock(Pawn pawn)
	{
		if (PawnHasCorrectBackstory(pawn) && PawnHasMeme(pawn) && PawnHasGene(pawn) && PawnIsMechanitor(pawn))
		{
			return PawnHasCorrectFocus(pawn);
		}
		return false;
	}

	private bool PawnHasMeme(Pawn pawn)
	{
		if (requiredMeme != null)
		{
			return pawn.Ideo?.memes.Contains(requiredMeme) ?? false;
		}
		return true;
	}

	private bool PawnHasGene(Pawn pawn)
	{
		if (requiredGene != null)
		{
			return pawn.genes?.GetGene(requiredGene)?.Active == true;
		}
		return true;
	}

	private bool PawnIsMechanitor(Pawn pawn)
	{
		if (requiredMechanitor)
		{
			return MechanitorUtility.IsMechanitor(pawn);
		}
		return true;
	}

	private bool PawnHasCorrectFocus(Pawn pawn)
	{
		if (requiredFocus != null)
		{
			return requiredFocus.CanPawnUse(pawn);
		}
		return true;
	}

	private bool PawnHasCorrectBackstory(Pawn pawn)
	{
		if (requiredBackstoriesAny.NullOrEmpty())
		{
			return true;
		}
		foreach (BackstoryCategoryAndSlot item in requiredBackstoriesAny)
		{
			List<string> list = pawn.story.GetBackstory(item.slot)?.spawnCategories;
			if (list != null && list.Contains(item.categoryName))
			{
				return true;
			}
		}
		return false;
	}

	public override void PostLoad()
	{
		base.PostLoad();
		LongEventHandler.ExecuteWhenFinished(delegate
		{
			if (!background.NullOrEmpty())
			{
				backgroundImage = ContentFinder<Texture2D>.Get(background);
			}
			if (!altBackground.NullOrEmpty())
			{
				altBackgroundImage = ContentFinder<Texture2D>.Get(altBackground);
			}
			if (width > 0 && height > 0)
			{
				Texture2D texture2D = new Texture2D(width, height);
				Color[] array = new Color[width * height];
				for (int i = 0; i < array.Length; i++)
				{
					array[i] = backgroundColor;
				}
				texture2D.SetPixels(array);
				texture2D.Apply();
				if (backgroundImage == null)
				{
					backgroundImage = texture2D;
				}
				if (altBackgroundImage == null)
				{
					altBackgroundImage = texture2D;
				}
			}
			if (backgroundImage == null && altBackgroundImage != null)
			{
				backgroundImage = altBackgroundImage;
			}
			if (altBackgroundImage == null && backgroundImage != null)
			{
				altBackgroundImage = backgroundImage;
			}
		});
	}

	public override void ResolveReferences()
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Expected O, but got Unknown
		base.ResolveReferences();
		if (Blank == null)
		{
			Blank = new AbilityDef();
		}
		TotalPoints++;
		abilities = new List<AbilityDef>();
		foreach (AbilityDef item in DefDatabase<AbilityDef>.AllDefsListForReading)
		{
			AbilityExtension_Psycast modExtension = ((Def)(object)item).GetModExtension<AbilityExtension_Psycast>();
			if (modExtension != null && modExtension.path == this)
			{
				abilities.Add(item);
			}
		}
		MaxLevel = abilities.Max((AbilityDef ab) => ab.Psycast().level);
		TotalPoints += abilities.Count;
		abilityLevelsInOrder = new AbilityDef[MaxLevel][];
		foreach (IGrouping<int, AbilityDef> item2 in from ab in abilities
			group ab by ab.Psycast().level)
		{
			abilityLevelsInOrder[item2.Key - 1] = item2.OrderBy((AbilityDef ab) => ab.Psycast().order).SelectMany((AbilityDef ab) => (!ab.Psycast().spaceAfter) ? Gen.YieldSingle<AbilityDef>(ab) : new List<AbilityDef> { ab, Blank }).ToArray();
		}
		HasAbilities = abilityLevelsInOrder.Any((AbilityDef[] arr) => !arr.NullOrEmpty());
		if (!HasAbilities)
		{
			return;
		}
		for (int num = 0; num < abilityLevelsInOrder.Length; num++)
		{
			if (abilityLevelsInOrder[num] == null)
			{
				abilityLevelsInOrder[num] = (AbilityDef[])(object)new AbilityDef[0];
			}
		}
	}
}
