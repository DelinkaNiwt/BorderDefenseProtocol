using BDP.Core.Semantics;
using BDP.Core.CombatModel;
using BDP.Core.AttackExecution;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 芯片定义翻译后的正式来源声明。
    /// 它是表达系统的上游输入，不是最终结果。
    /// </summary>
    internal sealed class ExpressionSourceDeclaration
    {
        /// <summary>
        /// 当前来源声明的稳定标识。
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 当前来源声明的结果大类。
        /// </summary>
        public ExpressionResultKind ResultKind { get; set; }

        /// <summary>
        /// 当前来源声明的武器模式。
        /// </summary>
        public WeaponExpressionMode WeaponMode { get; set; }

        /// <summary>
        /// 当前来源声明的显示名称。
        /// </summary>
        public string DisplayLabel { get; set; }

        /// <summary>
        /// 当前手动攻击入口按钮贴图路径。
        /// 留空时由下游按既定回退规则解析。
        /// </summary>
        public string ManualEntryIconTexPath { get; set; }

        /// <summary>
        /// 当前来源声明默认使用的单侧视觉预设 DefName。
        /// </summary>
        public string VisualPresetDefName { get; set; }

        /// <summary>
        /// 当前来源声明对基础视觉图层的局部覆盖预设 DefName。
        /// </summary>
        public string VisualGraphicOverrideDefName { get; set; }

        /// <summary>
        /// 当前来源声明参与复合表达时使用的视觉预设 DefName。
        /// </summary>
        public string CompositeVisualPresetDefName { get; set; }

        /// <summary>
        /// 当前来源声明是否强制压制宿主原装备贴图。
        /// </summary>
        public bool ForceSuppressHostEquipment { get; set; }

        /// <summary>
        /// 当前来源声明的视觉优先级。
        /// </summary>
        public int VisualPriority { get; set; }

        /// <summary>
        /// 当前来源声明的角色键。
        /// </summary>
        public string RoleKey { get; set; }

        /// <summary>
        /// 当前 Verb 来源的正规化主副身份。
        /// 非 Verb 来源默认为 None。
        /// </summary>
        public VerbAttackRole VerbAttackRole { get; set; }

        /// <summary>
        /// 当前来源声明附带的轻量标签集合。
        /// </summary>
        public IReadOnlyList<string> Tags { get; set; }

        /// <summary>
        /// 当前来源声明的 Trion 参数块。
        /// </summary>
        public ExpressionSourceTrionConfig Trion { get; set; }

        /// <summary>
        /// 当前来源声明的成立条件集合。
        /// </summary>
        public IReadOnlyList<ExpressionSourceConditionConfig> Conditions { get; set; }

        /// <summary>
        /// 当前来源声明附带的语义来源种类。
        /// </summary>
        public SemanticSourceKind SemanticSourceKind { get; set; }

        /// <summary>
        /// 当前来源声明所属的形态键。
        /// </summary>
        public string ModeKey { get; set; }

        /// <summary>
        /// 当前来源声明挂接的 Verb 属性定义。
        /// </summary>
        public VerbProperties VerbProps { get; set; }

        /// <summary>
        /// 当前来源声明解析出的正式运行时 Verb 规格。
        /// 它是表达系统对运行时行为的正式描述。
        /// </summary>
        public ResolvedVerbSpec ResolvedVerbSpec { get; set; }

        /// <summary>
        /// 当前来源声明挂接的近战 Tool。
        /// </summary>
        public Tool Tool { get; set; }

        /// <summary>
        /// 当前来源声明保留的全部近战 Tool。
        /// </summary>
        public IReadOnlyList<Tool> DeclaredTools { get; set; }

        /// <summary>
        /// 当前来源声明展开出的全部近战运行时表面。
        /// </summary>
        public IReadOnlyList<MeleeToolSurface> DeclaredMeleeToolSurfaces { get; set; }

        /// <summary>
        /// 当前来源声明挂接的 Maneuver。
        /// </summary>
        public ManeuverDef Maneuver { get; set; }

        /// <summary>
        /// 当前来源声明携带的正式执行风格。
        /// 它只描述攻击节奏与调度，不承担运行时推进。
        /// </summary>
        public AttackExecutionStyle ExecutionStyle { get; set; }

        /// <summary>
        /// 当前来源声明指向的 Ability Def 名称。
        /// </summary>
        public string AbilityDefName { get; set; }

        /// <summary>
        /// 当前来源声明指向的 Hediff Def 名称。
        /// </summary>
        public string HediffDefName { get; set; }

        /// <summary>
        /// 当前来源声明的 Hediff 应用模式键。
        /// </summary>
        public string HediffApplyModeKey { get; set; }

        /// <summary>
        /// 当前来源声明的被动键。
        /// </summary>
        public string PassiveKey { get; set; }

        /// <summary>
        /// 当前来源声明附带的被动暴露数据。
        /// </summary>
        public IReadOnlyList<PassiveExpressionExposedDatum> ExposedData { get; set; }

        /// <summary>
        /// 当前来源声明携带的远程攻击模块挂载快照。
        /// 这一层只负责传递，不解释任何模块语义。
        /// </summary>
        public IReadOnlyList<RangedModuleMountConfig> RangedModules { get; set; }

        /// <summary>
        /// 当前来源向其它远程表达结果发布的开放式增强声明。
        /// </summary>
        public IReadOnlyList<RangedModuleAugmentationConfig> RangedModuleAugmentations { get; set; }
    }
}
