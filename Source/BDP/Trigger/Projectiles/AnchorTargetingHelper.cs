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
    public static class AnchorTargetingHelper
    {
        // 自动绕行预览缓存：避免每帧重复做ObstacleRouter BFS。
        private static Map previewRouteMap;
        private static IntVec3 previewRouteFrom;
        private static IntVec3 previewRouteTo;
        private static ObstacleRouteResult? previewRoute;

        /// <summary>
        /// 启动多步锚点瞄准。
        /// </summary>
        /// <param name="verb">发射用的Verb（用于射程环绘制）</param>
        /// <param name="caster">施法者Pawn</param>
        /// <param name="maxAnchors">最大锚点数（不含最终目标）</param>
        /// <param name="weaponRange">武器射程（起点到目标直线距离限制）</param>
        /// <param name="onComplete">完成回调：(锚点列表, 最终目标)</param>
        public static void BeginAnchorTargeting(
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

            // 高亮绘制：已放置锚点折线 + 射程环 + LOS红线反馈 + 目标高亮
            Action<LocalTargetInfo> highlightAction = target =>
            {
                // 射程环
                GenDraw.DrawRadiusRing(caster.Position, weaponRange);
                // 绘制已放置锚点和折线（含LOS检测红线）
                DrawGuidedOverlay(caster, anchors, target, caster.Map);
                // 目标脚下白色圆圈（与非引导模式一致）
                if (target.IsValid)
                    GenDraw.DrawTargetHighlight(target);
            };

            // 目标校验：
            // - 加锚点（按住Shift）时：需要上一点到目标有LOS
            // - 确认最终目标时：允许无LOS（由自动绕行接管）
            Func<LocalTargetInfo, bool> validator = target =>
            {
                if (!target.IsValid || !target.Cell.InBounds(caster.Map)) return false;
                // 射程检查（起点到目标直线距离）
                if (caster.Position.DistanceTo(target.Cell) > weaponRange) return false;
                bool shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                bool canAddAnchor = shiftHeld && anchors.Count < maxAnchors;
                if (!canAddAnchor) return true;

                // 仅锚点添加需要LOS检查（上一点到目标）
                IntVec3 from = anchors.Count > 0 ? anchors[anchors.Count - 1] : caster.Position;
                return GenSight.LineOfSight(from, target.Cell, caster.Map);
            };

            // GUI绘制：仅提示文字（准心交给Targeter默认绘制，恢复圆圈样式）
            Action<LocalTargetInfo> onGuiAction = target =>
            {
                // 提示文字：已放置锚点数 / 最大数
                if (anchors.Count > 0)
                {
                    string label = "BDP_GuidedAnchors".Translate(anchors.Count, maxAnchors);
                    var pos = Event.current.mousePosition + new Vector2(20f, 20f);
                    Widgets.Label(new Rect(pos.x, pos.y, 200f, 30f), label);
                }
            };

            // 鼠标光标：使用verb的UIIcon（与非引导模式一致），回退到默认攻击准心
            Texture2D cursorIcon = (verb.UIIcon != BaseContent.BadTex) ? verb.UIIcon : null;

            Find.Targeter.BeginTargeting(
                targetParams, action, highlightAction, validator,
                caster, actionWhenFinished: null, mouseAttachment: cursorIcon,
                playSoundOnAction: true, onGuiAction: onGuiAction);
        }

        /// <summary>
        /// 绘制锚点折线和标记。
        /// 从施法者位置开始，经过所有锚点，到鼠标位置。
        /// 最后一段线检查LOS，不通过时显示红色。
        /// </summary>
        public static void DrawGuidedOverlay(Pawn caster, List<IntVec3> anchors, LocalTargetInfo mouseTarget, Map map)
        {
            if (anchors == null || anchors.Count == 0)
            {
                // 无锚点：有LOS时直线；无LOS时显示自动绕行的实际路径预览。
                if (mouseTarget.IsValid)
                {
                    bool hasLOS = GenSight.LineOfSight(caster.Position, mouseTarget.Cell, map);
                    if (hasLOS)
                    {
                        GenDraw.DrawLineBetween(caster.DrawPos, mouseTarget.CenterVector3);
                    }
                    else
                    {
                        var route = GetPreviewRoute(caster.Position, mouseTarget.Cell, map);
                        if (route.HasValue && route.Value.IsValid)
                        {
                            bool drew = false;
                            // 仅绘制逐段LOS全通的路径，不通的跳过
                            if (route.Value.LeftAnchors != null && route.Value.LeftAnchors.Count > 0
                                && VerbFlightState.IsPathClear(caster.Position, route.Value.LeftAnchors, mouseTarget.Cell, map))
                            {
                                DrawPreviewRoutePath(caster.DrawPos, route.Value.LeftAnchors, mouseTarget.CenterVector3);
                                drew = true;
                            }
                            if (route.Value.RightAnchors != null && route.Value.RightAnchors.Count > 0
                                && VerbFlightState.IsPathClear(caster.Position, route.Value.RightAnchors, mouseTarget.Cell, map))
                            {
                                DrawPreviewRoutePath(caster.DrawPos, route.Value.RightAnchors, mouseTarget.CenterVector3);
                                drew = true;
                            }
                            if (drew) return;
                        }
                        GenDraw.DrawLineBetween(caster.DrawPos, mouseTarget.CenterVector3, SimpleColor.Red);
                    }
                }
                return;
            }

            // 施法者 → 各锚点（已确认LOS的段，白色）
            Vector3 prev = caster.DrawPos;
            for (int i = 0; i < anchors.Count; i++)
            {
                Vector3 current = anchors[i].ToVector3Shifted();
                GenDraw.DrawLineBetween(prev, current);
                prev = current;
            }

            // 最后锚点 → 鼠标位置（预览线，检查LOS决定颜色）
            if (mouseTarget.IsValid)
            {
                IntVec3 from = anchors[anchors.Count - 1];
                bool hasLOS = GenSight.LineOfSight(from, mouseTarget.Cell, map);
                if (hasLOS)
                    GenDraw.DrawLineBetween(prev, mouseTarget.CenterVector3);
                else
                    GenDraw.DrawLineBetween(prev, mouseTarget.CenterVector3, SimpleColor.Red);
            }
        }

        /// <summary>获取自动绕行预览路由（按起点/终点/地图缓存）。</summary>
        private static ObstacleRouteResult? GetPreviewRoute(IntVec3 from, IntVec3 to, Map map)
        {
            if (map == null) return null;
            if (previewRouteMap == map && previewRouteFrom == from && previewRouteTo == to)
                return previewRoute;

            previewRouteMap = map;
            previewRouteFrom = from;
            previewRouteTo = to;
            previewRoute = ObstacleRouter.ComputeRoute(from, to, map);
            return previewRoute;
        }

        /// <summary>绘制一条自动绕行预览路径（起点→锚点序列→终点）。</summary>
        private static void DrawPreviewRoutePath(Vector3 start, List<IntVec3> anchors, Vector3 end)
        {
            if (anchors == null || anchors.Count == 0) return;

            Vector3 prev = start;
            for (int i = 0; i < anchors.Count; i++)
            {
                Vector3 current = anchors[i].ToVector3Shifted();
                GenDraw.DrawLineBetween(prev, current);
                prev = current;
            }
            GenDraw.DrawLineBetween(prev, end);
        }

    }
}
