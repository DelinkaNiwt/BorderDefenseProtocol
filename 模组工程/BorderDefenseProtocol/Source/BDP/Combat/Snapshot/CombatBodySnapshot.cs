using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BDP;
using RimWorld;
using Verse;

namespace BDP.Combat.Snapshot
{
    /// <summary>
    /// 战斗体快照系统核心类。
    /// 负责拍摄和恢复 Pawn 的完整状态（Hediff、需求、衣物、物品）。
    /// 实现 IThingHolder 接口以支持 ThingOwner 序列化。
    /// </summary>
    public class CombatBodySnapshot : IExposable, IThingHolder
    {
        // ── 容器 ──
        private ThingOwner<Apparel> originalApparelContainer;    // 原衣物
        private ThingOwner<Thing> originalInventoryContainer;    // 原物品
        private ThingOwner<Apparel> combatApparelContainer;      // 战斗体衣物（未激活时存放）

        // ── 状态记录 ──
        private Dictionary<int, ApparelState> apparelStates;     // key = thingIDNumber
        private Dictionary<int, ItemState> itemStates;

        // ── Hediff 快照 ──
        private List<HediffRecord> hediffSnapshots;

        // ── 需求快照 ──
        private Dictionary<NeedDef, float> needValues;

        // ── Pawn 引用 ──
        private Pawn pawn;

        // ── 构造函数 ──
        public CombatBodySnapshot()
        {
            // 无参构造函数用于序列化
        }

        public CombatBodySnapshot(Pawn pawn)
        {
            this.pawn = pawn;
            originalApparelContainer = new ThingOwner<Apparel>(this);
            originalInventoryContainer = new ThingOwner<Thing>(this);
            combatApparelContainer = new ThingOwner<Apparel>(this);
            apparelStates = new Dictionary<int, ApparelState>();
            itemStates = new Dictionary<int, ItemState>();
            hediffSnapshots = new List<HediffRecord>();
            needValues = new Dictionary<NeedDef, float>();
        }

        // ── 公开访问器（用于初始化战斗体装备和查看状态） ──
        public ThingOwner<Apparel> CombatApparelContainer => combatApparelContainer;
        public ThingOwner<Apparel> OriginalApparelContainer => originalApparelContainer;
        public ThingOwner<Thing> OriginalInventoryContainer => originalInventoryContainer;

        /// <summary>
        /// 获取Hediff快照列表（只读）。
        /// 用于外部检查hediff是否在快照中。
        /// </summary>
        public IReadOnlyList<HediffRecord> GetHediffSnapshots() => hediffSnapshots;

        // ── IThingHolder 接口 ──
        public IThingHolder ParentHolder => pawn;

        public ThingOwner GetDirectlyHeldThings() => originalApparelContainer;

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            if (originalApparelContainer != null)
                outChildren.Add((IThingHolder)originalApparelContainer);
            if (originalInventoryContainer != null)
                outChildren.Add((IThingHolder)originalInventoryContainer);
            if (combatApparelContainer != null)
                outChildren.Add((IThingHolder)combatApparelContainer);
        }

        // ═══════════════════════════════════════════════════════════════
        // 新架构API（v2.0）：职责拆分为记录层、执行层、清理层、恢复层
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// 记录层：拍摄所有快照（Hediff + 需求），不改变Pawn状态。
        /// </summary>
        public void SnapshotAll()
        {
            SnapshotHediffs();
            SnapshotNeeds();
        }

        /// <summary>
        /// 执行层：应用战斗体转换（换装 + 物品转移），改变Pawn状态。
        /// </summary>
        public void ApplyTransformation()
        {
            TransferApparelToCombat();
            TransferInventoryToSnapshot();
        }

        /// <summary>
        /// 清理层：移除所有非排除项的Hediff（清理真身状态，为战斗体Hediff腾出空间）。
        /// </summary>
        public void RemoveAllHediffsExceptExcluded()
        {
            var toRemove = pawn.health.hediffSet.hediffs
                .Where(h => !BDPSnapshotConfigDef.IsExcluded(h)).ToList();
            foreach (var h in toRemove)
            {
                pawn.health.RemoveHediff(h);
            }
        }

        /// <summary>
        /// 恢复层：恢复所有快照状态（衣物 + 物品 + 需求 + Hediff）。
        /// </summary>
        public void RestoreAll()
        {
            RestoreApparelFromSnapshot();
            RestoreInventoryFromSnapshot();
            RestoreNeeds();
            RestoreHediffs();
        }

        // ═══════════════════════════════════════════════════════════════
        // 向后兼容API（v1.0）：保留现有方法，内部调用新架构
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// 激活：拍摄快照 + 换装（向后兼容方法）。
        /// </summary>
        public void TakeSnapshotAndActivate()
        {
            SnapshotAll();
            ApplyTransformation();
        }

        /// <summary>
        /// 解除：恢复快照 + 换回（向后兼容方法）。
        /// </summary>
        public void RestoreSnapshotAndDeactivate()
        {
            RestoreAll();
        }

        // ═══════════════════════════════════════════════════════════════
        // 衣物转移逻辑
        // ═══════════════════════════════════════════════════════════════

        private void TransferApparelToCombat()
        {
            // 记录原衣物状态并转移到容器
            var wornApparel = pawn.apparel.WornApparel.ToList();
            apparelStates.Clear();

            foreach (var apparel in wornApparel)
            {
                // 记录状态
                apparelStates[apparel.thingIDNumber] = new ApparelState
                {
                    wasLocked = pawn.apparel.IsLocked(apparel),
                    wasForced = pawn.outfits?.forcedHandler?.IsForced(apparel) ?? false
                };

                // 转移到容器
                pawn.apparel.Remove(apparel);
                originalApparelContainer.TryAdd(apparel);
            }

            // 选项B：按原身服装生成战斗体装备副本（相同外观，无品质，耐久满）
            if (BDPModInstance.Settings.combatApparelMode == CombatApparelMode.MirrorOriginal)
            {
                combatApparelContainer.ClearAndDestroyContents();
                foreach (var original in originalApparelContainer.InnerListForReading)
                {
                    // 传入相同 Stuff，保证视觉颜色基底一致
                    var copy = (Apparel)ThingMaker.MakeThing(original.def, original.Stuff);

                    // 复制自定义颜色（CompColorable）
                    var srcColor = original.TryGetComp<CompColorable>();
                    var dstColor = copy.TryGetComp<CompColorable>();
                    if (srcColor != null && srcColor.Active && dstColor != null)
                        dstColor.SetColor(srcColor.Color);

                    // 复制意识形态风格（StyleDef）
                    if (original.StyleDef != null)
                        copy.StyleDef = original.StyleDef;

                    // 移除品质（战斗体装备无品质）
                    // ThingWithComps 有三处品质引用需全部清除：
                    //   1. comps 列表（私有，反射）
                    //   2. compQuality 缓存字段（公开，直接赋值）
                    //   3. compsByType 字典缓存（私有，反射）
                    var qualityComp = copy.TryGetComp<CompQuality>();
                    if (qualityComp != null)
                    {
                        var compsField = typeof(ThingWithComps).GetField(
                            "comps", BindingFlags.NonPublic | BindingFlags.Instance);
                        (compsField?.GetValue(copy) as List<ThingComp>)?.Remove(qualityComp);

                        copy.compQuality = null;

                        var byTypeField = typeof(ThingWithComps).GetField(
                            "compsByType", BindingFlags.NonPublic | BindingFlags.Instance);
                        (byTypeField?.GetValue(copy) as Dictionary<Type, ThingComp[]>)
                            ?.Remove(typeof(CompQuality));
                    }

                    combatApparelContainer.TryAdd(copy);
                }
            }

            // 穿上战斗体衣物（选项A/B 通用）
            var combatApparels = combatApparelContainer.InnerListForReading.ToList();
            foreach (var apparel in combatApparels)
            {
                combatApparelContainer.Remove(apparel);
                pawn.apparel.Wear(apparel, dropReplacedApparel: false, locked: true);
            }
        }

        private void RestoreApparelFromSnapshot()
        {
            // 脱下战斗体衣物
            var currentApparel = pawn.apparel.WornApparel.ToList();
            foreach (var apparel in currentApparel)
            {
                pawn.apparel.Remove(apparel);
                if (BDPModInstance.Settings.combatApparelMode == CombatApparelMode.MirrorOriginal)
                    apparel.Destroy(); // 选项B：销毁生成的副本
                else
                    combatApparelContainer.TryAdd(apparel); // 选项A：存回容器
            }

            // 穿回原衣物并恢复状态
            var originalApparels = originalApparelContainer.InnerListForReading.ToList();
            foreach (var apparel in originalApparels)
            {
                originalApparelContainer.Remove(apparel);

                // 获取原状态
                bool wasLocked = apparelStates.TryGetValue(apparel.thingIDNumber, out var state) && state.wasLocked;
                bool wasForced = state?.wasForced ?? false;

                // 穿上衣物
                pawn.apparel.Wear(apparel, dropReplacedApparel: false, locked: wasLocked);

                // 恢复强制状态
                if (wasForced && pawn.outfits?.forcedHandler != null)
                {
                    pawn.outfits.forcedHandler.SetForced(apparel, forced: true);
                }
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // 物品转移逻辑
        // ═══════════════════════════════════════════════════════════════

        private void TransferInventoryToSnapshot()
        {
            // 记录物品状态并转移到容器
            var items = pawn.inventory.innerContainer.ToList();
            itemStates.Clear();

            foreach (var item in items)
            {
                // 记录状态
                itemStates[item.thingIDNumber] = new ItemState
                {
                    wasNotForSale = pawn.inventory.NotForSale(item),
                    wasUnpackedCaravan = IsUnpackedCaravanItem(item)
                };

                // 转移到容器
                pawn.inventory.innerContainer.TryTransferToContainer(item, originalInventoryContainer);
            }
        }

        private void RestoreInventoryFromSnapshot()
        {
            // 恢复物品并恢复状态
            var items = originalInventoryContainer.InnerListForReading.ToList();
            foreach (var item in items)
            {
                // 转移回背包
                originalInventoryContainer.TryTransferToContainer(item, pawn.inventory.innerContainer);

                // 获取原状态
                if (itemStates.TryGetValue(item.thingIDNumber, out var state))
                {
                    // 恢复"不出售"标记
                    if (state.wasNotForSale)
                    {
                        pawn.inventory.TryAddItemNotForSale(item);
                    }

                    // 恢复"商队解包"标记
                    if (state.wasUnpackedCaravan)
                    {
                        AddToUnpackedCaravanItems(item);
                    }
                }
            }
        }

        // 反射访问 unpackedCaravanItems 私有字段
        private bool IsUnpackedCaravanItem(Thing item)
        {
            var field = typeof(Pawn_InventoryTracker).GetField("unpackedCaravanItems",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var list = field?.GetValue(pawn.inventory) as System.Collections.IList;
            return list?.Contains(item) ?? false;
        }

        private void AddToUnpackedCaravanItems(Thing item)
        {
            var field = typeof(Pawn_InventoryTracker).GetField("unpackedCaravanItems",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var list = field?.GetValue(pawn.inventory) as System.Collections.IList;
            if (list != null && !list.Contains(item))
            {
                list.Add(item);
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // Hediff 快照逻辑（复用验证逻辑）
        // ═══════════════════════════════════════════════════════════════

        private void SnapshotHediffs()
        {
            hediffSnapshots.Clear();

            foreach (var h in pawn.health.hediffSet.hediffs)
            {
                // 排除配置：不参与快照的hediff（启灵神经、机控中枢等）
                if (BDPSnapshotConfigDef.IsExcluded(h))
                    continue;

                // 跳过子部位的MissingBodyPart
                if (h.def == HediffDefOf.MissingBodyPart && h.Part?.parent != null)
                {
                    if (AnyAncestorIsUnavailable(h.Part))
                        continue;
                }

                // 计算同名部位索引
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
                    defName = h.def.defName,
                    severity = h.Severity,
                    bodyPartDefName = h.Part?.def?.defName ?? string.Empty,
                    bodyPartIndex = partIndex,
                    ageTicks = h.ageTicks,
                    sourceLabel = h.sourceLabel,
                    sourceDefName = h.sourceDef?.defName,
                    sourceToolLabel = h.sourceToolLabel,
                };

                // Hediff_Level支持
                if (h is Hediff_Level levelHediff)
                    record.level = levelHediff.level;

                // HediffComp_GetsPermanent状态
                var permComp = h.TryGetComp<HediffComp_GetsPermanent>();
                if (permComp != null)
                {
                    record.isPermanent = permComp.IsPermanent;
                    record.painCategory = (int)permComp.PainCategory;
                }

                // Hediff_MissingPart特殊字段
                if (h is Hediff_MissingPart missingPart)
                {
                    record.isFresh = missingPart.IsFresh;
                    record.lastInjuryDefName = missingPart.lastInjury?.defName;
                }

                hediffSnapshots.Add(record);
            }
        }

        private void RestoreHediffs()
        {
            // 移除激活期间新增的hediff（排除配置项）
            var toRemove = pawn.health.hediffSet.hediffs
                .Where(h => !BDPSnapshotConfigDef.IsExcluded(h)).ToList();
            foreach (var h in toRemove)
            {
                pawn.health.RemoveHediff(h);
            }

            // 重新添加快照中的hediff
            foreach (var record in hediffSnapshots)
            {
                var def = DefDatabase<HediffDef>.GetNamedSilentFail(record.defName);
                if (def == null)
                {
                    Log.Warning($"[BDP] 无法找到HediffDef: {record.defName}");
                    continue;
                }

                // 查找部位
                BodyPartRecord part = null;
                if (!string.IsNullOrEmpty(record.bodyPartDefName))
                {
                    var partDef = DefDatabase<BodyPartDef>.GetNamedSilentFail(record.bodyPartDefName);
                    if (partDef != null)
                    {
                        var candidates = pawn.RaceProps.body.AllParts.Where(p => p.def == partDef).ToList();
                        if (record.bodyPartIndex < candidates.Count)
                            part = candidates[record.bodyPartIndex];
                    }
                }

                // 添加hediff
                var hediff = pawn.health.AddHediff(def, part);

                // 恢复字段值（按顺序）
                // 1. 来源信息
                hediff.sourceLabel = record.sourceLabel;
                if (!string.IsNullOrEmpty(record.sourceDefName))
                    hediff.sourceDef = DefDatabase<ThingDef>.GetNamedSilentFail(record.sourceDefName);
                hediff.sourceToolLabel = record.sourceToolLabel;

                // 2. HediffComp_GetsPermanent（必须在Severity之前设置）
                var permComp = hediff.TryGetComp<HediffComp_GetsPermanent>();
                if (permComp != null && record.isPermanent.HasValue)
                {
                    // 使用IsPermanent属性setter（会自动设置permanentDamageThreshold和painCategory）
                    permComp.IsPermanent = record.isPermanent.Value;

                    // 如果有保存的painCategory，覆盖自动设置的随机值
                    if (record.painCategory.HasValue)
                    {
                        permComp.SetPainCategory((PainCategory)record.painCategory.Value);
                    }
                }

                // 3. Hediff_Level / Severity
                if (hediff is Hediff_Level levelHediff && record.level.HasValue)
                {
                    levelHediff.level = record.level.Value;
                    levelHediff.Severity = levelHediff.def.initialSeverity;
                }
                else
                {
                    hediff.Severity = record.severity;
                }

                // 4. ageTicks（在Severity之后设置）
                hediff.ageTicks = record.ageTicks;

                // 5. Hediff_MissingPart特殊字段
                if (hediff is Hediff_MissingPart missingPart)
                {
                    if (record.isFresh.HasValue)
                    {
                        missingPart.IsFresh = record.isFresh.Value;
                    }
                    if (!string.IsNullOrEmpty(record.lastInjuryDefName))
                    {
                        missingPart.lastInjury = DefDatabase<HediffDef>.GetNamedSilentFail(record.lastInjuryDefName);
                    }
                }

                // 6. 通知Hediff状态变更（刷新缓存）
                pawn.health.Notify_HediffChanged(hediff);
            }

            // 刷新缓存
            pawn.health.hediffSet.DirtyCache();
        }

        private bool AnyAncestorIsUnavailable(BodyPartRecord part)
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

        // ═══════════════════════════════════════════════════════════════
        // 需求快照逻辑
        // ═══════════════════════════════════════════════════════════════

        private void SnapshotNeeds()
        {
            needValues.Clear();
            if (pawn.needs == null) return;

            foreach (var need in pawn.needs.AllNeeds)
            {
                needValues[need.def] = need.CurLevel;
            }
        }

        private void RestoreNeeds()
        {
            if (pawn.needs == null) return;

            foreach (var kvp in needValues)
            {
                var need = pawn.needs.TryGetNeed(kvp.Key);
                if (need != null)
                {
                    need.CurLevel = kvp.Value;
                }
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // 序列化
        // ═══════════════════════════════════════════════════════════════

        public void ExposeData()
        {
            // 容器
            Scribe_Deep.Look(ref originalApparelContainer, "originalApparel", this);
            Scribe_Deep.Look(ref originalInventoryContainer, "originalInventory", this);
            Scribe_Deep.Look(ref combatApparelContainer, "combatApparel", this);

            // 状态字典（手动序列化 key-value）
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                var apparelKeys = apparelStates.Keys.ToList();
                var apparelValues = apparelStates.Values.ToList();
                Scribe_Collections.Look(ref apparelKeys, "apparelStateKeys", LookMode.Value);
                Scribe_Collections.Look(ref apparelValues, "apparelStateValues", LookMode.Deep);

                var itemKeys = itemStates.Keys.ToList();
                var itemValues = itemStates.Values.ToList();
                Scribe_Collections.Look(ref itemKeys, "itemStateKeys", LookMode.Value);
                Scribe_Collections.Look(ref itemValues, "itemStateValues", LookMode.Deep);
            }
            else if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                List<int> apparelKeys = null;
                List<ApparelState> apparelValues = null;
                Scribe_Collections.Look(ref apparelKeys, "apparelStateKeys", LookMode.Value);
                Scribe_Collections.Look(ref apparelValues, "apparelStateValues", LookMode.Deep);

                if (apparelKeys != null && apparelValues != null)
                {
                    apparelStates = new Dictionary<int, ApparelState>();
                    for (int i = 0; i < apparelKeys.Count; i++)
                        apparelStates[apparelKeys[i]] = apparelValues[i];
                }

                List<int> itemKeys = null;
                List<ItemState> itemValues = null;
                Scribe_Collections.Look(ref itemKeys, "itemStateKeys", LookMode.Value);
                Scribe_Collections.Look(ref itemValues, "itemStateValues", LookMode.Deep);

                if (itemKeys != null && itemValues != null)
                {
                    itemStates = new Dictionary<int, ItemState>();
                    for (int i = 0; i < itemKeys.Count; i++)
                        itemStates[itemKeys[i]] = itemValues[i];
                }
            }

            // Hediff 快照
            Scribe_Collections.Look(ref hediffSnapshots, "hediffSnapshots", LookMode.Deep);

            // 需求快照
            Scribe_Collections.Look(ref needValues, "needValues", LookMode.Def, LookMode.Value);

            // Pawn 引用
            Scribe_References.Look(ref pawn, "pawn");

            // 后初始化容器
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (originalApparelContainer == null)
                    originalApparelContainer = new ThingOwner<Apparel>(this);
                if (originalInventoryContainer == null)
                    originalInventoryContainer = new ThingOwner<Thing>(this);
                if (combatApparelContainer == null)
                    combatApparelContainer = new ThingOwner<Apparel>(this);
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // HediffRecord 类定义
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Hediff字段值快照记录。
        /// 保存Hediff的所有必要状态，用于战斗体解除后的完整恢复。
        /// </summary>
        public class HediffRecord : IExposable
        {
            // 基础字段
            public string defName;
            public float severity;
            public string bodyPartDefName;
            public int bodyPartIndex;
            public int ageTicks;

            // Hediff_Level支持
            public int? level;

            // HediffComp_GetsPermanent状态
            public bool? isPermanent;
            public int? painCategory;

            // 伤口来源信息
            public string sourceLabel;
            public string sourceDefName;
            public string sourceToolLabel;

            // Hediff_MissingPart特殊字段
            public bool? isFresh;
            public string lastInjuryDefName;

            public void ExposeData()
            {
                Scribe_Values.Look(ref defName, "defName");
                Scribe_Values.Look(ref severity, "severity");
                Scribe_Values.Look(ref bodyPartDefName, "bodyPartDefName");
                Scribe_Values.Look(ref bodyPartIndex, "bodyPartIndex");
                Scribe_Values.Look(ref ageTicks, "ageTicks");
                Scribe_Values.Look(ref level, "level");
                Scribe_Values.Look(ref isPermanent, "isPermanent");
                Scribe_Values.Look(ref painCategory, "painCategory");
                Scribe_Values.Look(ref sourceLabel, "sourceLabel");
                Scribe_Values.Look(ref sourceDefName, "sourceDefName");
                Scribe_Values.Look(ref sourceToolLabel, "sourceToolLabel");
                Scribe_Values.Look(ref isFresh, "isFresh");
                Scribe_Values.Look(ref lastInjuryDefName, "lastInjuryDefName");
            }
        }
    }
}

