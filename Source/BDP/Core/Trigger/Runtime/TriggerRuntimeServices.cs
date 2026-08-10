using BDP.Core.AttackExecution;
using BDP.Core.AttackExecution.RangedProtocol;
using BDP.Core.Expressions;
using BDP.Core.Expressions.Runtime;

namespace BDP.Core.Trigger.Runtime
{
    /// <summary>
    /// `CompTriggerBody` 运行时服务根。
    /// 它只持有当前 `Trigger` owner 的运行时执行服务，不承接 Def 期静态缓存。
    /// </summary>
    internal sealed class TriggerRuntimeServices
    {
        /// <summary>
        /// 当前 `Trigger` owner 复用的表达式运行时仓库。
        /// </summary>
        internal ExpressionRuntimeRepository ExpressionRuntimeRepository { get; }

        /// <summary>
        /// 当前 `Trigger` owner 复用的表达式正式服务。
        /// </summary>
        internal ExpressionService ExpressionService { get; }

        /// <summary>
        /// 当前 `Trigger` owner 复用的攻击执行服务。
        /// </summary>
        internal AttackExecutionService AttackExecutionService { get; }

        /// <summary>
        /// 当前 `Trigger` owner 复用的远程协议服务。
        /// </summary>
        internal RangedAttackProtocolService RangedAttackProtocolService { get; }

        /// <summary>
        /// 当前 `Trigger` owner 复用的远程模块运行时解析器。
        /// </summary>
        internal RangedAttackModuleResolver RangedAttackModuleResolver { get; }

        /// <summary>
        /// 当前 `Trigger` owner 复用的远程模块运行时宿主。
        /// </summary>
        internal RangedAttackModuleRuntimeHost RangedAttackModuleRuntimeHost { get; }

        /// <summary>
        /// 当前 `Trigger` owner 复用的远程 `Trion` 闸门。
        /// </summary>
        internal RangedAttackTrionGate RangedAttackTrionGate { get; }

        /// <summary>
        /// 当前 `Trigger` owner 专属的视觉运行时状态 owner。
        /// 它保存动态执行态和原版装备姿态样本。
        /// </summary>
        internal TriggerVisualRuntimeStateOwner TriggerVisualRuntimeStateOwner { get; }

        /// <summary>
        /// 当前 `Trigger` owner 复用的 `Trigger -> Trion` 绑定服务。
        /// </summary>
        internal TriggerTrionBindingService TriggerTrionBindingService { get; }

        /// <summary>
        /// 当前 `Trigger` owner 复用的拆卸收尾事务。
        /// </summary>
        internal TriggerDetachTeardownTransaction TriggerDetachTeardownTransaction { get; }

        /// <summary>
        /// 构造一套绑定到单个 `Trigger` owner 的运行时服务。
        /// </summary>
        public TriggerRuntimeServices()
        {
            ExpressionRuntimeRepository = new ExpressionRuntimeRepository();
            ExpressionService = new ExpressionService(ExpressionRuntimeRepository);
            RangedAttackModuleResolver = new RangedAttackModuleResolver();
            RangedAttackModuleRuntimeHost = new RangedAttackModuleRuntimeHost(RangedAttackModuleResolver);
            RangedAttackProtocolService = new RangedAttackProtocolService(
                RangedAttackModuleRuntimeHost,
                RangedAttackProtocolSurfaceAccess.CreateAimModules(),
                RangedAttackProtocolSurfaceAccess.CreatePrepareModules(),
                RangedAttackProtocolSurfaceAccess.CreateFireModules(),
                RangedAttackProtocolSurfaceAccess.CreateProjectileInitModules());
            RangedAttackTrionGate = new RangedAttackTrionGate();
            TriggerVisualRuntimeStateOwner = new TriggerVisualRuntimeStateOwner();
            AttackExecutionService = new AttackExecutionService();
            TriggerTrionBindingService = new TriggerTrionBindingService();
            TriggerDetachTeardownTransaction = new TriggerDetachTeardownTransaction(TriggerTrionBindingService);
        }
    }
}
