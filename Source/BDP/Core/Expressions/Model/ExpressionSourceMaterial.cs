using BDP.Core.Semantics;
using BDP.Core.CombatModel;
using BDP.Core.Trigger;
using BDP.Core.AttackExecution;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 进入表达系统后的来源材料。
    /// 它已经带上运行时侧别，但还不是最终表达结果。
    /// </summary>
    internal sealed class ExpressionSourceMaterial
    {
        /// <summary>
        /// 当前材料的稳定标识。
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 当前材料来自哪一侧。
        /// </summary>
        public TriggerSide Side { get; set; }

        /// <summary>
        /// 当前材料来自该侧的第几格。
        /// </summary>
        public int SlotIndex { get; set; }

        /// <summary>
        /// 当前材料的结果大类。
        /// </summary>
        public ExpressionResultKind ResultKind { get; set; }

        /// <summary>
        /// 当前材料的武器模式。
        /// </summary>
        public WeaponExpressionMode WeaponMode { get; set; }

        /// <summary>
        /// 产生这条材料的芯片实例。
        /// </summary>
        public Thing SourceChip { get; set; }

        /// <summary>
        /// 当前材料的来源追踪。
        /// 它只服务后续事务回到来源槽位，不参与战斗语义。
        /// </summary>
        public ExpressionSourceReference SourceReference { get; set; }

        /// <summary>
        /// 当前材料携带的芯片级运行参数快照。
        /// 它只在表达系统内部沿构建链传递，供单侧与复合构建读取。
        /// </summary>
        public ExpressionRuntimePayload RuntimePayload { get; set; }

        /// <summary>
        /// 当前材料的显示名称。
        /// </summary>
        public string DisplayLabel { get; set; }

        /// <summary>
        /// 当前手动攻击入口按钮贴图路径。
        /// 留空时由下游按既定回退规则解析。
        /// </summary>
        public string ManualEntryIconTexPath { get; set; }

        /// <summary>
        /// 当前材料默认使用的单侧视觉预设 DefName。
        /// </summary>
        public string VisualPresetDefName { get; set; }

        /// <summary>
        /// 当前材料参与复合表达时使用的视觉预设 DefName。
        /// </summary>
        public string CompositeVisualPresetDefName { get; set; }

        /// <summary>
        /// 当前材料是否强制压制宿主原装备贴图。
        /// </summary>
        public bool ForceSuppressHostEquipment { get; set; }

        /// <summary>
        /// 当前材料的视觉优先级。
        /// </summary>
        public int VisualPriority { get; set; }

        /// <summary>
        /// 当前材料的角色键。
        /// </summary>
        public string RoleKey { get; set; }

        /// <summary>
        /// 当前 Verb 材料的正规化主副身份。
        /// 非 Verb 材料默认为 None。
        /// </summary>
        public VerbAttackRole VerbAttackRole { get; set; }

        /// <summary>
        /// 当前材料附带的轻量标签集合。
        /// </summary>
        public IReadOnlyList<string> Tags { get; set; }

        /// <summary>
        /// 当前材料的 Trion 参数块。
        /// </summary>
        public ExpressionSourceTrionConfig Trion { get; set; }

        /// <summary>
        /// 当前材料的条件评估结果。
        /// </summary>
        public ExpressionConditionEvaluation ConditionEvaluation { get; set; }

        /// <summary>
        /// 当前材料在本轮是否启用。
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// 当前材料所属的形态键。
        /// </summary>
        public string ModeKey { get; set; }

        /// <summary>
        /// 当前材料附带的语义上下文。
        /// </summary>
        public ISemanticContext SemanticContext { get; set; }

        /// <summary>
        /// 当前 Verb 材料对应的宿主来源键。
        /// 非 Verb 材料为 null。
        /// </summary>
        public ExpressionVerbHostKey VerbHostKey { get; set; }

        /// <summary>
        /// 当前 Verb 材料对应的固定宿主入口。
        /// 非 Verb 材料为 None。
        /// </summary>
        public ExpressionVerbHostSlot VerbHostSlot { get; set; }

        /// <summary>
        /// 当前材料挂接的 Verb 属性定义。
        /// </summary>
        public VerbProperties VerbProps { get; set; }

        /// <summary>
        /// 当前材料解析出的正式运行时 Verb 规格。
        /// 它在进入最终结果前保持为只读真值，不再临时反射改写。
        /// </summary>
        public ResolvedVerbSpec ResolvedVerbSpec { get; set; }

        /// <summary>
        /// 当前材料挂接的近战 Tool。
        /// </summary>
        public Tool Tool { get; set; }

        /// <summary>
        /// 当前材料保留的全部近战 Tool。
        /// </summary>
        public IReadOnlyList<Tool> DeclaredTools { get; set; }

        /// <summary>
        /// 当前材料展开出的全部近战运行时表面。
        /// </summary>
        public IReadOnlyList<MeleeToolSurface> DeclaredMeleeToolSurfaces { get; set; }

        /// <summary>
        /// 当前材料挂接的 Maneuver。
        /// </summary>
        public ManeuverDef Maneuver { get; set; }

        /// <summary>
        /// 当前材料携带的正式执行风格。
        /// 它只描述攻击节奏与调度，不承担运行时推进。
        /// </summary>
        public AttackExecutionStyle ExecutionStyle { get; set; }

        /// <summary>
        /// 当前材料指向的 Ability Def 名称。
        /// </summary>
        public string AbilityDefName { get; set; }

        /// <summary>
        /// 当前材料指向的 Hediff Def 名称。
        /// </summary>
        public string HediffDefName { get; set; }

        /// <summary>
        /// 当前材料的 Hediff 应用模式键。
        /// </summary>
        public string HediffApplyModeKey { get; set; }

        /// <summary>
        /// 当前材料的被动键。
        /// </summary>
        public string PassiveKey { get; set; }

        /// <summary>
        /// 当前材料附带的被动暴露数据。
        /// </summary>
        public IReadOnlyList<PassiveExpressionExposedDatum> ExposedData { get; set; }

        /// <summary>
        /// 当前材料携带的远程攻击模块挂载快照。
        /// 这一层仍只负责顺序与配置快照传递。
        /// </summary>
        public IReadOnlyList<RangedModuleMountConfig> RangedModules { get; set; }

        /// <summary>
        /// 当前来源的可空变体键，用于身份判定和聚合键区分。
        /// </summary>
        public string SourceVariantKey { get; set; }

        /// <summary>
        /// 当前来源的可空变体显示标签，供表现层直接使用。
        /// </summary>
        public string SourceVariantLabel { get; set; }
    }
}
