using Verse;

namespace BDP.Core.Requirements
{
    /// <summary>
    /// 所有“角色能否满足某项使用条件”共同继承的中性配置基类。
    /// 芯片激活和 Combo 使用只决定调用时机，不重复实现条件规则。
    /// </summary>
    public abstract class PawnRequirement
    {
        /// <summary>玩家可读的条件名称。</summary>
        public abstract string Label { get; }

        /// <summary>构造不绑定角色的静态要求快照。</summary>
        public abstract PawnRequirementSnapshot Describe();

        /// <summary>检查本条作者配置；合法时返回空文本。</summary>
        public abstract string ValidateDefinition();

        /// <summary>对指定角色求值并返回玩家可读结果。</summary>
        public abstract PawnRequirementSnapshot Evaluate(Pawn pawn);
    }
}
