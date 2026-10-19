using BDP.Content.FloatingFlight;
using BDP.Content.FloatingFlight.Pathing;
using LudeonTK;
using Verse;

namespace BDP.Development.FloatingFlight
{
    /// <summary>浮空功能的开发者调试入口，所有状态只作用于当前调试运行。</summary>
    public static class FloatingFlightDebugActions
    {
        /// <summary>把当前选中 Pawn 的非玩家临时模式设为关闭。</summary>
        [DebugAction("Mods", "BDP：浮空调试：关闭", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.Playing, displayPriority = -200)]
        public static void SetOff()
        {
            SetSelected(FloatingFlightMode.Off);
        }

        /// <summary>把当前选中 Pawn 的非玩家临时模式设为仅移动浮空。</summary>
        [DebugAction("Mods", "BDP：浮空调试：仅移动", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.Playing, displayPriority = -201)]
        public static void SetMoveOnly()
        {
            SetSelected(FloatingFlightMode.MoveOnly);
        }

        /// <summary>把当前选中 Pawn 的非玩家临时模式设为始终浮空。</summary>
        [DebugAction("Mods", "BDP：浮空调试：始终浮空", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.Playing, displayPriority = -202)]
        public static void SetAlways()
        {
            SetSelected(FloatingFlightMode.Always);
        }

        /// <summary>清除当前选中 Pawn 的非玩家临时模式覆盖。</summary>
        [DebugAction("Mods", "BDP：浮空调试：清除覆盖", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.Playing, displayPriority = -203)]
        public static void ClearOverride()
        {
            Pawn pawn = SelectedPawn();
            FloatingFlightNonPlayerModeProvider.Instance.ClearMode(pawn);
            LogSnapshot(pawn);
        }

        /// <summary>输出当前选中 Pawn 的浮空状态快照。</summary>
        [DebugAction("Mods", "BDP：浮空调试：输出状态", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.Playing, displayPriority = -204)]
        public static void PrintSnapshot()
        {
            LogSnapshot(SelectedPawn());
        }

        /// <summary>设置当前选中 Pawn 的临时模式并输出快照。</summary>
        private static void SetSelected(FloatingFlightMode mode)
        {
            Pawn pawn = SelectedPawn();
            FloatingFlightNonPlayerModeProvider.Instance.SetMode(pawn, mode);
            LogSnapshot(pawn);
        }

        /// <summary>读取当前单选 Pawn。</summary>
        private static Pawn SelectedPawn()
        {
            return Find.Selector?.SingleSelectedThing as Pawn;
        }

        /// <summary>输出不改变正式状态的最小浮空诊断快照。</summary>
        private static void LogSnapshot(Pawn pawn)
        {
            if (pawn == null)
            {
                Log.Message("[BDP] 浮空调试：当前没有单选 Pawn。");
                return;
            }

            bool hasRecord = FloatingFlightLedgerService.TryGetRecord(
                pawn,
                out FloatingFlightRecord record);
            Log.Message(
                "[BDP] 浮空调试 "
                + pawn.LabelShort
                + " capability=" + FloatingFlightCapabilityResolver.HasCapability(pawn)
                + " preferred=" + FloatingFlightModeResolver.ResolvePreferredMode(pawn)
                + " effective=" + FloatingFlightModeResolver.ResolveEffectiveMode(pawn)
                + " path=" + FloatingFlightPathUtility.ResolvePathKind(pawn)
                + " hediff=" + FloatingFlightUtility.IsActuallyFlying(pawn)
                + " roof=" + (pawn.Map != null && pawn.Position.Roofed(pawn.Map))
                + " pendingLanding=" + (hasRecord && record.PendingSafeLanding));
        }
    }
}
