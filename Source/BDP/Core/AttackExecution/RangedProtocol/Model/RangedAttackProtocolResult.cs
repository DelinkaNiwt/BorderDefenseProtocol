using System.Collections.Generic;

namespace BDP.Core.AttackExecution.RangedProtocol.Model
{
    /// <summary>
    /// 远程攻击协议前半段的完整产物。
    /// 它把各阶段正式结果集中保存，便于上游执行器和宿主桥消费。
    /// </summary>
    internal sealed class RangedAttackProtocolResult
    {
        public RangedAttackEntry Entry { get; set; }

        public AimRecord Aim { get; set; }

        public PrepareRecord Prepare { get; set; }

        public FireRecord Fire { get; set; }

        public IReadOnlyList<ProjectileInitPlan> ProjectilePlans { get; set; }

        /// <summary>
        /// 交给 Verb 宿主消费的正式发射计划。
        /// 它把动作步发射语义显式化，避免宿主再次猜测多计划含义。
        /// </summary>
        public RangedVerbEmissionPlan VerbEmissionPlan { get; set; }

        public RangedProjectionSeed ProjectionSeed { get; set; }
    }
}
