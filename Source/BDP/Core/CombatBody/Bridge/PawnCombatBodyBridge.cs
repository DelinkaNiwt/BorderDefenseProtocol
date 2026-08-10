using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BDP.Core.CombatBody.Presentation;
using RimWorld;
using Verse;

namespace BDP.Core.CombatBody
{
    /// <summary>
    /// `Pawn` 战斗体宿主事务桥。
    /// 它只负责把宿主快照/前台衣物层接到 `Pawn` 身上，不承担会话编排职责。
    /// </summary>
    internal sealed class PawnCombatBodyBridge : ICombatBodyHost
    {
        /// <summary>
        /// 当前宿主持有的事务状态。
        /// </summary>
        private readonly HostState hostState;

        /// <summary>
        /// 当前宿主持有的快照服务。
        /// </summary>
        private readonly CombatBodySnapshotService snapshotService;

        /// <summary>
        /// 当前宿主持有的快照排除策略。
        /// </summary>
        private readonly CombatBodySnapshotPolicy snapshotPolicy;

        /// <summary>
        /// 构造 `Pawn` 战斗体宿主桥。
        /// </summary>
        public PawnCombatBodyBridge(
            Pawn pawn,
            HostState hostState,
            CombatBodySnapshotService snapshotService,
            CombatBodySnapshotPolicy snapshotPolicy)
        {
            Pawn = pawn;
            this.hostState = hostState;
            this.snapshotService = snapshotService;
            this.snapshotPolicy = snapshotPolicy;
        }

        /// <summary>
        /// 当前被桥接的 `Pawn` 宿主。
        /// </summary>
        public Pawn Pawn { get; }

        /// <summary>
        /// 读档后协调战斗体阶段与宿主回滚凭据。
        /// 旧档缺少完整快照时只关闭完整回滚资格，不猜测或改写 Pawn 当前实物。
        /// </summary>
        internal void ReconcileAfterLoad(CombatBodyPhase phase)
        {
            if (Pawn == null || hostState == null)
            {
                return;
            }

            bool requiresActiveHost = phase == CombatBodyPhase.Active
                || phase == CombatBodyPhase.Collapsing;
            if (!requiresActiveHost || HasValidRollbackSnapshot())
            {
                return;
            }

            Log.Warning("BDP_Message_CombatBody_LegacyHostRecoveryWarning".Translate(
                Pawn.LabelShortCap.Named("0"),
                hostState.TransformationApplied.Named("1"),
                hostState.HasSnapshot.Named("2"),
                (hostState.SnapshotState?.IsCaptured == true).Named("3")));

            // 阶段真值仍交给既有会话链；这里只确保之后的解除不会误走完整宿主回滚。
            hostState.TransformationApplied = false;
        }

        /// <summary>
        /// 应用进入战斗体时的宿主变换。
        /// 先抓原身快照，再挂前台衣物层。
        /// </summary>
        public void ApplyCombatBodyTransformation()
        {
            if (Pawn == null || hostState == null)
            {
                return;
            }

            TryRestoreRecoveredItemsToInventory();

            // 表现层必须在原衣物离身前捕获退场外观；失败由注册表隔离，不影响宿主事务。
            CombatBodyTransformPresentationRegistry.NotifyBegin(
                Pawn,
                CombatBodyTransformDirection.Enter);

            snapshotService?.Capture(Pawn, hostState);
            ApplyFrontReplacement(hostState.FrontState);
            RemoveCombatBodyEntryHediffs();
            AddCombatBodyActiveHediff();
            hostState.TransformationApplied = true;
            CombatBodyTransformPresentationRegistry.NotifyEnd(
                Pawn,
                CombatBodyTransformDirection.Enter);
        }

        /// <summary>
        /// 执行离开战斗体后的宿主恢复。
        /// 先拆前台衣物层，再恢复原身快照。
        /// </summary>
        public void RestoreFromCombatBody()
        {
            if (Pawn == null || hostState == null)
            {
                return;
            }

            // 表现层必须在战斗体衣物离身前捕获退场外观；玩法恢复仍按原顺序立即执行。
            CombatBodyTransformPresentationRegistry.NotifyBegin(
                Pawn,
                CombatBodyTransformDirection.Exit);

            if (!HasValidRollbackSnapshot())
            {
                RestoreInvalidLegacyCombatBody();
                CombatBodyTransformPresentationRegistry.NotifyEnd(
                    Pawn,
                    CombatBodyTransformDirection.Exit);
                return;
            }

            ExtinguishFire();
            RemoveCombatBodyEntryHediffs();
            RemoveCombatBodyActiveHediff();
            List<CombatBodySnapshotHediffRecord> restoredHediffBaseline = CopyHediffBaselineForFinalCleanup();
            RestoreFrontReplacement(hostState.FrontState);
            snapshotService?.Restore(Pawn, hostState);

            FinalCleanupResidualHediffs(restoredHediffBaseline);
            hostState.TransformationApplied = false;
            CombatBodyTransformPresentationRegistry.NotifyEnd(
                Pawn,
                CombatBodyTransformDirection.Exit);
        }

        /// <summary>
        /// 判断当前宿主是否持有可证明完整的原身回滚快照。
        /// 三个独立存档事实必须同时成立；容器为空仍可能是合法的裸体或空背包快照。
        /// </summary>
        private bool HasValidRollbackSnapshot()
        {
            return hostState != null
                && hostState.TransformationApplied
                && hostState.HasSnapshot
                && hostState.SnapshotState?.IsCaptured == true;
        }

        /// <summary>
        /// 安全解除缺少有效快照的旧档战斗体。
        /// 没有原身基线时只移除 BDP 激活标记，不触碰当前服装、背包、需求或普通伤势。
        /// </summary>
        private void RestoreInvalidLegacyCombatBody()
        {
            RemoveCombatBodyActiveHediff();
            PreserveInvalidTransactionItems();
            ClearInvalidHostTransactionRecords();
        }

        /// <summary>
        /// 把残缺事务容器中的未知实物保全到 Pawn 当前背包。
        /// 不强行穿回或覆盖当前状态；无背包或转移失败时继续留在原持有者中。
        /// </summary>
        private void PreserveInvalidTransactionItems()
        {
            if (hostState.SnapshotState?.RecoveredItemContainer == null)
            {
                return;
            }

            List<Thing> orphanedItems = new List<Thing>();
            if (hostState.SnapshotState?.OriginalApparelContainer != null)
            {
                orphanedItems.AddRange(hostState.SnapshotState.OriginalApparelContainer.InnerListForReading);
            }

            if (hostState.SnapshotState?.OriginalInventoryContainer != null)
            {
                orphanedItems.AddRange(hostState.SnapshotState.OriginalInventoryContainer.InnerListForReading);
            }

            if (hostState.FrontState?.CombatApparelContainer != null)
            {
                orphanedItems.AddRange(hostState.FrontState.CombatApparelContainer.InnerListForReading);
            }

            for (int i = 0; i < orphanedItems.Count; i++)
            {
                Thing thing = orphanedItems[i];
                thing.holdingOwner?.TryTransferToContainer(thing, hostState.SnapshotState.RecoveredItemContainer);
            }

            TryRestoreRecoveredItemsToInventory();
        }

        /// <summary>
        /// 尝试把旧档回收容器中的实物归还当前背包。
        /// 无背包或转移失败时保持原容器持有，后续激活前会再次尝试。
        /// </summary>
        private void TryRestoreRecoveredItemsToInventory()
        {
            ThingOwner<Thing> recoveredItems = hostState?.SnapshotState?.RecoveredItemContainer;
            if (Pawn?.inventory?.innerContainer == null || recoveredItems == null)
            {
                return;
            }

            List<Thing> things = recoveredItems.InnerListForReading.ToList();
            for (int i = 0; i < things.Count; i++)
            {
                recoveredItems.TryTransferToContainer(things[i], Pawn.inventory.innerContainer);
            }
        }

        /// <summary>
        /// 收敛残缺宿主事务的存档标记。
        /// 容器内无法转移的实物不在这里销毁，避免把无法确认归属的旧档实物当作垃圾处理。
        /// </summary>
        private void ClearInvalidHostTransactionRecords()
        {
            hostState.TransformationApplied = false;
            hostState.HasSnapshot = false;

            if (hostState.SnapshotState != null)
            {
                hostState.SnapshotState.IsCaptured = false;
            }

            hostState.FrontState?.ClearActiveRecord();
        }

        /// <summary>
        /// 挂载战斗体前台衣物层。
        /// </summary>
        private void ApplyFrontReplacement(CombatBodyFrontState frontState)
        {
            if (Pawn?.apparel == null || frontState == null)
            {
                return;
            }

            frontState.ClearActiveRecord();
            CombatBodyHostConfigDef config = CombatBodyHostConfigResolver.Resolve();

            switch (config.frontMode)
            {
                case CombatBodyFrontMode.MirrorOriginal:
                    ApplyMirrorOriginalFrontReplacement(frontState);
                    frontState.IsMirrorOriginal = true;
                    break;
                case CombatBodyFrontMode.Preset:
                default:
                    ApplyPresetFrontReplacement(frontState, config);
                    frontState.IsMirrorOriginal = false;
                    break;
            }

            List<Apparel> combatApparels = frontState.CombatApparelContainer.InnerListForReading.ToList();
            for (int i = 0; i < combatApparels.Count; i++)
            {
                Apparel apparel = combatApparels[i];
                frontState.CombatApparelContainer.Remove(apparel);
                Pawn.apparel.Wear(apparel, dropReplacedApparel: false, locked: true);
                frontState.ActiveApparelThingIds.Add(apparel.thingIDNumber);
            }

            frontState.IsApplied = frontState.ActiveApparelThingIds.Count > 0;
        }

        /// <summary>
        /// 拆除战斗体前台衣物层。
        /// 镜像模式销毁副本，预设模式存回容器。
        /// </summary>
        private void RestoreFrontReplacement(CombatBodyFrontState frontState)
        {
            if (Pawn?.apparel == null || frontState == null)
            {
                return;
            }

            HashSet<int> activeApparelThingIds = new HashSet<int>(frontState.ActiveApparelThingIds);
            List<Apparel> currentApparel = Pawn.apparel.WornApparel
                .Where(apparel => activeApparelThingIds.Contains(apparel.thingIDNumber))
                .ToList();
            for (int i = 0; i < currentApparel.Count; i++)
            {
                Apparel apparel = currentApparel[i];
                Pawn.apparel.Remove(apparel);

                if (frontState.IsMirrorOriginal)
                {
                    apparel.Destroy();
                }
                else
                {
                    frontState.CombatApparelContainer.TryAdd(apparel);
                }
            }

            frontState.ClearActiveRecord();
        }

        /// <summary>
        /// 使用正式预设 `Def` 生成或复用前台衣物层。
        /// </summary>
        private void ApplyPresetFrontReplacement(
            CombatBodyFrontState frontState,
            CombatBodyHostConfigDef config)
        {
            if (frontState.CombatApparelContainer.Count > 0)
            {
                return;
            }

            CombatBodyFrontPresetDef presetDef = ResolveFrontPresetDef(config);
            if (presetDef?.apparelDefNames == null)
            {
                return;
            }

            for (int i = 0; i < presetDef.apparelDefNames.Count; i++)
            {
                string apparelDefName = presetDef.apparelDefNames[i];
                ThingDef apparelDef = DefDatabase<ThingDef>.GetNamedSilentFail(apparelDefName);
                if (apparelDef == null)
                {
                    continue;
                }

                Apparel apparel = ThingMaker.MakeThing(apparelDef) as Apparel;
                if (apparel != null)
                {
                    frontState.CombatApparelContainer.TryAdd(apparel);
                }
            }
        }

        /// <summary>
        /// 按原衣物镜像生成前台衣物层。
        /// 复制 `def` / `Stuff` / 颜色 / `StyleDef`，并去掉品质。
        /// </summary>
        private void ApplyMirrorOriginalFrontReplacement(CombatBodyFrontState frontState)
        {
            frontState.CombatApparelContainer.ClearAndDestroyContents();

            if (hostState?.SnapshotState?.OriginalApparelContainer == null)
            {
                return;
            }

            List<Apparel> originalApparels = hostState.SnapshotState.OriginalApparelContainer.InnerListForReading.ToList();
            for (int i = 0; i < originalApparels.Count; i++)
            {
                Apparel mirroredApparel = CreateMirroredApparel(originalApparels[i]);
                if (mirroredApparel != null)
                {
                    frontState.CombatApparelContainer.TryAdd(mirroredApparel);
                }
            }
        }

        /// <summary>
        /// 解析当前宿主配置声明的前台预设。
        /// </summary>
        private CombatBodyFrontPresetDef ResolveFrontPresetDef(CombatBodyHostConfigDef config)
        {
            if (config == null || string.IsNullOrEmpty(config.frontPresetDefName))
            {
                return null;
            }

            return DefDatabase<CombatBodyFrontPresetDef>.GetNamedSilentFail(config.frontPresetDefName);
        }

        /// <summary>
        /// 复制单件原衣物，生成战斗体镜像副本。
        /// </summary>
        private static Apparel CreateMirroredApparel(Apparel original)
        {
            if (original == null)
            {
                return null;
            }

            Apparel copy = ThingMaker.MakeThing(original.def, original.Stuff) as Apparel;
            if (copy == null)
            {
                return null;
            }

            CopyApparelColor(original, copy);
            CopyApparelStyle(original, copy);
            RemoveQualityComp(copy);
            return copy;
        }

        /// <summary>
        /// 复制衣物颜色。
        /// </summary>
        private static void CopyApparelColor(Apparel original, Apparel copy)
        {
            CompColorable originalColorable = original.TryGetComp<CompColorable>();
            CompColorable copiedColorable = copy.TryGetComp<CompColorable>();
            if (originalColorable != null && originalColorable.Active && copiedColorable != null)
            {
                copiedColorable.SetColor(originalColorable.Color);
            }
        }

        /// <summary>
        /// 复制衣物风格。
        /// </summary>
        private static void CopyApparelStyle(Apparel original, Apparel copy)
        {
            if (original.StyleDef != null)
            {
                copy.StyleDef = original.StyleDef;
            }
        }

        /// <summary>
        /// 去掉镜像副本上的品质部件，保持旧版“前台副本无品质”表象。
        /// </summary>
        private static void RemoveQualityComp(Apparel copy)
        {
            CompQuality qualityComp = copy.TryGetComp<CompQuality>();
            if (qualityComp == null)
            {
                return;
            }

            FieldInfo compsField = typeof(ThingWithComps).GetField("comps", BindingFlags.NonPublic | BindingFlags.Instance);
            List<ThingComp> comps = compsField?.GetValue(copy) as List<ThingComp>;
            comps?.Remove(qualityComp);

            copy.compQuality = null;

            FieldInfo compsByTypeField = typeof(ThingWithComps).GetField("compsByType", BindingFlags.NonPublic | BindingFlags.Instance);
            Dictionary<Type, ThingComp[]> compsByType = compsByTypeField?.GetValue(copy) as Dictionary<Type, ThingComp[]>;
            compsByType?.Remove(typeof(CompQuality));
        }

        /// <summary>
        /// 清掉当前宿主身上所有不在排除表中的 `Hediff`。
        /// 进入战斗体时用于清空原身基线；退出战斗体时用于清理战斗期残留。
        /// </summary>
        private void RemoveCombatBodyEntryHediffs()
        {
            if (Pawn?.health?.hediffSet == null || snapshotPolicy == null)
            {
                return;
            }

            List<Hediff> toRemove = Pawn.health.hediffSet.hediffs
                .Where(hediff => !snapshotPolicy.IsExcluded(hediff))
                .ToList();

            for (int i = 0; i < toRemove.Count; i++)
            {
                Pawn.health.RemoveHediff(toRemove[i]);
            }
        }

        /// <summary>
        /// 移除旧版退出链中单独摘掉的 `BDP_CombatBodyActive`。
        /// 当前若正式 Def 尚未接回，这里安全空操作。
        /// </summary>
        private void RemoveCombatBodyActiveHediff()
        {
            if (Pawn?.health?.hediffSet == null)
            {
                return;
            }

            HediffDef activeDef = DefDatabase<HediffDef>.GetNamedSilentFail("BDP_CombatBodyActive");
            if (activeDef == null)
            {
                return;
            }

            Hediff activeHediff = Pawn.health.hediffSet.GetFirstHediffOfDef(activeDef, false);
            if (activeHediff != null)
            {
                Pawn.health.RemoveHediff(activeHediff);
            }
        }

        /// <summary>
        /// 显式挂上战斗体激活态 `Hediff`。
        /// 它承担旧版运行态表象承载，但不反向定义新架构里的激活真值。
        /// </summary>
        private void AddCombatBodyActiveHediff()
        {
            if (Pawn?.health?.hediffSet == null)
            {
                return;
            }

            HediffDef activeDef = DefDatabase<HediffDef>.GetNamedSilentFail("BDP_CombatBodyActive");
            if (activeDef == null)
            {
                return;
            }

            Hediff activeHediff = Pawn.health.hediffSet.GetFirstHediffOfDef(activeDef, false);
            if (activeHediff == null)
            {
                Pawn.health.AddHediff(activeDef);
            }
        }

        /// <summary>
        /// 移除旧版退出链里优先处理的火焰类 `Hediff`。
        /// </summary>
        private void ExtinguishFire()
        {
            if (Pawn?.health?.hediffSet == null)
            {
                return;
            }

            List<Hediff> fireHediffs = Pawn.health.hediffSet.hediffs
                .Where(hediff => hediff?.def != null && (hediff.def.defName.Contains("Fire") || hediff.def.defName.Contains("Flame")))
                .ToList();

            for (int i = 0; i < fireHediffs.Count; i++)
            {
                Pawn.health.RemoveHediff(fireHediffs[i]);
            }
        }

        /// <summary>
        /// 复制本轮退出需要用到的 `Hediff` 基线。
        /// </summary>
        private List<CombatBodySnapshotHediffRecord> CopyHediffBaselineForFinalCleanup()
        {
            if (hostState?.SnapshotState?.HediffSnapshots == null)
            {
                return new List<CombatBodySnapshotHediffRecord>();
            }

            // Restore() 会清空真实快照；最终清理只读这一轮退出前复制的基线，避免影响下一轮抓取。
            return new List<CombatBodySnapshotHediffRecord>(hostState.SnapshotState.HediffSnapshots);
        }

        /// <summary>
        /// 在快照恢复完成后再做一次残留 `Hediff` 清理。
        /// 保持和旧版一致的防御性收尾。
        /// </summary>
        private void FinalCleanupResidualHediffs(IReadOnlyList<CombatBodySnapshotHediffRecord> restoredHediffBaseline)
        {
            if (Pawn?.health?.hediffSet == null || snapshotPolicy == null || hostState?.SnapshotState == null)
            {
                return;
            }

            List<Hediff> residualHediffs = Pawn.health.hediffSet.hediffs
                .Where(hediff => !snapshotPolicy.IsExcluded(hediff) && !IsHediffInSnapshotBaseline(hediff, restoredHediffBaseline))
                .ToList();

            for (int i = 0; i < residualHediffs.Count; i++)
            {
                Pawn.health.RemoveHediff(residualHediffs[i]);
            }
        }

        /// <summary>
        /// 判断当前 `Hediff` 是否属于进入战斗体前记录下来的基线。
        /// 这里按旧版表象只匹配 `defName + bodyPartDefName`。
        /// </summary>
        private bool IsHediffInSnapshotBaseline(Hediff hediff, IReadOnlyList<CombatBodySnapshotHediffRecord> baselineRecords)
        {
            if (hediff == null || baselineRecords == null)
            {
                return false;
            }

            string bodyPartDefName = hediff.Part?.def?.defName ?? string.Empty;
            for (int i = 0; i < baselineRecords.Count; i++)
            {
                CombatBodySnapshotHediffRecord record = baselineRecords[i];
                if (record.defName == hediff.def.defName && record.bodyPartDefName == bodyPartDefName)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
