using System.Collections.Generic;

namespace BDP.Core.Admission
{
    /// <summary>
    /// 中性的字符串集合准入规则。
    /// 它只描述允许任一、必须全部和禁止任一，不解释候选值的业务含义。
    /// </summary>
    public sealed class ValueSetAdmissionRule
    {
        /// <summary>非空时，候选集合至少需要命中一项。</summary>
        public List<string> AllowedAny = new List<string>();

        /// <summary>候选集合必须包含这里声明的全部项目。</summary>
        public List<string> RequiredAll = new List<string>();

        /// <summary>候选集合命中任意一项即拒绝。</summary>
        public List<string> DeniedAny = new List<string>();
    }
}
