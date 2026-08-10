using System;

namespace BDP.Core.CombatBody
{
    /// <summary>
    /// 战斗体阶段事件口。
    /// 这里只广播阶段变化，不持有阶段真值。
    /// </summary>
    public interface ICombatBodyEvents
    {
        /// <summary>
        /// 当战斗体外层阶段发生变化时触发。
        /// 例如 Inactive -> Active、Active -> Collapsing、Active -> Inactive。
        /// </summary>
        event Action<CombatBodyPhaseChangedArgs> PhaseChanged;
    }
}
