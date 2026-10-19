using BDP.Core.Information;
using BDP.Content.Information.Features.Workers;

namespace BDP.Content.Information.Features.Sources
{
    /// <summary>从 Ability 效果组件读取一套近战攻击方式信息。</summary>
    public sealed class AbilityMeleeAttackFeatureSourceConfig : ExpressionEntryFeatureSourceConfig
    {
        /// <summary>当前 Ability 中按声明顺序读取的攻击效果序号。</summary>
        public int EffectIndex;

        /// <summary>默认使用 Ability 近战攻击信息 Worker。</summary>
        public AbilityMeleeAttackFeatureSourceConfig()
        {
            WorkerClass = typeof(AbilityMeleeAttackFeatureSourceWorker);
        }
    }
}
