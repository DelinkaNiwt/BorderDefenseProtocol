using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.Sound;
using UnityEngine;
using RimWorld.Planet;

namespace GD3
{
	public class CompUseEffect_StealIntelligence : CompUseEffect
	{
		public Building Server
		{
			get
			{
				return this.parent as Building;
			}
		}

        public override void CompTick()
        {
            base.CompTick();
			ticks++;
			if (ticks > 259)
            {
				ticks = 0;
				servers = Server.Map.listerBuildings.allBuildingsNonColonist.FindAll((Building b) => b.def.defName == "MechServer_True");
			}
        }

        public override AcceptanceReport CanBeUsedBy(Pawn p)
        {
			AcceptanceReport result = base.CanBeUsedBy(p);
			if (stolen == true)
            {
				result = "GD.ServerStolen".Translate();
            }
			if (!Find.World.GetComponent<MissionComponent>().keyGained)
            {
				result = "GD.KeyNeeded".Translate();
            }
			return result;
        }

        public override void DoEffect(Pawn user)
		{
			base.DoEffect(user);
			stolen = !stolen;
			GDDefOf.PuzzleTrigger.PlayOneShot(new TargetInfo(Server.PositionHeld, Server.MapHeld, false));
			List<Building> list = servers.FindAll((Building b) => b.TryGetComp<CompUseEffect_StealIntelligence>().stolen);
			MoteMaker.ThrowText(Server.DrawPos + new Vector3(0, 0, 0.3f), Server.Map, "GD.ServerStealComplete".Translate(list.Count, 25));
			if (25 == list.Count)
            {
				List<Thing> things = Server.Map.listerThings.AllThings.FindAll(t => t.def == GDDefOf.GD_ServerDummy);
				if (things.Count > 0)
				{
					GDUtility.SendSignal(GDUtility.GetQuestOfThing(things[0]), "serverStolen");
					GDDefOf.GD_Morse_STOLEN.PlayOneShotOnCamera();
				}
				/*List<Quest> quests = Find.QuestManager.QuestsListForReading;
				for (int i = 0; i < quests.Count; i++)
				{
					Quest quest = quests[i];
					if (quest.root.defName == "GD_Quest_Cluster_Fortress" && quest.State == QuestState.Ongoing && quest.QuestLookTargets.Contains((Site)parent.Map.Parent))
					{
						quest.End(QuestEndOutcome.Success, true, true);
						MissionComponent component = Find.World.GetComponent<MissionComponent>();
						component.intelligenceAdvanced += 2500;
					}
				}*/
			}
			if (GDSettings.DeveloperMode)
			{
				Log.Warning(list.Count.ToString() + " || " + servers.Count.ToString());
			}
		}

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            {
                yield return gizmo;
            }
            if (DebugSettings.ShowDevGizmos)
            {
                Command_Action command_Action = new Command_Action();
                command_Action.defaultLabel = "DEV: Stolen";
                command_Action.action = delegate
                {
					List<Thing> things = Server.Map.listerThings.AllThings.FindAll(t => t.def == GDDefOf.GD_ServerDummy);
					if (things.Count > 0)
					{
						GDUtility.SendSignal(GDUtility.GetQuestOfThing(things[0]), "serverStolen");
						GDDefOf.GD_Morse_STOLEN.PlayOneShotOnCamera();
					}
				};
				Command_Action command_Action2 = new Command_Action();
				command_Action2.defaultLabel = "DEV: Stole this one";
				command_Action2.action = delegate
				{
					Pawn user = Server.Map.mapPawns.AllPawnsSpawned.ToList().Find(p => p.IsColonist);
					DoEffect(user);
				};
				yield return command_Action;
				yield return command_Action2;
            }
            yield break;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
			Scribe_Values.Look<bool>(ref stolen, "stolen", false, false);
			Scribe_Collections.Look<Building>(ref servers, "servers", LookMode.Reference, Array.Empty<object>());
		}

		private int ticks;

		public bool stolen;

		private List<Building> servers;
    }
}
