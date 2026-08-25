using System.Collections.Generic;
using BDP.Core.AttackExecution;
using BDP.Core.Chips;
using BDP.Core.CombatModel;
using BDP.Core.Expressions;
using BDP.Core.Verbs;
using RimWorld;
using Verse;

namespace BDP.Core.Combos
{
    /// <summary>
    /// 组合技定义层的一条表达条目。
    /// 它尽量贴近芯片表达条目写法，只补组合技自己需要的自动求值声明。
    /// </summary>
    public sealed class ComboExpressionEntryConfig : ExpressionSourceConfigBase
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
        /// 当前条目对 VerbProps 的增量覆盖层。
        /// 只承载作者显式声明的字段级 delta，不是完整 VerbProperties。
        /// 未声明的字段为 null——代码始终以 Main 侧为基底，只在非 null 字段做覆盖。
        /// </summary>
        public VerbPropsOverlay VerbProps;

        /// <summary>
        /// 当前条目是否必须满足射手到语义目标的直射 LOS。
        /// 它只表达 dual 等入口层的必要直射语义，不覆盖模块内部的路径段 LOS 规则。
        /// </summary>
        public DirectTargetLineOfSightRequirementConfig DirectTargetLineOfSight = DirectTargetLineOfSightRequirementConfig.FromVerb;

        /// <summary>
        /// 当前条目缺失 VerbProps 字段时的自动求值声明。
        /// </summary>
        public ComboVerbPropsResolutionConfig VerbPropsResolve;

        /// <summary>
        /// 当前条目声明的近战 Tool。
        /// 主要用于近战 Verb 的原版初始化链。
        /// </summary>
        public Tool Tool;

        /// <summary>
        /// 当前条目声明的近战 Tool 列表。
        /// 当一条近战组合表达需要多把 Tool 参与运行时选择时，作者在这里按顺序声明。
        /// </summary>
        public List<Tool> tools;

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
        /// 当前条目缺失执行节奏字段时的自动求值声明。
        /// </summary>
        public ComboExecutionResolutionConfig ExecutionResolve;

        /// <summary>
        /// 当前条目缺失 Trion 字段时的自动求值声明。
        /// </summary>
        public ComboExpressionTrionResolutionConfig TrionResolve;

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
        /// combo 条目和芯片条目在这里应保持同构，
        /// 这样它们进入正式结果层后才能被完全等价对待。
        /// </summary>
        public List<RangedModuleMountConfig> RangedModules;

        /// <summary>
        /// 当前组合条目的中性投射物属性覆盖。
        /// 武装构型在组合结果副本上写入，随后随解释链进入正式运行时规格。
        /// </summary>
        public ProjectileOverrides ProjectileOverrides;

        /// <summary>
        /// 把组合技条目映射成现有芯片表达解释器可消费的普通条目。
        /// 组合技不新开第二套解释链，只在入口做一次结构对齐。
        /// </summary>
        public ChipExpressionEntryConfig ToChipExpressionEntryConfig()
        {
            return new ChipExpressionEntryConfig
            {
                Id = Id,
                DisplayLabel = DisplayLabel,
                DisplayLabelKey = DisplayLabelKey,
                RoleKey = RoleKey,
                Tags = Tags != null ? new List<string>(Tags) : new List<string>(),
                Conditions = Conditions != null ? new List<ExpressionSourceConditionConfig>(Conditions) : new List<ExpressionSourceConditionConfig>(),
                Trion = Trion,
                SemanticSourceKind = SemanticSourceKind,
                Kind = Kind,
                RelationKind = RelationKind,
                ParentEntryId = ParentEntryId,
                WeaponMode = WeaponMode,
                // 传递最小桩 VerbProps 通过解释器验证，实际数据由
                // ComboFormalExpressionResultFactory 从 Main 侧取。
                VerbProps = BuildStubVerbProps(),
                DirectTargetLineOfSight = DirectTargetLineOfSight,
                Tool = Tool,
                tools = tools != null ? new List<Tool>(tools) : new List<Tool>(),
                Maneuver = Maneuver,
                // Execution 不传递：combo 的执行风格由 ResolveComboExecutionStyle
                // 以 Main 侧为基底、ExecutionResolve 做 overlay 决定。
                // 传递会触发解释器创建默认风格，阻塞 Main 侧的完整数据。
                Execution = null,
                AbilityDefName = AbilityDefName,
                HediffDefName = HediffDefName,
                HediffApplyModeKey = HediffApplyModeKey,
                PassiveKey = PassiveKey,
                ExposedData = ExposedData != null ? new List<PassiveExpressionExposedDatumConfig>(ExposedData) : new List<PassiveExpressionExposedDatumConfig>(),
                RangedModules = RangedModules != null ? CloneRangedModules(RangedModules) : new List<RangedModuleMountConfig>(),
                ProjectileOverrides = ProjectileOverrides != null ? ProjectileOverrides.Clone() : null,
                Presentation = Presentation != null
                    ? new ExpressionPresentationConfig
                    {
                        ManualEntryIconTexPath = Presentation.ManualEntryIconTexPath,
                        VisualPresetDefName = Presentation.VisualPresetDefName,
                        VisualGraphicOverrideDefName = Presentation.VisualGraphicOverrideDefName,
                        CompositeVisualPresetDefName = Presentation.CompositeVisualPresetDefName,
                        ForceSuppressHostEquipment = Presentation.ForceSuppressHostEquipment,
                        VisualPriority = Presentation.VisualPriority
                    }
                    : null
            };
        }

        /// <summary>
        /// 构造最小桩 VerbProps。只用于通过解释器非近战条目验证，
        /// 实际 VerbProps 由 ComboFormalExpressionResultFactory 从 Main 侧完整数据构建。
        /// </summary>
        private static VerbProperties BuildStubVerbProps()
        {
            return new VerbProperties
            {
                verbClass = typeof(BdpVerb_Shoot),
                hasStandardCommand = true,
                label = "combo_stub"
            };
        }

        /// <summary>
        /// 深复制模块挂载快照。
        /// 组合技继续复用现有模块解释链，但不能把可变配置引用直接共享给下游。
        /// </summary>
        private static List<RangedModuleMountConfig> CloneRangedModules(IReadOnlyList<RangedModuleMountConfig> source)
        {
            List<RangedModuleMountConfig> result = new List<RangedModuleMountConfig>();
            if (source == null)
            {
                return result;
            }

            for (int i = 0; i < source.Count; i++)
            {
                RangedModuleMountConfig module = source[i];
                if (module == null)
                {
                    continue;
                }

                result.Add(module.Clone());
            }

            return result;
        }
    }
}
