using System.Collections.Generic;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 一条公开表达结果快照。
    /// 它只承接当前已发布投影里的稳定结果事实，不暴露内部结果对象本体。
    /// </summary>
    public sealed class ExpressionPublishedResultSnapshot
    {
        /// <summary>
        /// 当前结果稳定标识。
        /// </summary>
        public string ResultId { get; internal set; }

        /// <summary>
        /// 当前结果所属公开通道。
        /// </summary>
        public ExpressionPublishedChannelKind ChannelKind { get; internal set; }

        /// <summary>
        /// 当前结果武器模式键。
        /// 它是内部枚举的字符串投影，只服务公开读面辨识。
        /// </summary>
        public string WeaponModeKey { get; internal set; }

        /// <summary>
        /// 当前结果来源关系键。
        /// </summary>
        public string OriginKindKey { get; internal set; }

        /// <summary>
        /// 当前结果复合类型键。
        /// </summary>
        public string CompositeKindKey { get; internal set; }

        /// <summary>
        /// 当前结果来源的 ComboDef 名称。
        /// </summary>
        public string ComboDefName { get; internal set; }

        /// <summary>
        /// 当前结果显示名。
        /// </summary>
        public string DisplayLabel { get; internal set; }

        /// <summary>
        /// 当前结果在公开通道上的稳定发布键。
        /// Verb 对应执行槽位，Ability 对应 AbilityDefName，Hediff 对应 HediffDefName，Passive 对应 PassiveKey。
        /// </summary>
        public string PublishedKey { get; internal set; }

        /// <summary>
        /// 当前 Verb 结果的执行槽位键。
        /// </summary>
        public string ExecutionSlotKey { get; internal set; }

        /// <summary>
        /// 当前 Ability 结果的 AbilityDef 名称。
        /// </summary>
        public string AbilityDefName { get; internal set; }

        /// <summary>
        /// 当前 Hediff 结果的 HediffDef 名称。
        /// </summary>
        public string HediffDefName { get; internal set; }

        /// <summary>
        /// 当前 Hediff 结果的应用方式键。
        /// </summary>
        public string HediffApplyModeKey { get; internal set; }

        /// <summary>
        /// 当前 Passive 结果的被动键。
        /// </summary>
        public string PassiveKey { get; internal set; }

        /// <summary>
        /// 当前结果角色键。
        /// </summary>
        public string RoleKey { get; internal set; }

        /// <summary>
        /// 当前 Verb 结果的主副攻击身份。
        /// 非 Verb 结果默认会落成 None。
        /// </summary>
        public VerbAttackRole VerbAttackRole { get; internal set; }

        /// <summary>
        /// 当前结果轻量标签集合。
        /// </summary>
        public IReadOnlyList<string> Tags { get; internal set; }

        /// <summary>
        /// 当前结果形态键。
        /// </summary>
        public string ModeKey { get; internal set; }

        /// <summary>
        /// 当前结果是否为副攻击身份。
        /// </summary>
        public bool IsSecondaryAttack { get; internal set; }

        /// <summary>
        /// 当前结果是否可用。
        /// </summary>
        public bool IsAvailable { get; internal set; }

        /// <summary>
        /// 当前结果是否允许进入后续投影。
        /// </summary>
        public bool CanProject { get; internal set; }

        /// <summary>
        /// 当前结果是否已经具备最小公开发布条件。
        /// </summary>
        public bool IsPublished { get; internal set; }

        /// <summary>
        /// 当前结果对应的来源结果标识列表。
        /// 非复合结果通常为空。
        /// </summary>
        public IReadOnlyList<string> SourceResultIds { get; internal set; }

        /// <summary>
        /// 当前结果自身的公开来源槽位引用。
        /// </summary>
        public ExpressionPublishedSourceReference SourceReference { get; internal set; }

        /// <summary>
        /// 当前结果对应的主侧来源结果标识。
        /// </summary>
        public string MainSourceResultId { get; internal set; }

        /// <summary>
        /// 当前结果对应的副侧来源结果标识。
        /// </summary>
        public string SubSourceResultId { get; internal set; }

        /// <summary>
        /// 当前结果声明的使用 Trion 成本。
        /// </summary>
        public float TrionUseCost { get; internal set; }

        /// <summary>
        /// 当前结果声明的最低 Trion 需求。
        /// </summary>
        public float TrionMinimumRequired { get; internal set; }

        /// <summary>
        /// 当前结果按最终有效来源数声明的持续 Trion 总费用档位副本。
        /// </summary>
        public IReadOnlyList<ExpressionSustainCostBySourceCountConfig> TrionSustainCostBySourceCount { get; internal set; }

        /// <summary>
        /// 当前结果公开暴露的轻量键值数据集合。
        /// </summary>
        public IReadOnlyList<ExpressionPublishedDatum> ExposedData { get; internal set; }
    }
}
