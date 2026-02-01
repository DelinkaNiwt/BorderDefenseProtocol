using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using System;
using Verse;
using Verse.Sound;
using RimWorld;
using Verse.AI.Group;
using RimWorld.QuestGen;

namespace GD3
{
    public class QuestPart_InitiateScript : QuestPart
    {
        public string inSignal;

        public Map map;

        public Pawn pawnOwn;

        public MechanoidScriptDef tree;

        public override void Notify_QuestSignalReceived(Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);
            if (signal.tag == inSignal)
            {
                ScriptTree s = tree.scriptTree[0];
                List<Pawn> pawns = pawnOwn.Map.mapPawns.AllPawns.FindAll(p => p.Faction != null && p.Faction == Faction.OfPlayer);
                pawns.SortBy(p => p.Position.DistanceTo(pawnOwn.Position));
                Pawn pawn = pawns[0];
                Find.WindowStack.Add(new CommunicationWindow_BlackMech(s.title, s.dialogue, s.graphic, s.drawSize, s.drawOffset, map, pawn, s.buttons, tree.scriptTree, 0));
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref map, "map");
            Scribe_References.Look(ref pawnOwn, "pawnOwn");
            Scribe_Defs.Look(ref tree, "tree");
            Scribe_Values.Look(ref inSignal, "inSignal");
        }
    }
}