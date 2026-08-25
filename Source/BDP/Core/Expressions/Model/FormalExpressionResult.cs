using BDP.Core.Semantics;
using BDP.Core.CombatModel;
using BDP.Core.AttackExecution;
using BDP.Core.Requirements;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 一条已经正式成立的表达结果。
    /// 它只保存“这一刻系统承认了什么”。
    /// </summary>
    internal sealed class FormalExpressionResult
    {
        /// <summary>
        /// 当前结果的稳定标识。
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 当前结果的结果大类。
        /// </summary>
        public ExpressionResultKind ResultKind { get; set; }

        /// <summary>
        /// 当前结果的武器模式。
        /// </summary>
        public WeaponExpressionMode WeaponMode { get; set; }

        /// <summary>
        /// 当前结果的来源关系。
        /// </summary>
        public ExpressionOriginKind OriginKind { get; set; }

        /// <summary>
        /// 当前结果的来源追踪。
        /// 它只服务少数需要回到来源槽位处理事务的流程。
        /// </summary>
        public ExpressionSourceReference SourceReference { get; set; }

        /// <summary>
        /// 当前结果的高层合成类型。
        /// 若为 DualWeapon，这条结果只表示“复合攻击入口”，不天然等于单侧发射真值。
        /// </summary>
        public CompositeExpressionKind CompositeKind { get; set; }

        /// <summary>
        /// 当前结果若来自组合技，则这里记录来源 ComboDefName。
        /// 它用于让下游稳定知道这条结果具体来自哪一个组合技定义。
        /// </summary>
        public string ComboDefName { get; set; }

        /// <summary>
        /// 当前结果所属来源的可空变体键，用于身份判定。
        /// </summary>
        public string SourceVariantKey { get; set; }

        /// <summary>
        /// 当前结果所属来源的可空变体显示标签。
        /// </summary>
        public string SourceVariantLabel { get; set; }

        /// <summary>
        /// 当前结果的显示名称。
        /// </summary>
        public string DisplayLabel { get; set; }

        /// <summary>
        /// 当前手动攻击入口按钮贴图路径。
        /// 留空时由下游按既定回退规则解析。
        /// </summary>
        public string ManualEntryIconTexPath { get; set; }

        /// <summary>
        /// 当前结果默认使用的单侧视觉预设 DefName。
        /// </summary>
        public string VisualPresetDefName { get; set; }

        /// <summary>
        /// 当前结果对基础视觉图层的局部覆盖预设 DefName。
        /// </summary>
        public string VisualGraphicOverrideDefName { get; set; }

        /// <summary>
        /// 当前结果参与复合表达时使用的视觉预设 DefName。
        /// </summary>
        public string CompositeVisualPresetDefName { get; set; }

        /// <summary>
        /// 当前结果是否强制压制宿主原装备贴图。
        /// </summary>
        public bool ForceSuppressHostEquipment { get; set; }

        /// <summary>
        /// 当前结果的视觉优先级。
        /// </summary>
        public int VisualPriority { get; set; }

        /// <summary>
        /// 当前手动攻击入口的稳定聚合键。
        /// 它只表达“这是哪个攻击入口”，不应携带 Pawn 或芯片实例 ThingID 等当轮实例身份。
        /// </summary>
        public string ManualEntryAggregationKey { get; set; }

        /// <summary>
        /// 当前结果的角色键。
        /// </summary>
        public string RoleKey { get; set; }

        /// <summary>
        /// 当前 Verb 结果的正规化主副身份。
        /// 非 Verb 结果默认为 None。
        /// </summary>
        public VerbAttackRole VerbAttackRole { get; set; }

        /// <summary>
        /// 当前结果附带的轻量标签集合。
        /// </summary>
        public IReadOnlyList<string> Tags { get; set; }

        /// <summary>
        /// 当前结果暴露给攻击编排层的执行槽位键。
        /// </summary>
        public string ExecutionSlotKey { get; set; }

        /// <summary>
        /// 当前结果是否为副攻击身份。
        /// </summary>
        public bool IsSecondaryAttack { get; set; }

        /// <summary>
        /// 当前结果的 Trion 参数块。
        /// </summary>
        public ExpressionSourceTrionConfig Trion { get; set; }

        /// <summary>
        /// 当前结果是否可用。
        /// </summary>
        public bool IsAvailable { get; set; }

        /// <summary>
        /// Combo 自己的角色使用条件检查。
        /// 空值表示这不是 Combo 结果；不满足时结果仍然存在，但不得被实际使用。
        /// </summary>
        public PawnRequirementCheckResult UseRequirementCheck { get; set; }

        /// <summary>
        /// 当前结果是否允许进入投影层。
        /// </summary>
        public bool CanProject { get; set; }

        /// <summary>
        /// 当前结果附带的语义上下文。
        /// </summary>
        public ISemanticContext SemanticContext { get; set; }

        /// <summary>
        /// 当前结果所属的形态键。
        /// </summary>
        public string ModeKey { get; set; }

        /// <summary>
        /// 当前结果携带的正式执行风格。
        /// 它只描述攻击节奏与调度，不承担运行时推进，也不直接决定每个 emit 的发射真值。
        /// </summary>
        public AttackExecutionStyle ExecutionStyle { get; set; }

        /// <summary>
        /// 当前结果挂接的 Verb 属性定义。
        /// 对单侧结果，它通常同时也是作者声明的会话表面。
        /// 对复合结果，它只服务复合入口的会话承接，不应被下游误当成所有 emit 的统一真值来源。
        /// </summary>
        public VerbProperties VerbProps { get; set; }

        /// <summary>
        /// 当前结果解析出的正式运行时 Verb 规格。
        /// 它是下游攻击执行与宿主运行时应该消费的正式真值。
        /// </summary>
        public ResolvedVerbSpec ResolvedVerbSpec { get; set; }

        /// <summary>
        /// 当前结果挂接的近战 Tool。
        /// </summary>
        public Tool Tool { get; set; }

        /// <summary>
        /// 当前结果保留的全部近战 Tool。
        /// 它是后续每刀 Tool 选择的候选集。
        /// </summary>
        public IReadOnlyList<Tool> DeclaredTools { get; set; }

        /// <summary>
        /// 当前结果展开出的全部近战运行时表面。
        /// </summary>
        public IReadOnlyList<MeleeToolSurface> DeclaredMeleeToolSurfaces { get; set; }

        /// <summary>
        /// 当前结果挂接的 Maneuver。
        /// </summary>
        public ManeuverDef Maneuver { get; set; }

        /// <summary>
        /// 当前结果指向的 Ability Def 名称。
        /// </summary>
        public string AbilityDefName { get; set; }

        /// <summary>
        /// 当前结果指向的 Hediff Def 名称。
        /// </summary>
        public string HediffDefName { get; set; }

        /// <summary>
        /// 当前结果的 Hediff 应用模式键。
        /// </summary>
        public string HediffApplyModeKey { get; set; }

        /// <summary>
        /// 当前结果的被动键。
        /// </summary>
        public string PassiveKey { get; set; }

        /// <summary>
        /// 当前结果附带的被动暴露数据。
        /// </summary>
        public IReadOnlyList<PassiveExpressionExposedDatum> ExposedData { get; set; }

        /// <summary>
        /// 当前结果携带的远程攻击模块挂载快照。
        /// 它是后续交互前置链与执行主骨架的统一模块来源。
        /// </summary>
        public IReadOnlyList<RangedModuleMountConfig> RangedModules { get; set; }

        /// <summary>
        /// 当前结果向其它远程表达结果发布的开放式增强声明。
        /// 被动结果只通过它发布“可追加什么”，不记录目标芯片身份。
        /// </summary>
        public IReadOnlyList<RangedModuleAugmentationConfig> RangedModuleAugmentations { get; set; }

        /// <summary>
        /// 当前结果在最终入口显示时应追加的名称前缀列表。
        /// 来源变体后缀仍由 SourceVariantLabel 单独负责。
        /// </summary>
        public IReadOnlyList<string> DisplayLabelPrefixes { get; set; }
    }
}
