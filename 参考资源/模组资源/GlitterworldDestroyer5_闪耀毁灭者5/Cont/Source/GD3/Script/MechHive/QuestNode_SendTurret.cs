using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using RimWorld.QuestGen;
using Verse.AI.Group;

namespace GD3
{
	public class QuestNode_SendTurret : QuestNode
	{

		protected override bool TestRunInt(Slate slate)
		{
			if (slate.Get<Map>("map") != null)
			{
				return true;
			}
			return false;
		}

		protected override void RunInt()
		{
			Quest quest = QuestGen.quest;
			Slate slate = QuestGen.slate;
			Map map = slate.Get<Map>("map");
			Building turret = ThingMaker.MakeThing(GDDefOf.PlayerTurret_Broken) as Building;
			turret.SetFaction(Faction.OfPlayer);
			slate.Set("turret", turret);
			quest.Delay(1200, delegate
			{
				List<Thing> list = new List<Thing>();
				list.Add(turret);
				quest.DropPods(map.Parent, list, "[turretArriveLetterLabel]", null, "[turretArriveLetterText]", null, true, useTradeDropSpot: true, joinPlayer: false, makePrisoners: false, null, null, QuestPart.SignalListenMode.OngoingOnly, null, destroyItemsOnCleanup: true, dropAllInSamePod: true);

			});
		}
	}
}