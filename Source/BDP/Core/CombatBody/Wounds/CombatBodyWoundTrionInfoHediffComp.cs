using Verse;

namespace BDP.Core.CombatBody.Wounds
{
    /// <summary>
    /// 战斗体伤口 Trion 流失提示组件。
    /// 它只给原版伤口提示补充 BDP 流失信息，不参与伤口计算或 Trion 账本发布。
    /// </summary>
    public sealed class CombatBodyWoundTrionInfoHediffComp : HediffComp
    {
        /// <summary>供外部显示层读取本伤口已发布流失；不重算、不发布或续期来源。</summary>
        public bool TryGetPublishedDrainPerSecond(out float drainPerSecond)
        {
            return CombatBodyWoundTrionDrainUtility.TryResolvePublishedDrainPerSecond(parent, out drainPerSecond);
        }

        /// <summary>
        /// 给伤口悬停提示追加当前 Trion 流失。
        /// 没有正数流失时返回 null，让普通伤口提示保持原样。
        /// </summary>
        public override string CompTipStringExtra
        {
            get
            {
                if (!TryGetPublishedDrainPerSecond(out float drainPerSecond))
                {
                    return null;
                }

                return "BDP_Hediff_CombatBody_TrionDrain".Translate(
                    drainPerSecond.ToString("F1"));
            }
        }
    }
}
