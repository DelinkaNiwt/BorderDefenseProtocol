using System.Collections.Generic;
using BDP.Core.Genes;
using BDP.Core.Trion;
using BDP.Core.Trion.Capacity;
using BDP.Core.Trion.Intensity;
using BDP.Content.Trion.Talent.Capacity;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Content.Trion.Talent
{
    /// <summary>
    /// Trion 天赋检测业务的 Pawn 侧状态。
    /// 这是 Content 业务状态，不属于 Core 的 Trion 资源账本。
    /// </summary>
    public sealed class CompTrionTalentAssessment : ThingComp
    {
        /// <summary>
        /// 是否已经完成一次 Trion 天赋检测。
        /// </summary>
        private bool completed;

        /// <summary>
        /// 读取当前 Pawn 是否已经完成检测。
        /// </summary>
        public bool IsCompleted
        {
            get { return completed; }
        }

        /// <summary>
        /// 原子提交一次检测完成状态。
        /// </summary>
        public bool TryMarkCompleted()
        {
            if (completed)
            {
                return false;
            }

            completed = true;
            return true;
        }

        /// <summary>
        /// 保存 Content 侧检测状态。
        /// </summary>
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref completed, "bdpTrionTalentAssessmentCompleted", false);
        }

        /// <summary>
        /// 在检测完成后向角色信息面板追加两项结果。
        /// </summary>
        public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
        {
            IEnumerable<StatDrawEntry> baseStats = base.SpecialDisplayStats();
            if (baseStats != null)
            {
                foreach (StatDrawEntry entry in baseStats)
                {
                    yield return entry;
                }
            }

            Pawn pawn = parent as Pawn;
            if (!completed || pawn == null)
            {
                yield break;
            }

            ITrionReader reader = TrionSurfaceAccess.ResolveReader(pawn);
            if (reader == null)
            {
                yield break;
            }

            TrionCapacityPotentialBandDef band = TrionCapacityPotentialBandResolver.Instance.Resolve(
                reader.TrionCapacityPotential);
            if (band != null)
            {
                yield return new StatDrawEntry(
                    StatCategoryDefOf.BasicsPawn,
                    "BDP_Stat_TrionCapacityPotentialLabel".Translate(),
                    band.LabelCap,
                    band.description,
                    1000);
            }

            // 植入腺体后使用原版 Stat 修正后的有效释放力；未植入时显示先天底数。
            int displayedIntensity = TrionGlandEligibility.HasActiveTrionGland(pawn)
                ? TrionIntensityUtility.GetEffective(pawn)
                : reader.InnateTrionIntensity;
            yield return new StatDrawEntry(
                StatCategoryDefOf.BasicsPawn,
                "BDP_Stat_TrionIntensityLabel".Translate(),
                TrionIntensityUtility.FormatLevel(displayedIntensity),
                "BDP_Stat_TrionIntensityDescription".Translate(),
                999);
        }
    }

    /// <summary>
    /// Trion 天赋检测状态的 Pawn Comp 配置。
    /// </summary>
    public sealed class CompProperties_TrionTalentAssessment : CompProperties
    {
        /// <summary>
        /// 构造并绑定 Content 侧检测状态组件。
        /// </summary>
        public CompProperties_TrionTalentAssessment()
        {
            compClass = typeof(CompTrionTalentAssessment);
        }
    }
}
