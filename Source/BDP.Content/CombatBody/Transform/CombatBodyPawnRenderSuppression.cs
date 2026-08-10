using System.Collections.Generic;
using Verse;

namespace BDP.Content.CombatBody.Transform
{
    /// <summary>
    /// 扫描交接期间对原版完整人物绘制的短时替代状态。
    /// </summary>
    internal static class CombatBodyPawnRenderSuppression
    {
        /// <summary>
        /// 当前各 Pawn 生效中的替代状态。
        /// </summary>
        private static readonly Dictionary<int, SuppressionState> ActiveByPawnId =
            new Dictionary<int, SuppressionState>();

        /// <summary>
        /// 下一枚替代令牌。
        /// </summary>
        private static int nextToken;

        /// <summary>
        /// 为指定 Pawn 注册一次短时完整绘制替代。
        /// </summary>
        internal static int Begin(Pawn pawn, int timeoutTicks)
        {
            if (pawn == null)
            {
                return 0;
            }

            int token = NextToken();
            int currentTick = Find.TickManager?.TicksGame ?? 0;
            ActiveByPawnId[pawn.thingIDNumber] = new SuppressionState(
                token,
                currentTick + timeoutTicks);
            return token;
        }

        /// <summary>
        /// 判断指定 Pawn 的原版完整人物绘制当前是否由扫描快照接管。
        /// </summary>
        internal static bool ShouldSuppress(Pawn pawn)
        {
            if (pawn == null
                || (Find.UIRoot != null && Find.UIRoot.HideMotes)
                || !ActiveByPawnId.TryGetValue(pawn.thingIDNumber, out SuppressionState state))
            {
                return false;
            }

            int currentTick = Find.TickManager?.TicksGame ?? 0;
            if (currentTick > state.ExpirationTick)
            {
                ActiveByPawnId.Remove(pawn.thingIDNumber);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 只在令牌仍属于同一轮扫描时恢复指定 Pawn 的原版绘制。
        /// </summary>
        internal static void End(Pawn pawn, int token)
        {
            if (pawn == null
                || token == 0
                || !ActiveByPawnId.TryGetValue(pawn.thingIDNumber, out SuppressionState state)
                || state.Token != token)
            {
                return;
            }

            ActiveByPawnId.Remove(pawn.thingIDNumber);
        }

        /// <summary>
        /// 生成非零替代令牌。
        /// </summary>
        private static int NextToken()
        {
            unchecked
            {
                nextToken++;
                if (nextToken == 0)
                {
                    nextToken++;
                }
            }

            return nextToken;
        }

        /// <summary>
        /// 单个 Pawn 当前生效中的完整绘制替代数据。
        /// </summary>
        private sealed class SuppressionState
        {
            /// <summary>
            /// 创建一条短时完整绘制替代状态。
            /// </summary>
            internal SuppressionState(int token, int expirationTick)
            {
                Token = token;
                ExpirationTick = expirationTick;
            }

            /// <summary>
            /// 本轮扫描的唯一令牌。
            /// </summary>
            internal int Token { get; }

            /// <summary>
            /// 防止异常残留的截止游戏 tick。
            /// </summary>
            internal int ExpirationTick { get; }
        }
    }
}
