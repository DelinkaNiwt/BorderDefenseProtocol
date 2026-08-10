namespace BDP.Core.CombatBody
{
    /// <summary>
    /// 战斗体外层相位。
    /// 第一版只先稳定这层状态，不提前把内部细节混进来。
    /// </summary>
    public enum CombatBodyPhase
    {
        // 当前未处于战斗体状态。
        Inactive,
        // 当前已进入战斗体状态。
        Active,
        // 当前处于崩解表现阶段。
        Collapsing,
        // 当前处于退出后的冷却阶段。
        Cooldown
    }
}
