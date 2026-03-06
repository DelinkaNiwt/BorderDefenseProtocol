using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BDP.Combat;
using RimWorld;
using Verse;
using LudeonTK;

namespace BDP.Combat.Debug
{
    /// <summary>
    /// 战斗体系统阶段0前置验证工具。
    /// 通过 [DebugAction] 注册到游戏调试菜单（Dev模式下可用）。
    ///
    /// 验证任务1：Hediff快照回滚副作用验证
    /// 验证任务2：ThingOwner容器转移顺序验证
    ///
    /// 使用方法：
    ///   1. 开启Dev模式（选项→开发者模式）
    ///   2. 打开调试菜单（工具栏调试图标）→ BDP分类
    ///   3. 点击对应按钮，再点击目标Pawn
    ///   4. 查看游戏日志（Dev模式下右上角日志窗口）
    /// </summary>
    [StaticConstructorOnStartup]
    public static class CombatBodyDebugActions
    {
        // ── 日志前缀，便于过滤 ──
        private const string TAG = "[BDP阶段0验证]";

        // ═══════════════════════════════════════════
        //  验证任务1：Hediff快照回滚副作用验证
        // ═══════════════════════════════════════════

        /// <summary>
        /// 验证通过 RemoveHediff + AddHediff 回放Hediff时的副作用。
        /// 产出：副作用清单 + 是否需要 RestoreScope 的结论。
        /// </summary>
        [DebugAction("BDP", "验证Hediff快照回滚", actionType = DebugActionType.ToolMapForPawns)]
        private static void VerifyHediffSnapshot(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null)
            {
                Log.Warning($"{TAG} 目标Pawn无效");
                return;
            }

            Log.Message($"{TAG} ===== 开始验证Hediff快照回滚 [{pawn.LabelShortCap}] =====");

            // ── 步骤1：记录快照前状态 ──
            int originalCount = pawn.health.hediffSet.hediffs.Count;  // 过滤前的原始总数
            var records = TakeHediffRecords(pawn);
            Log.Message($"{TAG} 快照前 Hediff 数量: {originalCount}（记录 {records.Count} 条，跳过 {originalCount - records.Count} 条子部位MissingBodyPart）");
            foreach (var r in records)
                Log.Message($"{TAG}   · {r.defName} | severity={r.severity:F3} | part={r.bodyPartDefName}[{r.bodyPartIndex}]" +
                    $"{(r.level.HasValue ? $" | level={r.level}" : "")}" +
                    $"{(r.isPermanent.HasValue ? $" | perm={r.isPermanent} pain={r.painCategory}" : "")}" +
                    $"{(!string.IsNullOrEmpty(r.sourceLabel) ? $" | src={r.sourceLabel}" : "")}");

            // ── 步骤2：移除所有Hediff（排除配置项），观察副作用 ──
            Log.Message($"{TAG} --- 开始移除Hediff（跳过排除项）---");
            var toRemove = pawn.health.hediffSet.hediffs
                .Where(h => !BDPSnapshotConfigDef.IsExcluded(h)).ToList();
            foreach (var h in toRemove)
            {
                Log.Message($"{TAG}   RemoveHediff: {h.def.defName}");
                pawn.health.RemoveHediff(h);
            }
            int excludedCount = pawn.health.hediffSet.hediffs.Count;
            Log.Message($"{TAG} 移除后 Hediff 数量: {excludedCount}（预期=排除项数量）");

            // ── 步骤3：重新添加，观察副作用 ──
            Log.Message($"{TAG} --- 开始重新添加Hediff ---");
            int restoredCount = 0;
            foreach (var record in records)
            {
                var def = DefDatabase<HediffDef>.GetNamedSilentFail(record.defName);
                if (def == null)
                {
                    Log.Warning($"{TAG}   跳过未找到的Def: {record.defName}");
                    continue;
                }

                // 查找对应的BodyPartRecord
                BodyPartRecord part = null;
                if (!string.IsNullOrEmpty(record.bodyPartDefName))
                {
                    part = pawn.RaceProps.body.AllParts
                        .Where(p => p.def.defName == record.bodyPartDefName)
                        .ElementAtOrDefault(record.bodyPartIndex);
                    if (part == null)
                        Log.Warning($"{TAG}   未找到部位: {record.bodyPartDefName}[{record.bodyPartIndex}]");
                }

                Log.Message($"{TAG}   AddHediff: {record.defName} → part={part?.def?.defName ?? "whole"}");
                var restored = pawn.health.AddHediff(def, part);
                if (restored != null)
                {
                    // ── 恢复顺序：来源信息 → GetsPermanent → Level/Severity ──

                    // 问题4：恢复来源信息
                    if (!string.IsNullOrEmpty(record.sourceLabel))
                        restored.sourceLabel = record.sourceLabel;
                    if (!string.IsNullOrEmpty(record.sourceDefName))
                        restored.sourceDef = DefDatabase<ThingDef>.GetNamedSilentFail(record.sourceDefName);
                    if (!string.IsNullOrEmpty(record.sourceToolLabel))
                        restored.sourceToolLabel = record.sourceToolLabel;

                    // 问题3：恢复HediffComp_GetsPermanent
                    // isPermanentInt是公开字段，直接赋值；painCategory用SetPainCategory()写入
                    if (record.isPermanent.HasValue)
                    {
                        var permComp = restored.TryGetComp<HediffComp_GetsPermanent>();
                        if (permComp != null)
                        {
                            permComp.isPermanentInt = record.isPermanent.Value;
                            if (record.painCategory.HasValue)
                                permComp.SetPainCategory((PainCategory)record.painCategory.Value);
                        }
                    }

                    // 问题2：恢复Hediff_Level（必须先设level再设Severity）
                    if (restored is Hediff_Level restoredLevel && record.level.HasValue)
                    {
                        restoredLevel.level = record.level.Value;
                        restoredLevel.Severity = record.severity;
                    }
                    else
                    {
                        // 普通Severity恢复
                        restored.Severity = record.severity;
                    }

                    restoredCount++;
                }
            }

            // ── 步骤4：调用DirtyCache ──
            pawn.health.hediffSet.DirtyCache();
            Log.Message($"{TAG} DirtyCache() 已调用");

            // ── 步骤5：对比结果 ──
            int finalCount = pawn.health.hediffSet.hediffs.Count;
            Log.Message($"{TAG} --- 验证结果 ---");
            Log.Message($"{TAG} 原始数量: {originalCount} | 排除项: {excludedCount} | 记录数量: {records.Count} | 恢复数量: {restoredCount} | 当前数量: {finalCount}");

            // 正确基准：恢复后总数应等于快照前原始总数（排除项保留 + 义肢/MissingBodyPart子部位自动补全）
            if (finalCount == originalCount)
                Log.Message($"{TAG} ✓ Hediff数量一致");
            else
                Log.Warning($"{TAG} ✗ Hediff数量不一致！差值: {finalCount - originalCount}（原始{originalCount} → 当前{finalCount}）");

            Log.Message($"{TAG} ===== 验证任务1完成 =====");
            Log.Message($"{TAG} 请检查上方日志中 AddHediff/RemoveHediff 触发的额外通知（思想变化、受伤消息等）");
        }

        /// <summary>
        /// 记录Pawn当前所有Hediff的字段值快照。
        /// 修复5个已知问题：
        ///   问题1：跳过子部位MissingBodyPart（父部位已缺失时子部位自动缺失，无需单独记录）
        ///   问题2：记录Hediff_Level的level字段
        ///   问题3：记录HediffComp_GetsPermanent的isPermanent和painCategory
        ///   问题4：记录伤口来源信息（sourceLabel/sourceDef/sourceToolLabel）
        /// 排除机制：BDPSnapshotConfigDef.IsExcluded() 过滤不参与快照的hediff（如启灵神经、机控中枢）
        /// </summary>
        private static List<HediffRecord> TakeHediffRecords(Pawn pawn)
        {
            var records = new List<HediffRecord>();
            foreach (var h in pawn.health.hediffSet.hediffs)
            {
                // 排除配置：不参与快照的hediff（启灵神经、机控中枢等）
                if (BDPSnapshotConfigDef.IsExcluded(h))
                    continue;

                // 问题1：跳过子部位的MissingBodyPart
                // 当任意祖先部位"不可用"时（有MissingBodyPart 或 有义肢/仿生替换），
                // 子部位的MissingBodyPart会在祖先恢复时自动补全，无需单独记录
                if (h.def == HediffDefOf.MissingBodyPart && h.Part?.parent != null)
                {
                    if (AnyAncestorIsUnavailable(pawn, h.Part))
                        continue;
                }

                // 计算同名部位索引（区分左右手等）
                int partIndex = 0;
                if (h.Part != null)
                {
                    partIndex = pawn.RaceProps.body.AllParts
                        .Where(p => p.def == h.Part.def)
                        .ToList()
                        .IndexOf(h.Part);
                    if (partIndex < 0) partIndex = 0;
                }

                var record = new HediffRecord
                {
                    defName        = h.def.defName,
                    severity       = h.Severity,
                    bodyPartDefName = h.Part?.def?.defName ?? string.Empty,
                    bodyPartIndex  = partIndex,
                    // 问题4：来源信息
                    sourceLabel    = h.sourceLabel,
                    sourceDefName  = h.sourceDef?.defName,
                    sourceToolLabel = h.sourceToolLabel,
                };

                // 问题2：Hediff_Level（灵能等级等）
                if (h is Hediff_Level levelHediff)
                    record.level = levelHediff.level;

                // 问题3：HediffComp_GetsPermanent（老枪伤/酸痛）
                var permComp = h.TryGetComp<HediffComp_GetsPermanent>();
                if (permComp != null)
                {
                    record.isPermanent  = permComp.isPermanentInt;
                    record.painCategory = (int)permComp.PainCategory;  // 只读属性
                }

                records.Add(record);
            }
            return records;
        }

        /// <summary>
        /// 检查part的任意祖先部位是否"不可用"：
        /// 有MissingBodyPart 或 有义肢/仿生替换（addedPartProps != null）。
        /// 用于问题1过滤：子部位的MissingBodyPart在祖先恢复时会自动补全。
        /// </summary>
        private static bool AnyAncestorIsUnavailable(Pawn pawn, BodyPartRecord part)
        {
            var current = part.parent;
            while (current != null)
            {
                bool unavailable = pawn.health.hediffSet.hediffs.Any(h =>
                    h.Part == current &&
                    (h.def == HediffDefOf.MissingBodyPart || h.def.addedPartProps != null));
                if (unavailable) return true;
                current = current.parent;
            }
            return false;
        }

        // ═══════════════════════════════════════════
        //  验证任务2：ThingOwner容器转移顺序验证
        // ═══════════════════════════════════════════

        /// <summary>
        /// 验证衣物/物品从Pawn转移到ThingOwner容器的正确顺序，以及恢复时的反向流程。
        /// 产出：正确的转移顺序 + holdingOwner冲突情况。
        /// </summary>
        [DebugAction("BDP", "验证容器转移顺序", actionType = DebugActionType.ToolMapForPawns)]
        private static void VerifyContainerTransfer(Pawn pawn)
        {
            if (pawn?.apparel == null || pawn.inventory == null)
            {
                Log.Warning($"{TAG} 目标Pawn无效");
                return;
            }

            Log.Message($"{TAG} ===== 开始验证容器转移顺序 [{pawn.LabelShortCap}] =====");
            Log.Message($"{TAG} 初始状态 | 衣物: {pawn.apparel.WornApparelCount} | 物品: {pawn.inventory.innerContainer.Count}");

            // ── 创建临时容器（模拟快照容器） ──
            // 注意：owner传null，因为快照容器是独立的IThingHolder
            var apparelContainer = new ThingOwner<Apparel>(null);
            var inventoryContainer = new ThingOwner<Thing>(null);

            // ── 步骤1：衣物转移（记录所有状态：locked + forced） ──
            Log.Message($"{TAG} --- 衣物转移（Remove → TryAdd）---");
            var wornApparel = pawn.apparel.WornApparel.ToList();
            var lockedFlags = new Dictionary<Apparel, bool>();
            var forcedFlags = new Dictionary<Apparel, bool>();

            foreach (var apparel in wornApparel)
            {
                // 记录锁定状态
                lockedFlags[apparel] = pawn.apparel.IsLocked(apparel);

                // 记录强制状态（OutfitForcedHandler）
                forcedFlags[apparel] = pawn.outfits?.forcedHandler?.IsForced(apparel) ?? false;

                Log.Message($"{TAG}   转移前: {apparel.def.defName} | locked={lockedFlags[apparel]} | forced={forcedFlags[apparel]}");

                pawn.apparel.Remove(apparel);
                bool added = apparelContainer.TryAdd(apparel);

                Log.Message($"{TAG}   转移后: added={added} | holdingOwner={apparel.holdingOwner?.GetType()?.Name ?? "null"}");
            }
            Log.Message($"{TAG} 衣物转移后 | Pawn衣物: {pawn.apparel.WornApparelCount}（预期0）| 容器: {apparelContainer.Count}");

            // ── 步骤2：物品转移（记录状态：notForSale + unpackedCaravan） ──
            Log.Message($"{TAG} --- 物品转移（TryTransferToContainer）---");
            var items = pawn.inventory.innerContainer.ToList();
            var notForSaleFlags = new Dictionary<Thing, bool>();
            var unpackedCaravanFlags = new Dictionary<Thing, bool>();

            foreach (var item in items)
            {
                // 记录"不出售"标记
                notForSaleFlags[item] = pawn.inventory.NotForSale(item);

                // 记录"商队解包"标记（通过反射访问私有字段）
                var unpackedField = typeof(Pawn_InventoryTracker).GetField("unpackedCaravanItems",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var unpackedList = unpackedField?.GetValue(pawn.inventory) as System.Collections.IList;
                unpackedCaravanFlags[item] = unpackedList?.Contains(item) ?? false;

                Log.Message($"{TAG}   转移前: {item.def.defName} | notForSale={notForSaleFlags[item]} | unpacked={unpackedCaravanFlags[item]}");

                bool transferred = pawn.inventory.innerContainer.TryTransferToContainer(item, inventoryContainer);
                Log.Message($"{TAG}   转移后: transferred={transferred}");
            }
            Log.Message($"{TAG} 物品转移后 | Pawn物品: {pawn.inventory.innerContainer.Count}（预期0）| 容器: {inventoryContainer.Count}");

            // ── 步骤3：恢复衣物（Wear + 恢复 locked + forced 状态） ──
            Log.Message($"{TAG} --- 衣物恢复（Remove → Wear → 恢复状态）---");
            var toRestoreApparel = apparelContainer.InnerListForReading.ToList();
            foreach (var apparel in toRestoreApparel)
            {
                apparelContainer.Remove(apparel);

                bool wasLocked = lockedFlags.TryGetValue(apparel, out bool locked) && locked;
                bool wasForced = forcedFlags.TryGetValue(apparel, out bool forced) && forced;

                // 穿上衣物（locked参数会调用Lock()）
                pawn.apparel.Wear(apparel, dropReplacedApparel: false, locked: wasLocked);

                // 恢复强制状态（OutfitForcedHandler）
                if (wasForced && pawn.outfits?.forcedHandler != null)
                {
                    pawn.outfits.forcedHandler.SetForced(apparel, forced: true);
                }

                // 验证恢复结果
                bool isLockedNow = pawn.apparel.IsLocked(apparel);
                bool isForcedNow = pawn.outfits?.forcedHandler?.IsForced(apparel) ?? false;

                string status = (wasLocked == isLockedNow && wasForced == isForcedNow) ? "✓" : "✗";
                Log.Message($"{TAG}   {status} {apparel.def.defName} | locked:{wasLocked}→{isLockedNow} | forced:{wasForced}→{isForcedNow}");
            }

            // ── 步骤4：恢复物品（TryTransferToContainer + 恢复状态标记） ──
            Log.Message($"{TAG} --- 物品恢复（TryTransferToContainer → 恢复状态）---");
            var toRestoreItems = inventoryContainer.InnerListForReading.ToList();
            foreach (var item in toRestoreItems)
            {
                bool wasNotForSale = notForSaleFlags.TryGetValue(item, out bool notForSale) && notForSale;
                bool wasUnpacked = unpackedCaravanFlags.TryGetValue(item, out bool unpacked) && unpacked;

                bool transferred = inventoryContainer.TryTransferToContainer(item, pawn.inventory.innerContainer);

                // 恢复"不出售"标记
                if (wasNotForSale)
                {
                    pawn.inventory.TryAddItemNotForSale(item);
                }

                // 恢复"商队解包"标记（通过反射访问私有字段）
                if (wasUnpacked)
                {
                    var unpackedField = typeof(Pawn_InventoryTracker).GetField("unpackedCaravanItems",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var unpackedList = unpackedField?.GetValue(pawn.inventory) as System.Collections.IList;
                    if (unpackedList != null && !unpackedList.Contains(item))
                    {
                        unpackedList.Add(item);
                    }
                }

                // 验证恢复结果
                bool isNotForSaleNow = pawn.inventory.NotForSale(item);
                var unpackedListNow = typeof(Pawn_InventoryTracker)
                    .GetField("unpackedCaravanItems", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.GetValue(pawn.inventory) as System.Collections.IList;
                bool isUnpackedNow = unpackedListNow?.Contains(item) ?? false;

                string status = (wasNotForSale == isNotForSaleNow && wasUnpacked == isUnpackedNow) ? "✓" : "✗";
                Log.Message($"{TAG}   {status} {item.def.defName} | notForSale:{wasNotForSale}→{isNotForSaleNow} | unpacked:{wasUnpacked}→{isUnpackedNow}");
            }

            // ── 步骤5：输出最终结果 ──
            Log.Message($"{TAG} --- 验证结果 ---");
            Log.Message($"{TAG} 恢复后 | 衣物: {pawn.apparel.WornApparelCount}（预期{wornApparel.Count}）| 物品: {pawn.inventory.innerContainer.Count}（预期{items.Count}）");

            if (pawn.apparel.WornApparelCount == wornApparel.Count && pawn.inventory.innerContainer.Count == items.Count)
                Log.Message($"{TAG} ✓ 容器转移验证通过，无数量损失");
            else
                Log.Warning($"{TAG} ✗ 容器转移存在数量差异！");

            Log.Message($"{TAG} ===== 验证任务2完成 =====");
        }

        // ═══════════════════════════════════════════
        //  内部数据结构
        // ═══════════════════════════════════════════

        /// <summary>
        /// Hediff字段值快照（轻量记录，仅用于验证阶段）。
        /// 正式实现见 CombatBodySnapshot.HediffRecord（阶段1）。
        /// </summary>
        private class HediffRecord
        {
            public string defName;
            public float severity;
            public string bodyPartDefName;  // 空字符串 = 全身性hediff
            public int bodyPartIndex;       // 同名部位索引（区分左右手等）

            // 问题2：Hediff_Level支持（灵能等级等）
            public int? level;

            // 问题3：HediffComp_GetsPermanent状态（老枪伤/酸痛）
            public bool? isPermanent;
            public int? painCategory;       // PainCategory枚举值

            // 问题4：伤口来源信息（割伤来源武器/工具）
            public string sourceLabel;
            public string sourceDefName;
            public string sourceToolLabel;
        }
    }
}
