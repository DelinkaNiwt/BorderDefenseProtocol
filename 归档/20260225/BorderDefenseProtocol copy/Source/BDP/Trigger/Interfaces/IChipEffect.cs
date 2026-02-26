using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 芯片效果的统一协议——扩展点。
    /// 新增芯片类型只需实现此接口，不修改任何现有代码（OCP）。
    ///
    /// 设计约定：
    ///   · 无状态：实现类不持有运行时数据，所有状态存在Pawn/Hediff/CompTriggerBody中
    ///   · 幂等：Activate()可安全重复调用（读档恢复时使用）
    ///   · 无参构造：通过Activator.CreateInstance实例化，必须有无参构造函数
    /// </summary>
    public interface IChipEffect
    {
        /// <summary>激活：芯片开始输出效果。</summary>
        void Activate(Pawn pawn, Thing triggerBody);

        /// <summary>关闭：芯片停止输出效果，清理所有副作用。</summary>
        void Deactivate(Pawn pawn, Thing triggerBody);

        /// <summary>每tick逻辑（可选，默认空实现）。</summary>
        void Tick(Pawn pawn, Thing triggerBody);

        /// <summary>前置条件检查：当前是否可以激活（供UI灰显和ActivateChip前置检查）。</summary>
        bool CanActivate(Pawn pawn, Thing triggerBody);
    }
}
