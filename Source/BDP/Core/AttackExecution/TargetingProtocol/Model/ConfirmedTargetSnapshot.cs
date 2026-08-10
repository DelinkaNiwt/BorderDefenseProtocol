using Verse;

namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// 已确认目标冻结快照。
    /// 它把导航目标与语义目标拆成两条中性真值，供执行链分别读取。
    /// </summary>
    public sealed class ConfirmedTargetSnapshot : IAttackContextNode
    {
        /// <summary>
        /// 当前确认结果冻结后的导航目标。
        /// 它服务首段物理导航、`LOS（直线可视命中）` 与宿主发射方向，不负责表达更细的业务语义。
        /// </summary>
        public LocalTargetInfo NavigationTarget { get; set; }

        /// <summary>
        /// <summary>
        /// 当前确认结果冻结后的语义目标。
        /// 它服务命中语义、`dual（双侧）` 另一侧目标与后半段 `intended target（意图目标）`。
        /// </summary>
        public LocalTargetInfo SemanticTarget { get; set; }

        /// <summary>
        /// 复制当前冻结目标节点。
        /// 冻结到攻击上下文时使用复制体，避免共享运行态引用。
        /// </summary>
        public IAttackContextNode Clone()
        {
            return new ConfirmedTargetSnapshot
            {
                NavigationTarget = NavigationTarget,
                SemanticTarget = SemanticTarget
            };
        }

        /// <summary>
        /// 统一序列化当前冻结目标节点。
        /// </summary>
        public void ExposeData()
        {
            LocalTargetInfo navigationTarget = NavigationTarget;
            LocalTargetInfo semanticTarget = SemanticTarget;
            Scribe_TargetInfo.Look(ref navigationTarget, "navigationTarget");
            Scribe_TargetInfo.Look(ref semanticTarget, "semanticTarget");
            NavigationTarget = navigationTarget;
            SemanticTarget = semanticTarget;
        }
    }
}
