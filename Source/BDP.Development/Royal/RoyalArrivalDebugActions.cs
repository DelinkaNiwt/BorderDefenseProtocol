using BDP.Content.Royal.Quest;
using LudeonTK;
using RimWorld;
using Verse;

namespace BDP.Development.Royal
{
    /// <summary>独立开发入口，通过正常任务生成和接受流程测试皇室入口。</summary>
    public static class RoyalArrivalDebugActions
    {
        /// <summary>只生成可接受任务，不直接招募、派兵或替玩家缔约。</summary>
        [DebugAction("BDP", requiresBiotech = true, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static DebugActionNode TaskMenu()
        {
            var root = new DebugActionNode("BDP_Dev_RoyalArrivalMenu".Translate());
            root.AddChild(new DebugActionNode("BDP_Dev_RoyalArrivalGenerate".Translate(), action: Generate));
            return root;
        }

        /// <summary>准入也经过正式节点，便于验证有皇室、已有任务和无敌对派系等边界。</summary>
        private static void Generate()
        {
            QuestScriptDef definition = DefDatabase<QuestScriptDef>.GetNamed(QuestNode_Root_RoyalArrival.QuestDefName);
            Map map = Find.CurrentMap;
            float points = StorytellerUtility.DefaultThreatPointsNow(map);
            if (!definition.CanRun(points, map))
            {
                Messages.Message("BDP_Dev_RoyalArrivalUnavailable".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }
            QuestUtility.GenerateQuestAndMakeAvailable(definition, points);
            Messages.Message("BDP_Dev_RoyalArrivalGenerated".Translate(), MessageTypeDefOf.PositiveEvent, false);
        }
    }
}
