using System.Collections.Generic;
using BDP.Content.PathInput;
using BDP.Core.Abilities;
using BDP.Core.PathInput;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Content.Grasshopper
{
    /// <summary>
    /// 蚱蜢能力的 Verb — 继承 BdpVerb_CastAbility 复用 Trion 扣费。
    ///
    /// 支持两种模式：
    ///   1. 普通模式：点击目标 → 单次跳跃（与旧版行为一致）
    ///   2. 路径模式：Shift+点击追加锚点 → 点击确认最终目标 → 按路径逐段跳跃
    ///
    /// 多段跳跃通过 PawnFlyer 落地回调链式触发，所有跳跃在一次激活内完成。
    /// TryCommitTrionCosts 覆写为按段数×单段成本预扣。
    /// </summary>
    public class Verb_CastAbilityGrasshopper : BdpVerb_CastAbility
    {
        // ─── 路径输入状态 ───
        protected PathInputState pathInputState = new PathInputState();
        protected PathInputConfig pathInputConfig;
        protected bool pathInputDelegatesWired = false;

        // ─── 多段跳跃待执行队列（CompAbilityEffect 消费） ───
        protected List<PathAnchor> pendingWaypoints;
        protected int pendingWaypointIndex;

        // ─── 缓存的射程 ───
        private float cachedEffectiveRange = -1f;

        /// <summary>支持多选：路径输入期间 Targeter 保持打开。</summary>
        public override bool MultiSelect => true;

        /// <summary>
        /// 当前有效圆心：有锚点时跟随最新锚点，否则以施法者为圆心。
        /// 距离判定、LOS、射程圈、校验委托等全部以此为准，改一处全处同步。
        /// </summary>
        protected IntVec3 EffectiveOrigin
        {
            get
            {
                IntVec3 origin = PathInputHandler.ResolveSegmentOriginCell(CasterPawn, pathInputState);
                return origin.IsValid ? origin : caster.Position;
            }
        }

        /// <summary>
        /// 禁用施法抖动：跳跃本身就是位移，施法后坐力抖动会导致双重晃动。
        /// </summary>
        protected override void TriggerCastJitter() { }

        /// <summary>
        /// 有效射程 — 优先读 JumpRange stat，fallback 到 verbProps.range。
        /// </summary>
        public override float EffectiveRange
        {
            get
            {
                if (cachedEffectiveRange < 0f)
                {
                    if (base.EquipmentSource != null)
                        cachedEffectiveRange = base.EquipmentSource.GetStatValue(StatDefOf.JumpRange);
                    else
                        cachedEffectiveRange = verbProps.range;
                }
                return cachedEffectiveRange;
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  Trion 成本
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// 覆写 Trion 提交：基类扣第一段费，多段时不做额外操作。
        /// 多段成本由 CompAbilityEffect.GrasshopperMultiJump 逐段直接消耗。
        /// </summary>
        protected override bool TryCommitTrionCosts()
        {
            return base.TryCommitTrionCosts();
        }

        // ═══════════════════════════════════════════════════════════
        //  目标选择 — Shift 锚点 / 最终确认
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// 延迟初始化 PathInputConfig 并注入蚱蜢专属的动态射程校验委托。
        /// 锚点/最终目标的射程基准点跟随最新锚点位置，而非始终以施法者为圆心。
        /// </summary>
        protected virtual void EnsurePathInputDelegates()
        {
            if (pathInputDelegatesWired) return;
            pathInputDelegatesWired = true;

            if (pathInputConfig == null)
            {
                pathInputConfig = new PathInputConfig
                {
                    MaxAnchors = 6,
                    AllowGroundFinal = true,
                    AllowThingFinal = false
                };
            }

            // ─── 锚点追加校验：距离判定走 EffectiveOrigin ───
            pathInputConfig.AnchorAppendValidator = (Map map, IntVec3 candidateCell, PathInputState state) =>
            {
                float dist = EffectiveOrigin.DistanceTo(candidateCell);
                if (dist > EffectiveRange)
                    return "BDP_Grasshopper_OutOfRange".Translate();
                return null;
            };

            // ─── 最终目标校验：同上 ───
            pathInputConfig.FinalTargetValidator = (LocalTargetInfo finalTarget, Pawn pawn, PathInputState state) =>
            {
                float dist = EffectiveOrigin.DistanceTo(finalTarget.Cell);
                if (dist > EffectiveRange)
                    return "BDP_Grasshopper_OutOfRange".Translate();
                return null;
            };
        }

        /// <summary>
        /// 覆写目标选择，区分 Shift+点击（追加锚点）与普通点击（确认最终目标）。
        /// 路径模式下排队一次施法，后续段由 CompAbilityEffect 链式触发。
        /// </summary>
        public override void OrderForceTarget(LocalTargetInfo target)
        {
            EnsurePathInputDelegates();
            bool shiftHeld = Event.current?.shift ?? false;

            if (shiftHeld)
            {
                HandleShiftClick(target);
                return;
            }

            HandleFinalConfirm(target);
        }

        /// <summary>
        /// Shift+点击：校验并追加锚点，不进入 Job 队列。
        /// </summary>
        protected virtual void HandleShiftClick(LocalTargetInfo target)
        {
            IntVec3 origin = PathInputHandler.ResolveSegmentOriginCell(CasterPawn, pathInputState);
            string rejectReason = PathInputHandler.TryAppendAnchor(
                pathInputState, pathInputConfig, target, CasterPawn.Map, origin);

            if (rejectReason != null)
            {
                Messages.Message(rejectReason, MessageTypeDefOf.RejectInput, false);
            }
            // MultiSelect=true → Targeter 保持打开
        }

        /// <summary>
        /// 普通点击：确认最终目标 → 冻结路径 → 排队首段施法 Job。
        /// 后续段在 CompAbilityEffect.Apply() 中通过 PawnFlyer 回调链式触发。
        /// </summary>
        protected virtual void HandleFinalConfirm(LocalTargetInfo target)
        {
            IntVec3 origin = PathInputHandler.ResolveSegmentOriginCell(CasterPawn, pathInputState);
            string rejectReason = PathInputHandler.TryConfirmFinalTarget(
                pathInputState, pathInputConfig, target, CasterPawn, origin, CasterPawn.Map);

            if (rejectReason != null)
            {
                Messages.Message(rejectReason, MessageTypeDefOf.RejectInput, false);
                return;
            }

            // 冻结路径
            PathConfirmedData confirmed = PathInputHandler.Freeze(pathInputState);
            List<PathAnchor> waypoints = confirmed.GetAllWaypoints();

            if (waypoints.Count == 0)
            {
                return;
            }

            // 将完整路径存入 Verb 状态供 TryCommitTrionCosts 和 CompAbilityEffect 消费
            pendingWaypoints = waypoints;
            pendingWaypointIndex = 0;

            // 重置输入状态必须在 OrderJump 之前：OrderJump 会立即 StartJob，
            // JobDriver 随即调 TryStartCastOn → CanHitTargetFrom → EffectiveOrigin，
            // 若锚点未清则 EffectiveOrigin=最后锚点(超远)→距离校验失败。
            pathInputState.Reset();

            // 排队首段施法（后续段在 CompAbilityEffect.Apply 中链式触发）
            GrasshopperUtility.OrderJump(
                CasterPawn,
                new LocalTargetInfo(waypoints[0].ToCell()),
                this,
                EffectiveRange);
        }

        /// <summary>
        /// CompAbilityEffect 消费：获取下一条待跳跃路径点。
        /// 返回 null 表示没有更多路径点。
        /// </summary>
        public virtual PathAnchor ConsumeNextWaypoint()
        {
            if (pendingWaypoints == null) return null;
            if (pendingWaypointIndex >= pendingWaypoints.Count)
            {
                pendingWaypoints = null;
                pendingWaypointIndex = 0;
                return null;
            }
            PathAnchor wp = pendingWaypoints[pendingWaypointIndex];
            pendingWaypointIndex++;
            if (pendingWaypointIndex >= pendingWaypoints.Count)
            {
                pendingWaypoints = null;
                pendingWaypointIndex = 0;
            }
            return wp;
        }

        /// <summary>
        /// 检查是否还有待跳跃的路径点（供 CompAbilityEffect 判断是否链式触发下一段）。
        /// </summary>
        public virtual bool HasPendingWaypoints =>
            pendingWaypoints != null && pendingWaypointIndex < pendingWaypoints.Count;

        /// <summary>路径总段数（首段调用前快照用）。</summary>
        public int PendingWaypointCount =>
            pendingWaypoints != null ? pendingWaypoints.Count : 0;

        // ═══════════════════════════════════════════════════════════
        //  GUI 预览
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// UI 层绘制瞄准光标。路径预览不在 UI 层画（会闪烁），留给 DrawHighlight 做世界空间绘制。
        /// </summary>
        public override void OnGUI(LocalTargetInfo target)
        {
            if (CanHitTarget(target)
                && GrasshopperUtility.ValidJumpTarget(caster.Map, target.Cell))
            {
                base.OnGUI(target);
            }
            else
            {
                GenUI.DrawMouseAttachment(TexCommand.CannotShoot);
            }
        }

        /// <summary>
        /// 世界空间层绘制路径预览。委托给共享渲染器 PathPreviewRenderer。
        /// </summary>
        protected virtual void DrawPathPreview(LocalTargetInfo currentTarget)
        {
            PathPreviewData preview = PathInputHandler.BuildPreview(
                pathInputState, CasterPawn, currentTarget, isLastSegmentBlocked: false);

            PathPreviewRenderer.DrawPreview(preview, pathInputState);
        }

        /// <summary>
        /// 目标验证 — 必须在射程内且落点可行走。
        /// </summary>
        public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            if (caster == null) return false;
            if (!CanHitTarget(target)
                || !GrasshopperUtility.ValidJumpTarget(caster.Map, target.Cell))
                return false;
            return true;
        }

        /// <summary>
        /// 判断从 root 能否命中目标。
        /// 锚点存在时以最新锚点为圆心做距离+视野判定，否则以施法者为圆心。
        /// CanHitTarget / ValidateTarget / OnGUI 最终都落入此方法。
        /// </summary>
        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            float num = EffectiveRange * EffectiveRange;
            if ((float)EffectiveOrigin.DistanceToSquared(targ.Cell) > num)
                return false;
            return GenSight.LineOfSight(EffectiveOrigin, targ.Cell, CasterPawn.Map);
        }

        /// <summary>绘制射程指示圈、有效落点高亮和路径预览折线。</summary>
        public override void DrawHighlight(LocalTargetInfo target)
        {
            if (pathInputState.Anchors.Count > 0)
            {
                DrawPathPreview(target);
            }

            if (target.IsValid
                && GrasshopperUtility.ValidJumpTarget(caster.Map, target.Cell))
            {
                GenDraw.DrawTargetHighlightWithLayer(
                    target.CenterVector3, AltitudeLayer.MetaOverlays);
            }

            GenDraw.DrawRadiusRing(
                EffectiveOrigin, EffectiveRange, Color.white,
                (IntVec3 c) => GenSight.LineOfSight(EffectiveOrigin, c, caster.Map)
                    && GrasshopperUtility.ValidJumpTarget(caster.Map, c));
        }
    }
}
