using BDP.Core.Trion;
using RimWorld;

namespace BDP.Core.Genes
{
    /// <summary>容量属性只限制显示，计算与内部资源读取仍保留真值。</summary>
    public sealed class StatWorker_TrionCapacity : StatWorker
    {
        /// <summary>业务允许显示容量时继续沿用原版属性显示资格。</summary>
        public override bool ShouldShowFor(StatRequest req)
        {
            return TrionResourceRuleRegistry.GetRules(req.Thing).CapacityInfoVisible
                && base.ShouldShowFor(req);
        }
    }
}
