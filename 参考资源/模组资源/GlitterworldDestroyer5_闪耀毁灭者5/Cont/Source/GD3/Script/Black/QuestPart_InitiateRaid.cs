using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.Sound;
using RimWorld;
using Verse.AI.Group;

namespace GD3
{
    public class QuestPart_InitiateRaid : QuestPart
    {
        public string inSignal;

        public Pawn pawn;

        public float point;

        public string title;

        public string desc;

        public override void Notify_QuestSignalReceived(Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);
            if (signal.tag == inSignal)
            {
                StorytellerComp storytellerComp = Find.Storyteller.storytellerComps.First((StorytellerComp x) => x is StorytellerComp_OnOffCycle || x is StorytellerComp_RandomMain);
                IncidentParms parms = storytellerComp.GenerateParms(IncidentCategoryDefOf.ThreatBig, pawn.Map);
                parms.faction = Find.FactionManager.FirstFactionOfDef(FactionDefOf.Mechanoid);
                parms.points = point;
                parms.raidStrategy = RaidStrategyDefOf.ImmediateAttack;
                parms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn;
                parms.customLetterLabel = title.Translate();
                parms.customLetterText = desc.Translate();
                IncidentDefOf.RaidEnemy.Worker.TryExecute(parms);

                foreach (Lord lord in Find.CurrentMap.lordManager.lords)
                {
                    if (!(lord.CurLordToil is LordToil_Stage item))
                    {
                        continue;
                    }
                    foreach (Transition transition in lord.Graph.transitions)
                    {
                        if (transition.sources.Contains(item) && (transition.target is LordToil_AssaultColony || transition.target is LordToil_AssaultColonyBreaching || transition.target is LordToil_AssaultColonyPrisoners || transition.target is LordToil_AssaultColonySappers || transition.target is LordToil_AssaultColonyBossgroup || transition.target is LordToil_MoveInBossgroup))
                        {
                            if (GDSettings.DeveloperMode)
                            {
                                Log.Warning("Forced Mechanoid Assault");
                            }
                            lord.GotoToil(transition.target);
                            return;
                        }
                    }
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref pawn, "pawn");
            Scribe_Values.Look(ref point, "point");
            Scribe_Values.Look(ref title, "title");
            Scribe_Values.Look(ref desc, "desc");
            Scribe_Values.Look(ref inSignal, "inSignal");
        }
    }
}