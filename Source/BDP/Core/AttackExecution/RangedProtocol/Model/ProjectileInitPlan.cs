using System.Collections.Generic;
using BDP.Core.Projectiles.RangedFlightProtocol.Model;
using BDP.Core.Semantics;
using UnityEngine;
using Verse;

namespace BDP.Core.AttackExecution.RangedProtocol.Model
{
    /// <summary>
    /// 投射物初始化正式计划。
    /// BdpVerb_Shoot 只能消费它，不能绕过它自己重算复杂意图。
    /// </summary>
    internal sealed class ProjectileInitPlan : IExposable
    {
        /// <summary>
        /// 当前 projectile 所属的攻击实例标识。
        /// </summary>
        public string AttackInstanceId { get; set; }

        /// <summary>
        /// 当前 projectile 对应的正式结果标识。
        /// </summary>
        public string ResultId { get; set; }

        /// <summary>
        /// 当前 projectile 在本次发射中的 emit 序号。
        /// 它只标识"本窗口内第几发",逐射窗口每窗口只有一发时恒为 0。
        /// </summary>
        public int EmitIndex { get; set; }

        /// <summary>
        /// 当前 projectile 在轮次内的发射序号。
        /// 构建时按 动作步序号 × 本窗口 emit 数 + emitIndex 计算,每轮起手从 0 重新开始;
        /// 飞行/到达阶段按它区分子弹(如左右路交替),读档缺失时回退为 EmitIndex。
        /// </summary>
        public int EmitSequence { get; set; }

        /// <summary>
        /// 当前要生成的 projectile Def。
        /// </summary>
        public ThingDef ProjectileDef { get; set; }

        /// <summary>
        /// 当前 projectile 的发射者 Pawn。
        /// </summary>
        public Pawn Launcher { get; set; }

        /// <summary>
        /// 当前 projectile 归属的来源宿主。
        /// </summary>
        public Thing SourceThing { get; set; }

        /// <summary>
        /// 当前 projectile 相对施放者实时 DrawPos 的发射偏移。
        /// 默认基线只保留这个相对偏移，不提前冻结绝对世界坐标。
        /// </summary>
        public Vector3 OriginOffsetWorld { get; set; }

        /// <summary>
        /// 当前 projectile 是否显式声明了发射点随机散布区间。
        /// </summary>
        public bool HasOriginSpreadRange { get; set; }

        /// <summary>
        /// 当前 projectile 横向最小随机偏移。
        /// </summary>
        public float OriginSpreadLateralMin { get; set; }

        /// <summary>
        /// 当前 projectile 横向最大随机偏移。
        /// </summary>
        public float OriginSpreadLateralMax { get; set; }

        /// <summary>
        /// 当前 projectile 前后最小随机偏移。
        /// </summary>
        public float OriginSpreadForwardMin { get; set; }

        /// <summary>
        /// 当前 projectile 前后最大随机偏移。
        /// </summary>
        public float OriginSpreadForwardMax { get; set; }

        /// <summary>
        /// 当前 projectile 是否显式声明绝对发射原点覆盖。
        /// 只有显式覆盖时，宿主才绕过原版实时 DrawPos 取点。
        /// </summary>
        public bool HasAbsoluteOriginWorld { get; set; }

        /// <summary>
        /// 显式指定的绝对发射原点。
        /// </summary>
        public Vector3 AbsoluteOriginWorld { get; set; }

        /// <summary>
        /// 当前 `projectile（投射物）` 真正要朝向的发射目标。
        /// 它主要服务发射朝向、首段飞行和宿主层的物理导航。
        /// </summary>
        public LocalTargetInfo LaunchTarget { get; set; }

        /// <summary>
        /// 当前 `projectile（投射物）` 使用的瞄准目标。
        /// 它主要服务散布、命中解算与瞄准语义，不要求与 `LaunchTarget（发射目标）` 完全相同。
        /// </summary>
        public LocalTargetInfo AimTarget { get; set; }

        /// <summary>
        /// 当前 `projectile（投射物）` 持有的宿主 `CurrentTarget（当前目标）` 快照。
        /// 它是给宿主兼容链消费的冻结值，不重新定义 `LaunchTarget（发射目标）` 或 `AimTarget（瞄准目标）`。
        /// </summary>
        public LocalTargetInfo CurrentTarget { get; set; }

        /// <summary>
        /// 当前 projectile 的完整目标语义快照。
        /// 它明确区分最终目标、第一段目标、飞行实时最终目标和飞行实时下一目标。
        /// </summary>
        public RangedProjectileTargetSemantics TargetSemantics { get; set; }

        /// <summary>
        /// 当前 projectile 初始速度倍率。
        /// </summary>
        public float InitialSpeedFactor { get; set; }

        /// <summary>
        /// 当前 projectile 初始伤害倍率。
        /// </summary>
        public float InitialDamageFactor { get; set; }

        /// <summary>
        /// 当前投射物在正式开火时冻结的原版精度事实。
        /// 后半段只消费这份快照，不回头读取实时射手或天气。
        /// </summary>
        public ProjectileAccuracySnapshot AccuracySnapshot { get; set; }

        /// <summary>
        /// 当前 projectile 继承的命中倍率真值。
        /// 宿主发射裁决只消费它，不再回头重算 Aim 阶段概率。
        /// </summary>
        public float AccuracyFactor { get; set; }

        /// <summary>
        /// 当前 projectile 继承的强制失准半径真值。
        /// 宿主发射时优先消费它，而不是只读旧 Verb 规格。
        /// </summary>
        public float ForcedMissRadius { get; set; }

        /// <summary>
        /// 当前 projectile 是否携带来源芯片的独立精度覆盖。
        /// 为 true 时宿主发射必须使用 AccuracyTouch/Short/Medium/Long 替代 Verb.verbProps 精度。
        /// 双持场景下不同 projectile 可能来自不同芯片，精度必须各自独立。
        /// </summary>
        public bool HasAccuracy { get; set; }

        /// <summary>
        /// 当前 projectile 来源芯片的贴身精度（≤3 格）。
        /// </summary>
        public float AccuracyTouch { get; set; }

        /// <summary>
        /// 当前 projectile 来源芯片的近距精度（3~12 格过渡）。
        /// </summary>
        public float AccuracyShort { get; set; }

        /// <summary>
        /// 当前 projectile 来源芯片的中距精度（12~25 格过渡）。
        /// </summary>
        public float AccuracyMedium { get; set; }

        /// <summary>
        /// 当前 projectile 来源芯片的远距精度（>40 格）。
        /// </summary>
        public float AccuracyLong { get; set; }

        /// <summary>
        /// 当前 projectile 是否显式声明首段触发比例。
        /// </summary>
        public bool HasInitialSegmentTriggerRatio { get; set; }

        /// <summary>
        /// 当前 projectile 首段飞行到该比例时允许模块接管。
        /// </summary>
        public float InitialSegmentTriggerRatio { get; set; }

        /// <summary>
        /// 当前 projectile 显式声明的首段飞行路径快照。
        /// 它只承载几何飞行真值，不承载“追踪”等业务语义。
        /// </summary>
        public ProjectileFlightPathSnapshot InitialFlightPathSnapshot { get; set; }

        /// <summary>
        /// 当前 projectile 继承的语义上下文。
        /// </summary>
        public ISemanticContext SemanticContext { get; set; }

        /// <summary>
        /// 当前 projectile 的视觉附加来源 Def 集合。
        /// 这些 Def 只提供中性视觉 provider；读档后用它们重建每发独立附加件。
        /// </summary>
        public List<ThingDef> VisualAttachmentProviderDefs { get; set; } = new List<ThingDef>();

        /// <summary>
        /// 当前 projectile 附带的阶段标签。
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// 当前 projectile 携带的统一攻击上下文快照。
        /// 后半段只消费它，不再回头读运行时会话或零散碎片上下文。
        /// </summary>
        public AttackContextSnapshot AttackContextSnapshot { get; set; }

        /// <summary>
        /// 统一序列化当前投射物初始化计划。
        /// 这里只保存后半段继续运行必需的冻结数据，不保存运行时模块实例。
        /// </summary>
        public void ExposeData()
        {
            string attackInstanceId = AttackInstanceId;
            string resultId = ResultId;
            int emitIndex = EmitIndex;
            int emitSequence = EmitSequence;
            ThingDef projectileDef = ProjectileDef;
            Pawn launcher = Launcher;
            Thing sourceThing = SourceThing;
            Vector3 originOffsetWorld = OriginOffsetWorld;
            bool hasOriginSpreadRange = HasOriginSpreadRange;
            float originSpreadLateralMin = OriginSpreadLateralMin;
            float originSpreadLateralMax = OriginSpreadLateralMax;
            float originSpreadForwardMin = OriginSpreadForwardMin;
            float originSpreadForwardMax = OriginSpreadForwardMax;
            bool hasAbsoluteOriginWorld = HasAbsoluteOriginWorld;
            Vector3 absoluteOriginWorld = AbsoluteOriginWorld;
            LocalTargetInfo launchTarget = LaunchTarget;
            LocalTargetInfo aimTarget = AimTarget;
            LocalTargetInfo currentTarget = CurrentTarget;
            RangedProjectileTargetSemantics targetSemantics = TargetSemantics;
            float initialSpeedFactor = InitialSpeedFactor;
            float initialDamageFactor = InitialDamageFactor;
            ProjectileAccuracySnapshot accuracySnapshot = AccuracySnapshot;
            float accuracyFactor = AccuracyFactor;
            float forcedMissRadius = ForcedMissRadius;
            bool hasAccuracy = HasAccuracy;
            float accuracyTouch = AccuracyTouch;
            float accuracyShort = AccuracyShort;
            float accuracyMedium = AccuracyMedium;
            float accuracyLong = AccuracyLong;
            bool hasInitialSegmentTriggerRatio = HasInitialSegmentTriggerRatio;
            float initialSegmentTriggerRatio = InitialSegmentTriggerRatio;
            ProjectileFlightPathSnapshot initialFlightPathSnapshot = InitialFlightPathSnapshot;
            SemanticContext semanticContext = SemanticContext as SemanticContext;
            List<string> tags = Tags;
            AttackContextSnapshot attackContextSnapshot = AttackContextSnapshot;
            List<ThingDef> visualAttachmentProviderDefs = VisualAttachmentProviderDefs;

            Scribe_Values.Look(ref attackInstanceId, "attackInstanceId");
            Scribe_Values.Look(ref resultId, "resultId");
            Scribe_Values.Look(ref emitIndex, "emitIndex", 0);
            Scribe_Values.Look(ref emitSequence, "emitSequence", -1);
            Scribe_Defs.Look(ref projectileDef, "projectileDef");
            Scribe_References.Look(ref launcher, "launcher");
            Scribe_References.Look(ref sourceThing, "sourceThing");
            Scribe_Values.Look(ref originOffsetWorld, "originOffsetWorld");
            Scribe_Values.Look(ref hasOriginSpreadRange, "hasOriginSpreadRange", false);
            Scribe_Values.Look(ref originSpreadLateralMin, "originSpreadLateralMin", 0f);
            Scribe_Values.Look(ref originSpreadLateralMax, "originSpreadLateralMax", 0f);
            Scribe_Values.Look(ref originSpreadForwardMin, "originSpreadForwardMin", 0f);
            Scribe_Values.Look(ref originSpreadForwardMax, "originSpreadForwardMax", 0f);
            Scribe_Values.Look(ref hasAbsoluteOriginWorld, "hasAbsoluteOriginWorld", false);
            Scribe_Values.Look(ref absoluteOriginWorld, "absoluteOriginWorld");
            Scribe_TargetInfo.Look(ref launchTarget, "launchTarget");
            Scribe_TargetInfo.Look(ref aimTarget, "aimTarget");
            Scribe_TargetInfo.Look(ref currentTarget, "currentTarget");
            Scribe_Deep.Look(ref targetSemantics, "targetSemantics");
            Scribe_Values.Look(ref initialSpeedFactor, "initialSpeedFactor", 1f);
            Scribe_Values.Look(ref initialDamageFactor, "initialDamageFactor", 1f);
            Scribe_Deep.Look(ref accuracySnapshot, "accuracySnapshot");
            Scribe_Values.Look(ref accuracyFactor, "accuracyFactor", 1f);
            Scribe_Values.Look(ref forcedMissRadius, "forcedMissRadius", 0f);
            Scribe_Values.Look(ref hasAccuracy, "hasAccuracy", false);
            Scribe_Values.Look(ref accuracyTouch, "accuracyTouch", 0f);
            Scribe_Values.Look(ref accuracyShort, "accuracyShort", 0f);
            Scribe_Values.Look(ref accuracyMedium, "accuracyMedium", 0f);
            Scribe_Values.Look(ref accuracyLong, "accuracyLong", 0f);
            Scribe_Values.Look(ref hasInitialSegmentTriggerRatio, "hasInitialSegmentTriggerRatio", false);
            Scribe_Values.Look(ref initialSegmentTriggerRatio, "initialSegmentTriggerRatio", 0f);
            Scribe_Deep.Look(ref initialFlightPathSnapshot, "initialFlightPathSnapshot");
            Scribe_Deep.Look(ref semanticContext, "semanticContext");
            Scribe_Collections.Look(ref tags, "tags", LookMode.Value);
            Scribe_Deep.Look(ref attackContextSnapshot, "attackContextSnapshot");
            Scribe_Collections.Look(ref visualAttachmentProviderDefs, "visualAttachmentProviderDefs", LookMode.Def);

            AttackInstanceId = attackInstanceId;
            ResultId = resultId;
            EmitIndex = emitIndex;
            // 旧存档没有 emitSequence(-1) 时回退到 emitIndex,保证读档行为与构建期一致。
            EmitSequence = emitSequence >= 0 ? emitSequence : emitIndex;
            ProjectileDef = projectileDef;
            Launcher = launcher;
            SourceThing = sourceThing;
            OriginOffsetWorld = originOffsetWorld;
            HasOriginSpreadRange = hasOriginSpreadRange;
            OriginSpreadLateralMin = originSpreadLateralMin;
            OriginSpreadLateralMax = originSpreadLateralMax;
            OriginSpreadForwardMin = originSpreadForwardMin;
            OriginSpreadForwardMax = originSpreadForwardMax;
            HasAbsoluteOriginWorld = hasAbsoluteOriginWorld;
            AbsoluteOriginWorld = absoluteOriginWorld;
            LaunchTarget = launchTarget;
            AimTarget = aimTarget;
            CurrentTarget = currentTarget;
            TargetSemantics = targetSemantics;
            InitialSpeedFactor = initialSpeedFactor;
            InitialDamageFactor = initialDamageFactor;
            AccuracySnapshot = accuracySnapshot;
            AccuracyFactor = accuracyFactor;
            ForcedMissRadius = forcedMissRadius;
            HasAccuracy = hasAccuracy;
            AccuracyTouch = accuracyTouch;
            AccuracyShort = accuracyShort;
            AccuracyMedium = accuracyMedium;
            AccuracyLong = accuracyLong;
            HasInitialSegmentTriggerRatio = hasInitialSegmentTriggerRatio;
            InitialSegmentTriggerRatio = initialSegmentTriggerRatio;
            InitialFlightPathSnapshot = initialFlightPathSnapshot;
            SemanticContext = semanticContext;
            Tags = tags ?? new List<string>();
            AttackContextSnapshot = attackContextSnapshot;
            VisualAttachmentProviderDefs = visualAttachmentProviderDefs ?? new List<ThingDef>();
            if (TargetSemantics == null)
            {
                SyncTargetSemanticsFromLegacyTargets();
            }
        }

        /// <summary>
        /// 从旧的三目标兼容字段同步完整目标语义。
        /// 路径模块覆盖 LaunchTarget 后，第一段目标会落入 IntentFirst/LiveNext。
        /// </summary>
        internal void SyncTargetSemanticsFromLegacyTargets()
        {
            LocalTargetInfo finalTarget = CurrentTarget.IsValid ? CurrentTarget : AimTarget;
            LocalTargetInfo firstTarget = LaunchTarget.IsValid ? LaunchTarget : finalTarget;
            TargetSemantics = RangedProjectileTargetSemantics.CreateFromTargets(finalTarget, firstTarget);
        }
    }
}
