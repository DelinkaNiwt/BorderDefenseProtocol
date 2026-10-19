using System;
using System.Collections.Generic;
using BDP.Core.Genes;
using BDP.Core.Trion;
using BDP.Core.Trion.Capacity;
using BDP.Core.Trigger;
using BDP.Core.Trigger.Runtime;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Content.Trigger.Black
{
    /// <summary>
    /// 黑触发器的 Trion 容量业务实现。
    /// 每个黑触发体通过自身 Def 上的 CompProperties_Trion.baseMax 声明固定容量。
    /// </summary>
    public sealed class BlackTriggerTrionCapacityBinding :
        ITrionCapacityContributor,
        ITriggerTrionCapacityBinding
    {
        /// <summary>
        /// 黑触发器类别 Def 名称；类别身份仍由 Def 声明，Core 不依赖此业务值。
        /// </summary>
        private const string BlackTriggerCategoryDefName = "BDP_TriggerCategory_Black";

        /// <summary>
        /// 正在完成拆分的触发体；防止卸下通知期间 Stat 读取再次把容量加回去。
        /// </summary>
        private static readonly HashSet<CompTriggerBody> detachingTriggers =
            new HashSet<CompTriggerBody>();

        /// <summary>
        /// 返回当前 Pawn 主手黑触发器提供的额外容量。
        /// </summary>
        public float GetAdditionalCapacity(Pawn pawn)
        {
            if (pawn == null
                || !TrionGlandEligibility.HasActiveTrionGland(pawn)
                || pawn.equipment?.Primary == null)
            {
                return 0f;
            }

            CompTriggerBody trigger = pawn.equipment.Primary.TryGetComp<CompTriggerBody>();
            if (!Handles(trigger))
            {
                return 0f;
            }

            if (detachingTriggers.Contains(trigger))
            {
                if (pawn.equipment.Primary == trigger.ParentThing)
                {
                    return 0f;
                }

                detachingTriggers.Remove(trigger);
            }

            ITrionReader reader = TrionSurfaceAccess.ResolveReader(trigger.ParentThing);
            return reader != null && reader.Max > 0f ? reader.Max : 0f;
        }

        /// <summary>
        /// 返回当前 Pawn 的触发体容量绑定贡献，供触发体生命周期注册表读取。
        /// </summary>
        public float GetCapacityContribution(Pawn pawn)
        {
            return GetAdditionalCapacity(pawn);
        }

        /// <summary>
        /// 判断指定触发体是否声明为黑触发器类别。
        /// </summary>
        public bool Handles(CompTriggerBody trigger)
        {
            return string.Equals(
                trigger?.TriggerCategory?.defName,
                BlackTriggerCategoryDefName,
                StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 黑触发器装备后，把独立容器当前量并入 Pawn 合并池。
        /// </summary>
        public void NotifyEquipped(Pawn pawn, CompTriggerBody trigger)
        {
            if (pawn == null
                || trigger == null
                || !Handles(trigger)
                || !TrionGlandEligibility.HasActiveTrionGland(pawn)
                || pawn.equipment?.Primary != trigger.ParentThing)
            {
                return;
            }

            detachingTriggers.Remove(trigger);
            ITrionReader triggerReader = TrionSurfaceAccess.ResolveReader(trigger.ParentThing);
            ITrionCommands triggerCommands = TrionSurfaceAccess.ResolveCommands(trigger.ParentThing);
            ITrionCommands pawnCommands = TrionSurfaceAccess.ResolveCommands(pawn);
            CompTrion pawnComp = pawn.GetComp<CompTrion>();
            if (triggerReader == null
                || triggerCommands == null
                || pawnCommands == null
                || pawnComp == null
                || triggerReader.Max <= 0f
                || triggerReader.IsBoundToExternalPool)
            {
                return;
            }

            float triggerCurrent = SanitizeNonNegative(triggerReader.Cur);
            pawnComp.RefreshDerivedStats(TrionEligibilityChangeReason.Recalculate);
            TrionCurrentWriteResult addResult = pawnCommands.AdjustCurrent(triggerCurrent);
            if (addResult == null || !addResult.Succeeded)
            {
                return;
            }

            TrionCurrentWriteResult clearResult = triggerCommands.TrySetCurrent(0f);
            if (clearResult == null || !clearResult.Succeeded)
            {
                pawnCommands.AdjustCurrent(-triggerCurrent);
                return;
            }

            triggerCommands.SetBoundToExternalPool(true);
        }

        /// <summary>
        /// 黑触发器正常卸下时，按两侧上限拆回当前量并丢弃最终溢出。
        /// </summary>
        public void NotifyUnequipped(Pawn pawn, CompTriggerBody trigger)
        {
            if (pawn == null || trigger == null || !Handles(trigger))
            {
                return;
            }

            ITrionReader triggerReader = TrionSurfaceAccess.ResolveReader(trigger.ParentThing);
            ITrionCommands triggerCommands = TrionSurfaceAccess.ResolveCommands(trigger.ParentThing);
            ITrionReader pawnReader = TrionSurfaceAccess.ResolveReader(pawn);
            ITrionCommands pawnCommands = TrionSurfaceAccess.ResolveCommands(pawn);
            CompTrion pawnComp = pawn.GetComp<CompTrion>();
            if (triggerReader == null
                || triggerCommands == null
                || pawnReader == null
                || pawnCommands == null
                || pawnComp == null
                || !triggerReader.IsBoundToExternalPool)
            {
                return;
            }

            float triggerMax = SanitizeNonNegative(triggerReader.Max);
            float combinedCurrent = SanitizeNonNegative(pawnReader.Cur);
            float pawnMax = ResolvePawnOwnMax(pawn, trigger, triggerMax);
            BlackTriggerTrionCapacitySplitResult split =
                BlackTriggerTrionCapacityCalculator.Calculate(combinedCurrent, pawnMax, triggerMax);

            detachingTriggers.Add(trigger);
            TrionCurrentWriteResult triggerResult = triggerCommands.TrySetCurrent(split.TriggerCurrent);
            if (triggerResult == null || !triggerResult.Succeeded)
            {
                detachingTriggers.Remove(trigger);
                return;
            }

            triggerCommands.SetBoundToExternalPool(false);
            pawnComp.RefreshDerivedStats(TrionEligibilityChangeReason.Recalculate);
            pawnCommands.TrySetCurrent(split.PawnCurrent);
            if (pawn.equipment?.Primary != trigger.ParentThing)
            {
                detachingTriggers.Remove(trigger);
            }
        }

        /// <summary>
        /// 腺体资格失效时，在触发体被移除前先保存其可容纳余量。
        /// </summary>
        public void NotifyEligibilityLost(Pawn pawn, CompTriggerBody trigger)
        {
            if (pawn == null || trigger == null || !Handles(trigger))
            {
                return;
            }

            ITrionReader triggerReader = TrionSurfaceAccess.ResolveReader(trigger.ParentThing);
            ITrionCommands triggerCommands = TrionSurfaceAccess.ResolveCommands(trigger.ParentThing);
            ITrionReader pawnReader = TrionSurfaceAccess.ResolveReader(pawn);
            ITrionCommands pawnCommands = TrionSurfaceAccess.ResolveCommands(pawn);
            if (triggerReader == null
                || triggerCommands == null
                || pawnReader == null
                || pawnCommands == null
                || !triggerReader.IsBoundToExternalPool)
            {
                return;
            }

            float triggerMax = SanitizeNonNegative(triggerReader.Max);
            float combinedCurrent = SanitizeNonNegative(pawnReader.Cur);
            float pawnMax = ResolvePawnOwnMax(pawn, trigger, triggerMax);
            BlackTriggerTrionCapacitySplitResult split =
                BlackTriggerTrionCapacityCalculator.Calculate(combinedCurrent, pawnMax, triggerMax);

            detachingTriggers.Add(trigger);
            TrionCurrentWriteResult triggerResult = triggerCommands.TrySetCurrent(split.TriggerCurrent);
            if (triggerResult == null || !triggerResult.Succeeded)
            {
                detachingTriggers.Remove(trigger);
                return;
            }

            triggerCommands.SetBoundToExternalPool(false);
            pawnCommands.TrySetCurrent(split.PawnCurrent);
        }

        /// <summary>
        /// 触发体摧毁时无法再回写触发器容器，只保留 Pawn 自身上限内的当前量。
        /// </summary>
        public void NotifyDestroyed(Pawn pawn, CompTriggerBody trigger)
        {
            if (pawn == null || trigger == null || !Handles(trigger))
            {
                return;
            }

            ITrionReader triggerReader = TrionSurfaceAccess.ResolveReader(trigger.ParentThing);
            ITrionCommands triggerCommands = TrionSurfaceAccess.ResolveCommands(trigger.ParentThing);
            ITrionReader pawnReader = TrionSurfaceAccess.ResolveReader(pawn);
            ITrionCommands pawnCommands = TrionSurfaceAccess.ResolveCommands(pawn);
            CompTrion pawnComp = pawn.GetComp<CompTrion>();
            if (triggerReader == null
                || pawnReader == null
                || pawnCommands == null
                || pawnComp == null
                || !triggerReader.IsBoundToExternalPool)
            {
                return;
            }

            float triggerMax = SanitizeNonNegative(triggerReader.Max);
            float pawnMax = ResolvePawnOwnMax(pawn, trigger, triggerMax);
            float pawnCurrent = Mathf.Min(
                SanitizeNonNegative(pawnReader.Cur),
                pawnMax);
            triggerCommands?.SetBoundToExternalPool(false);
            pawnComp.RefreshDerivedStats(TrionEligibilityChangeReason.Recalculate);
            pawnCommands.TrySetCurrent(pawnCurrent);
            detachingTriggers.Remove(trigger);
        }

        /// <summary>
        /// 解析不包含当前黑触发器的 Pawn 自身容量。
        /// </summary>
        private static float ResolvePawnOwnMax(Pawn pawn, CompTriggerBody trigger, float triggerMax)
        {
            if (pawn == null)
            {
                return 0f;
            }

            float pawnMax = pawn.GetStatValue(TrionStatDefOf.BDP_TrionCapacity, true);
            if (pawn.equipment?.Primary == trigger?.ParentThing)
            {
                pawnMax -= triggerMax;
            }

            return SanitizeNonNegative(pawnMax);
        }

        /// <summary>
        /// 把异常浮点输入安全压回非负有限值。
        /// </summary>
        private static float SanitizeNonNegative(float value)
        {
            return float.IsNaN(value) || float.IsInfinity(value)
                ? 0f
                : Mathf.Max(0f, value);
        }
    }
}
