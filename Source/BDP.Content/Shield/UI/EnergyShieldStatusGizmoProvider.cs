using System;
using System.Collections.Generic;
using BDP.Core.Expressions;
using BDP.Core.Trigger;
using BDP.Core.UI.StatusCards;
using UnityEngine;
using Verse;

namespace BDP.Content.Shield.UI
{
    /// <summary>将已启用芯片的共享护盾真值映射到通用状态卡，不拥有耐久或冷却。</summary>
    public sealed class EnergyShieldStatusGizmoProvider : ITriggerExternalGizmoProvider
    {
        /// <summary>复刻参考盾卡的灰色填充；颜色属于护盾展示业务。</summary>
        private static readonly Color BarColor = new Color(0.75f, 0.75f, 0.75f, 0.8f);

        /// <summary>只为已征召的单选玩家人物显示盾卡；未征召仍照常防护，同组多来源去重。</summary>
        public IEnumerable<Gizmo> BuildGizmos(TriggerExternalGizmoContext context)
        {
            Pawn pawn = context?.OwnerPawn;
            if (pawn == null || !pawn.Drafted || !pawn.IsPlayerControlled || Find.Selector.SingleSelectedThing != pawn
                || context.LoadoutReader == null) yield break;

            Dictionary<string, HediffDef> enabledGroups = ResolveEnabledGroups(context.LoadoutReader);
            foreach (EnergyShieldSnapshot snapshot in EnergyShieldApi.GetSnapshots(pawn))
            {
                if (!enabledGroups.TryGetValue(snapshot.GroupKey, out HediffDef definition)
                    || (!snapshot.HasDefenseHost && !snapshot.IsBroken)) continue;

                string groupKey = snapshot.GroupKey;
                yield return new Gizmo_StatusCard(() => ReadCard(pawn, groupKey, definition));
            }
        }

        /// <summary>从正式启用槽位的当前形态和姿态读取盾组；不依赖破盾时已被移除的防护宿主。</summary>
        private static Dictionary<string, HediffDef> ResolveEnabledGroups(ITriggerLoadoutReader reader)
        {
            var groups = new Dictionary<string, HediffDef>();
            foreach (ITriggerSlotState slot in reader.GetActiveSlots())
            {
                if (slot == null || !slot.IsActive || slot.IsBindingMirror || slot.LoadedChip == null) continue;
                ChipExpressionResolvedContract resolved = ChipExpressionContractSurfaceAccess.Resolve(slot.LoadedChip, reader);
                if (resolved?.Validation?.IsValid != true || resolved.Contract?.Entries == null) continue;
                foreach (ChipExpressionEntryContract entry in resolved.Contract.Entries)
                {
                    if (entry == null || string.IsNullOrEmpty(entry.HediffDefName)) continue;
                    HediffDef definition = DefDatabase<HediffDef>.GetNamedSilentFail(entry.HediffDefName);
                    HediffCompProperties_EnergyShield properties = FindShieldProperties(definition);
                    if (properties != null && !string.IsNullOrEmpty(properties.durabilityGroupKey)
                        && !groups.ContainsKey(properties.durabilityGroupKey))
                        groups.Add(properties.durabilityGroupKey, definition);
                }
            }
            return groups;
        }

        /// <summary>读取定义中的护盾组件配置，不使用芯片名称或硬编码盾组列表识别业务。</summary>
        private static HediffCompProperties_EnergyShield FindShieldProperties(HediffDef definition)
        {
            if (definition?.comps == null) return null;
            foreach (HediffCompProperties properties in definition.comps)
                if (properties is HediffCompProperties_EnergyShield shield) return shield;
            return null;
        }

        /// <summary>每次绘制重新读取当前真值；绝对完成刻只用于展示，查询不推进恢复。</summary>
        private static StatusCardSnapshot ReadCard(Pawn pawn, string groupKey, HediffDef definition)
        {
            foreach (EnergyShieldSnapshot snapshot in EnergyShieldApi.GetSnapshots(pawn))
            {
                if (snapshot.GroupKey != groupKey) continue;
                HediffCompProperties_EnergyShield properties = FindShieldProperties(definition);
                string title = string.IsNullOrEmpty(properties?.statusCardLabelKey)
                    ? definition.LabelCap.ToString() : properties.statusCardLabelKey.Translate().ToString();
                if (snapshot.IsBroken)
                {
                    int remaining = Math.Max(0, snapshot.RecoveryUntilTick - EnergyShieldApi.CurrentTick);
                    float progress = 1f - remaining / (float)Math.Max(1, snapshot.RecoveryDurationTicks);
                    return new StatusCardSnapshot(title,
                        "BDP_Shield_Status_Seconds".Translate(remaining.TicksToSeconds().ToString("F1")), progress,
                        BarColor, "BDP_Shield_Status_RecoveringTip".Translate());
                }
                if (!snapshot.HasDefenseHost) return null;
                return new StatusCardSnapshot(title,
                    "BDP_Shield_Status_Durability".Translate(snapshot.CurrentDurability.ToString("F0"),
                        snapshot.MaximumDurability.ToString("F0")), snapshot.DurabilityFraction,
                    BarColor, "BDP_Shield_Status_DurabilityTip".Translate());
            }
            return null;
        }
    }
}
