using System;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 统一判断正式表达结果是否来自同一枚芯片实例。
    /// 优先使用运行期 ThingID；读档恢复边界缺失 ThingID 时，回退到侧位、槽位与 DefName。
    /// </summary>
    internal static class ExpressionSourceReferenceMatcher
    {
        /// <summary>
        /// 构建稳定的芯片实例键；来源为空时返回空值。
        /// </summary>
        internal static string BuildChipInstanceKey(ExpressionSourceReference sourceReference)
        {
            if (sourceReference == null)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(sourceReference.ChipThingId))
            {
                return "thing:" + sourceReference.ChipThingId;
            }

            string chipDefName = !string.IsNullOrWhiteSpace(sourceReference.ChipDefName)
                ? sourceReference.ChipDefName
                : "unknown";
            return "slot:" + sourceReference.Side + ":" + sourceReference.SlotIndex + ":" + chipDefName;
        }

        /// <summary>
        /// 判断两个正式来源是否指向同一枚芯片实例。
        /// 任一来源无法形成实例键时返回否，避免把两个空来源误判为相同。
        /// </summary>
        internal static bool AreSameChipInstance(
            ExpressionSourceReference left,
            ExpressionSourceReference right)
        {
            string leftKey = BuildChipInstanceKey(left);
            string rightKey = BuildChipInstanceKey(right);
            return !string.IsNullOrWhiteSpace(leftKey)
                && !string.IsNullOrWhiteSpace(rightKey)
                && string.Equals(leftKey, rightKey, StringComparison.Ordinal);
        }
    }
}
