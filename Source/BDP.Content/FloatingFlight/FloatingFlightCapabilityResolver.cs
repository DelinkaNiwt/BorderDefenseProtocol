using System.Collections.Generic;
using BDP.Core.Expressions;
using Verse;

namespace BDP.Content.FloatingFlight
{
    /// <summary>
    /// 浮空芯片能力解析器。
    /// 只读取 Core 发布的被动表达投影，不扫描装备实现细节。
    /// </summary>
    public static class FloatingFlightCapabilityResolver
    {
        /// <summary>浮空芯片发布的稳定被动身份键。</summary>
        public const string PassiveIdentityKey = "BDP_FloatingFlight";

        /// <summary>判断指定 Pawn 当前是否至少有一个有效浮空能力来源。</summary>
        public static bool HasCapability(Pawn pawn)
        {
            ExpressionPublishedProjectionSnapshot projection =
                ExpressionSurfaceAccess.ResolvePublishedProjection(pawn);
            return FindAvailablePassive(projection) != null;
        }

        /// <summary>从公开表达投影中读取一条有效浮空被动结果。</summary>
        private static ExpressionPublishedResultSnapshot FindAvailablePassive(
            ExpressionPublishedProjectionSnapshot projection)
        {
            if (projection?.PassiveResultsByIdentityKey == null
                || !projection.PassiveResultsByIdentityKey.TryGetValue(
                    PassiveIdentityKey,
                    out IReadOnlyList<ExpressionPublishedResultSnapshot> results)
                || results == null)
            {
                return null;
            }

            for (int index = 0; index < results.Count; index++)
            {
                ExpressionPublishedResultSnapshot result = results[index];
                if (result != null && result.IsAvailable && result.IsPublished)
                {
                    return result;
                }
            }

            return null;
        }
    }
}
