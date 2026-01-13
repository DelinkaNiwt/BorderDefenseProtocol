using System;
using System.Collections.Generic;
using Verse;
using RimWorld;
using ProjectTrion.Core;
using ProjectTrion.Components;
using UnityEngine;

namespace ProjectTrion_MVP
{
    /// <summary>
    /// Trion_MVP 的默认生命周期策略实现
    /// MVP使用统一的策略，所有Pawn的Trion管理都由此策略处理
    ///
    /// 策略职责：
    /// - 提供天赋初始化（MVP中返回null，延迟到首次检测仪扫描）
    /// - 提供基础维持消耗（固定5.0/60Tick）
    /// - 处理Bail Out脱离的条件判断和目标位置
    /// </summary>
    public class DefaultTrionStrategy : ILifecycleStrategy
    {
        /// <summary>
        /// 持有的CompTrion引用
        /// </summary>
        private CompTrion _comp;

        /// <summary>
        /// 构造函数 - 框架通过反射调用，传入CompTrion参数
        /// </summary>
        public DefaultTrionStrategy(CompTrion comp)
        {
            _comp = comp;
        }

        /// <summary>
        /// 策略的唯一标识符
        /// </summary>
        public string StrategyId => "DefaultTrionStrategy";

        /// <summary>
        /// 获取单位的初始天赋
        ///
        /// MVP使用延迟初始化：返回null，天赋在首次检测仪扫描时才生成
        /// 这样避免了全局Harmony补丁，符合"投机取巧"的设计理念
        /// </summary>
        public TalentGrade? GetInitialTalent(CompTrion comp)
        {
            // MVP延迟初始化：天赋在首次检测仪扫描时生成
            return null;
        }

        /// <summary>
        /// 战斗体生成时的回调
        /// 在快照保存完成后、组件初始化完成后调用
        /// </summary>
        public void OnCombatBodyGenerated(CompTrion comp)
        {
            if (comp?.parent is Pawn pawn)
            {
                Log.Message($"[Trion] {pawn.LabelShort} 的战斗体已生成，" +
                    $"可用Trion: {comp.Available:F1}/{comp.Capacity:F1}");
            }
        }

        /// <summary>
        /// 战斗体摧毁时的回调
        /// 处理摧毁后的清理、日志记录等
        /// </summary>
        public void OnCombatBodyDestroyed(CompTrion comp, DestroyReason reason)
        {
            if (comp?.parent is Pawn pawn)
            {
                string reasonStr = reason switch
                {
                    DestroyReason.Manual => "玩家解除",
                    DestroyReason.TrionDepleted => "Trion耗尽",
                    DestroyReason.VitalPartDestroyed => "关键部位摧毁",
                    DestroyReason.BailOutSuccess => "Bail Out成功",
                    DestroyReason.BailOutFailed => "Bail Out失败",
                    _ => "其他原因"
                };

                Log.Message($"[Trion] {pawn.LabelShort} 的战斗体已摧毁 (原因: {reasonStr})");

                // 记录摧毁前的数据，便于调试
                Log.Message($"[Trion] 摧毁时数据 - Consumed: {comp.Consumed:F1}, " +
                    $"Reserved: {comp.Reserved:F1}, Available: {comp.Available:F1}");
            }
        }

        /// <summary>
        /// 获取基础维持消耗
        /// 在每个CompTick周期（60Tick）调用一次
        ///
        /// MVP设定：固定维持消耗 5.0 Trion per 60 ticks
        /// 实际消耗 = 基础维持 + 装备组件消耗 + 伤口泄漏
        /// </summary>
        public float GetBaseMaintenance()
        {
            // MVP配置：基础维持消耗为5.0
            return 5.0f;
        }

        /// <summary>
        /// 每个CompTick周期的回调（每60Tick调用一次）
        /// 用于复杂逻辑处理，MVP中暂无特殊逻辑
        /// </summary>
        public void OnTick(CompTrion comp)
        {
            // MVP阶段暂无特殊逻辑
            // 后续可用于AI决策、特殊能力计算等
        }

        /// <summary>
        /// 关键部位（Trion供给器官）被摧毁时的回调
        /// 在MVP中，摧毁关键部位会导致战斗体强制破裂
        /// </summary>
        public void OnVitalPartDestroyed(CompTrion comp, BodyPartRecord part)
        {
            if (comp?.parent is Pawn pawn && part != null)
            {
                Log.Warning($"[Trion] {pawn.LabelShort} 的关键部位 {part.Label} 被摧毁，" +
                    $"战斗体将被强制破裂");

                // 框架会自动破裂战斗体，这里只记录日志
            }
        }

        /// <summary>
        /// Trion可用量耗尽时的回调（Available <= 0）
        /// 在MVP中，此时会自动触发Bail Out
        /// </summary>
        public void OnDepleted(CompTrion comp)
        {
            if (comp?.parent is Pawn pawn)
            {
                Log.Message($"[Trion] {pawn.LabelShort} 的Trion已耗尽，尝试执行Bail Out...");
            }
        }

        /// <summary>
        /// 检查是否可以执行Bail Out脱离
        /// MVP中始终允许Bail Out（如果有有效目标）
        /// </summary>
        public bool CanBailOut(CompTrion comp)
        {
            // MVP设定：只要能找到目标位置就允许脱离
            return GetBailOutTarget(comp) != IntVec3.Invalid;
        }

        /// <summary>
        /// 获取Bail Out的目标位置
        ///
        /// 新架构设计（文档11确认）：
        /// - Bail Out 总是成功的（一定能找到传送位置）
        /// - 优先级1：传送锚（Building_BailOutAnchor）
        /// - 优先级2：基地附近（RCellFinder.RandomSpotJustOutsideColony）
        /// - 返回值永远有效，不会返回 Invalid
        /// </summary>
        public IntVec3 GetBailOutTarget(Pawn pawn)
        {
            Map map = pawn?.Map;

            if (map == null)
            {
                Log.Error("[Trion] 无法获取地图，传送失败");
                return IntVec3.Invalid;
            }

            // 优先级1：查询传送锚（Building_BailOutAnchor）
            var anchors = map.listerThings.ThingsOfDef(
                DefDatabase<ThingDef>.GetNamed("Building_BailOutAnchor", false)
            );

            if (anchors != null && anchors.Count > 0)
            {
                Building_BailOutAnchor nearest = null;
                float nearestDist = float.MaxValue;

                foreach (var thing in anchors)
                {
                    if (thing is Building_BailOutAnchor anchor && anchor.IsActive)
                    {
                        float dist = pawn.Position.DistanceTo(anchor.Position);
                        if (dist < nearestDist)
                        {
                            nearestDist = dist;
                            nearest = anchor;
                        }
                    }
                }

                if (nearest != null)
                {
                    Log.Message($"[Trion] 传送到传送锚 @ {nearest.Position}");
                    return nearest.Position;
                }
            }

            // 优先级2：传送到地图边缘（无锚点时的应急传送）
            var emergencySpot = CellFinder.RandomEdgeCell(map);

            Log.Message($"[Trion] 传送到地图边缘 @ {emergencySpot}");
            return emergencySpot;
        }

        /// <summary>
        /// 重载版本：支持旧的 CompTrion 参数
        /// </summary>
        public IntVec3 GetBailOutTarget(CompTrion comp)
        {
            return GetBailOutTarget(comp?.parent as Pawn);
        }
    }
}
