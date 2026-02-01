using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.Sound;
using UnityEngine;

namespace GD3
{
    public class BlackMechTradeWorker
    {
        public BlackMechTradeDef def;

        public virtual void Run(Pawn user, Map map)
        {
            SoundDefOf.ExecuteTrade.PlayOneShotOnCamera();
        }
    }

    public class BlackMechTradeWorker_Goods : BlackMechTradeWorker
    {
        public override void Run(Pawn user, Map map)
        {
            Thing goods = ThingMaker.MakeThing(def.thing);
            goods.stackCount = def.count;
            TradeUtility.SpawnDropPod(DropCellFinder.TradeDropSpot(map), map, goods);
        }
    }

    public class BlackMechTradeWorker_Ally : BlackMechTradeWorker
    {
        public override void Run(Pawn user, Map map)
        {
            StorytellerComp storytellerComp = Find.Storyteller.storytellerComps.First((StorytellerComp x) => x is StorytellerComp_OnOffCycle || x is StorytellerComp_RandomMain);
            IncidentParms parms = storytellerComp.GenerateParms(IncidentCategoryDefOf.ThreatBig, user.Map);
            parms.faction = Find.FactionManager.FirstFactionOfDef(GDDefOf.BlackMechanoid);
            parms.points = 100000;
            parms.raidStrategy = RaidStrategyDefOf.ImmediateAttackFriendly;
            parms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeDrop;
            IncidentDefOf.RaidFriendly.Worker.TryExecute(parms);
        }
    }
}
