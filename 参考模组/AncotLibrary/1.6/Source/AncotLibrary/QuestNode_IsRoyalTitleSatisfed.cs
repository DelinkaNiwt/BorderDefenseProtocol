using System.Collections.Generic;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace AncotLibrary;

public class QuestNode_IsRoyalTitleSatisfed : QuestNode
{
	[NoTranslate]
	public SlateRef<List<RoyalTitleDef>> royalTitleDefs;

	[NoTranslate]
	public SlateRef<FactionDef> factionDef;

	[NoTranslate]
	public SlateRef<string> addToList;

	[NoTranslate]
	public SlateRef<IEnumerable<string>> addToLists;

	[NoTranslate]
	public SlateRef<string> storePawnMostSeniorAs;

	[NoTranslate]
	public SlateRef<string> storeRoyalMostSeniorAs;

	protected override bool TestRunInt(Slate slate)
	{
		Map map = slate.Get<Map>("map");
		List<Pawn> freeColonistsSpawned = map.mapPawns.FreeColonistsSpawned;
		Faction faction = Find.FactionManager.FirstFactionOfDef(factionDef.GetValue(slate));
		List<RoyalTitleDef> value = royalTitleDefs.GetValue(slate);
		if (map == null || faction == null || freeColonistsSpawned.NullOrEmpty())
		{
			return false;
		}
		foreach (Pawn item in freeColonistsSpawned)
		{
			if (value.NullOrEmpty())
			{
				if (item.royalty != null && item.royalty.HasAnyTitleIn(faction))
				{
					return true;
				}
			}
			else if (item.royalty != null && item.royalty.HasAnyTitleIn(faction) && value.Contains(item.royalty.GetCurrentTitle(faction)))
			{
				return true;
			}
		}
		return false;
	}

	protected override void RunInt()
	{
		Slate slate = QuestGen.slate;
		Map map = slate.Get<Map>("map");
		List<Pawn> freeColonistsSpawned = map.mapPawns.FreeColonistsSpawned;
		Faction faction = Find.FactionManager.FirstFactionOfDef(factionDef.GetValue(slate));
		List<RoyalTitleDef> value = royalTitleDefs.GetValue(slate);
		List<Pawn> list = new List<Pawn>();
		if (map == null || faction == null || freeColonistsSpawned.NullOrEmpty())
		{
			return;
		}
		foreach (Pawn item in freeColonistsSpawned)
		{
			if (value.NullOrEmpty())
			{
				if (item.royalty != null && item.royalty.HasAnyTitleIn(faction))
				{
					list.Add(item);
				}
			}
			else if (item.royalty != null && item.royalty.HasAnyTitleIn(faction) && value.Contains(item.royalty.GetCurrentTitle(faction)))
			{
				list.Add(item);
			}
		}
		if (list.NullOrEmpty())
		{
			return;
		}
		AddToList(slate, list);
		if (storePawnMostSeniorAs.GetValue(slate) == null)
		{
			return;
		}
		Pawn pawn = null;
		int num = 0;
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i].royalty.GetCurrentTitle(faction).seniority > num)
			{
				num = list[i].royalty.GetCurrentTitle(faction).seniority;
				pawn = list[i];
			}
		}
		if (pawn != null)
		{
			slate.Set(storePawnMostSeniorAs.GetValue(slate), pawn);
			if (storePawnMostSeniorAs.GetValue(slate) != null)
			{
				slate.Set(storeRoyalMostSeniorAs.GetValue(slate), pawn.royalty.GetCurrentTitle(faction));
			}
		}
	}

	protected void AddToList(Slate slate, List<Pawn> pawn)
	{
		if (addToList.GetValue(slate) != null)
		{
			QuestGenUtility.AddToOrMakeList(QuestGen.slate, addToList.GetValue(slate), pawn);
		}
		if (addToLists.GetValue(slate) == null)
		{
			return;
		}
		foreach (string item in addToLists.GetValue(slate))
		{
			QuestGenUtility.AddToOrMakeList(QuestGen.slate, item, pawn);
		}
	}
}
