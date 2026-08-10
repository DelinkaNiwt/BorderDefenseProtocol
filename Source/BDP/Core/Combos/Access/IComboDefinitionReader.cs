namespace BDP.Core.Combos
{
    /// <summary>
    /// 组合技定义正式读取口。
    /// 主模组内部其它系统应统一从这里读组合技声明结果。
    /// </summary>
    internal interface IComboDefinitionReader
    {
        /// <summary>
        /// 读取指定 ComboDef 的正式组合技定义结果。
        /// </summary>
        ComboDefinitionReadResult Read(ComboDef comboDef);

        /// <summary>
        /// 按 DefName 读取指定组合技的正式定义结果。
        /// </summary>
        ComboDefinitionReadResult Read(string defName);
    }
}
