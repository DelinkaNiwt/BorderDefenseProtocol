using LudeonTK;
using RimWorld;
using Verse;

namespace BDP.Development.Trigger.ExactBodyPartRemoval
{
    /// <summary>
    /// 开发者精确部位移除测试触发体的生成入口。
    /// </summary>
    public static class ExactBodyPartRemovalDebugActions
    {
        /// <summary>
        /// 开发者测试触发体的 ThingDef 名称。
        /// </summary>
        private const string TestTriggerDefName = "BDP_TriggerBody_DeveloperWeapon";

        /// <summary>
        /// 在当前选中玩家殖民者附近生成预装测试芯片的完整触发体。
        /// </summary>
        [DebugAction(
            "Mods",
            "BDP：开发者：生成精确部位移除测试体",
            actionType = DebugActionType.Action,
            allowedGameStates = AllowedGameStates.Playing,
            displayPriority = -240)]
        public static void SpawnTestTrigger()
        {
            Pawn pawn = Find.Selector?.SingleSelectedThing as Pawn;
            if (pawn == null
                || !pawn.IsColonistPlayerControlled
                || !pawn.Spawned
                || pawn.Map == null)
            {
                Messages.Message(
                    "BDP_ExactBodyPartRemoval_Debug_NoPawn".Translate(),
                    MessageTypeDefOf.RejectInput,
                    false);
                return;
            }

            ThingDef triggerDef = DefDatabase<ThingDef>.GetNamedSilentFail(TestTriggerDefName);
            if (triggerDef == null)
            {
                Messages.Message(
                    "BDP_ExactBodyPartRemoval_Debug_DefMissing".Translate(),
                    MessageTypeDefOf.RejectInput,
                    false);
                return;
            }

            Thing trigger = ThingMaker.MakeThing(triggerDef);
            if (!GenPlace.TryPlaceThing(trigger, pawn.Position, pawn.Map, ThingPlaceMode.Near))
            {
                if (!trigger.Destroyed)
                {
                    trigger.Destroy(DestroyMode.Vanish);
                }

                Messages.Message(
                    "BDP_ExactBodyPartRemoval_Debug_SpawnFailed".Translate(),
                    MessageTypeDefOf.RejectInput,
                    false);
                return;
            }

            Messages.Message(
                "BDP_ExactBodyPartRemoval_Debug_Spawned".Translate(trigger.LabelCap),
                MessageTypeDefOf.PositiveEvent,
                false);
        }
    }
}
