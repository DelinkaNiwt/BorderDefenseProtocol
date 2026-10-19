using BDP.Core.AttackExecution;
using BDP.Content.HitPartPolicy;
using Verse;

namespace BDP.Content.RangedModules.VitalHit
{
    /// <summary>
    /// 要害命中模块的 Content 配置快照。
    /// </summary>
    public sealed class VitalHitConfig : RangedModuleConfigNode
    {
        /// <summary>
        /// 命中要害候选部位的概率，范围为 0 到 1。
        /// </summary>
        public float VitalHitChance = 1f;

        /// <summary>
        /// 当前模块使用的命中部位策略；默认保持原有的偏好要害行为。
        /// </summary>
        public HitPartSelectionMode SelectionMode = HitPartSelectionMode.PreferVital;

        /// <summary>精确选择的身体部位定义；只在精确模式消费。</summary>
        public BodyPartDef ExactBodyPartDef;

        /// <summary>完整身体树中同名部位的出现序号。</summary>
        public int ExactBodyPartOccurrenceIndex = -1;

        /// <summary>
        /// 复制当前要害命中配置。
        /// </summary>
        public override RangedModuleConfigNode Clone()
        {
            return CloneTyped();
        }

        /// <summary>
        /// 生成强类型配置快照。
        /// </summary>
        public VitalHitConfig CloneTyped()
        {
            return new VitalHitConfig
            {
                VitalHitChance = VitalHitChance,
                SelectionMode = SelectionMode,
                ExactBodyPartDef = ExactBodyPartDef,
                ExactBodyPartOccurrenceIndex = ExactBodyPartOccurrenceIndex
            };
        }
    }
}
