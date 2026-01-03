using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace AncotLibrary;

public class ScenPart_StartDrones : ScenPart
{
	public PawnKindDef droneKind;

	public int droneAmount = 1;

	private IEnumerable<PawnKindDef> PossibleDrones => DefDatabase<PawnKindDef>.AllDefs.Where((PawnKindDef td) => td.RaceProps.IsMechanoid && td.race.GetCompProperties<CompProperties_Drone>() != null);

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Defs.Look(ref droneKind, "droneKind");
		Scribe_Values.Look(ref droneAmount, "droneAmount", 0);
	}

	public override IEnumerable<string> GetSummaryListEntries(string tag)
	{
		if (tag == "PlayerStartsWith" && !PossibleDrones.EnumerableNullOrEmpty())
		{
			string text = "Ancot.DroneSection".Translate().CapitalizeFirst() + ": " + droneKind.LabelCap;
			if (droneAmount != 1)
			{
				text = text + " x" + droneAmount;
			}
			yield return text;
		}
	}

	public override void DoEditInterface(Listing_ScenEdit listing)
	{
		if (PossibleDrones.EnumerableNullOrEmpty())
		{
			return;
		}
		Rect scenPartRect = listing.GetScenPartRect(this, 2f * ScenPart.RowHeight + 4f);
		if (Widgets.ButtonText(new Rect(scenPartRect.xMin, scenPartRect.yMin, scenPartRect.width, ScenPart.RowHeight), (droneKind != null) ? droneKind.LabelCap : "RandomMech".Translate()))
		{
			List<FloatMenuOption> list = new List<FloatMenuOption>();
			list.Add(new FloatMenuOption("RandomMech".Translate().CapitalizeFirst(), delegate
			{
				droneKind = null;
			}));
			foreach (PawnKindDef possibleDrone in PossibleDrones)
			{
				PawnKindDef localKind = possibleDrone;
				list.Add(new FloatMenuOption(localKind.LabelCap, delegate
				{
					droneKind = localKind;
				}));
			}
			Find.WindowStack.Add(new FloatMenu(list));
		}
		Rect rect = new Rect(scenPartRect.xMin, scenPartRect.yMin + ScenPart.RowHeight + 4f, scenPartRect.width, ScenPart.RowHeight);
		TaggedString taggedString = "Ancot.droneAmount".Translate();
		float x = Text.CalcSize(taggedString).x;
		Widgets.Label(new Rect(rect.xMin, rect.yMin, x, ScenPart.RowHeight), taggedString);
		Rect rect2 = new Rect(rect.xMin + x + 4f, rect.yMin, rect.width - x - 4f, ScenPart.RowHeight);
		string editBuffer = droneAmount.ToString();
		Widgets.IntEntry(rect2, ref droneAmount, ref editBuffer);
	}

	public override IEnumerable<Thing> PlayerStartingThings()
	{
		if (PossibleDrones.EnumerableNullOrEmpty())
		{
			yield break;
		}
		if (droneKind == null)
		{
			droneKind = PossibleDrones.RandomElement();
		}
		if (droneAmount > 0)
		{
			for (int i = 0; i < droneAmount; i++)
			{
				yield return PawnGenerator.GeneratePawn(new PawnGenerationRequest(droneKind, Faction.OfPlayer, PawnGenerationContext.NonPlayer, null, forceGenerateNewPawn: false, allowDead: false, allowDowned: false, canGeneratePawnRelations: true, mustBeCapableOfViolence: false, 1f, forceAddFreeWarmLayerIfNeeded: false, allowGay: true, allowPregnant: false, allowFood: true, allowAddictions: true, inhabitant: false, certainlyBeenInCryptosleep: false, forceRedressWorldPawnIfFormerColonist: false, worldPawnFactionDoesntMatter: false, 0f, 0f, null, 1f, null, null, null, null, null, null, null, null, null, null, null, null, forceNoIdeo: false, forceNoBackstory: false, forbidAnyTitle: false, forceDead: false, null, null, null, null, null, 0f, DevelopmentalStage.Newborn)
				{
					BiologicalAgeRange = new FloatRange(1f, 10f)
				});
			}
		}
	}

	public override void Randomize()
	{
		if (!PossibleDrones.EnumerableNullOrEmpty())
		{
			droneKind = PossibleDrones.RandomElement();
			droneAmount = Rand.Range(1, 3);
		}
	}

	public override int GetHashCode()
	{
		return base.GetHashCode() ^ ((droneKind != null) ? droneKind.GetHashCode() : 0);
	}
}
