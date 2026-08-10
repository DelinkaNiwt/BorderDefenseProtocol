using System.Collections.Generic;

namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// 模块阶段共享裁决器。
    /// 当前只收口同维度覆盖与冻结判定，不解释业务本身。
    /// </summary>
    internal sealed class ModuleStageArbitrator
    {
        /// <summary>
        /// 当前阶段各覆盖维度的最终拥有者索引。
        /// </summary>
        private readonly Dictionary<string, int> overrideOwners = new Dictionary<string, int>();

        /// <summary>
        /// 当前阶段的冻结集合。
        /// </summary>
        public ModuleStageFreezeSet FreezeSet { get; } = new ModuleStageFreezeSet();

        /// <summary>
        /// 为一组覆盖维度登记最新拥有者。
        /// 后写模块会覆盖前写模块的 owner。
        /// </summary>
        public bool TryClaimOverride<TDimension>(IReadOnlyList<TDimension> dimensions, int moduleIndex)
        {
            if (dimensions == null)
            {
                return false;
            }

            bool claimed = false;
            for (int i = 0; i < dimensions.Count; i++)
            {
                string dimensionKey = BuildDimensionKey(dimensions[i]);
                if (string.IsNullOrWhiteSpace(dimensionKey) || FreezeSet.IsFrozen(dimensionKey))
                {
                    continue;
                }

                overrideOwners[dimensionKey] = moduleIndex;
                claimed = true;
            }

            return claimed;
        }

        /// <summary>
        /// 判断当前模块是否可以对指定维度生效。
        /// </summary>
        public bool CanApply<TDimension>(TDimension dimension, int moduleIndex, bool claimsOverride)
        {
            string dimensionKey = BuildDimensionKey(dimension);
            if (string.IsNullOrWhiteSpace(dimensionKey) || FreezeSet.IsFrozen(dimensionKey))
            {
                return false;
            }

            if (!claimsOverride)
            {
                return true;
            }

            return overrideOwners.TryGetValue(dimensionKey, out int ownerIndex) && ownerIndex == moduleIndex;
        }

        /// <summary>
        /// 把阶段维度值映射成稳定字符串键。
        /// </summary>
        private static string BuildDimensionKey<TDimension>(TDimension dimension)
        {
            return dimension != null ? dimension.ToString() : null;
        }
    }
}
