using System.Collections.Generic;
using BDP.Core.AttackExecution.RangedProtocol.Aim;
using BDP.Core.AttackExecution.RangedProtocol.Fire;
using BDP.Core.AttackExecution.RangedProtocol.Prepare;
using BDP.Core.AttackExecution.RangedProtocol.ProjectileInit;
using BDP.Core.Trigger;
using Verse;

namespace BDP.Core.AttackExecution.RangedProtocol
{
    /// <summary>
    /// 远程攻击前半段协议的统一装配入口。
    /// 它只负责持有默认模块表并暴露已装配服务，不承担读单或宿主职责。
    /// </summary>
    internal static class RangedAttackProtocolSurfaceAccess
    {
        /// <summary>
        /// 当前正式运行路径统一使用的攻击协议服务实例。
        /// </summary>
        

        /// <summary>
        /// 当前正式运行路径统一使用的远程 Trion 闸门。
        /// </summary>
        

        /// <summary>
        /// 读取当前已装配好的攻击协议服务。
        /// </summary>
        public static RangedAttackProtocolService Resolve(Pawn pawn)
        {
            CompTriggerBody triggerBody = TriggerSurfaceAccess.ResolveComp(pawn);
            return triggerBody != null ? triggerBody.RuntimeServices?.RangedAttackProtocolService : null;
        }

        /// <summary>
        /// 读取当前已装配好的远程 Trion 闸门。
        /// </summary>
        public static RangedAttackTrionGate ResolveTrionGate(Pawn pawn)
        {
            CompTriggerBody triggerBody = TriggerSurfaceAccess.ResolveComp(pawn);
            return triggerBody != null ? triggerBody.RuntimeServices?.RangedAttackTrionGate : null;
        }

        /// <summary>
        /// 创建默认瞄准阶段模块表。
        /// 无模块时返回空表，保持现有 baseline 行为。
        /// 返回顺序就是正式协议顺序，未来芯片 Def 声明顺序必须原样保留。
        /// </summary>
        internal static IEnumerable<IAimStageModule> CreateAimModules()
        {
            return new IAimStageModule[0];
        }

        /// <summary>
        /// 创建默认准备阶段模块表。
        /// 无模块时返回空表，保持现有 baseline 行为。
        /// 返回顺序就是正式协议顺序，未来芯片 Def 声明顺序必须原样保留。
        /// </summary>
        internal static IEnumerable<IPrepareStageModule> CreatePrepareModules()
        {
            return new IPrepareStageModule[]
            {
                new RangedTrionPrepareModule()
            };
        }

        /// <summary>
        /// 创建默认发射阶段模块表。
        /// 无模块时返回空表，保持现有 baseline 行为。
        /// 返回顺序就是正式协议顺序，未来芯片 Def 声明顺序必须原样保留。
        /// </summary>
        internal static IEnumerable<IFireStageModule> CreateFireModules()
        {
            return new IFireStageModule[0];
        }

        /// <summary>
        /// 创建默认投射物初始化阶段模块表。
        /// 无模块时返回空表，保持现有 baseline 行为。
        /// 返回顺序就是正式协议顺序，未来芯片 Def 声明顺序必须原样保留。
        /// </summary>
        internal static IEnumerable<IProjectileInitStageModule> CreateProjectileInitModules()
        {
            return new IProjectileInitStageModule[0];
        }
    }
}
