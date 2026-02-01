using System;
using System.Collections.Generic;
using System.Linq;
using Verse.Sound;
using Verse;
using RimWorld;
using System.Text;
using UnityEngine;

namespace GD3
{
	public class CompAnalyzableSubcore : CompAnalyzable
	{
		public new CompProperties_CompAnalyzableSubcore Props => (CompProperties_CompAnalyzableSubcore)props;

		public override int AnalysisID => Props.analysisID;

		public override AcceptanceReport CanInteract(Pawn activateBy = null, bool checkOptionalItems = true)
		{
			AcceptanceReport result = base.CanInteract(activateBy, checkOptionalItems);
			if (!result.Accepted)
			{
				return result;
			}
			if (activateBy == null)
			{
				return false;
			}
			if (activateBy.skills == null || activateBy.skills.GetSkill(SkillDefOf.Intellectual).PermanentlyDisabled || activateBy.skills.GetSkill(SkillDefOf.Intellectual).Level < 10)
			{
				return "GD.LevelNotEnoughToAnalyse".Translate();
			}
			if ((Find.TickManager.TicksGame - this.lastTick) <= 20000)
            {
				return "GD.SubcoreCooldown".Translate(((20000f - (float)Find.TickManager.TicksGame + (float)this.lastTick) / 2500f).ToString("F1"));
            }
			return true;
		}

        public override void CompTick()
        {
            base.CompTick();
			Find.AnalysisManager.TryGetAnalysisProgress(AnalysisID, out AnalysisDetails details);
			Thing t = this.parent;
			if (details != null && details.Satisfied)
            {
				this.destroyTick++;
				if (destroyTick > 120)
				{
					QuestUtility.SendQuestTargetSignals(t.questTags, "Analyzed", t.Named("SUBJECT"));
				}
			}
		}

        public override void OnAnalyzed(Pawn pawn)
		{
			base.OnAnalyzed(pawn);
			this.lastTick = Find.TickManager.TicksGame;
			Find.AnalysisManager.TryGetAnalysisProgress(AnalysisID, out AnalysisDetails details);
			if (details == null)
            {
				return;
            }
			Thing t = this.parent;
			Effecter effecter = GDDefOf.ApocrionAoeWarmup.SpawnAttached(t, t.MapHeld, 1f);
			effecter.Trigger(t, t, -1);
			effecter.Cleanup();
			SoundDefOf.MechSerumUsed.PlayOneShot(new TargetInfo(t.PositionHeld, t.MapHeld, false));
			System.Random random = new System.Random();
			int i = random.Next(0, 100);
			if (i >= pawn.skills.GetSkill(SkillDefOf.Intellectual).Level * 5)
			{
				int j = random.Next(0, 100);

				if (j <= 20)
				{
					StorytellerComp storytellerComp = Find.Storyteller.storytellerComps.First((StorytellerComp x) => x is StorytellerComp_OnOffCycle || x is StorytellerComp_RandomMain);
					IncidentParms parms = storytellerComp.GenerateParms(IncidentCategoryDefOf.ThreatBig, Find.CurrentMap);
					parms.faction = Find.FactionManager.FirstFactionOfDef(FactionDefOf.Mechanoid);
					parms.points *= 0.6f;
					if (parms.points < 100f)
					{
						parms.points = 100f;
					}
					parms.raidStrategy = RaidStrategyDefOf.ImmediateAttack;
					parms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeDrop;
					parms.customLetterLabel = "GD.SubcoreRaidTitle".Translate();
					parms.customLetterText = "GD.SubcoreRaidDesc".Translate();
					IncidentDefOf.RaidEnemy.Worker.TryExecute(parms);
				}

				else if (j <= 60)
				{
					if (details.timesDone >= 1)
					{
						details.timesDone -= 1; 
					}
					Find.LetterStack.ReceiveLetter("GD.SubcoreBadEvent1Title".Translate(), "GD.SubcoreBadEvent1Desc".Translate(), LetterDefOf.NeutralEvent, new LookTargets(this.parent));
					FleckCreationData dataStatic = FleckMaker.GetDataStatic(t.DrawPos, t.Map, FleckDefOf.MicroSparksFast, 1f);
					t.Map.flecks.CreateFleck(dataStatic);
					FleckMaker.ThrowFireGlow(this.parent.DrawPos, this.parent.Map, 1f);
				}

				else
				{
					if (details.timesDone >= 1)
					{
						details.timesDone -= 1;
					}
					Find.LetterStack.ReceiveLetter("GD.SubcoreBadEvent2Title".Translate(), "GD.SubcoreBadEvent2Desc".Translate(), LetterDefOf.NeutralEvent, new LookTargets(this.parent));
					FleckCreationData dataStatic = FleckMaker.GetDataStatic(t.DrawPos, t.Map, FleckDefOf.MicroSparksFast, 1f);
					t.Map.flecks.CreateFleck(dataStatic);
					FleckMaker.ThrowSmoke(this.parent.DrawPos, this.parent.Map, 1.4f);
				}
			}
		}

        public override string CompInspectStringExtra()
        {
			Find.AnalysisManager.TryGetAnalysisProgress(AnalysisID, out AnalysisDetails details);
			if (details == null)
            {
				return null;
            }
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("GD.SubcoreProgress".Translate(details.timesDone, details.required));
			if ((Find.TickManager.TicksGame - this.lastTick) <= 20000)
            {
				stringBuilder.AppendLine();
				stringBuilder.Append("GD.SubcoreCooldown".Translate(((20000f - (float)Find.TickManager.TicksGame + (float)this.lastTick) / 2500f).ToString("F1")));
			}
			return stringBuilder.ToString();
		}

		public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			Find.AnalysisManager.TryGetAnalysisProgress(AnalysisID, out AnalysisDetails details);
			if (details.timesDone == 10)
            {
				Command_Action command_Action4 = new Command_Action();
				command_Action4.defaultLabel = "GD.SendSubcore".Translate();
				command_Action4.defaultDesc = "GD.SendSubcoreDesc".Translate();
				command_Action4.icon = ContentFinder<Texture2D>.Get("UI/Buttons/SubcoreLend", true);
				command_Action4.action = delegate
				{
					Thing t = this.parent;
					QuestUtility.SendQuestTargetSignals(t.questTags, "Analyzed", t.Named("SUBJECT"));
				};
				yield return command_Action4;
			}
			if (DebugSettings.ShowDevGizmos)
			{
				Command_Action command_Action = new Command_Action();
				command_Action.defaultLabel = "DEV: Reset last tick";
				command_Action.action = delegate
				{
					lastTick = -30000;
				};
				Command_Action command_Action2 = new Command_Action();
				command_Action2.defaultLabel = "DEV: Raid";
				command_Action2.action = delegate
				{
					StorytellerComp storytellerComp = Find.Storyteller.storytellerComps.First((StorytellerComp x) => x is StorytellerComp_OnOffCycle || x is StorytellerComp_RandomMain);
					IncidentParms parms = storytellerComp.GenerateParms(IncidentCategoryDefOf.ThreatBig, Find.CurrentMap);
					parms.faction = Find.FactionManager.FirstFactionOfDef(FactionDefOf.Mechanoid);
					parms.points *= 0.6f;
					if (parms.points < 100f)
                    {
						parms.points = 100f;
                    }
					parms.raidStrategy = RaidStrategyDefOf.ImmediateAttack;
					parms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeDrop;
					parms.customLetterLabel = "GD.SubcoreRaidTitle".Translate();
					parms.customLetterText = "GD.SubcoreRaidDesc".Translate();
					IncidentDefOf.RaidEnemy.Worker.TryExecute(parms);
				};
				Command_Action command_Action3 = new Command_Action();
				command_Action3.defaultLabel = "DEV: Stage to 9";
				command_Action3.action = delegate
				{
					details.timesDone = 9;
				};
				yield return command_Action;
				yield return command_Action2;
				yield return command_Action3;
			}
			yield break;
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look<int>(ref lastTick, "lastTick", 0, false);
			Scribe_Values.Look<int>(ref destroyTick, "destroyTick", 0, false);
		}

		public int lastTick = -30000;

		private int destroyTick;
	}
}
