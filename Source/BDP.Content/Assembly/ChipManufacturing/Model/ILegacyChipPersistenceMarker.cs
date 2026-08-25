namespace BDP.Content.Assembly.ChipManufacturing.Model
{
    /// <summary>标记物品是否命中旧版或缺失当前组合记录的制造芯片持久化状态。</summary>
    public interface ILegacyChipPersistenceMarker
    {
        /// <summary>旧版字段存在或当前组合记录缺失时为 true。</summary>
        bool LegacyPersistenceDetected { get; }
    }
}
