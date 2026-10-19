using System.Collections.Generic;
using RimWorld;
using Verse;

namespace BDP.Content.Trigger.Black.Formation
{
    /// <summary>主动燃尽生命与 Development 调试入口共享的准备上下文。</summary>
    public static class BlackTriggerSacrificeService
    {
        private static readonly Dictionary<string, BlackTriggerPreparedAttempt> preparedAttempts = new Dictionary<string, BlackTriggerPreparedAttempt>();
        private static readonly HashSet<string> debugContexts = new HashSet<string>();

        /// <summary>判断人物是否正由主动燃尽流程处理。</summary>
        public static bool IsInSacrificeContext(Pawn pawn)
        {
            return pawn != null && preparedAttempts.ContainsKey(pawn.GetUniqueLoadID());
        }

        /// <summary>判断人物是否来自 Development 调试入口。</summary>
        public static bool IsInDebugContext(Pawn pawn)
        {
            return pawn != null && debugContexts.Contains(pawn.GetUniqueLoadID());
        }

        /// <summary>在杀死人前先冻结并确认确实存在候选。</summary>
        public static bool TryPrepare(Pawn pawn, bool debug, out string failureReasonKey)
        {
            failureReasonKey = null;
            if (pawn == null) { failureReasonKey = "BDP_BlackTrigger_Formation_SourceInvalid"; return false; }
            string loadId = pawn.GetUniqueLoadID();
            if (preparedAttempts.ContainsKey(loadId)) return false;
            BlackTriggerPreparedAttempt attempt;
            if (!BlackTriggerFormationWorldService.TryPrepare(pawn, BlackTriggerFormationSource.Sacrifice, null, null, debug, false, out attempt, out failureReasonKey)) return false;
            preparedAttempts.Add(loadId, attempt);
            if (debug) debugContexts.Add(loadId);
            return true;
        }

        /// <summary>供死亡补丁读取已经验证的准备结果。</summary>
        public static BlackTriggerPreparedAttempt GetPrepared(Pawn pawn)
        {
            BlackTriggerPreparedAttempt attempt;
            return pawn != null && preparedAttempts.TryGetValue(pawn.GetUniqueLoadID(), out attempt) ? attempt : null;
        }

        /// <summary>无论死亡是否完成，都清理一次性上下文。</summary>
        public static void EndContext(Pawn pawn)
        {
            if (pawn == null) return;
            string loadId = pawn.GetUniqueLoadID();
            preparedAttempts.Remove(loadId);
            debugContexts.Remove(loadId);
        }

        /// <summary>Development 入口：绕过危机、概率和冷却，但无候选时绝不杀人。</summary>
        public static void ExecuteDebug(Pawn pawn)
        {
            string failureReasonKey;
            if (!TryPrepare(pawn, true, out failureReasonKey))
            {
                string key = failureReasonKey.NullOrEmpty() ? "BDP_BlackTrigger_Sacrifice_NoCompatibleTrigger" : failureReasonKey;
                Messages.Message(key.Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }
            try
            {
                pawn.Kill(null, null);
            }
            finally
            {
                EndContext(pawn);
            }
        }
    }
}
