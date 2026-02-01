using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.QuestGen;
using RimWorld.Planet;
using Verse;

namespace GD3
{
	public class QuestNode_Root_Structure : QuestNode
	{
		public SlateRef<TileMutatorDef> structureMutator;

		public SlateRef<SitePartDef> sitePartDef;

		protected override bool TestRunInt(Slate slate)
		{
			return true;
		}

		protected override void RunInt()
		{
			Quest quest = QuestGen.quest;
			Slate slate = QuestGen.slate;
			slate.Get("points", 0f);
			PlanetTile tile = slate.Get<PlanetTile>("siteTile");
			tile.Tile.AddMutator(structureMutator.GetValue(slate));
			Site site = QuestGen_Sites.GenerateSite(new SitePartDefWithParams[1]
			{
			new SitePartDefWithParams(sitePartDef.GetValue(slate), new SitePartParams())
			}, tile, Faction.OfAncientsHostile, hiddenSitePartsPossible: false, null, WorldObjectDefOf.ClaimableSite);
			quest.SpawnWorldObject(site);
			slate.Set("site", site);
			slate.Set("atLandmark", site.Tile.Tile.Landmark != null);
			slate.Set("landmarkName", site.Tile.Tile.Landmark?.name ?? "");
		}
	}

}
