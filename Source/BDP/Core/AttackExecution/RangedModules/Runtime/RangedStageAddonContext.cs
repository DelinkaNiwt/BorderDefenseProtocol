using BDP.Core.Semantics;
using UnityEngine;
using Verse;

namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// 远程阶段附加挂件上下文。
    /// 它只暴露阶段最终已经成立的中性事实，不暴露主逻辑可写入口。
    /// </summary>
    public readonly struct RangedStageAddonContext
    {
        /// <summary>
        /// 创建一份新的阶段附加挂件上下文。
        /// </summary>
        internal RangedStageAddonContext(
            RangedStageKind stage,
            Pawn pawn,
            Map map,
            string attackInstanceId,
            string resultId,
            int emitIndex,
            Projectile projectile,
            Thing launcher,
            Thing sourceThing,
            LocalTargetInfo aimTarget,
            LocalTargetInfo currentTarget,
            Vector3 currentDestination,
            Thing hitThing,
            IntVec3 hitCell,
            ISemanticContext semanticContext,
            AttackContextSnapshot attackContextSnapshot)
        {
            Stage = stage;
            Pawn = pawn;
            Map = map;
            AttackInstanceId = attackInstanceId;
            ResultId = resultId;
            EmitIndex = emitIndex;
            Projectile = projectile;
            Launcher = launcher;
            SourceThing = sourceThing;
            AimTarget = aimTarget;
            CurrentTarget = currentTarget;
            CurrentDestination = currentDestination;
            HitThing = hitThing;
            HitCell = hitCell;
            SemanticContext = semanticContext;
            AttackContextSnapshot = attackContextSnapshot;
        }

        /// <summary>
        /// 当前附加逻辑所属阶段。
        /// </summary>
        public RangedStageKind Stage { get; }

        /// <summary>
        /// 当前阶段所属施放 Pawn。
        /// </summary>
        public Pawn Pawn { get; }

        /// <summary>
        /// 当前阶段所在地图。
        /// </summary>
        public Map Map { get; }

        /// <summary>
        /// 当前攻击实例标识。
        /// </summary>
        public string AttackInstanceId { get; }

        /// <summary>
        /// 当前阶段绑定的正式结果标识。
        /// </summary>
        public string ResultId { get; }

        /// <summary>
        /// 当前阶段绑定的 emit 序号。
        /// </summary>
        public int EmitIndex { get; }

        /// <summary>
        /// 当前阶段所属投射物宿主。
        /// </summary>
        public Projectile Projectile { get; }

        /// <summary>
        /// 当前阶段所属发射者宿主。
        /// </summary>
        public Thing Launcher { get; }

        /// <summary>
        /// 当前阶段所属来源宿主。
        /// </summary>
        public Thing SourceThing { get; }

        /// <summary>
        /// 当前阶段沿用的瞄准目标。
        /// </summary>
        public LocalTargetInfo AimTarget { get; }

        /// <summary>
        /// 当前阶段沿用的实际目标。
        /// </summary>
        public LocalTargetInfo CurrentTarget { get; }

        /// <summary>
        /// 当前阶段成立后的目的地。
        /// </summary>
        public Vector3 CurrentDestination { get; }

        /// <summary>
        /// 当前阶段成立后的命中 Thing。
        /// </summary>
        public Thing HitThing { get; }

        /// <summary>
        /// 当前阶段成立后的命中格子。
        /// </summary>
        public IntVec3 HitCell { get; }

        /// <summary>
        /// 当前攻击继承的统一语义上下文。
        /// </summary>
        public ISemanticContext SemanticContext { get; }

        /// <summary>
        /// 当前攻击会话携带的统一攻击上下文快照。
        /// Addon 是后置只读面，因此只暴露冻结态读取入口。
        /// </summary>
        public AttackContextSnapshot AttackContextSnapshot { get; }
    }

    /// <summary>
    /// 远程阶段正式阶段名。
    /// </summary>
    public enum RangedStageKind
    {
        /// <summary>
        /// 手动入口阶段。
        /// </summary>
        ManualEntry = 0,

        /// <summary>
        /// 目标选择阶段。
        /// </summary>
        Targeting = 1,

        /// <summary>
        /// 预览反馈阶段。
        /// </summary>
        Preview = 2,

        /// <summary>
        /// 确认冻结阶段。
        /// </summary>
        Confirm = 3,

        /// <summary>
        /// 瞄准阶段。
        /// </summary>
        Aim = 4,

        /// <summary>
        /// 准备阶段。
        /// </summary>
        Prepare = 5,

        /// <summary>
        /// 发射阶段。
        /// </summary>
        Fire = 6,

        /// <summary>
        /// 投射物初始化阶段。
        /// </summary>
        ProjectileInit = 7,

        /// <summary>
        /// 飞行阶段。
        /// </summary>
        Flight = 8,

        /// <summary>
        /// 到达阶段。
        /// </summary>
        Arrival = 9,

        /// <summary>
        /// 命中解释阶段。
        /// </summary>
        Hit = 10,

        /// <summary>
        /// 落地效果阶段。
        /// </summary>
        Impact = 11
    }
}
