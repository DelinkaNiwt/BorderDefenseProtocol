using RimWorld;
using Verse;

namespace BDP.Core.Projectiles.Interaction
{
    /// <summary>
    /// 统一解释投射物额外许可的拦截判定。
    ///
    /// 该类只处理“有额外许可时如何放行非目标”的共性规则；没有许可时，调用方继续使用原版结果。
    /// </summary>
    internal static class ProjectileInterceptionDecision
    {
        /// <summary>
        /// 把明确目标发射分支的原版命中标志适配为当前许可语义。
        /// 无许可或没有实体目标时原样返回，保证普通投射物保持原版行为。
        /// </summary>
        /// <param name="vanillaFlags">调用方原本准备传给原版 Launch 的命中标志。</param>
        /// <param name="intendedTarget">当前投射物的意图目标。</param>
        /// <param name="policy">当前投射物冻结的交互策略。</param>
        /// <returns>适配后的原版命中标志。</returns>
        internal static ProjectileHitFlags ResolveIntendedLaunchHitFlags(
            ProjectileHitFlags vanillaFlags,
            LocalTargetInfo intendedTarget,
            ProjectileInteractionPolicy policy)
        {
            if (!AllowsNonTargetPassThrough(policy) || !intendedTarget.HasThing)
            {
                return vanillaFlags;
            }

            // IntendedTarget 保留，确保最终目标仍可命中；两个非目标类别移除后，
            // 原版拦截器会自然放行中途 Pawn 与世界物体。
            return vanillaFlags
                & ~ProjectileHitFlags.NonTargetWorld
                & ~ProjectileHitFlags.NonTargetPawns;
        }

        /// <summary>
        /// 判断 BDP 自有路径扫描发现的实体是否应作为非目标放行。
        /// </summary>
        /// <param name="candidate">路径当前发现的实体。</param>
        /// <param name="intendedTarget">当前投射物的意图目标。</param>
        /// <param name="policy">当前投射物冻结的交互策略。</param>
        /// <returns>应放行时返回 true。</returns>
        internal static bool ShouldPassThroughNonTarget(
            Thing candidate,
            LocalTargetInfo intendedTarget,
            ProjectileInteractionPolicy policy)
        {
            if (candidate == null
                || !candidate.Spawned
                || !intendedTarget.HasThing
                || candidate == intendedTarget.Thing)
            {
                return false;
            }

            return AllowsNonTargetPassThrough(policy);
        }

        /// <summary>
        /// 读取统一的非目标放行策略。
        /// </summary>
        private static bool AllowsNonTargetPassThrough(ProjectileInteractionPolicy policy)
        {
            return policy != null && policy.AllowNonTargetPassThrough;
        }
    }
}
