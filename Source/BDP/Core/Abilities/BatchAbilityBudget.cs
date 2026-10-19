using System;

namespace BDP.Core.Abilities
{
    /// <summary>批量使用的中性费用计算；不保存资源或次数状态。</summary>
    public static class BatchAbilityBudget
    {
        /// <summary>计算有效目标数量对应的有限总费用。</summary>
        public static float TotalCost(float unitCost, int count)
        {
            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));
            if (unitCost < 0f || float.IsNaN(unitCost) || float.IsInfinity(unitCost))
                throw new ArgumentOutOfRangeException(nameof(unitCost));
            float total = unitCost * count;
            if (float.IsNaN(total) || float.IsInfinity(total))
                throw new ArgumentOutOfRangeException(nameof(count));
            return total;
        }

        /// <summary>总费用与单次最低资格门槛取较大值；门槛本身不重复扣除。</summary>
        public static float RequiredCost(float unitCost, float minimum, int count)
        {
            if (minimum < 0f || float.IsNaN(minimum) || float.IsInfinity(minimum))
                throw new ArgumentOutOfRangeException(nameof(minimum));
            return Math.Max(TotalCost(unitCost, count), minimum);
        }
    }
}
