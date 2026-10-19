using System;
using System.Collections.Generic;
using BDP.Core.Projectiles.RangedFlightProtocol.Effects;
using BDP.Core.Projectiles.RangedFlightProtocol.Model;
using UnityEngine;
using Verse;

namespace BDP.Content.RangedModules.ImpactVisual
{
    /// <summary>
    /// 刀刃命中特效计划执行器。
    /// </summary>
    public sealed class ImpactVisualEffectExecutor : IExtraEffectPlanExecutor
    {
        /// <summary>
        /// 刀刃命中特效的效果键。
        /// </summary>
        public const string EffectKindId = "impactBladeBurst";

        /// <summary>
        /// Effecter Def 参数键。
        /// </summary>
        public const string EffecterDefParameter = "effecterDef";

        /// <summary>
        /// 命中瞬间精确平面位置的 X 坐标参数键。
        /// </summary>
        public const string ImpactExactXParameter = "impactExactX";

        /// <summary>
        /// 命中瞬间精确平面位置的 Z 坐标参数键。
        /// </summary>
        public const string ImpactExactZParameter = "impactExactZ";

        /// <summary>
        /// 命中瞬间目标显示中心的 X 坐标参数键。
        /// </summary>
        public const string ImpactTargetXParameter = "impactTargetX";

        /// <summary>
        /// 命中瞬间目标显示中心的 Z 坐标参数键。
        /// </summary>
        public const string ImpactTargetZParameter = "impactTargetZ";

        /// <summary>
        /// 同一攻击实例对同一 Thing（实体）的短期去重窗口。
        /// </summary>
        private const int DuplicateWindowTicks = 120;

        /// <summary>
        /// 缺少攻击实例标识时使用的保守时间桶大小。
        /// </summary>
        private const int FallbackBucketTicks = 30;

        /// <summary>
        /// 已播放的短期去重键及其生成时间，不进入存档。
        /// </summary>
        private static readonly Dictionary<string, int> RecentlyPlayed = new Dictionary<string, int>();

        /// <summary>
        /// 当前执行器承接的效果种类键。
        /// </summary>
        public string EffectKind
        {
            get { return EffectKindId; }
        }

        /// <summary>
        /// 解析目标与 Effecter 后生成一次受维护的原版特效。
        /// </summary>
        public bool TryExecute(ExtraEffectPlan effectPlan, ExtraEffectExecutionContext context)
        {
            if (effectPlan == null || context == null)
            {
                return false;
            }

            Thing targetThing = context.TargetThing;
            if (targetThing == null
                || !targetThing.Spawned
                || context.Map == null
                || targetThing.Map != context.Map
                || !context.TargetCell.IsValid
                || !context.TargetCell.InBounds(context.Map))
            {
                return false;
            }

            EffecterDef effecterDef = ResolveEffecterDef(effectPlan);
            if (effecterDef == null)
            {
                return false;
            }

            int currentTick = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            CleanupExpired(currentTick);
            string deduplicationKey = BuildDeduplicationKey(context, targetThing, currentTick);
            if (RecentlyPlayed.ContainsKey(deduplicationKey))
            {
                return false;
            }

            RecentlyPlayed[deduplicationKey] = currentTick;
            Vector3 targetPosition;
            bool hasTargetPosition = TryResolveImpactTargetPosition(
                effectPlan,
                targetThing,
                out targetPosition);
            Vector3 exactPosition;
            TryResolveImpactExactPosition(
                effectPlan,
                context.TargetCell,
                targetPosition,
                out exactPosition);
            Vector3 impactAnchorPosition = hasTargetPosition
                ? targetPosition
                : exactPosition;
            SpawnMaintainedAtExactPosition(
                effecterDef,
                context.TargetCell,
                context.Map,
                targetThing,
                impactAnchorPosition,
                1f);
            return true;
        }

        /// <summary>
        /// 使用原版 Effecter（特效器）接口在命中瞬间目标锚点生成并维护特效。
        /// SpawnMaintained 没有小数偏移重载，因此复用 Spawn + EffecterMaintainer 的原版实现路径。
        /// </summary>
        /// <param name="effecterDef">待生成的特效器定义。</param>
        /// <param name="targetCell">命中所在格，供原版维护器定位。</param>
        /// <param name="map">当前地图。</param>
        /// <param name="impactAnchorPosition">命中瞬间的目标显示中心；没有目标快照时才使用精确命中点。</param>
        /// <param name="scale">Effecter 基础缩放；目标尺寸由动态刀刃子特效器单独解析。</param>
        private static void SpawnMaintainedAtExactPosition(
            EffecterDef effecterDef,
            IntVec3 targetCell,
            Map map,
            Thing targetThing,
            Vector3 impactAnchorPosition,
            float scale)
        {
            IntVec3 sourceCell = impactAnchorPosition.ToIntVec3();
            if (!sourceCell.InBounds(map))
            {
                sourceCell = targetCell;
                impactAnchorPosition = sourceCell.ToVector3Shifted();
            }

            Vector3 anchorOffset = impactAnchorPosition - sourceCell.ToVector3Shifted();
            anchorOffset.y = 0f;
            Effecter effecter = effecterDef.Spawn();
            effecter.offset = anchorOffset;
            effecter.scale = scale;
            TargetInfo sourceInfo = new TargetInfo(sourceCell, map);
            TargetInfo targetInfo = new TargetInfo(targetThing);
            effecter.Trigger(sourceInfo, targetInfo);
            if (effecterDef.maintainTicks != 0)
            {
                map.effecterMaintainer.AddEffecterToMaintain(
                    effecter,
                    targetCell,
                    effecterDef.maintainTicks);
            }
            else
            {
                effecter.Cleanup();
            }
        }

        /// <summary>
        /// 读取模块在投射物销毁前缓存的精确命中位置。
        /// </summary>
        /// <param name="effectPlan">当前投射物的额外效果计划。</param>
        /// <param name="targetCell">命中格回退值。</param>
        /// <param name="exactPosition">输出命中瞬间的精确平面位置。</param>
        /// <returns>成功读取缓存坐标时返回 true。</returns>
        private static bool TryResolveImpactExactPosition(
            ExtraEffectPlan effectPlan,
            IntVec3 targetCell,
            Vector3 targetPosition,
            out Vector3 exactPosition)
        {
            exactPosition = targetPosition;
            if (!IsFinite(exactPosition.x) || !IsFinite(exactPosition.z))
            {
                exactPosition = targetCell.ToVector3Shifted();
            }

            float exactX;
            float exactZ;
            if (!TryReadFiniteFloat(effectPlan, ImpactExactXParameter, out exactX)
                || !TryReadFiniteFloat(effectPlan, ImpactExactZParameter, out exactZ))
            {
                return false;
            }

            exactPosition = new Vector3(exactX, 0f, exactZ);
            return true;
        }

        /// <summary>
        /// 读取模块在投射物销毁前缓存的目标显示中心；缺失时回退到当前 Thing 中心。
        /// </summary>
        /// <param name="effectPlan">当前投射物的额外效果计划。</param>
        /// <param name="targetThing">当前受击 Thing（实体）。</param>
        /// <param name="targetPosition">输出命中瞬间目标显示中心。</param>
        /// <returns>成功读取缓存坐标时返回 true。</returns>
        private static bool TryResolveImpactTargetPosition(
            ExtraEffectPlan effectPlan,
            Thing targetThing,
            out Vector3 targetPosition)
        {
            targetPosition = targetThing != null ? targetThing.DrawPos : Vector3.zero;
            targetPosition.y = 0f;
            float targetX;
            float targetZ;
            if (!TryReadFiniteFloat(effectPlan, ImpactTargetXParameter, out targetX)
                || !TryReadFiniteFloat(effectPlan, ImpactTargetZParameter, out targetZ))
            {
                return false;
            }

            targetPosition = new Vector3(targetX, 0f, targetZ);
            return true;
        }

        /// <summary>
        /// 判断浮点坐标是否有效。
        /// </summary>
        /// <param name="value">待检查的坐标值。</param>
        /// <returns>坐标值不是 NaN（非数字）或无穷值时返回 true。</returns>
        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        /// <summary>
        /// 读取一个使用不变区域格式化的有限浮点参数。
        /// </summary>
        /// <param name="effectPlan">当前额外效果计划。</param>
        /// <param name="key">参数键。</param>
        /// <param name="value">输出浮点值。</param>
        /// <returns>参数存在且可解析时返回 true。</returns>
        private static bool TryReadFiniteFloat(
            ExtraEffectPlan effectPlan,
            string key,
            out float value)
        {
            value = 0f;
            string rawValue;
            if (effectPlan == null
                || effectPlan.Parameters == null
                || !effectPlan.Parameters.TryGetValue(key, out rawValue)
                || !float.TryParse(
                    rawValue,
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out value))
            {
                return false;
            }

            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        /// <summary>
        /// 从额外效果参数解析 Effecter Def。
        /// </summary>
        private static EffecterDef ResolveEffecterDef(ExtraEffectPlan effectPlan)
        {
            string defName;
            if (effectPlan.Parameters == null
                || !effectPlan.Parameters.TryGetValue(EffecterDefParameter, out defName)
                || string.IsNullOrWhiteSpace(defName))
            {
                return null;
            }

            return DefDatabase<EffecterDef>.GetNamedSilentFail(defName);
        }

        /// <summary>
        /// 构造包含攻击实例与受击 Thing 身份的短期去重键。
        /// </summary>
        private static string BuildDeduplicationKey(
            ExtraEffectExecutionContext context,
            Thing targetThing,
            int currentTick)
        {
            string attackInstanceId = context.AttackInstanceId;
            if (string.IsNullOrWhiteSpace(attackInstanceId))
            {
                int instigatorId = context.Instigator != null ? context.Instigator.thingIDNumber : 0;
                attackInstanceId = "fallback:" + instigatorId + ":" + (currentTick / FallbackBucketTicks);
            }

            return attackInstanceId + ":thing:" + targetThing.thingIDNumber;
        }

        /// <summary>
        /// 清理过期键，保持静态短表不会随战斗次数增长。
        /// </summary>
        private static void CleanupExpired(int currentTick)
        {
            if (RecentlyPlayed.Count == 0)
            {
                return;
            }

            List<string> expiredKeys = null;
            foreach (KeyValuePair<string, int> entry in RecentlyPlayed)
            {
                if (currentTick - entry.Value <= DuplicateWindowTicks)
                {
                    continue;
                }

                if (expiredKeys == null)
                {
                    expiredKeys = new List<string>();
                }

                expiredKeys.Add(entry.Key);
            }

            if (expiredKeys == null)
            {
                return;
            }

            for (int index = 0; index < expiredKeys.Count; index++)
            {
                RecentlyPlayed.Remove(expiredKeys[index]);
            }
        }
    }
}
