using System.Collections.Generic;

namespace BDP.Core.Abilities
{
    /// <summary>有序批量目标的筛选规则；只处理合法性结果，不知道地图或具体效果。</summary>
    public static class BatchAbilityTargetSelection
    {
        /// <summary>两端必须有效；显式允许时跳过非法中间目标，否则整批拒绝。</summary>
        public static bool TrySelect(IReadOnlyList<bool> validity, bool skipInvalidInterior,
            out List<int> selectedIndices)
        {
            selectedIndices = new List<int>();
            if (validity == null || validity.Count == 0 || !validity[0] || !validity[validity.Count - 1])
                return false;
            for (int index = 0; index < validity.Count; index++)
            {
                if (validity[index]) selectedIndices.Add(index);
                else if (!skipInvalidInterior)
                {
                    selectedIndices.Clear();
                    return false;
                }
            }
            return true;
        }
    }
}
