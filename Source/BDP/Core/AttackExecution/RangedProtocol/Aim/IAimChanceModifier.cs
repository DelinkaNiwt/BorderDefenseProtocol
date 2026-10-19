using Verse;

namespace BDP.Core.AttackExecution.RangedProtocol.Aim
{
    /// <summary>
    /// 在该发真实射击报告生成后修正标准目标瞄准概率的中性扩展口。
    /// 不另掷骰，不改目标体型、掩体、强制失准或物理碰撞。
    /// </summary>
    public interface IAimChanceModifier
    {
        /// <summary>返回当前模块修正后的标准目标瞄准概率。</summary>
        float ModifyStandardAimChance(in AimChanceContext context);
    }

    /// <summary>正式发射时的只读精度事实，不暴露可修改的发射计划。</summary>
    public readonly struct AimChanceContext
    {
        /// <summary>使用与本发原版射击报告一致的发射者和来源构造上下文。</summary>
        public AimChanceContext(Thing caster, string resultId, float standardAimChance)
        {
            Caster = caster;
            ResultId = resultId;
            StandardAimChance = standardAimChance;
        }

        /// <summary>生成原版射击报告的实际发射者。</summary>
        public Thing Caster { get; }

        /// <summary>该发真实来源结果标识，供混合双持隔离模块影响。</summary>
        public string ResultId { get; }

        /// <summary>已经包含既有精度倍率的标准目标瞄准概率。</summary>
        public float StandardAimChance { get; }
    }
}
