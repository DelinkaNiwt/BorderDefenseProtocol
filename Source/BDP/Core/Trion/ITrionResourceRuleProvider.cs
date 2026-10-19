using Verse;

namespace BDP.Core.Trion
{
    /// <summary>具体业务提供宿主规则，Core 只使用决策结果。</summary>
    public interface ITrionResourceRuleProvider
    {
        /// <summary>读取当前宿主的规则；非业务对象应返回默认规则。</summary>
        TrionResourceRules GetRules(Thing owner);
    }
}
