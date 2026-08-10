using System;
using System.Collections.Generic;
using BDP.Core.Chips;
using BDP.Core.CombatBody;
using BDP.Core.Expressions;
using BDP.Core.Trigger;
using Verse;

namespace BDP.Content.CombatBody.Escape
{
    /// <summary>
    /// 紧急脱离徽标三态解析器。
    /// 它只聚合装载、芯片定义和表达结果，不持有新的业务真值。
    /// </summary>
    internal sealed class CombatBodyEmergencyEscapeBadgeStateResolver
    {
        /// <summary>
        /// 紧急脱离正式就绪解析器。
        /// </summary>
        private readonly CombatBodyEmergencyEscapeResolver emergencyEscapeResolver;

        /// <summary>
        /// 构造紧急脱离徽标三态解析器。
        /// </summary>
        internal CombatBodyEmergencyEscapeBadgeStateResolver(
            CombatBodyEmergencyEscapeResolver emergencyEscapeResolver)
        {
            this.emergencyEscapeResolver = emergencyEscapeResolver
                ?? throw new ArgumentNullException(nameof(emergencyEscapeResolver));
        }

        /// <summary>
        /// 解析当前 Pawn 的紧急脱离徽标状态。
        /// </summary>
        internal CombatBodyEmergencyEscapeBadgeState Resolve(Pawn pawn, ICombatBodyReader combatBodyReader)
        {
            if (!HasMountedEmergencyEscapeChip(pawn))
            {
                return CombatBodyEmergencyEscapeBadgeState.NotInstalled;
            }

            if (combatBodyReader != null && combatBodyReader.Phase == CombatBodyPhase.Collapsing)
            {
                CombatBodyEmergencyEscapeResolution cachedResolution =
                    pawn?.GetComp<CompCombatBodyEmergencyEscapeState>()?.PreparedResolution;
                if (cachedResolution != null)
                {
                    return cachedResolution.IsAvailable
                        ? CombatBodyEmergencyEscapeBadgeState.Ready
                        : CombatBodyEmergencyEscapeBadgeState.InstalledNotReady;
                }
            }

            CombatBodyEmergencyEscapeResolution resolution = emergencyEscapeResolver.Resolve(pawn);
            return resolution != null && resolution.IsAvailable
                ? CombatBodyEmergencyEscapeBadgeState.Ready
                : CombatBodyEmergencyEscapeBadgeState.InstalledNotReady;
        }

        /// <summary>
        /// 判断 Trigger 正式装载面中是否存在紧急脱离能力芯片。
        /// </summary>
        private static bool HasMountedEmergencyEscapeChip(Pawn pawn)
        {
            ITriggerLoadoutReader loadoutReader = TriggerSurfaceAccess.ResolveLoadoutReader(pawn);
            IEnumerable<ITriggerSlotState> slots = loadoutReader?.GetAllSlots();
            if (slots == null)
            {
                return false;
            }

            foreach (ITriggerSlotState slot in slots)
            {
                if (slot?.LoadedChip == null || slot.IsBindingMirror)
                {
                    continue;
                }

                if (DeclaresEmergencyEscape(slot))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 判断指定槽位中的芯片定义是否声明紧急脱离能力。
        /// </summary>
        private static bool DeclaresEmergencyEscape(ITriggerSlotState slot)
        {
            // 优先通过 Core 中性实例读取面取得动态配置，回退到静态 Def。
            ChipDefinitionConfig instanceDefinition;
            ChipExpressionConfig config = ChipInstanceSurfaceAccess.TryGetDefinition(
                slot.LoadedChip,
                out instanceDefinition)
                ? instanceDefinition.Expression
                : slot.LoadedChip.def?.GetModExtension<ChipExpressionConfig>();
            if (config == null)
            {
                return false;
            }

            return ContainsEmergencyEscape(config.Entries);
        }

        /// <summary>
        /// 判断基础表达条目中是否声明紧急脱离能力。
        /// </summary>
        private static bool ContainsEmergencyEscape(List<ChipExpressionEntryConfig> entries)
        {
            if (entries == null)
            {
                return false;
            }

            for (int index = 0; index < entries.Count; index++)
            {
                if (IsEmergencyEscapeEntry(entries[index]))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 判断单条表达声明是否为紧急脱离被动。
        /// </summary>
        private static bool IsEmergencyEscapeEntry(ChipExpressionEntryConfig entry)
        {
            return entry != null
                && entry.Kind == ChipExpressionEntryKindConfig.Passive
                && string.Equals(
                    entry.PassiveKey,
                    CombatBodyEmergencyEscapeResolver.EmergencyEscapePassiveKey,
                    StringComparison.Ordinal);
        }
    }
}
