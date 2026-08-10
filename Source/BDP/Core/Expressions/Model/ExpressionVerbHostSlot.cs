namespace BDP.Core.Expressions
{
    /// <summary>
    /// 表达系统正式承认的固定 Verb 宿主入口。
    /// </summary>
    public enum ExpressionVerbHostSlot
    {
        /// <summary>
        /// 当前条目不对应任何 Verb 宿主入口。
        /// </summary>
        None = 0,

        /// <summary>
        /// 主侧主攻击 Verb 入口。
        /// </summary>
        MainPrimaryVerb = 1,

        /// <summary>
        /// 主侧副攻击 Verb 入口。
        /// </summary>
        MainSecondaryVerb = 2,

        /// <summary>
        /// 副侧主攻击 Verb 入口。
        /// </summary>
        SubPrimaryVerb = 3,

        /// <summary>
        /// 副侧副攻击 Verb 入口。
        /// </summary>
        SubSecondaryVerb = 4,

        /// <summary>
        /// 双武器主攻击 Verb 入口。
        /// </summary>
        DualPrimaryVerb = 5,

        /// <summary>
        /// 双武器副攻击 Verb 入口。
        /// </summary>
        DualSecondaryVerb = 6,

        /// <summary>
        /// 组合技主攻击 Verb 入口。
        /// </summary>
        ComboPrimaryVerb = 7,

        /// <summary>
        /// 组合技副攻击 Verb 入口。
        /// </summary>
        ComboSecondaryVerb = 8
    }
}
