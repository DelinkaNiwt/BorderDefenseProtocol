using RimWorld;
using Verse;

namespace BDP.Content.Trion.Talent.Jobs
{
    /// <summary>
    /// Trion 双人检测使用的工作定义引用。
    /// </summary>
    [DefOf]
    public static class TrionTalentAssessmentJobDefOf
    {
        /// <summary>操作员检测工作。</summary>
        public static JobDef BDP_TrionTalentAssessment;

        /// <summary>受检者配合等待工作。</summary>
        public static JobDef BDP_WaitForTrionTalentAssessment;

        /// <summary>固定检测仪的研究员操作工作。</summary>
        public static JobDef BDP_OperateTrionDetector;

        /// <summary>确保定义引用完成初始化。</summary>
        static TrionTalentAssessmentJobDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(TrionTalentAssessmentJobDefOf));
        }
    }
}
