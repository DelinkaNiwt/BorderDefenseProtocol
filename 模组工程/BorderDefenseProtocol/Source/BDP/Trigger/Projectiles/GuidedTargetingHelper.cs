using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 多步锚点瞄准静态工具类——封装变化弹的 Shift+点击 瞄准逻辑。
    ///
    /// 操作流程：
    ///   Shift+左键 → 放置锚点（需与上一点有视线，起点到此点直线距离≤射程）
    ///   左键       → 确认最终目标并发射
    ///   右键       → 取消全部（由 Targeter 原生处理）
    ///   达到 maxAnchors → 下次点击自动作为最终目标
    ///
    /// 使用 Find.Targeter.BeginTargeting(params, action, highlightAction, validator, ...)
    /// 回调形式实现，不修改 Targeter 本身。
    /// </summary>
    public static class GuidedTargetingHelper
    {
        /// <summary>
        /// 启动多步锚点瞄准。
        /// </summary>
        /// <param name="verb">发射用的Verb（用于射程环绘制）</param>
        /// <param name="caster">施法者Pawn</param>
        /// <param name="maxAnchors">最大锚点数（不含最终目标）</param>
        /// <param name="weaponRange">武器射程（起点到目标直线距离限制）</param>
        /// <param name="onComplete">完成回调：(锚点列表, 最终目标)</param>
        public static void BeginGuidedTargeting(
            Verb verb, Pawn caster, int maxAnchors, float weaponRange,
            Action<List<IntVec3>, LocalTargetInfo> onComplete)
        {
            var anchors = new List<IntVec3>();
            BeginNextStep(verb, caster, anchors, maxAnchors, weaponRange, onComplete);
        }

        /// <summary>递归启动下一步瞄准（每放置一个锚点后重新调用）。</summary>
        private static void BeginNextStep(
            Verb verb, Pawn caster, List<IntVec3> anchors,
            int maxAnchors, float weaponRange,
            Action<List<IntVec3>, LocalTargetInfo> onComplete)
        {
            // 瞄准参数：允许点击地面和Thing
            var targetParams = new TargetingParameters
            {
                canTargetLocations = true,
                canTargetPawns = true,
                canTargetBuildings = true,
                canTargetSelf = false,
            };

            // 上一个参考点（起点或最后一个锚点）
            IntVec3 lastPoint = anchors.Count > 0 ? anchors[anchors.Count - 1] : caster.Position;

            // 点击回调
            Action<LocalTargetInfo> action = target =>
            {
                if (!target.IsValid) return;

                bool shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                bool canAddAnchor = shiftHeld && anchors.Count < maxAnchors;

                if (canAddAnchor)
                {
                    // 校验视线：上一点到此点
                    if (!GenSight.LineOfSight(lastPoint, target.Cell, caster.Map))
                    {
                        Messages.Message("BDP_GuidedNoLOS".Translate(), MessageTypeDefOf.RejectInput, false);
                        // 重新开始当前步骤
                        BeginNextStep(verb, caster, anchors, maxAnchors, weaponRange, onComplete);
                        return;
                    }
                    // 校验射程：起点到此点直线距离
                    if (caster.Position.DistanceTo(target.Cell) > weaponRange)
                    {
                        Messages.Message("BDP_GuidedOutOfRange".Translate(), MessageTypeDefOf.RejectInput, false);
                        BeginNextStep(verb, caster, anchors, maxAnchors, weaponRange, onComplete);
                        return;
                    }
                    // 存为锚点，继续下一步
                    anchors.Add(target.Cell);
                    BeginNextStep(verb, caster, anchors, maxAnchors, weaponRange, onComplete);
                }
                else
                {
                    // 最终目标——完成瞄准
                    onComplete(anchors, target);
                }
            };

            // 高亮绘制：已放置锚点折线 + 射程环
            Action<LocalTargetInfo> highlightAction = target =>
            {
                // 射程环
                GenDraw.DrawRadiusRing(caster.Position, weaponRange);
                // 绘制已放置锚点和折线
                DrawGuidedOverlay(caster, anchors, target);
            };

            // 目标校验：视线 + 射程
            Func<LocalTargetInfo, bool> validator = target =>
            {
                if (!target.IsValid || !target.Cell.InBounds(caster.Map)) return false;
                // 射程检查（起点到目标直线距离）
                if (caster.Position.DistanceTo(target.Cell) > weaponRange) return false;
                // 视线检查（上一点到目标）
                IntVec3 from = anchors.Count > 0 ? anchors[anchors.Count - 1] : caster.Position;
                return GenSight.LineOfSight(from, target.Cell, caster.Map);
            };

            // GUI绘制：鼠标预览线
            Action<LocalTargetInfo> onGuiAction = target =>
            {
                // 鼠标光标
                Texture2D icon = target.IsValid ? TexCommand.Attack : TexCommand.CannotShoot;
                GenUI.DrawMouseAttachment(icon);

                // 提示文字：已放置锚点数 / 最大数
                if (anchors.Count > 0)
                {
                    string label = "BDP_GuidedAnchors".Translate(anchors.Count, maxAnchors);
                    var pos = Event.current.mousePosition + new Vector2(20f, 20f);
                    Widgets.Label(new Rect(pos.x, pos.y, 200f, 30f), label);
                }
            };

            Find.Targeter.BeginTargeting(
                targetParams, action, highlightAction, validator,
                caster, actionWhenFinished: null, mouseAttachment: null,
                playSoundOnAction: true, onGuiAction: onGuiAction);
        }

        /// <summary>
        /// 绘制锚点折线和标记。
        /// 从施法者位置开始，经过所有锚点，到鼠标位置。
        /// </summary>
        public static void DrawGuidedOverlay(Pawn caster, List<IntVec3> anchors, LocalTargetInfo mouseTarget)
        {
            if (anchors == null || anchors.Count == 0)
            {
                // 无锚点：从施法者到鼠标画预览线
                if (mouseTarget.IsValid)
                {
                    GenDraw.DrawLineBetween(
                        caster.DrawPos,
                        mouseTarget.CenterVector3);
                }
                return;
            }

            // 施法者 → 第一个锚点
            Vector3 prev = caster.DrawPos;
            for (int i = 0; i < anchors.Count; i++)
            {
                Vector3 current = anchors[i].ToVector3Shifted();
                GenDraw.DrawLineBetween(prev, current);
                // 锚点标记
                GenDraw.DrawTargetHighlight(new LocalTargetInfo(anchors[i]));
                prev = current;
            }

            // 最后锚点 → 鼠标位置（预览线）
            if (mouseTarget.IsValid)
            {
                GenDraw.DrawLineBetween(prev, mouseTarget.CenterVector3);
            }
        }
    }
}