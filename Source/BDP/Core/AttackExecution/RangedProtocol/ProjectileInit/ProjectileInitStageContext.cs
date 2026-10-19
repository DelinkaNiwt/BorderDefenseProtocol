using System.Collections.Generic;
using BDP.Core.AttackExecution.RangedProtocol.Model;
using BDP.Core.AttackExecution.RangedModules.Runtime;
using BDP.Core.Semantics;
using Verse;

namespace BDP.Core.AttackExecution.RangedProtocol.ProjectileInit
{
    /// <summary>
    /// ProjectileInit 阶段上下文。
    /// 模块只能把 Fire 结果翻译成 projectile 初始化计划，不能偷改后半段结论。
    /// </summary>
    public readonly struct ProjectileInitStageContext
    {
        internal ProjectileInitStageContext(
            RangedAttackEntry entry,
            AimRecord aim,
            PrepareRecord prepare,
            FireRecord fire,
            IRangedAttackModuleRuntime currentRuntime, IReadOnlyList<ProjectileInitContribution> previousContributions = null)
        {
            PreviousContributions = previousContributions;
            Entry = entry;
            Aim = aim;
            Prepare = prepare;
            Fire = fire;
            ModuleSession = entry != null ? entry.ModuleSession : null;
            AttackContext = entry != null ? entry.AttackContext : new AttackContext();
            CurrentRuntime = currentRuntime;
            Pawn = entry != null ? entry.Pawn : null;
            Map = Pawn != null ? Pawn.Map : null;
            AttackInstanceId = entry != null ? entry.AttackInstanceId : null;
            RequestedTarget = entry != null ? entry.Target : LocalTargetInfo.Invalid;
            FinalTarget = aim != null ? aim.FinalTarget : LocalTargetInfo.Invalid;
            AccuracyFactor = aim != null ? aim.AccuracyFactor : 1f;
            ForcedMissRadius = aim != null ? aim.ForcedMissRadius : 0f;
            ResourceCost = prepare != null ? prepare.ResourceCost : 0f;
            MinimumRequired = prepare != null ? prepare.MinimumRequired : 0f;
            SkipResourceConsumption = prepare != null && prepare.SkipResourceConsumption;
            ProjectileDef = fire != null ? fire.ProjectileDef : null;
            EmitCount = fire != null && fire.Emits != null ? fire.Emits.Count : 0;
            // 本窗口在轮次内的起始发射序号 = 动作步序号 × 本窗口 emit 数。
            // 逐射编排每个动作步一个窗口,模块用 EmitSequenceBase + emitIndex 即得"该发在轮内的第几发";
            // 每轮起手重建动作步,序号从 0 重新开始,保证轮内交替而非轮间交替。
            EmitSequenceBase = entry != null && entry.RuntimeStep != null
                ? entry.RuntimeStep.StepIndex * EmitCount
                : 0;
            SemanticContext = entry != null ? entry.SemanticContext : null;
        }

        /// <summary>前序贡献仅供读取控制权；不向业务暴露计划对象。</summary>
        private IReadOnlyList<ProjectileInitContribution> PreviousContributions { get; }

        /// <summary>检查该发是否已被前序贡献独占。</summary>
        public bool HasPreviousExclusiveFlightControl(int emitIndex)
        {
            if (PreviousContributions == null) return false;
            for (int i = 0; i < PreviousContributions.Count; i++)
                foreach (ProjectileInitPlanContribution plan in PreviousContributions[i].PlanContributions)
                    if (plan != null && plan.EmitIndex == emitIndex && plan.ExclusiveFlightControl) return true;
            return false;
        }

        internal RangedAttackEntry Entry { get; }

        internal AimRecord Aim { get; }

        internal PrepareRecord Prepare { get; }

        internal FireRecord Fire { get; }

        internal RangedAttackModuleSession ModuleSession { get; }

        /// <summary>
        /// 当前阶段绑定的统一攻击上下文。
        /// 前半段跨阶段传递只认这条主干。
        /// </summary>
        public AttackContext AttackContext { get; }

        internal IRangedAttackModuleRuntime CurrentRuntime { get; }

        /// <summary>
        /// 当前初始化阶段所属攻击实例标识。
        /// </summary>
        public string AttackInstanceId { get; }

        /// <summary>
        /// 当前初始化阶段所属施放 Pawn。
        /// </summary>
        public Pawn Pawn { get; }

        /// <summary>
        /// 当前初始化阶段所在地图。
        /// </summary>
        public Map Map { get; }

        /// <summary>
        /// 进入 Aim 前确认下来的原始请求目标。
        /// </summary>
        public LocalTargetInfo RequestedTarget { get; }

        /// <summary>
        /// Aim 阶段裁定后的正式目标。
        /// </summary>
        public LocalTargetInfo FinalTarget { get; }

        /// <summary>
        /// Aim 阶段输出的命中倍率。
        /// </summary>
        public float AccuracyFactor { get; }

        /// <summary>
        /// Aim 阶段输出的强制失准半径。
        /// </summary>
        public float ForcedMissRadius { get; }

        /// <summary>
        /// Prepare 阶段裁定后的资源消耗。
        /// </summary>
        public float ResourceCost { get; }

        /// <summary>
        /// Prepare 阶段裁定后的最小资源要求。
        /// </summary>
        public float MinimumRequired { get; }

        /// <summary>
        /// Prepare 阶段是否要求跳过资源扣除。
        /// </summary>
        public bool SkipResourceConsumption { get; }

        /// <summary>
        /// Fire 阶段裁定后的基线投射物 Def。
        /// </summary>
        public ThingDef ProjectileDef { get; }

        /// <summary>
        /// Fire 阶段最终展开出的 emit 数量。
        /// </summary>
        public int EmitCount { get; }

        /// <summary>
        /// 本窗口在轮次内的起始发射序号。
        /// 它与 emitIndex 相加即"该发在轮内的第几发",供模块按发射顺序区分子弹(如左右路交替)。
        /// </summary>
        public int EmitSequenceBase { get; }

        /// <summary>
        /// 当前攻击继承的统一语义上下文。
        /// </summary>
        public ISemanticContext SemanticContext { get; }

        /// <summary>
        /// 读取指定 emit 的来源结果标识。
        /// 模块可用它把自己的影响约束在所属来源上。
        /// </summary>
        public bool TryGetEmitSourceResultId(int emitIndex, out string sourceResultId)
        {
            sourceResultId = null;
            if (Fire?.Emits == null || emitIndex < 0 || emitIndex >= Fire.Emits.Count)
            {
                return false;
            }

            sourceResultId = Fire.Emits[emitIndex]?.SourceResultId;
            return !string.IsNullOrWhiteSpace(sourceResultId);
        }

        /// <summary>
        /// 读取指定 emit 声明的原版发射点散布区间。
        /// 这是初始化阶段的中性事实访问器，模块可据此协调自己的发射几何。
        /// </summary>
        public bool TryGetEmitOriginSpread(
            int emitIndex,
            out float lateralMin,
            out float lateralMax,
            out float forwardMin,
            out float forwardMax)
        {
            lateralMin = 0f;
            lateralMax = 0f;
            forwardMin = 0f;
            forwardMax = 0f;
            if (Fire?.Emits == null || emitIndex < 0 || emitIndex >= Fire.Emits.Count)
            {
                return false;
            }

            FireEmitRecord emit = Fire.Emits[emitIndex];
            if (emit == null || !emit.HasOriginSpreadRange)
            {
                return false;
            }

            lateralMin = emit.OriginSpreadLateralMin;
            lateralMax = emit.OriginSpreadLateralMax;
            forwardMin = emit.OriginSpreadForwardMin;
            forwardMax = emit.OriginSpreadForwardMax;
            return true;
        }

        /// <summary>
        /// 读取当前模块自己的私有上下文。
        /// </summary>
        public T GetPrivateContext<T>()
            where T : class, IRangedModulePrivateContext
        {
            return ModuleSession != null && CurrentRuntime != null
                ? ModuleSession.GetPrivateContext<T>(CurrentRuntime)
                : null;
        }

        /// <summary>
        /// 尝试读取当前模块自己的私有上下文。
        /// </summary>
        public bool TryGetPrivateContext<T>(out T context)
            where T : class, IRangedModulePrivateContext
        {
            context = GetPrivateContext<T>();
            return context != null;
        }

        /// <summary>
        /// 读取或创建当前模块自己的私有上下文。
        /// </summary>
        public T GetOrCreatePrivateContext<T>()
            where T : class, IRangedModulePrivateContext, new()
        {
            return ModuleSession != null && CurrentRuntime != null
                ? ModuleSession.GetOrCreatePrivateContext<T>(CurrentRuntime)
                : null;
        }
    }
}
