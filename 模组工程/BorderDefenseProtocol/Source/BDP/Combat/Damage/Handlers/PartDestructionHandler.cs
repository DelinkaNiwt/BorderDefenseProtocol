using System.Collections.Generic;
using System.Linq;
using Verse;
using BDP.Core;

namespace BDP.Combat
{
    /// <summary>
    /// 部位破坏处理器。
    /// 当影子HP耗尽时，在真身上添加"战斗体部位摧毁"Hediff。
    ///
    /// 职责：
    /// - 执行部位破坏操作（AddHediff BDP_CombatBodyPartDestroyed）
    /// - 管理已破坏部位集合（避免重复处理）
    /// - 提供查询接口
    /// - 战斗体解除时移除所有破坏 Hediff（恢复部位）
    ///
    /// 设计说明：
    /// - 使用自定义 HediffDef（无疼痛/流血）而非原版 MissingBodyPart
    /// - 存储 Hediff 引用以便解除时移除
    /// - 战斗体解除时恢复所有被破坏的部位
    /// </summary>
    public class PartDestructionHandler : IExposable
    {
        // 已破坏部位的 Hediff 引用（用于解除时移除）
        private List<Hediff> destroyedPartHediffs = new List<Hediff>();

        // 已破坏部位集合（key: part.Index，唯一索引）
        private HashSet<int> destroyedParts = new HashSet<int>();

        /// <summary>
        /// 处理部位破坏。
        /// </summary>
        /// <param name="pawn">目标Pawn</param>
        /// <param name="part">被破坏的部位</param>
        /// <returns>true=成功标记部位破坏，false=已破坏或失败</returns>
        public bool Handle(Pawn pawn, BodyPartRecord part)
        {
            // 基础检查
            if (pawn == null || part == null) return false;

            // 检查 pawn 状态（防止在死亡/销毁过程中操作）
            if (pawn.Dead || pawn.Destroyed)
            {
                Log.Warning($"[BDP] 部位破坏跳过: {pawn.LabelShort} 已死亡或销毁");
                return false;
            }

            // 检查 health 系统
            if (pawn.health == null || pawn.health.hediffSet == null)
            {
                Log.Error($"[BDP] 部位破坏失败: {pawn.LabelShort} 的 health 系统为 null");
                return false;
            }

            int partKey = part.Index;

            // 检查是否已破坏
            if (destroyedParts.Contains(partKey))
            {
                Log.Message($"[BDP] 部位 {part.def.defName} 已被破坏，跳过");
                return false;
            }

            // 执行部位破坏
            try
            {
                // 使用自定义 HediffDef（无疼痛/流血）
                var hediffDef = BDP.Core.BDP_DefOf.BDP_CombatBodyPartDestroyed;
                if (hediffDef == null)
                {
                    Log.Error("[BDP] BDP_CombatBodyPartDestroyed HediffDef 未找到");
                    return false;
                }

                // 记录添加前的能力
                float movingBefore = pawn.health.capacities.GetLevel(RimWorld.PawnCapacityDefOf.Moving);
                bool canWalkBefore = pawn.health.capacities.CapableOf(RimWorld.PawnCapacityDefOf.Moving);

                var hediff = pawn.health.AddHediff(hediffDef, part);

                // PostAdd（Hediff_MissingPart）已自动执行 RestorePart + 递归子部位缺失，无需手动清理

                // 记录添加后的能力
                float movingAfter = pawn.health.capacities.GetLevel(RimWorld.PawnCapacityDefOf.Moving);
                bool canWalkAfter = pawn.health.capacities.CapableOf(RimWorld.PawnCapacityDefOf.Moving);

                Log.Message($"[BDP] ⚠ 影子部位被毁: {part.LabelShort} (defName={part.def.defName})");
                Log.Message($"[BDP]   部位Index: {partKey}");
                Log.Message($"[BDP]   部位标签: {part.Label} / {part.LabelCap} / {part.LabelShort}");
                Log.Message($"[BDP]   部位customLabel: {part.customLabel ?? "null"}");
                Log.Message($"[BDP]   部位woundAnchorTag: {part.woundAnchorTag ?? "null"}");
                Log.Message($"[BDP]   添加的Hediff: {hediff?.def.defName ?? "null"} 在部位 {hediff?.Part?.LabelShort ?? "null"}");
                Log.Message($"[BDP]   移动能力: {movingBefore:P1} → {movingAfter:P1}");
                Log.Message($"[BDP]   可行走: {canWalkBefore} → {canWalkAfter}");
                Log.Message($"[BDP]   是否倒地: {pawn.Downed}");

                // 记录 Hediff 引用（用于解除时移除）
                if (hediff != null)
                {
                    destroyedPartHediffs.Add(hediff);
                }

                // 记录已破坏
                destroyedParts.Add(partKey);

                // 触发部位破坏事件：手部缺失联动
                // 被毁部位本身是Hand，或其子树包含Hand（手臂/肩膀被毁时级联）
                BodyPartRecord handPart;
                if (FindHandInSubtree(part, out handPart))
                {
                    Log.Message($"[BDP]   检测到手部关联破坏: {part.def.defName} → Hand ({handPart.LabelShort})");
                    BDPEvents.TriggerPartDestroyedEvent(new BDP.Core.PartDestroyedEventArgs
                    {
                        Pawn = pawn,
                        Part = handPart, // 传入实际的Hand部位（用于正确判断左右）
                        IsHandPart = true,
                        HandSide = GetHandSide(handPart)
                    });
                }

                return true;
            }
            catch (System.Exception e)
            {
                Log.Error($"[BDP] 标记部位破坏失败: {part.def.defName}, 错误: {e}");
                return false;
            }
        }

        /// <summary>
        /// 检查部位是否已被破坏。
        /// </summary>
        public bool IsDestroyed(BodyPartRecord part)
        {
            if (part == null) return false;
            return destroyedParts.Contains(part.Index);
        }

        /// <summary>
        /// 清理已破坏部位记录并移除所有破坏 Hediff（战斗体解除时调用）。
        /// </summary>
        public void Clear(Pawn pawn)
        {
            // 按 def 类型移除所有 BDP_CombatBodyPartDestroyed（含 PostAdd 递归生成的子部位缺失）
            if (pawn?.health?.hediffSet != null)
            {
                var hediffDef = BDP.Core.BDP_DefOf.BDP_CombatBodyPartDestroyed;
                var toRemove = pawn.health.hediffSet.hediffs
                    .Where(h => h.def == hediffDef)
                    .ToList();

                foreach (var hediff in toRemove)
                {
                    pawn.health.RemoveHediff(hediff);
                    Log.Message($"[BDP] 战斗体解除：恢复部位 {hediff.Part?.Label}(Index={hediff.Part?.Index})");
                }
            }

            destroyedPartHediffs.Clear();
            destroyedParts.Clear();
        }

        /// <summary>
        /// 在部位子树中查找Hand部位。
        /// 覆盖场景：Hand直接被毁、Arm被毁（Hand为子部位）、Shoulder被毁（Hand为孙部位）。
        /// </summary>
        /// <param name="part">被毁的根部位</param>
        /// <param name="handPart">找到的Hand部位（out）</param>
        /// <returns>true=子树中存在Hand</returns>
        private bool FindHandInSubtree(BodyPartRecord part, out BodyPartRecord handPart)
        {
            handPart = null;
            if (part == null) return false;

            // 自身就是Hand
            if (part.def.defName == "Hand")
            {
                handPart = part;
                return true;
            }

            // 递归搜索子部位
            foreach (var child in part.parts)
            {
                if (FindHandInSubtree(child, out handPart))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 获取手部侧边（左手/右手）。
        /// 优先使用原版的 woundAnchorTag 字段（显式标签），降级到 LabelShort 字符串匹配。
        /// </summary>
        private BDP.Core.HandSide GetHandSide(BodyPartRecord part)
        {
            var current = part;
            while (current != null)
            {
                // 优先使用 woundAnchorTag（原版标准方法）
                if (!string.IsNullOrEmpty(current.woundAnchorTag))
                {
                    // 检查是否包含 Left 或 Right
                    if (current.woundAnchorTag.Contains("Left"))
                    {
                        return BDP.Core.HandSide.Left;
                    }
                    if (current.woundAnchorTag.Contains("Right"))
                    {
                        return BDP.Core.HandSide.Right;
                    }
                }

                // 降级方案：使用 LabelShort（兼容没有标签的情况）
                string label = current.LabelShort.ToLower();
                if (label.Contains("left") || label.Contains("左"))
                {
                    return BDP.Core.HandSide.Left;
                }
                if (label.Contains("right") || label.Contains("右"))
                {
                    return BDP.Core.HandSide.Right;
                }

                current = current.parent;
            }

            // 如果找不到左右标识，记录警告
            Log.Warning($"[BDP] 无法判断手部侧边: {part.LabelShort} (defName: {part.def.defName})");
            return BDP.Core.HandSide.Left;
        }

        /// <summary>
        /// 序列化部位破坏数据。
        /// </summary>
        public void ExposeData()
        {
            // 序列化 Hediff 列表
            Scribe_Collections.Look(ref destroyedPartHediffs, "destroyedPartHediffs", LookMode.Reference);

            // 序列化已破坏部位集合
            List<int> destroyedPartsList = null;
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                destroyedPartsList = new List<int>(destroyedParts);
            }

            Scribe_Collections.Look(ref destroyedPartsList, "destroyedParts", LookMode.Value);

            // 读档时重建 HashSet
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                destroyedParts = new HashSet<int>();
                if (destroyedPartsList != null)
                {
                    foreach (var part in destroyedPartsList)
                    {
                        destroyedParts.Add(part);
                    }
                }

                // 初始化列表（如果为null）
                if (destroyedPartHediffs == null)
                {
                    destroyedPartHediffs = new List<Hediff>();
                }

                Log.Message($"[BDP] PartDestructionHandler读档完成: {destroyedParts.Count}个已破坏部位");
            }
        }
    }
}

