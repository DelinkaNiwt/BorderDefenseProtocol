namespace BDP.Trigger
{
    /// <summary>
    /// 组合效果类型枚举。
    /// 用于标识不同类型的组合效果（Verb/Ability/Hediff）。
    /// </summary>
    public enum ComboEffectType
    {
        /// <summary>攻击动作（生成第4个攻击 Gizmo）。</summary>
        Verb,

        /// <summary>技能（授予原版 Ability）。</summary>
        Ability,

        /// <summary>被动效果（授予 Hediff）。</summary>
        Hediff
    }
}
