using System.Collections.Generic;
using BDP.Core.Semantics;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 统一表达条目可复用的最小公共字段基类。
    /// 虽然文件名仍沿用旧名，
    /// 但职责已经收缩为“条目公共声明块”，不再代表四列表主结构。
    /// </summary>
    public abstract class ExpressionSourceConfigBase
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
        /// 当前条目的角色键。
        /// 第一版先保留为字符串，不抢先做完整角色词典。
        /// </summary>
        public string RoleKey;

        /// <summary>
        /// 当前条目的轻量标签集合。
        /// </summary>
        public List<string> Tags;

        /// <summary>
        /// 当前条目声明的成立条件集合。
        /// 第一版先只保留原始结构，不在这里实现判定器。
        /// </summary>
        public List<ExpressionSourceConditionConfig> Conditions;

        /// <summary>
        /// 当前条目自己的 Trion 参数块。
        /// </summary>
        public ExpressionSourceTrionConfig Trion;

        /// <summary>
        /// 当前条目附带的语义来源种类。
        /// </summary>
        public SemanticSourceKind SemanticSourceKind = SemanticSourceKind.Unknown;

        /// <summary>
        /// 当前条目的表现配置块。
        /// 它只承载轻量表现引用，不直接承载完整视觉作者参数。
        /// </summary>
        public ExpressionPresentationConfig Presentation;
    }
}
