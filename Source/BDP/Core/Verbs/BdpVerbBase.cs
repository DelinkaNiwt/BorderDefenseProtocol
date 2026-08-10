using BDP.Core.Semantics;
using Verse;

namespace BDP.Core.Verbs
{
    /// <summary>
    /// BDP 自定义 Verb 的最小公共基类。
    /// 它只负责承载攻击语义，不主动接管原版攻击流程。
    /// </summary>
    public abstract class BdpVerbBase : Verb, IBdpSemanticCarrier
    {
        /// <summary>
        /// 当前这次攻击携带的语义。
        /// 只有直接继承 `Verb` 的 BDP 原生 Verb 适合复用这里。
        /// </summary>
        public ISemanticContext SemanticContext { get; set; }
    }
}
