using System;
using BDP.Core.Semantics;

namespace BDP.Core.CombatBody
{
    /// <summary>
    /// 战斗体阶段变化事件参数。
    /// 它只表达“阶段发生了什么变化”，不替代阶段真值本身。
    /// </summary>
    public sealed class CombatBodyPhaseChangedArgs : EventArgs
    {
        /// <summary>
        /// 变化前的阶段。
        /// </summary>
        public CombatBodyPhase PreviousPhase;

        /// <summary>
        /// 变化后的阶段。
        /// </summary>
        public CombatBodyPhase CurrentPhase;

        /// <summary>
        /// 变化后当前锁定的 Trion 量。
        /// </summary>
        public float AllocatedTrion;

        /// <summary>
        /// 如果这次变化与崩解有关，这里记录原因。
        /// 其他变化可以为 null。
        /// </summary>
        public string Reason;

        /// <summary>
        /// 当前阶段变化携带的语义上下文。
        /// 第一版先重点用于承接崩裂触发原因。
        /// </summary>
        public ISemanticContext SemanticContext;
    }
}
