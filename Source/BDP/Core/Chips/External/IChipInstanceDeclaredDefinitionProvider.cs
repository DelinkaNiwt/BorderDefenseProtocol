namespace BDP.Core.Chips
{
    /// <summary>
    /// 为芯片实例可选提供应用构型前的基础定义。
    /// Core 不关心基础定义来自制造记录还是其它正式内容来源。
    /// </summary>
    public interface IChipInstanceDeclaredDefinitionProvider
    {
        /// <summary>尝试读取当前实例的基础芯片定义。</summary>
        bool TryGetDeclaredChipDefinition(out ChipDefinitionConfig definition);
    }
}
