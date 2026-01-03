using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace AlienRace;

[StaticConstructorOnStartup]
public class ScenPart_StartingHumanlikes : ScenPart
{
	private PawnKindDef kindDef;

	private int pawnCount;

	private string buffer;

	public PawnKindDef KindDef
	{
		get
		{
			return kindDef ?? (kindDef = PawnKindDefOf.Villager);
		}
		set
		{
			kindDef = value;
		}
	}

	[Obsolete("Written in xml as HAR_StartingHumanlikes")]
	static ScenPart_StartingHumanlikes()
	{
		ScenPartDef scenPart = new ScenPartDef
		{
			defName = "StartingHumanlikes",
			label = "Starting with Humanlikes",
			scenPartClass = typeof(ScenPart_StartingHumanlikes),
			category = ScenPartCategory.StartingImportant,
			selectionWeight = 1f,
			summaryPriority = 10f
		};
		scenPart.ResolveReferences();
		scenPart.PostLoad();
		DefDatabase<ScenPartDef>.Add(scenPart);
	}

	public override void DoEditInterface(Listing_ScenEdit listing)
	{
		Rect scenPartRect = listing.GetScenPartRect(this, ScenPart.RowHeight * 3f);
		if (Widgets.ButtonText(scenPartRect.TopPart(0.45f), KindDef.label.CapitalizeFirst()))
		{
			List<FloatMenuOption> list = new List<FloatMenuOption>();
			list.AddRange(from pkd in DefDatabase<RaceSettings>.AllDefsListForReading.Where((RaceSettings ar) => ar.pawnKindSettings.startingColonists != null).SelectMany((RaceSettings ar) => ar.pawnKindSettings.startingColonists.SelectMany((FactionPawnKindEntry ste) => ste.pawnKindEntries.SelectMany((PawnKindEntry pke) => pke.kindDefs)))
				where pkd != null
				select new FloatMenuOption(((string)pkd.LabelCap != (string)pkd.race.LabelCap) ? $"{pkd.LabelCap} | {pkd.race.LabelCap}" : ((string)pkd.LabelCap), delegate
				{
					KindDef = pkd;
				}));
			list.Add(new FloatMenuOption(PawnKindDefOf.Villager.LabelCap, delegate
			{
				KindDef = PawnKindDefOf.Villager;
			}));
			list.Add(new FloatMenuOption(PawnKindDefOf.Slave.LabelCap, delegate
			{
				KindDef = PawnKindDefOf.Slave;
			}));
			Find.WindowStack.Add(new FloatMenu(list));
		}
		Widgets.TextFieldNumeric(scenPartRect.BottomPart(0.45f), ref pawnCount, ref buffer);
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref pawnCount, "alienRaceScenPawnCount", 0);
		Scribe_Defs.Look(ref kindDef, "PawnKindDefAlienRaceScen");
	}

	public override string Summary(Scenario scen)
	{
		return ScenSummaryList.SummaryWithList(scen, "PlayerStartsWith", ScenPart_StartingThing_Defined.PlayerStartWithIntro);
	}

	public override IEnumerable<string> GetSummaryListEntries(string tag)
	{
		if (tag == "PlayerStartsWith")
		{
			yield return string.Concat(KindDef.LabelCap + " x", pawnCount.ToString());
		}
	}

	public override bool TryMerge(ScenPart other)
	{
		if (!(other is ScenPart_StartingHumanlikes others) || others.KindDef != KindDef)
		{
			return false;
		}
		pawnCount += others.pawnCount;
		return true;
	}

	public IEnumerable<Pawn> GetPawns()
	{
		List<WorkTypeDef> list = DefDatabase<WorkTypeDef>.AllDefsListForReading.Where((WorkTypeDef wtd) => wtd.requireCapableColonist).ToList();
		for (int i = 0; i < pawnCount; i++)
		{
			Pawn newPawn = null;
			for (int x = 0; x < 200; x++)
			{
				newPawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(KindDef, Faction.OfPlayer, PawnGenerationContext.PlayerStarter, null, forceGenerateNewPawn: true, allowDead: false, allowDowned: false, canGeneratePawnRelations: true, mustBeCapableOfViolence: false, 26f, forceAddFreeWarmLayerIfNeeded: true));
				if (PawnCheck(newPawn))
				{
					x = 200;
				}
			}
			yield return newPawn;
		}
		bool PawnCheck(Pawn p)
		{
			if (p != null)
			{
				return list.TrueForAll((WorkTypeDef w) => !p.WorkTypeIsDisabled(w));
			}
			return false;
		}
	}
}
