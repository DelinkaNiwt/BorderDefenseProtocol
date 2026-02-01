using System;
using RimWorld;
using RimWorld.Planet;
using Verse;
using System.Collections.Generic;
using System.Linq;

namespace GD3
{
	public class MainComponent : WorldComponent
	{
		public Pawn Mechanitor
        {
            get
            {
				if (!ModsConfig.BiotechActive)
                {
					return null;
                }
				List<Map> list = Find.Maps;
				for (int i = 0; i < list.Count; i++)
				{
					Map map = list[i];
					if (map != null)
					{
						IEnumerable<Pawn> list2 = map.mapPawns.AllPawnsSpawned.Where((Pawn p) => MechanitorUtility.IsMechanitor(p));
						if (list2.Any())
						{
							return list2.First();
						}
					}
				}
				return null;
			}
        }

		public bool CanReinforce => GenTicks.TicksGame > reinforceTick + 60000 * 10;

		public MainComponent(World world) : base(world)
		{
		}

		public override void WorldComponentTick()
		{
			bool flag = !GDSettings.ReinforceNotApply && (Find.TickManager.TicksGame & 1023) == 511;
			if (flag)
            {
				if (DebugSettings.ShowDevGizmos)
                {
					this.num = 0;
					List<Map> list = Find.Maps;
					for (int i = 0; i < list.Count; i++)
					{
						Map map = list[i];
						if (map.IsPlayerHome)
						{
							this.num += map.wealthWatcher.WealthTotal;
						}
					}
					if (this.num < 100 * 10000)
					{
						triggered2 = false;
					}
				}
				if (GDSettings.DeveloperMode)
                {
					Log.Message("GlitterWorld Destroyer: Now Reinforce Condition: " + this.num + "||" + triggered2);
				}
				if (!triggered2)
				{
					if (WealthUtility.PlayerWealth > 100 * 10000)
					{
						triggered2 = true;
						Find.LetterStack.ReceiveLetter("ReinforcedTip".Translate(), "ReinforcedTipDesc".Translate(), LetterDefOf.NeutralEvent);
					}
				}
			}
			if (!ModsConfig.BiotechActive)
            {
				return;
            }
			/*bool flag2 = !this.triggered && Find.ResearchManager.GetProgress(GDDefOf.GD3_Weapons) >= GDDefOf.GD3_Weapons.baseCost && (Find.TickManager.TicksGame & 1023) == 512;
			if (flag2)
			{
				this.DoTrigger();
			}*/
			if (GDSettings.NotNeedToResearch)
            {
				if (Find.ResearchManager.GetProgress(GDDefOf.GD3_Puzzle) < GDDefOf.GD3_Puzzle.baseCost)
                {
					Find.ResearchManager.FinishProject(GDDefOf.GD3_Puzzle);
				}
				return;
            }
			if (this.Mechanitor != null && (Find.TickManager.TicksGame & 1023) == 512)
            {
				if (Find.ResearchManager.GetProgress(GDDefOf.GD3_Puzzle) < GDDefOf.GD3_Puzzle.baseCost)
                {
					this.TrySpawnQuest(GDDefOf.GD_Puzzle, this.Mechanitor.Map);
				}
            }
		}

        #region 旧版本赠送机械邮件方法
        private void DoTrigger()
		{
			if (this.Mechanitor != null)
            {
				Pawn pawn = this.Mechanitor;
				this.triggered = true;
				GenPlace.TryPlaceThing(ThingMaker.MakeThing(GDDefOf.GD_DescriptionChip, null), pawn.Position, pawn.Map, ThingPlaceMode.Near, null, null, default(Rot4));
				Find.LetterStack.ReceiveLetter("BlackTip".Translate(), "BlackTipDesc".Translate(string.Format("{0}", pawn.Name)), LetterDefOf.NeutralEvent);
			}
		}
        #endregion

        private void TrySpawnQuest(QuestScriptDef quest, Map map)
        {
			puzzleFlag = false;
			List<Quest> list = Find.QuestManager.QuestsListForReading;
			if (list.Count > 0)
            {
				for (int i = 0; i < list.Count; i++)
				{
					Quest q = list[i];
					if (q.root.defName == "GD_Puzzle" && (q.State == QuestState.NotYetAccepted || q.State == QuestState.Ongoing || q.State == QuestState.EndedSuccess))
                    {
						puzzleFlag = true;
                    }
				}
			}
			if (puzzleFlag)
            {
				return;
            }
			QuestUtility.SendLetterQuestAvailable(QuestUtility.GenerateQuestAndMakeAvailable(quest, new IncidentParms
			{
				target = map,
				points = StorytellerUtility.DefaultThreatPointsNow(map)
			}.points));
		}

		public bool ClusterAssistanceAvailable(Map map, out WorldObject artillery)
		{
			artillery = null;
			if (!map.Tile.Valid)
            {
				return false;
            }
			return (artillery = Find.WorldObjects.AllWorldObjectsOnLayer(map.Tile.Layer).Find(obj => obj.def == GDDefOf.GD_AllyCluster)) != null;
		}

		public override void ExposeData()
		{
			Scribe_Values.Look<bool>(ref this.triggered, "triggered", false, false);
			Scribe_Values.Look<bool>(ref triggered2, "triggered2", false, false);
			Scribe_Values.Look<int>(ref defeated, "defeated", 0, false);
			Scribe_Values.Look<float>(ref this.num, "num", 0, false);
			Scribe_Values.Look<bool>(ref this.puzzleFlag, "puzzleFlag", false, false);
			Scribe_Collections.Look<string>(ref list_str, "list_str", LookMode.Value, Array.Empty<object>());
			Scribe_Values.Look(ref reinforceTick, "reinforceTick", -1);
			Scribe_Values.Look(ref artilleryStrikeNumber, "artilleryStrikeNumber", -1);
			Scribe_Values.Look(ref artilleryStrikeCooldown, "artilleryStrikeCooldown", -1);
		}

		public float num = 0;

		public int defeated = 0;

		private bool triggered = false;

		public bool triggered2 = false;

		private bool puzzleFlag = false;

		public List<string> list_str = new List<string>();

		public int reinforceTick = -1;

		public int artilleryStrikeNumber = -1;

		public int artilleryStrikeCooldown = -1;
	}
}
