using System;
using Verse;
using RimWorld;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using Verse.Sound;

namespace GD3
{
    public class ExostriderDummy : ThingWithComps
    {
        protected override void Tick()
        {
            base.Tick();
            if (ticks < 60000)
            {
                ticks++;
                if (ticks == 20000)
                {
                    helps.Add("DropPod");
                    helps.Add("DropPod");
                    helps.Add("DropPod");
                }
                else if (ticks == 40000)
                {
                    helps.Add("Bombardment");
                }
                else if (ticks == 60000)
                {
                    helps.Add("SuperBeam");
                    helps.Add("SuperBeam");
                    helps.Add("SuperBeam");
                    helps.Remove("Beam");
                    helps.Remove("Bombardment");
                }
            }
        }

        public void DoHelp(Exostrider exo, List<string> overrideHelps = null)
        {
            List<Pawn> pawns = Map.mapPawns.AllPawns.FindAll(p => p.Faction != null && p.HostileTo(Faction.OfMechanoids));
            if (pawns.Count == 0)
            {
                return;
            }
            Pawn victim = pawns.RandomElement();
            IntVec3 vec = victim.Position;

            string str = overrideHelps == null ? helps.RandomElement() : overrideHelps.RandomElement();
            if (str == "Beam")
            {
                Messages.Message("GD.ExoBeamAttack".Translate(), new TargetInfo(vec, Map), MessageTypeDefOf.NegativeEvent);
                PowerBeam obj = (PowerBeam)GenSpawn.Spawn(ThingDefOf.PowerBeam, vec, Map);
                obj.duration = 600;
                obj.instigator = exo;
                obj.weaponDef = null;
                obj.StartStrike();
            }
            else if (str == "Bombardment")
            {
                Messages.Message("GD.ExoBombardmentAttack".Translate(), new TargetInfo(vec, Map), MessageTypeDefOf.NegativeEvent);
                Bombardment obj = (Bombardment)GenSpawn.Spawn(ThingDefOf.Bombardment, vec, Map);
                obj.duration = 540;
                obj.instigator = exo;
                obj.weaponDef = null;
            }
            else if (str == "DropPod")
            {
                StorytellerComp storytellerComp = Find.Storyteller.storytellerComps.First((StorytellerComp x) => x is StorytellerComp_OnOffCycle || x is StorytellerComp_RandomMain);
                IncidentParms parms = storytellerComp.GenerateParms(IncidentCategoryDefOf.ThreatBig, Map);
                parms.faction = Find.FactionManager.FirstFactionOfDef(FactionDefOf.Mechanoid);
                parms.points = 20000f;
                parms.raidStrategy = RaidStrategyDefOf.ImmediateAttack;
                parms.raidArrivalMode = GDDefOf.RandomDrop;
                parms.customLetterLabel = "GD.ExoMechanoids".Translate();
                parms.customLetterText = "GD.ExoMechanoidsDesc".Translate();
                IncidentDefOf.RaidEnemy.Worker.TryExecute(parms);
            }
            else if (str == "SuperBeam")
            {
                Messages.Message("GD.ExoSuperBeamAttack".Translate(), MessageTypeDefOf.NegativeEvent);
                for (int i = 0; i < pawns.Count; i++)
                {
                    PowerBeam obj = (PowerBeam)GenSpawn.Spawn(ThingDefOf.PowerBeam, pawns[i].Position, Map);
                    obj.duration = 600;
                    obj.instigator = exo;
                    obj.weaponDef = null;
                    obj.StartStrike();
                }
                for (int i = 0; i < 8; i++)
                {
                    PowerBeam obj = (PowerBeam)GenSpawn.Spawn(ThingDefOf.PowerBeam, Map.AllCells.RandomElement(), Map);
                    obj.duration = 600;
                    obj.instigator = exo;
                    obj.weaponDef = null;
                    obj.StartStrike();
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ticks, "ticks");
            Scribe_Collections.Look(ref helps, "helps", LookMode.Value, Array.Empty<object>());
        }

        public int ticks;

        public List<string> helps = new List<string>() {"Beam"};
    }
}
