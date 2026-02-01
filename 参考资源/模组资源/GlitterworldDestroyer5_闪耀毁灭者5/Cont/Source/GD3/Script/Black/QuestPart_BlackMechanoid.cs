using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.Grammar;
using UnityEngine;
using RimWorld;
using RimWorld.QuestGen;

namespace GD3
{
    public static class QuestPart_BlackMechanoid
    {
        public static QuestPart_PlayEffect PlaySubcoreEffect(this Quest quest, Thing thing, EffecterDef effecter, Map map, string inSignal = null, QuestPart.SignalListenMode signalListenMode = QuestPart.SignalListenMode.OngoingOnly)
        {
            QuestPart_PlayEffect questPart_PlayEffect = new QuestPart_PlayEffect();
            questPart_PlayEffect.inSignal = (inSignal ?? QuestGen.slate.Get<string>("inSignal"));
            questPart_PlayEffect.signalListenMode = signalListenMode;
            questPart_PlayEffect.thing = thing;
            questPart_PlayEffect.effecter = effecter;
            questPart_PlayEffect.map = map;
            quest.AddPart(questPart_PlayEffect);
            return questPart_PlayEffect;
        }

        public static QuestPart_InitiateRaid InitiateRaid(this Quest quest, Pawn pawn, float point, string title, string desc, string inSignal = null, QuestPart.SignalListenMode signalListenMode = QuestPart.SignalListenMode.OngoingOnly)
        {
            QuestPart_InitiateRaid questPart_InitiateRaid = new QuestPart_InitiateRaid();
            questPart_InitiateRaid.inSignal = (inSignal ?? QuestGen.slate.Get<string>("inSignal"));
            questPart_InitiateRaid.pawn = pawn;
            questPart_InitiateRaid.point = point;
            questPart_InitiateRaid.title = title;
            questPart_InitiateRaid.desc = desc;
            quest.AddPart(questPart_InitiateRaid);
            return questPart_InitiateRaid;
        }

        public static QuestPart_InitiateComet InitiateComet(this Quest quest, Pawn pawn, PawnKindDef pawnKindDef, FactionDef factionDef, string inSignal = null, QuestPart.SignalListenMode signalListenMode = QuestPart.SignalListenMode.OngoingOnly)
        {
            QuestPart_InitiateComet questPart_InitiateComet = new QuestPart_InitiateComet();
            questPart_InitiateComet.inSignal = (inSignal ?? QuestGen.slate.Get<string>("inSignal"));
            questPart_InitiateComet.pawn = pawn;
            questPart_InitiateComet.pawnKindDef = pawnKindDef;
            questPart_InitiateComet.factionDef = factionDef;
            quest.AddPart(questPart_InitiateComet);
            return questPart_InitiateComet;
        }

        public static QuestPart_Drysea GenerateDrysea(this Quest quest, Site site, Pawn pawn, string inSignal = null, QuestPart.SignalListenMode signalListenMode = QuestPart.SignalListenMode.OngoingOnly)
        {
            QuestPart_Drysea questPart_Drysea = new QuestPart_Drysea();
            questPart_Drysea.inSignal = (inSignal ?? QuestGen.slate.Get<string>("inSignal"));
            questPart_Drysea.site = site;
            questPart_Drysea.pawn = pawn;
            quest.AddPart(questPart_Drysea);
            return questPart_Drysea;
        }

        public static QuestPart_InitiateScript InitiateScript(this Quest quest, Map map, Pawn pawnOwn, MechanoidScriptDef tree, string inSignal = null, QuestPart.SignalListenMode signalListenMode = QuestPart.SignalListenMode.OngoingOnly)
        {
            QuestPart_InitiateScript questPart_InitiateScript = new QuestPart_InitiateScript();
            questPart_InitiateScript.inSignal = (inSignal ?? QuestGen.slate.Get<string>("inSignal"));
            questPart_InitiateScript.map = map;
            questPart_InitiateScript.pawnOwn = pawnOwn;
            questPart_InitiateScript.tree = tree;
            quest.AddPart(questPart_InitiateScript);
            return questPart_InitiateScript;
        }

        public static QuestPart_BackHome BackHome(this Quest quest, Pawn pawn, string inSignal = null, QuestPart.SignalListenMode signalListenMode = QuestPart.SignalListenMode.OngoingOnly)
        {
            QuestPart_BackHome questPart_BackHome = new QuestPart_BackHome();
            questPart_BackHome.inSignal = (inSignal ?? QuestGen.slate.Get<string>("inSignal"));
            questPart_BackHome.pawn = pawn;
            quest.AddPart(questPart_BackHome);
            return questPart_BackHome;
        }

        public static QuestPart_CheckQuestFail CheckQuestFail(this Quest quest, string inSignal = null, QuestPart.SignalListenMode signalListenMode = QuestPart.SignalListenMode.OngoingOnly)
        {
            QuestPart_CheckQuestFail questPart_CheckQuestFail = new QuestPart_CheckQuestFail();
            questPart_CheckQuestFail.inSignal = (inSignal ?? QuestGen.slate.Get<string>("inSignal"));
            quest.AddPart(questPart_CheckQuestFail);
            return questPart_CheckQuestFail;
        }

        public static QuestPart_GenerateMilitor GenerateMilitor(this Quest quest, Site site, string inSignal = null, QuestPart.SignalListenMode signalListenMode = QuestPart.SignalListenMode.OngoingOnly)
        {
            QuestPart_GenerateMilitor questPart_GenerateMilitor = new QuestPart_GenerateMilitor();
            questPart_GenerateMilitor.site = site;
            questPart_GenerateMilitor.inSignal = (inSignal ?? QuestGen.slate.Get<string>("inSignal"));
            quest.AddPart(questPart_GenerateMilitor);
            return questPart_GenerateMilitor;
        }

        public static QuestPart_StartBattle StartBattle(this Quest quest, Pawn enemy1, Site site, Pawn enemy2, string inSignal = null, QuestPart.SignalListenMode signalListenMode = QuestPart.SignalListenMode.OngoingOnly)
        {
            QuestPart_StartBattle questPart_StartBattle = new QuestPart_StartBattle();
            questPart_StartBattle.inSignal = (inSignal ?? QuestGen.slate.Get<string>("inSignal"));
            questPart_StartBattle.site = site;
            questPart_StartBattle.enemy1 = enemy1;
            questPart_StartBattle.enemy2 = enemy2;
            quest.AddPart(questPart_StartBattle);
            return questPart_StartBattle;
        }

        public static QuestPart_AddIntel AddIntel(this Quest quest, string inSignal = null, QuestPart.SignalListenMode signalListenMode = QuestPart.SignalListenMode.OngoingOnly)
        {
            QuestPart_AddIntel questPart_AddIntel = new QuestPart_AddIntel();
            questPart_AddIntel.inSignal = (inSignal ?? QuestGen.slate.Get<string>("inSignal"));
            quest.AddPart(questPart_AddIntel);
            return questPart_AddIntel;
        }
    }
}
