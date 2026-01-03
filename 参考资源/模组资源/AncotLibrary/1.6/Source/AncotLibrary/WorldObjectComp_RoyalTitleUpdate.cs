using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace AncotLibrary;

public class WorldObjectComp_RoyalTitleUpdate : WorldObjectComp
{
	public WorldObjectCompProperties_RoyalTitleUpdate Props => (WorldObjectCompProperties_RoyalTitleUpdate)props;

	public override IEnumerable<Gizmo> GetCaravanGizmos(Caravan caravan)
	{
		if (!RoyalUtility.CanUpdateTitleForPawns(caravan.PawnsListForReading, out var pawnsForTitle, parent.Faction))
		{
			yield break;
		}
		yield return new Command_Action
		{
			defaultLabel = "Ancot.RoyalTitleUpdate".Translate(),
			defaultDesc = "Ancot.RoyalTitleUpdateDesc".Translate(),
			icon = parent.Faction.def.FactionIcon,
			action = delegate
			{
				List<FloatMenuOption> list = new List<FloatMenuOption>();
				for (int i = 0; i < pawnsForTitle.Count; i++)
				{
					Pawn pawn = pawnsForTitle[i];
					FloatMenuOption item = new FloatMenuOption(pawn.LabelCap + " (" + pawn.royalty.GetCurrentTitle(parent.Faction).LabelCap + ")", delegate
					{
						RoyalTitleDef nextTitle = pawn.royalty.GetCurrentTitle(parent.Faction).GetNextTitle(parent.Faction);
						pawn.royalty.TryUpdateTitle(parent.Faction, sendLetter: true, nextTitle);
					}, MenuOptionPriority.Default, null, null, 29f);
					list.Add(item);
				}
				Find.WindowStack.Add(new FloatMenu(list));
			}
		};
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
	}
}
