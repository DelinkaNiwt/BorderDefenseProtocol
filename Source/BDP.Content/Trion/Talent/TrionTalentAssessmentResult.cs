using BDP.Core.Trion.Intensity;
using BDP.Content.Trion.Talent.Capacity;
using Verse;

namespace BDP.Content.Trion.Talent
{
    /// <summary>
    /// 检测资格或提交操作的统一结果。
    /// </summary>
    public sealed class TrionTalentAssessmentResult
    {
        /// <summary>操作是否成功。</summary>
        public bool Succeeded { get; private set; }

        /// <summary>失败原因或成功提示。</summary>
        public string Message { get; private set; }

        /// <summary>成功时解析到的容量潜质模糊档位。</summary>
        public TrionCapacityPotentialBandDef CapacityPotentialBand { get; private set; }

        /// <summary>成功时检测出的先天 Trion 释放力。</summary>
        public int TrionIntensity { get; private set; }

        /// <summary>创建失败结果。</summary>
        public static TrionTalentAssessmentResult Fail(string message)
        {
            return new TrionTalentAssessmentResult { Succeeded = false, Message = message };
        }

        /// <summary>创建资格通过但尚未提交的结果。</summary>
        public static TrionTalentAssessmentResult Eligible()
        {
            return new TrionTalentAssessmentResult { Succeeded = true };
        }

        /// <summary>创建检测成功结果。</summary>
        public static TrionTalentAssessmentResult Success(
            TrionCapacityPotentialBandDef capacityPotentialBand,
            int trionIntensity)
        {
            return new TrionTalentAssessmentResult
            {
                Succeeded = true,
                CapacityPotentialBand = capacityPotentialBand,
                TrionIntensity = trionIntensity,
                Message = capacityPotentialBand == null
                    ? null
                    : "BDP_Message_TrionTalent_AssessmentComplete".Translate(
                        capacityPotentialBand.LabelCap,
                        TrionIntensityUtility.FormatLevel(trionIntensity))
            };
        }
    }
}
