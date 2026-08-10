namespace BDP.Core.Trigger
{
    /// <summary>
    /// Trigger 槽位禁用原因码。
    /// 它属于正式读取结果的一部分，用来表达“为什么当前槽位不可用”。
    /// </summary>
    public enum TriggerDisableReason
    {
        /// <summary>
        /// 当前没有禁用原因。
        /// </summary>
        None = 0,

        /// <summary>
        /// 宿主缺失了该侧继续可用所需的关键身体部位。
        /// 当前阶段先统一覆盖手、臂、肩这类基础缺失。
        /// </summary>
        MissingRequiredBodyPart = 1,

        /// <summary>
        /// 当前战斗体不可用。
        /// 它仍然属于正式禁用态，只是来源不是身体缺失。
        /// </summary>
        CombatBodyUnavailable = 2
    }
}
