using BDP.Core.CombatModel;
using BDP.Core.AttackExecution;
using BDP.Core.Chips;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 芯片定义层的一条表达条目。
    /// 这一层描述“作者写了什么”，不是最终运行时事实。
    /// </summary>
    public sealed class ChipExpressionEntryConfig : ExpressionSourceConfigBase
    {
        /// <summary>
        /// 当前条目的定义种类。
        /// </summary>
        public ChipExpressionEntryKindConfig Kind = ChipExpressionEntryKindConfig.PrimaryVerb;

        /// <summary>
        /// 当前条目的轻量关系种类。
        /// </summary>
        public ChipExpressionRelationKindConfig RelationKind = ChipExpressionRelationKindConfig.Independent;

        /// <summary>
        /// 当前条目挂接到的父条目标识。
        /// </summary>
        public string ParentEntryId;

        /// <summary>
        /// 当前条目的武器模式。
        /// 只有 Verb 条目会消费这个字段。
        /// </summary>
        public VerbExpressionModeConfig WeaponMode = VerbExpressionModeConfig.None;

        /// <summary>
        /// 当前条目声明的 Verb 属性定义。
        /// </summary>
        public VerbProperties VerbProps;

        /// <summary>
        /// 当前条目是否必须满足射手到语义目标的直射 LOS。
        /// 它只表达 dual 等入口层的必要直射语义，不覆盖模块内部的路径段 LOS 规则。
        /// </summary>
        public DirectTargetLineOfSightRequirementConfig DirectTargetLineOfSight = DirectTargetLineOfSightRequirementConfig.FromVerb;

        /// <summary>
        /// 当前条目声明的近战 Tool。
        /// 主要用于近战 Verb 的原版初始化链。
        /// </summary>
        public Tool Tool;

        /// <summary>
        /// 当前条目声明的近战 Tool 列表。
        /// 当一条近战表达需要多把 Tool 参与运行时选择时，作者在这里按顺序声明。
        /// </summary>
        public List<Tool> tools;

        /// <summary>
        /// 当前条目全部 Tool 的可选语言包名称键。
        /// 顺序依次对应单 Tool（若有）和 tools 列表；缺少对应项时沿用 Tool 原始标签。
        /// </summary>
        public List<string> ToolLabelKeys;

        /// <summary>
        /// 当前条目声明的 Maneuver。
        /// </summary>
        public ManeuverDef Maneuver;

        /// <summary>
        /// 当前条目的统一作者侧执行配置。
        /// 推荐作者使用这组字段，而不是直接写内部正式模型。
        /// </summary>
        public ChipAttackExecutionConfig Execution;

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
        /// 当前条目向外声明的被动键。
        /// </summary>
        public string PassiveKey;

        /// <summary>
        /// 当前条目向外暴露的轻量附加数据。
        /// </summary>
        public List<PassiveExpressionExposedDatumConfig> ExposedData;

        /// <summary>
        /// 当前条目挂载的远程攻击模块列表。
        /// 顺序语义完全以 XML 中的书写顺序为准。
        /// </summary>
        public List<RangedModuleMountConfig> RangedModules;

        /// <summary>
        /// 当前条目向其它符合能力条件的远程表达结果发布的开放式增强声明。
        /// 它与 RangedModules（当前结果自身模块）严格分开。
        /// </summary>
        public List<RangedModuleAugmentationConfig> RangedModuleAugmentations;

        /// <summary>
        /// 当前条目的中性投射物属性覆盖。
        /// 为 null = 不覆盖任何投射物属性，沿用投射物 Def 原生值。
        /// </summary>
        public ProjectileOverrides ProjectileOverrides;
    }
}
