namespace BDP.Content.Assembly.ChipManufacturing.Model
{
    /// <summary>
    /// Content 中持有芯片组合记录的统一业务边界。
    /// </summary>
    public interface IChipCombinationRecordHolder
    {
        /// <summary>读取当前对象持有的组合记录。</summary>
        ChipCombinationRecord CombinationRecord { get; }
    }
}
