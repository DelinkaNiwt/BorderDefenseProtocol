using BDP.Core.AttackExecution;
using BDP.Core.AttackExecution.RangedModules.Runtime;
using BDP.Core.AttackExecution.RangedProtocol.ProjectileInit;
using BDP.Core.Projectiles.Visual;
using Verse;

namespace BDP.Content.RangedModules.HighAngleShooting
{
    /// <summary>
    /// 独立高角射击模块。
    /// 它负责不可视目标的高角投射物选择、视觉轨迹参数提交与屋顶约束。
    /// </summary>
    public sealed class HighAngleShootingModule :
        IRangedAttackModuleRuntime,
        ITargetingStageModule,
        IConfirmStageModule,
        IProjectileInitStageModule
    {
        /// <summary>
        /// 当前运行时实例冻结的模块配置。
        /// </summary>
        private HighAngleShootingConfig config;

        /// <summary>
        /// 初始化当前模块运行时实例。
        /// </summary>
        /// <param name="context">模块运行时初始化上下文。</param>
        void IRangedAttackModuleRuntime.Initialize(RangedAttackModuleRuntimeContext context)
        {
            config = context != null && context.Config is HighAngleShootingConfig typedConfig
                ? typedConfig.Clone() as HighAngleShootingConfig
                : new HighAngleShootingConfig();
        }

        /// <summary>
        /// 在当前目标阶段拒绝需要高角射击、但受屋顶约束的候选位置。
        /// 这里只覆盖当前候选点的即时合法性，不修改原版目标参数，也不接管其他模块的合法性结果。
        /// </summary>
        /// <param name="record">当前目标阶段记录。</param>
        void ITargetingStageModule.Contribute(TargetingRecord record)
        {
            if (record == null || record.HasExplicitNavigation)
            {
                return;
            }

            LocalTargetInfo candidate = ResolveCurrentCandidate(record);
            if (!candidate.IsValid
                || !TryResolveRoofRejectReason(
                    record.Pawn,
                    record.Pawn != null ? record.Pawn.Map : null,
                    candidate,
                    out string rejectReason))
            {
                return;
            }

            record.HasCurrentTargetLegalityOverride = true;
            record.CurrentTargetIsLegal = false;
            record.CurrentTargetRejectReason = rejectReason;
        }

        /// <summary>
        /// 在手动确认阶段拒绝不具备高角射击空间的目标。
        /// 该阶段负责尽早反馈，自动攻击路径仍由 ProjectileInit 阶段再次兜底。
        /// </summary>
        /// <param name="record">确认阶段记录。</param>
        void IConfirmStageModule.Contribute(ConfirmRecord record)
        {
            if (record == null || record.HasExplicitNavigation)
            {
                return;
            }

            LocalTargetInfo target = record.SemanticTarget.IsValid
                ? record.SemanticTarget
                : record.Target;
            if (TryResolveRoofRejectReason(
                record.Pawn,
                record.Pawn != null ? record.Pawn.Map : null,
                target,
                out string rejectReason))
            {
                record.IsAllowed = false;
                record.RejectReason = rejectReason;
            }
        }

        /// <summary>
        /// 按最终瞄准目标的可视性提交每发投射物的高角覆盖。
        /// </summary>
        /// <param name="context">投射物初始化阶段上下文。</param>
        /// <param name="contribution">当前模块的初始化贡献。</param>
        void IProjectileInitStageModule.Contribute(
            in ProjectileInitStageContext context,
            ProjectileInitContribution contribution)
        {
            if (contribution == null)
            {
                return;
            }

            if (config == null || context.EmitCount <= 0)
            {
                return;
            }

            bool hasUncontrolledEmit = false;
            for (int i = 0; i < context.EmitCount; i++)
                hasUncontrolledEmit |= !context.HasPreviousExclusiveFlightControl(i);
            if (!hasUncontrolledEmit) return;

            if (TryResolveRoofRejectReason(
                context.Pawn,
                context.Map,
                context.FinalTarget,
                out string rejectReason))
            {
                contribution.Stop.IsRequested = true;
                contribution.Stop.Reason = rejectReason;
                contribution.Stop.Scope = RangedStageStopScope.Attack;
                return;
            }

            if (!IsOutOfSight(context))
            {
                return;
            }

            ThingDef overheadProjectileDef = ResolveOverheadProjectileDef();
            if (overheadProjectileDef == null)
            {
                return;
            }

            ProjectileVisualTrajectoryProfile visualProfile =
                config.CreateVisualTrajectoryProfile();
            for (int emitIndex = 0; emitIndex < context.EmitCount; emitIndex++)
            {
                if (context.HasPreviousExclusiveFlightControl(emitIndex)) continue;
                contribution.PlanContributions.Add(new ProjectileInitPlanContribution
                {
                    EmitIndex = emitIndex,
                    HasOverrideProjectileDef = true,
                    OverrideProjectileDef = overheadProjectileDef,
                    HasVisualTrajectoryProfile = true,
                    VisualTrajectoryProfile = visualProfile
                });
            }
        }

        /// <summary>
        /// 判断射击者到最终瞄准目标是否不可视。
        /// </summary>
        /// <param name="context">投射物初始化阶段上下文。</param>
        /// <returns>不可视时返回 true。</returns>
        private static bool IsOutOfSight(in ProjectileInitStageContext context)
        {
            return IsHighAngleRequired(context.Pawn, context.Map, context.FinalTarget);
        }

        /// <summary>
        /// 判断当前目标是否确实需要高角射击。
        /// 屋顶约束只在高角模式成立时生效，避免改变普通直射行为。
        /// </summary>
        /// <param name="pawn">射击者。</param>
        /// <param name="map">射击所在地图。</param>
        /// <param name="target">最终瞄准目标。</param>
        /// <returns>不可视且可判定时返回 true。</returns>
        private static bool IsHighAngleRequired(
            Pawn pawn,
            Map map,
            LocalTargetInfo target)
        {
            return pawn != null
                && map != null
                && pawn.Spawned
                && target.IsValid
                && target.Cell.IsValid
                && target.Cell.InBounds(map)
                && !GenSight.LineOfSight(pawn.Position, target.Cell, map);
        }

        /// <summary>
        /// 解析当前目标阶段正在检查的候选目标。
        /// 探针会同时填充 selected/hovered，普通目标循环则优先使用已确认的 selected。
        /// </summary>
        /// <param name="record">当前目标阶段记录。</param>
        /// <returns>当前候选目标；没有候选目标时返回无效目标。</returns>
        private static LocalTargetInfo ResolveCurrentCandidate(TargetingRecord record)
        {
            if (record?.InputFrame == null)
            {
                return LocalTargetInfo.Invalid;
            }

            if (record.InputFrame.SelectedTarget.IsValid)
            {
                return record.InputFrame.SelectedTarget;
            }

            return record.InputFrame.HoveredTarget;
        }

        /// <summary>
        /// 解析高角射击的屋顶拒绝原因。
        /// 发射者位于任意屋顶下、目标位于厚屋顶下始终拒绝；普通薄屋顶由配置决定。
        /// </summary>
        /// <param name="pawn">射击者。</param>
        /// <param name="map">射击所在地图。</param>
        /// <param name="target">最终瞄准目标。</param>
        /// <param name="rejectReason">拒绝原因语言键。</param>
        /// <returns>需要拒绝时返回 true。</returns>
        private bool TryResolveRoofRejectReason(
            Pawn pawn,
            Map map,
            LocalTargetInfo target,
            out string rejectReason)
        {
            rejectReason = null;
            if (config == null || !IsHighAngleRequired(pawn, map, target))
            {
                return false;
            }

            if (map.roofGrid.Roofed(pawn.Position))
            {
                rejectReason = "BDP_HighAngle_SourceUnderRoof";
                return true;
            }

            RoofDef targetRoof = map.roofGrid.RoofAt(target.Cell);
            if (targetRoof == null)
            {
                return false;
            }

            if (targetRoof.isThickRoof)
            {
                rejectReason = "BDP_HighAngle_TargetUnderThickRoof";
                return true;
            }

            if (!config.AllowTargetUnderThinRoof)
            {
                rejectReason = "BDP_HighAngle_TargetUnderThinRoof";
                return true;
            }

            return false;
        }

        /// <summary>
        /// 解析配置中的高角投射物 Def。
        /// </summary>
        /// <returns>高角投射物 Def；配置无效时返回空。</returns>
        private ThingDef ResolveOverheadProjectileDef()
        {
            return !string.IsNullOrWhiteSpace(config.OverheadProjectileDefName)
                ? DefDatabase<ThingDef>.GetNamedSilentFail(config.OverheadProjectileDefName)
                : null;
        }
    }
}
