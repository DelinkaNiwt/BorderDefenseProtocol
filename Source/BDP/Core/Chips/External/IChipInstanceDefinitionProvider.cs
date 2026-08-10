namespace BDP.Core.Chips
{
    /// <summary>
    /// 为某个芯片实例提供当前运行定义的中性契约。
    /// Core 不关心定义来自制造组合、存档还是其它正式内容。
    /// </summary>
    public interface IChipInstanceDefinitionProvider
    {
        /// <summary>
        /// 尝试读取当前实例的芯片定义。
        /// </summary>
        bool TryGetChipDefinition(out ChipDefinitionConfig definition);
    }
}
