namespace BDP.Core.Trigger
{
    /// <summary>
    /// 触发体芯片配置的玩家控制边界。
    /// 这是 Core 的中性能力定义，不代表具体触发器类别。
    /// </summary>
    public enum TriggerLoadoutControlMode
    {
        /// <summary>
        /// 玩家可以通过正式装配入口装入、卸下和调整芯片。
        /// </summary>
        PlayerConfigurable,

        /// <summary>
        /// 玩家不能通过装配入口修改芯片配置。
        /// </summary>
        PlayerNonConfigurable
    }
}
