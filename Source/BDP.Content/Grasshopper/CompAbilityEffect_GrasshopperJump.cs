using BDP.Core.Abilities;
using BDP.Core.PathInput;
using BDP.Core.Trion;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Content.Grasshopper
{
    /// <summary>
    /// 蚱蜢跳跃 Ability 效果配置。
    /// 参照 SenkuKogetsuWave 模式：业务参数在 Props 声明，业务逻辑在 Apply() 执行。
    /// </summary>
    public class CompProperties_GrasshopperJump : CompProperties_AbilityEffect
    {
        /// <summary>蚱蜢跳跃使用的 PawnFlyer ThingDef。</summary>
        public ThingDef pawnFlyerDef;

        /// <summary>构造并绑定对应的 effect comp 类型。</summary>
        public CompProperties_GrasshopperJump()
        {
            compClass = typeof(CompAbilityEffect_GrasshopperJump);
        }
    }

    /// <summary>
    /// 蚱蜢跳跃 Ability 效果组件。
    ///
    /// 支持两种模式：
    ///   1. 单段跳跃（降级）：无路径数据时执行标准单次弹射。
    ///   2. 多段链式跳跃：Verb 提供路径数据时，逐段链式弹射——每段落地后
    ///      通过 PawnFlyer.onLanded 回调自动触发下一段。
    ///
    /// 完整调用链：
    ///   BdpVerb_CastAbility.TryCastShot()
    ///     → TryCommitTrionCosts()      扣第一段 Trion
    ///     → TriggerCastJitter()        抖动（蚱蜢禁用了）
    ///     → ability.Activate()
    ///         → PreActivate()          StartCooldown()
    ///         → ApplyEffects()
    ///             → CompAbilityEffect_GrasshopperJump.Apply()  跳跃 ← 本类
    /// </summary>
    public class CompAbilityEffect_GrasshopperJump : CompAbilityEffect
    {
        /// <summary>当前效果组件使用的强类型配置。</summary>
        protected new CompProperties_GrasshopperJump Props
        {
            get { return (CompProperties_GrasshopperJump)props; }
        }

        /// <summary>
        /// 对当前目标应用蚱蜢跳跃效果。
        ///
        /// 多段模式：
        ///   - 首段：本方法被 ApplyEffects 直接调用，对首个路径点执行跳跃。
        ///   - 后续段：由前一段的 PawnFlyer.onLanded 回调递归触发。
        ///
        /// 每段独立扣 Trion（除首段已在 TryCommitTrionCosts 中扣除）。
        /// </summary>
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            Pawn pawn = parent.pawn;
            if (pawn == null) return;

            Map map = pawn.Map;
            if (map == null) return;

            ThingDef flyerDef = Props.pawnFlyerDef;
            if (flyerDef == null)
            {
                Log.Error("[Grasshopper] pawnFlyerDef not configured in CompProperties_GrasshopperJump.");
                return;
            }

            VerbProperties verbProps = parent.def?.verbProperties;

            // 检查是否为多段链式跳跃
            Verb_CastAbilityGrasshopper grasshopperVerb = parent.verb as Verb_CastAbilityGrasshopper;
            if (grasshopperVerb != null && grasshopperVerb.HasPendingWaypoints)
            {
                // 快照总段数（供最终段累加冷却用）
                totalChainSegments = grasshopperVerb.PendingWaypointCount;

                // 多段模式：执行当前段，链式触发后续段
                ExecuteChainJump(pawn, grasshopperVerb, verbProps, flyerDef, isFirstSegment: true);
            }
            else
            {
                // 单段降级：标准跳跃
                GrasshopperUtility.DoJump(pawn, target.Cell, verbProps, flyerDef);
            }
        }

        /// <summary>链式跳跃总段数（供最终段累加冷却）。</summary>
        protected int totalChainSegments = 1;

        /// <summary>
        /// 多段链式跳跃执行。
        /// 首段：读取 Verb 的第一个路径点执行跳跃，消耗 Verb 状态。
        /// 后续段：由 onLanded 回调递归触发，无需再读 Verb。
        /// </summary>
        /// <param name="isFirstSegment">true 表示首段（需从 Verb 消费路径点并扣 Trion），false 表示回调触发。</param>
        protected virtual void ExecuteChainJump(
            Pawn pawn,
            Verb_CastAbilityGrasshopper verb,
            VerbProperties verbProps,
            ThingDef flyerDef,
            bool isFirstSegment)
        {
            if (verb == null) return;

            // 获取下一个路径点
            PathAnchor waypoint = verb.ConsumeNextWaypoint();
            if (waypoint == null) return;

            IntVec3 targetCell = waypoint.ToCell();

            // 后续段需要独立扣 Trion（首段已在 TryCommitTrionCosts 中扣除）
            if (!isFirstSegment)
            {
                if (!TryConsumeSegmentTrion(pawn))
                {
                    // Trion 不足，中断链式跳跃
                    return;
                }
            }

            // 【关键】PawnFlyer.MakeFlyer 内部会 despawn Pawn，
            // 所以必须在调用前保存 Map、Position 和选中状态。
            Map map = pawn.Map;
            IntVec3 startPosition = pawn.Position;
            bool wasSelected = Find.Selector.IsSelected(pawn);

            // 起跳前朝向跳跃方向（链式跳跃绕过了 JobDriver，需手动更新旋转）
            pawn.rotationTracker?.FaceCell(targetCell);

            // 执行本段跳跃
            PawnFlyer flyer = PawnFlyer.MakeFlyer(
                flyerDef, pawn, targetCell,
                verbProps?.flightEffecterDef,
                verbProps?.soundLanding,
                verbProps?.flyWithCarriedThing ?? false,
                null);

            if (flyer == null) return;

            // 子类钩子：每段起跳前的自定义逻辑（如清空伤害集）
            OnBeforeSegmentJump(pawn, flyer);

            // 设置链式回调：本段落地后自动触发下一段
            bool hasMoreSegments = verb.HasPendingWaypoints;
            if (hasMoreSegments)
            {
                PawnFlyer_Grasshopper grasshopperFlyer = flyer as PawnFlyer_Grasshopper;
                if (grasshopperFlyer != null)
                {
                    grasshopperFlyer.onLanded = () =>
                    {
                        ExecuteChainJump(pawn, verb, verbProps, flyerDef, isFirstSegment: false);
                    };
                }
            }
            else
            {
                // 最终段落地后：累加冷却（N 段 × 基础冷却值）
                ApplyCumulativeCooldown();
            }

            // 生成飞行器（使用保存的 map）
            GenSpawn.Spawn(flyer, targetCell, map);

            // 起跳前特效
            Vector3 vector = (targetCell - startPosition).ToVector3();
            vector.Normalize();
            FleckMaker.ThrowDustPuff(startPosition.ToVector3Shifted() - vector, map, 2f);

            // 保持选中状态：MakeFlyer despawn 了 Pawn，需用之前保存的状态重选
            if (wasSelected)
            {
                Find.Selector.Select(pawn, playSound: false, forceDesignatorDeselect: false);
            }
        }

        /// <summary>
        /// 为后续段（非首段）消耗单段 Trion 成本。
        /// 首段已在 TryCommitTrionCosts → TryCommitCastCost 中扣除。
        /// 后续段通过 TrionSurfaceAccess 公共 API 直接消耗。
        /// </summary>
        /// <returns>是否成功消耗。</returns>
        protected virtual bool TryConsumeSegmentTrion(Pawn pawn)
        {
            ITrionCommands commands = TrionSurfaceAccess.ResolveCommands(pawn);
            if (commands == null) return true; // 无 Trion 系统则免费

            // 从表达系统动态读取 UseCost（与首段使用同一套 Trion 配置）
            float segmentCost = ResolveUseCost();
            if (segmentCost <= 0f) return true;

            return commands.TryConsume(segmentCost);
        }

        /// <summary>
        /// 从 Ability 绑定的 Trion 成本组件动态读取 UseCost。
        /// 与 TryCommitTrionCosts（首段）走相同的表达系统绑定路径。
        /// </summary>
        /// <summary>
        /// 每段起跳前的钩子。子类覆写以注入自定义逻辑（如弹射砍击清空本段伤害记录）。
        /// </summary>
        protected virtual void OnBeforeSegmentJump(Pawn pawn, PawnFlyer flyer) { }

        /// <summary>
        /// 最终段落地后累加冷却：基础冷却 × 总段数。
        /// PreActivate 首段已设过一次基础冷却，这里用总值覆盖。
        /// 子类可覆写，弹射砍击（不同冷却值）自动继承生效。
        /// </summary>
        protected virtual void ApplyCumulativeCooldown()
        {
            if (parent == null) return;
            int baseCooldown = parent.def != null
                ? parent.def.cooldownTicksRange.RandomInRange : 300;
            int totalCooldown = baseCooldown * totalChainSegments;
            parent.StartCooldown(totalCooldown);
        }

        protected float ResolveUseCost()
        {
            if (parent?.EffectComps != null)
            {
                for (int i = 0; i < parent.EffectComps.Count; i++)
                {
                    if (parent.EffectComps[i] is CompAbilityEffect_BdpTrionCost trionCost)
                    {
                        return trionCost.TrionCost;
                    }
                }
            }
            return 30f; // 降级默认值
        }
    }
}
