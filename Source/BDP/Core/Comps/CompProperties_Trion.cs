using UnityEngine;
using Verse;

namespace BDP.Core
{
    /// <summary>
    /// CompTrion的XML可配置参数。
    /// 所有数值通过XML Def配置，C#只实现机制框架。
    /// </summary>
    public class CompProperties_Trion : CompProperties
    {
        // ── 容量与初始化 ──

        /// <summary>非Pawn载体的基础最大容量。Pawn载体此值被Stat聚合覆盖。</summary>
        public float baseMax = 0f;

        /// <summary>初始Trion百分比（1.0=满）。</summary>
        public float startPercent = 1.0f;

        // ── 建筑/物品专用 ──

        /// <summary>每天被动消耗量（建筑维持消耗）。0=无被动消耗。</summary>
        public float passiveDrainPerDay = 0f;

        /// <summary>每天自动恢复量。Pawn的恢复由CompTrion.CompTick()自驱动（通过Stat读取）。</summary>
        public float recoveryPerDay = 0f;

        // ── 显示 ──

        /// <summary>是否在选中时显示Gizmo资源条。</summary>
        public bool showGizmo = true;

        /// <summary>可用段资源条颜色（明亮青绿，RGBA）。</summary>
        public Color barColor = new Color(0.3f, 0.85f, 0.55f, 1.0f);

        /// <summary>占用段资源条颜色（暗青，RGBA）。</summary>
        public Color allocatedBarColor = new Color(0.25f, 0.4f, 0.45f, 1.0f);

        // ── Pawn专用 ──

        // ── 刷新与结算间隔 ──

        /// <summary>从Stat系统刷新max的tick间隔。250 ticks ≈ 4秒。</summary>
        public int statRefreshInterval = 250;

        /// <summary>聚合消耗（drainRegistry）结算间隔（ticks）。60 ticks ≈ 1秒。</summary>
        public int drainSettleInterval = 60;

        /// <summary>Pawn恢复间隔（ticks）。150 ticks = 原NeedInterval周期。</summary>
        public int recoveryInterval = 150;

        public CompProperties_Trion()
        {
            compClass = typeof(CompTrion);
        }
    }
}
