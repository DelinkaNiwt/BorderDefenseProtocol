namespace BDP.Core
{
    /// <summary>
    /// 战斗体支持接口。由触发器系统实现,供战斗体系统调用。
    /// 定义在Core层,避免Core依赖Trigger层。
    ///
    /// 设计目的:
    /// - 消除反射调用,提供类型安全的接口
    /// - 解耦Core层和Trigger层的依赖关系
    /// - 提高性能(虚方法调用比反射快10倍)
    /// </summary>
    public interface ICombatBodySupport
    {
        /// <summary>
        /// 尝试为战斗体分配Trion占用量。
        /// 遍历所有槽位,逐个调用CompTrion.Allocate()。
        /// </summary>
        /// <returns>true=至少一个芯片占用成功或无芯片需要占用,false=TrionComp不存在</returns>
        bool TryAllocateForCombatBody();

        /// <summary>
        /// 释放战斗体的Trion占用量。
        /// 调用CompTrion.Release(allocated)。
        /// </summary>
        void ReleaseFromCombatBody();

        /// <summary>
        /// 激活所有特殊槽芯片。
        /// 遍历specialSlots,调用ActivateSlot()。
        /// </summary>
        void ActivateSpecialSlots();

        /// <summary>
        /// 关闭所有特殊槽芯片。
        /// 遍历specialSlots,调用DeactivateSlot()。
        /// </summary>
        void DeactivateSpecialSlots();
    }
}
