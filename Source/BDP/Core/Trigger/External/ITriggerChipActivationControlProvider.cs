using Verse;

namespace BDP.Core.Trigger
{
    /// <summary>
    /// Trigger 芯片分类提供器。
    /// Core 只定义可插拔的类别查询能力，不认识具体内容程序集 DefName。
    /// </summary>
    public interface ITriggerChipActivationControlProvider
    {
        /// <summary>
        /// 判断指定芯片是否拥有独立激活控制范围。
        /// </summary>
        bool HasIndependentActivationControl(Thing chip);
    }
}
