using System.Collections.Generic;

namespace BDP.Core.Information
{
    /// <summary>
    /// 芯片功能来源的只读取值 Worker 契约。
    /// Worker 只定位并读取事实，不决定功能身份，也不修改游戏对象。
    /// </summary>
    public interface IChipFeatureSourceWorker
    {
        /// <summary>
        /// 从给定来源和读取上下文生成类型化属性贡献。
        /// </summary>
        IReadOnlyList<ChipFeatureValueContribution> Resolve(
            ChipFeatureSourceConfig source,
            ChipFeatureReadContext context);
    }
}
