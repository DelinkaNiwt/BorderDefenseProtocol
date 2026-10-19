using System.Runtime.CompilerServices;
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
        /// 颜色记录只随 Pawn 实例存活，不强持有已离图且不再绘制的角色。
        /// </summary>
        private static readonly ConditionalWeakTable<Pawn, ColorOverrideEntry> Overrides =
            new ConditionalWeakTable<Pawn, ColorOverrideEntry>();

        /// <summary>
        /// 为 Pawn 注册一次命中反馈颜色覆盖。
        /// </summary>
        internal static void Register(Pawn pawn, Color color)
        {
            if (pawn == null)
            {
                return;
            }

            Overrides.Remove(pawn);
            Overrides.Add(pawn, new ColorOverrideEntry
            {
                Color = color,
                LastAppliedTick = CurrentTick()
            });
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
