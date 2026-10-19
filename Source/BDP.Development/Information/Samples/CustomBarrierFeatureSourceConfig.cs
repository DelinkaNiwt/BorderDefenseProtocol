using BDP.Core.Information;

namespace BDP.Development.Information.Samples
{
    /// <summary>第三方自定义护盾实现的最小来源配置样例。</summary>
    public sealed class CustomBarrierFeatureSourceConfig : ChipFeatureSourceConfig
    {
        /// <summary>提供自定义护盾属性值的 Development 业务规则 DefName。</summary>
        public string RuleDefName;
    }
}
