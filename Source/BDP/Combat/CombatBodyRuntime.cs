using BDP.Combat.Snapshot;
using BDP.Core;
using Verse;

namespace BDP.Combat
{
    /// <summary>
    /// 战斗体运行时聚合体。
    /// 持有战斗体的全部运行时子系统，提供统一访问API。
    ///
    /// 设计目的:
    /// - 消除外部代码对Gene_TrionGland内部结构的直接依赖
    /// - 提供清晰的战斗体运行时数据访问接口
    /// - 简化外部代码的查找逻辑（CombatBodyRuntime.Of(pawn)）
    ///
    /// 职责:
    /// - 聚合战斗体子系统（State, Snapshot）
    /// - 提供统一的激活/解除接口
    /// - 提供静态查找方法（Of(pawn)）
    ///
    /// 非职责:
    /// - 不参与序列化（Gene负责序列化各子系统）
    /// - 不持有业务逻辑（委托给Orchestrator）
    ///
    /// 重构说明:
    /// - 已删除ShadowHP和PartDestruction字段（重构后不再需要）
    /// </summary>
    public class CombatBodyRuntime
    {
        // ═══════════════════════════════════════════
        //  子系统引用（由Gene设置，不可变）
        // ═══════════════════════════════════════════

        /// <summary>战斗体状态聚合器</summary>
        public CombatBodyState State { get; }

        /// <summary>战斗体快照</summary>
        public CombatBodySnapshot Snapshot { get; }

        // ═══════════════════════════════════════════
        //  内部引用
        // ═══════════════════════════════════════════

        private readonly Pawn pawn;
        private readonly Gene_TrionGland gene;
        private readonly CombatBodyOrchestrator orchestrator;

        // ═══════════════════════════════════════════
        //  构造函数（由Gene调用）
        // ═══════════════════════════════════════════

        /// <summary>
        /// 构造战斗体运行时聚合体。
        /// 由Gene_TrionGland在PostAdd/PostLoadInit中调用。
        /// </summary>
        public CombatBodyRuntime(
            Pawn pawn,
            Gene_TrionGland gene,
            CombatBodyState state,
            CombatBodySnapshot snapshot,
            CombatBodyOrchestrator orchestrator)
        {
            this.pawn = pawn;
            this.gene = gene;
            this.State = state;
            this.Snapshot = snapshot;
            this.orchestrator = orchestrator;
        }

        // ═══════════════════════════════════════════
        //  公开查询
        // ═══════════════════════════════════════════

        /// <summary>
        /// 战斗体是否处于激活状态。
        /// </summary>
        public bool IsActive => State?.IsActive ?? false;

        /// <summary>
        /// 获取基因定义（供Orchestrator访问配置）。
        /// </summary>
        public GeneDef GeneDef => gene?.def;

        // ═══════════════════════════════════════════
        //  公开操作
        // ═══════════════════════════════════════════

        /// <summary>
        /// 尝试激活战斗体。
        /// 委托给Orchestrator执行完整的激活流程。
        /// </summary>
        /// <returns>true=激活成功，false=激活失败</returns>
        public bool TryActivate()
        {
            if (orchestrator == null)
            {
                Log.Error($"[BDP] CombatBodyRuntime.TryActivate: orchestrator为null");
                return false;
            }
            return orchestrator.TryActivate(pawn, this);
        }

        /// <summary>
        /// 解除战斗体。
        /// 委托给Orchestrator执行完整的解除流程。
        /// </summary>
        /// <param name="isEmergency">是否为紧急脱离</param>
        public void Deactivate(bool isEmergency = false)
        {
            if (orchestrator == null)
            {
                Log.Error($"[BDP] CombatBodyRuntime.Deactivate: orchestrator为null");
                return;
            }
            orchestrator.Deactivate(pawn, this, isEmergency);
        }

        // ═══════════════════════════════════════════
        //  静态查找（核心便利方法）
        // ═══════════════════════════════════════════

        /// <summary>
        /// 从Pawn获取战斗体运行时聚合体。
        /// 这是外部代码访问战斗体系统的统一入口。
        /// </summary>
        /// <param name="pawn">目标Pawn</param>
        /// <returns>战斗体运行时聚合体，如果Pawn没有Trion腺体则返回null</returns>
        public static CombatBodyRuntime Of(Pawn pawn)
        {
            return pawn?.genes?.GetFirstGeneOfType<Gene_TrionGland>()?.Runtime;
        }
    }
}
