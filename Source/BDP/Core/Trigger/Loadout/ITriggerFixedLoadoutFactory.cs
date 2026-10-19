using Verse;

namespace BDP.Core.Trigger
{
    /// <summary>
    /// 固定装载实体芯片工厂。
    /// Core只定义成品创建边界，不认识具体芯片功能预设或Content程序集类型。
    /// </summary>
    public interface ITriggerFixedLoadoutFactory
    {
        /// <summary>
        /// 按实体芯片Def和可选初始化键创建一枚已经完成初始化的实体芯片。
        /// 工厂不处理槽位装入；调用方会继续执行统一固定装载和回滚流程。
        /// </summary>
        bool TryCreate(
            ThingDef chipDef,
            string initializationKey,
            out Thing chip);
    }
}
