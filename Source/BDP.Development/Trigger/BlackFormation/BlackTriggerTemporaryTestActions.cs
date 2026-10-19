using BDP.Content.Trigger.Black.Formation;
using LudeonTK;
using RimWorld;
using Verse;

namespace BDP.Development.Trigger.BlackFormation
{
    /// <summary>临时测试黑触发器生成入口。</summary>
    public static class BlackTriggerTemporaryTestActions
    {
        private const string TemporaryTestDefName = "BDP_TriggerBody_BlackTest";

        /// <summary>在当前玩家殖民者附近生成临时测试体。</summary>
        [DebugAction("Mods", "BDP：黑触发器：生成临时测试体", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.Playing, displayPriority = -230)]
        public static void SpawnTemporaryTestTrigger()
        {
            Pawn pawn = Find.Selector?.SingleSelectedThing as Pawn;
            if (pawn == null || !pawn.IsColonistPlayerControlled || !pawn.Spawned || pawn.Map == null)
            {
                Messages.Message("BDP_BlackTrigger_Debug_NoPawn".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }
            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(TemporaryTestDefName);
            if (def == null) { Messages.Message("BDP_BlackTrigger_Test_DefMissing".Translate(), MessageTypeDefOf.RejectInput, false); return; }
            Thing trigger = ThingMaker.MakeThing(def);
            if (!GenPlace.TryPlaceThing(trigger, pawn.Position, pawn.Map, ThingPlaceMode.Near))
            {
                if (!trigger.Destroyed) trigger.Destroy(DestroyMode.Vanish);
                Messages.Message("BDP_BlackTrigger_Test_SpawnFailed".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }
            Messages.Message("BDP_BlackTrigger_Test_Spawned".Translate(trigger.LabelCap), MessageTypeDefOf.PositiveEvent, false);
        }
    }
}
