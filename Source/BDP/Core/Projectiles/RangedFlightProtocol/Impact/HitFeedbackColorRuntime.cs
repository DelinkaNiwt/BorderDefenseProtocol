using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace BDP.Core.Projectiles.RangedFlightProtocol.Impact
{
    /// <summary>
    /// 保存短暂的逐 Pawn 命中反馈颜色覆盖。
    /// 它只服务原版受击闪烁表现，不参与伤害或目标判定。
    /// </summary>
    internal static class HitFeedbackColorRuntime
    {
        /// <summary>
        /// 原版受击闪烁持续的总 Tick 数。
        /// </summary>
        private const int HitFlashTicksTotal = 16;

        /// <summary>
        /// 逐 Pawn 保存的颜色覆盖记录。
        /// </summary>
        private sealed class ColorOverrideEntry
        {
            /// <summary>
            /// 当前 Pawn 使用的命中反馈颜色。
            /// </summary>
            public Color Color;

            /// <summary>
            /// 最近一次写入颜色的游戏 Tick。
            /// </summary>
            public int LastAppliedTick;
        }

        /// <summary>
        /// 当前仍处于受击闪烁窗口的 Pawn 颜色记录。
        /// </summary>
        private static readonly Dictionary<Pawn, ColorOverrideEntry> Overrides =
            new Dictionary<Pawn, ColorOverrideEntry>();

        /// <summary>
        /// 为 Pawn 注册一次命中反馈颜色覆盖。
        /// </summary>
        internal static void Register(Pawn pawn, Color color)
        {
            if (pawn == null)
            {
                return;
            }

            Overrides[pawn] = new ColorOverrideEntry
            {
                Color = color,
                LastAppliedTick = CurrentTick()
            };
        }

        /// <summary>
        /// 读取 Pawn 当前是否仍在颜色覆盖窗口内。
        /// </summary>
        internal static bool TryGetColor(Pawn pawn, out Color color)
        {
            color = Color.white;
            if (pawn == null)
            {
                return false;
            }

            ColorOverrideEntry entry;
            if (!Overrides.TryGetValue(pawn, out entry) || entry == null)
            {
                return false;
            }

            int currentTick = CurrentTick();
            if (currentTick > entry.LastAppliedTick + HitFlashTicksTotal)
            {
                Overrides.Remove(pawn);
                return false;
            }

            color = entry.Color;
            return true;
        }

        /// <summary>
        /// 读取当前游戏 Tick；启动期没有 TickManager 时回退为零。
        /// </summary>
        private static int CurrentTick()
        {
            return Find.TickManager != null ? Find.TickManager.TicksGame : 0;
        }
    }
}
