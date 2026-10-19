using System.Collections.Generic;
using System.Globalization;
using BDP.Core.AttackExecution;
using BDP.Core.AttackExecution.RangedModules.Runtime;
using BDP.Core.Projectiles.RangedFlightProtocol.Impact;
using BDP.Core.Projectiles.RangedFlightProtocol.Model;
using UnityEngine;
using Verse;

namespace BDP.Content.RangedModules.ImpactVisual
{
    /// <summary>
    /// 投射物命中特效模块配置。
    /// </summary>
    public sealed class ImpactVisualConfig : RangedModuleConfigNode
    {
        /// <summary>
        /// 命中后要生成的原版 Effecter（特效器）。
        /// </summary>
        public EffecterDef EffecterDef;

        /// <summary>
        /// 复制配置快照，避免运行时继续引用 Def 默认配置对象。
        /// </summary>
        public override RangedModuleConfigNode Clone()
        {
            return new ImpactVisualConfig
            {
                EffecterDef = EffecterDef
            };
        }
    }

    /// <summary>
    /// 投射物命中后追加刀刃视觉计划。
    /// 该模块只接入 Impact 阶段，不接管投射物飞行或伤害裁决。
    /// </summary>
    public sealed class ImpactVisualModule :
        IRangedAttackModuleRuntime,
        IImpactStageModule
    {
        /// <summary>
        /// 当前攻击实例冻结的视觉配置。
        /// </summary>
        private ImpactVisualConfig config;

        /// <summary>
        /// 初始化当前模块运行时。
        /// </summary>
        void IRangedAttackModuleRuntime.Initialize(RangedAttackModuleRuntimeContext context)
        {
            config = context != null && context.Config is ImpactVisualConfig typedConfig
                ? typedConfig.Clone() as ImpactVisualConfig
                : new ImpactVisualConfig();
        }

        /// <summary>
        /// 为真实命中的任意 Thing（实体）追加一条中性额外效果计划。
        /// </summary>
        void IImpactStageModule.Contribute(
            in ImpactStageContext context,
            ImpactContribution contribution)
        {
            if (contribution == null
                || config == null
                || config.EffecterDef == null
                || context.Projectile == null
                || !context.IsValidHit
                || context.HitThing == null
                || !context.HitCell.IsValid)
            {
                return;
            }

            Thing targetThing = context.HitThing;

            Dictionary<string, string> parameters = new Dictionary<string, string>
            {
                {
                    ImpactVisualEffectExecutor.EffecterDefParameter,
                    config.EffecterDef.defName
                }
            };
            if (TryCaptureImpactExactPosition(context.Projectile, out Vector3 exactPosition))
            {
                parameters[ImpactVisualEffectExecutor.ImpactExactXParameter] =
                    exactPosition.x.ToString("R", CultureInfo.InvariantCulture);
                parameters[ImpactVisualEffectExecutor.ImpactExactZParameter] =
                    exactPosition.z.ToString("R", CultureInfo.InvariantCulture);
            }
            if (TryCaptureImpactTargetPosition(targetThing, out Vector3 targetPosition))
            {
                parameters[ImpactVisualEffectExecutor.ImpactTargetXParameter] =
                    targetPosition.x.ToString("R", CultureInfo.InvariantCulture);
                parameters[ImpactVisualEffectExecutor.ImpactTargetZParameter] =
                    targetPosition.z.ToString("R", CultureInfo.InvariantCulture);
            }

            contribution.ExtraEffectsToAppend.Add(new ExtraEffectPlan
            {
                EffectKind = ImpactVisualEffectExecutor.EffectKindId,
                TargetScope = ExtraEffectTargetScope.DirectHitThing,
                ExecutionTiming = ExtraEffectExecutionTiming.BeforeDamage,
                TargetCell = context.HitCell,
                Parameters = parameters
            });
        }

        /// <summary>
        /// 在投射物被销毁前缓存命中瞬间的精确平面位置。
        /// </summary>
        /// <param name="projectile">当前命中的投射物。</param>
        /// <param name="exactPosition">输出去除飞行高度后的命中位置。</param>
        /// <returns>位置有效时返回 true。</returns>
        private static bool TryCaptureImpactExactPosition(
            Projectile projectile,
            out Vector3 exactPosition)
        {
            exactPosition = Vector3.zero;
            if (projectile == null)
            {
                return false;
            }

            exactPosition = projectile.ExactPosition;
            exactPosition.y = 0f;
            return IsFinite(exactPosition.x) && IsFinite(exactPosition.z);
        }

        /// <summary>
        /// 在投射物被销毁前缓存命中瞬间的目标显示中心。
        /// </summary>
        /// <param name="targetThing">当前实际命中的 Thing（实体）。</param>
        /// <param name="targetPosition">输出去除绘制高度后的目标中心。</param>
        /// <returns>位置有效时返回 true。</returns>
        private static bool TryCaptureImpactTargetPosition(
            Thing targetThing,
            out Vector3 targetPosition)
        {
            targetPosition = Vector3.zero;
            if (targetThing == null)
            {
                return false;
            }

            targetPosition = targetThing.DrawPos;
            targetPosition.y = 0f;
            return IsFinite(targetPosition.x) && IsFinite(targetPosition.z);
        }

        /// <summary>
        /// 判断精确命中坐标是否不是 NaN（非数字）或无穷值。
        /// </summary>
        /// <param name="value">待检查的坐标值。</param>
        /// <returns>坐标值可安全传递时返回 true。</returns>
        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
