using System.Collections.Generic;
using BDP.Core.AttackExecution;
using BDP.Core.Chips;
using BDP.Core.Semantics;
using BDP.Core.CombatModel;
using RimWorld;
using Verse;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 主模组正式承认的一条芯片表达条目。
    /// Verb 条目在这一层已经完成正规化。
    /// </summary>
    public sealed class ChipExpressionEntryContract
    {
        /// <summary>
        /// 当前条目的稳定标识。
        /// </summary>
        public string Id;

        /// <summary>
        /// 当前条目的显示名称。
        /// </summary>
        public string DisplayLabel;

        /// <summary>
        /// 当前手动攻击入口按钮贴图路径。
        /// 留空时由下游按既定回退规则解析。
        /// </summary>
        public string ManualEntryIconTexPath;

        /// <summary>
        /// 当前条目默认使用的单侧视觉预设 DefName。
        /// </summary>
        public string VisualPresetDefName;

        /// <summary>
        /// 当前条目参与复合表达时使用的视觉预设 DefName。
        /// </summary>
        public string CompositeVisualPresetDefName;

        /// <summary>
        /// 当前条目是否强制压制宿主原装备贴图。
        /// </summary>
        public bool ForceSuppressHostEquipment = false;

        /// <summary>
        /// 当前条目的视觉优先级。
        /// </summary>
        public int VisualPriority = 0;

        /// <summary>
        /// 当前条目的正式种类。
        /// </summary>
        public ChipExpressionEntryKind Kind = ChipExpressionEntryKind.PrimaryVerb;

        /// <summary>
        /// 当前 Verb 条目的正规化主副身份。
        /// 非 Verb 条目默认为 None。
        /// </summary>
        public VerbAttackRole VerbAttackRole = VerbAttackRole.None;

        /// <summary>
        /// 当前条目的角色键。
        /// </summary>
        public string RoleKey;

        /// <summary>
        /// 当前条目的轻量标签集合。
        /// </summary>
        public List<string> Tags;

        /// <summary>
        /// 当前条目的成立条件集合。
        /// </summary>
        public List<ExpressionSourceConditionConfig> Conditions;

        /// <summary>
        /// 当前条目的 Trion 参数块。
        /// </summary>
        public ExpressionSourceTrionConfig Trion;

        /// <summary>
        /// 当前条目的轻量关系种类。
        /// </summary>
        public ChipExpressionRelationKind RelationKind = ChipExpressionRelationKind.Independent;

        /// <summary>
        /// 当前条目挂接到的父条目标识。
        /// </summary>
        public string ParentEntryId;

        /// <summary>
        /// 当前条目的武器模式。
        /// </summary>
        public VerbExpressionModeConfig WeaponMode = VerbExpressionModeConfig.None;

        /// <summary>
        /// 当前条目所属的形态键。
        /// </summary>
        public string ModeKey;

        /// <summary>
        /// 当前条目声明的 Verb 属性定义。
        /// </summary>
        public VerbProperties VerbProps;

        /// <summary>
        /// 当前条目解析出的正式运行时 Verb 规格。
        /// 它是 BDP 自己的正式真值，不再依赖运行时反射补写。
        /// </summary>
        public ResolvedVerbSpec ResolvedVerbSpec;

        /// <summary>
        /// 当前条目声明的近战 Tool。
        /// </summary>
        public Tool Tool;

        /// <summary>
        /// 当前条目声明的全部近战 Tool。
        /// 它保留作者原始多 tool 意图，供下游按步选择。
        /// </summary>
        public IReadOnlyList<Tool> DeclaredTools;

        /// <summary>
        /// 当前条目按每把 Tool 展开的近战运行时表面。
        /// </summary>
        public IReadOnlyList<MeleeToolSurface> DeclaredMeleeToolSurfaces;

        /// <summary>
        /// 当前条目声明的 Maneuver。
        /// </summary>
        public ManeuverDef Maneuver;

        /// <summary>
        /// 当前条目声明的正式执行风格。
        /// 它只描述攻击节奏与调度，不承担运行时推进。
        /// </summary>
        public AttackExecutionStyle ExecutionStyle;

        /// <summary>
        /// 当前条目指向的 Ability Def 名称。
        /// </summary>
        public string AbilityDefName;

        /// <summary>
        /// 当前条目指向的 Hediff Def 名称。
        /// </summary>
        public string HediffDefName;

        /// <summary>
        /// 当前条目的 Hediff 应用模式键。
        /// </summary>
        public string HediffApplyModeKey;

        /// <summary>
        /// 当前条目声明的被动键。
        /// </summary>
        public string PassiveKey;

        /// <summary>
        /// 当前条目暴露的轻量附加数据。
        /// </summary>
        public List<PassiveExpressionExposedDatumConfig> ExposedData;

        /// <summary>
        /// 当前条目附带的语义来源种类。
        /// </summary>
        public SemanticSourceKind SemanticSourceKind = SemanticSourceKind.Unknown;

        /// <summary>
        /// 当前条目正式承认的远程攻击模块挂载快照。
        /// 顺序语义已冻结为作者书写顺序。
        /// </summary>
        public IReadOnlyList<RangedModuleMountConfig> RangedModules;

        /// <summary>
        /// 当前条目的投射物属性覆盖（来自 GunClass 合并后）。
        /// 为 null = 不覆盖任何投射物属性。
        /// </summary>
        public ProjectileOverrides ProjectileOverrides;
    }
}
