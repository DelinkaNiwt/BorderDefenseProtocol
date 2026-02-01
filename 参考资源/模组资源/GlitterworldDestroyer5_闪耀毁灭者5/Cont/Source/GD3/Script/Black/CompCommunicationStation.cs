using System;
using Verse;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GD3
{
    public class CompCommunicationStation : ThingComp
    {
        private CompPowerTrader powerComp => parent.TryGetComp<CompPowerTrader>();

        private CompFlickable flickComp => parent.TryGetComp<CompFlickable>();

        public float Percent => portionYieldPct;

        private int ticks;

        public bool CanOperateNow()
        {
            if (powerComp != null && !powerComp.PowerOn)
            {
                return false;
            }
            if (flickComp != null && !flickComp.SwitchIsOn)
            {
                return false;
            }
            return true;
        }

        public override void CompTick()
        {
            base.CompTick();
            ticks++;
            if (ticks > 65)
            {
                ticks = 0;
                List<Thing> things = parent.Position.GetThingList(parent.Map);
                if (!things.Any(t => t.def == GDDefOf.HiTechResearchBench))
                {
                    Building b = parent as Building;
                    b.Destroy(DestroyMode.Deconstruct);
                }
            }
        }

        public void OperateWorkDone(Pawn user)
        {
            float statValue = user.GetStatValue(StatDefOf.ResearchSpeed);
            portionProgress += statValue;
            portionYieldPct = portionProgress / 10000f;
            if (portionYieldPct > 1f)
            {
                int num = 10;
                if (Find.World.GetComponent<MissionComponent>().ShouldPayTax)
                {
                    num = 9;
                }

                System.Random random = new System.Random();
                int i = random.Next(1, 101);
                if (Find.World.GetComponent<MissionComponent>().firewallLevel == MissionComponent.FirewallLevel.Unstable && i > 50)
                {
                    num *= 10;
                    MoteMaker.ThrowText(this.parent.DrawPos, this.parent.Map, "GD.ExtraInt".Translate(), 5f);
                }

                if (Find.World.GetComponent<MissionComponent>().Advanced && !toggle)
                {
                    Find.World.GetComponent<MissionComponent>().intelligenceAdvanced += num;
                }
                else
                {
                    Find.World.GetComponent<MissionComponent>().intelligencePrimary += num;
                }

                if (Find.World.GetComponent<MissionComponent>().firewallLevel == MissionComponent.FirewallLevel.Unstable && Rand.Chance(0.01f) || Find.World.GetComponent<MissionComponent>().firewallLevel == MissionComponent.FirewallLevel.Alert)
                {
                    DoRaid();
                }

                portionProgress = 0f;
                portionYieldPct = 0f;
            }
        }

        public override string CompInspectStringExtra()
        {
            if (parent.Spawned)
            {
                return "GD.HackProgress".Translate() + ": " + portionYieldPct.ToStringPercent("F0");
            }

            return null;
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            {
                yield return gizmo;
            }
            if (Find.World.GetComponent<MissionComponent>().Advanced)
            {
                Command_Toggle toggle = new Command_Toggle
                {
                    defaultLabel = "GD.ToggleLabel".Translate(this.toggle ? "On".Translate() : "Off".Translate()),
                    defaultDesc = "GD.ToggleDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Buttons/StationToggle", true),
                    toggleAction = delegate ()
                    {
                        this.toggle = !this.toggle;
                    },
                    isActive = (() => this.toggle)
                };
                yield return toggle;
            }
            if (DebugSettings.ShowDevGizmos)
            {
                Command_Action command_Action = new Command_Action();
                command_Action.defaultLabel = "DEV: Add 1000 intelligence-1";
                command_Action.action = delegate
                {
                    Find.World.GetComponent<MissionComponent>().intelligencePrimary += 1000;
                };
                Command_Action command_Action2 = new Command_Action();
                command_Action2.defaultLabel = "DEV: Add 1000 intelligence-2";
                command_Action2.action = delegate
                {
                    Find.World.GetComponent<MissionComponent>().intelligenceAdvanced += 1000;
                };
                Command_Action command_Action3 = new Command_Action();
                command_Action3.defaultLabel = "DEV: Change Firewall";
                command_Action3.action = delegate
                {
                    Find.World.GetComponent<MissionComponent>().ChangeFirewallRandom();
                };
                Command_Action command_Action4 = new Command_Action();
                command_Action4.defaultLabel = "DEV: Discover BlackMech?";
                command_Action4.action = delegate
                {
                    Find.World.GetComponent<MissionComponent>().blackMechDiscoverd = !Find.World.GetComponent<MissionComponent>().blackMechDiscoverd;
                };
                Command_Action command_Action5 = new Command_Action();
                command_Action5.defaultLabel = "DEV: Militor?";
                command_Action5.action = delegate
                {
                    if (Find.World.GetComponent<MissionComponent>().BranchDict != null)
                    {
                        Find.World.GetComponent<MissionComponent>().BranchDict["WillMilitorDie"] = !Find.World.GetComponent<MissionComponent>().BranchDict["WillMilitorDie"];
                    }
                    else
                    {
                        Log.Warning("Haven't touched this branch yet.");
                    }
                };
                yield return command_Action;
                yield return command_Action2;
                yield return command_Action3;
                if (GDSettings.DeveloperMode)
                {
                    yield return command_Action4;
                    yield return command_Action5;
                }
            }
            yield break;
        }

        public void DoRaid()
        {
            StorytellerComp storytellerComp = Find.Storyteller.storytellerComps.First((StorytellerComp x) => x is StorytellerComp_OnOffCycle || x is StorytellerComp_RandomMain);
            IncidentParms parms = storytellerComp.GenerateParms(IncidentCategoryDefOf.ThreatBig, Find.RandomPlayerHomeMap);
            parms.faction = Find.FactionManager.FirstFactionOfDef(FactionDefOf.Mechanoid);
            parms.points *= 1.2f;
            GDDefOf.MechCluster_Giant_Incident.Worker.TryExecute(parms);
            Messages.Message("GD.RevengeRaid".Translate(), MessageTypeDefOf.NegativeEvent, true);
        }

        public override void PostExposeData()
        {
            Scribe_Values.Look(ref portionProgress, "portionProgress", 0f);
            Scribe_Values.Look(ref portionYieldPct, "portionYieldPct", 0f);
            Scribe_Values.Look(ref toggle, "toggle", false);
        }

        private bool toggle;

        private float portionProgress;

        private float portionYieldPct;
    }
}
