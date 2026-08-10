namespace BDP.Core.Chips
{
    /// <summary>
    /// 芯片定义层承认的正式声明块种类。
    /// 它用于统一说明“这枚芯片声明了哪些块”。
    /// </summary>
    internal enum ChipDefinitionDeclaredBlock
    {
        /// <summary>
        /// 画像声明块。
        /// </summary>
        Profile,

        /// <summary>
        /// 装载声明块。
        /// </summary>
        Loadout,

        /// <summary>
        /// 表达声明块。
        /// </summary>
        Expression,

        /// <summary>
        /// Trion 声明块。
        /// </summary>
        Trion,

        /// <summary>
        /// 激活条件声明块。
        /// </summary>
        ActivationRequirements,

        /// <summary>
        /// 扩展说明块。
        /// </summary>
        Extensions
    }
}
