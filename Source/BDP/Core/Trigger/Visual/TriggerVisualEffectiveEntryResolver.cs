using BDP.Core.Expressions;
using Verse;

namespace BDP.Core.Trigger.Visual
{
    /// <summary>
    /// 解析当前阶段真正生效的视觉条目与视觉预设。
    /// 绘制入口和发射根点入口必须共同消费这份结果，避免两条链路各自解释组合技视觉。
    /// </summary>
    internal static class TriggerVisualEffectiveEntryResolver
    {
        /// <summary>
        /// 按发射计划中的结果标识找到视觉解析应使用的来源常驻条目。
        /// 普通结果直接命中；组合结果则回到组合技绑定声明的绘制归属来源。
        /// </summary>
        internal static VisualResidentEntry FindLaunchSourceEntry(
            VisualExpressionProjection visualProjection,
            string sourceResultId)
        {
            if (visualProjection?.ComboAttackVisualBindings == null
                || string.IsNullOrWhiteSpace(sourceResultId))
            {
                return FindResidentEntry(visualProjection, sourceResultId);
            }

            for (int index = 0; index < visualProjection.ComboAttackVisualBindings.Count; index++)
            {
                ComboAttackVisualBinding binding = visualProjection.ComboAttackVisualBindings[index];
                if (binding == null
                    || !string.Equals(
                        binding.ComboResultId,
                        sourceResultId,
                        System.StringComparison.Ordinal))
                {
                    continue;
                }

                VisualResidentEntry ownerEntry = FindResidentEntry(
                    visualProjection,
                    binding.OwnerSourceResultId);
                if (ownerEntry != null)
                {
                    return ownerEntry;
                }
            }

            return FindResidentEntry(visualProjection, sourceResultId);
        }

        /// <summary>
        /// 把来源常驻条目解析为当前动作阶段真正要使用的条目、主预设和图层覆盖预设。
        /// 组合技阶段命中时构造一次性组合结果条目；待命或普通攻击继续返回来源条目。
        /// </summary>
        internal static bool TryResolve(
            VisualExpressionProjection visualProjection,
            VisualResidentEntry sourceEntry,
            WeaponVisualStageSnapshot weaponStageSnapshot,
            out VisualResidentEntry effectiveEntry,
            out ExpressionVisualPresetDef preset,
            out ExpressionVisualPresetDef graphicOverridePreset)
        {
            effectiveEntry = null;
            preset = null;
            graphicOverridePreset = null;
            if (sourceEntry == null)
            {
                return false;
            }

            ComboAttackVisualBinding comboBinding = ResolveComboAttackVisualBinding(
                visualProjection,
                weaponStageSnapshot);
            ExpressionVisualPresetDef comboPreset = ResolveComboAttackPreset(comboBinding);
            if (comboPreset != null)
            {
                if (!string.Equals(
                    sourceEntry.ResultId,
                    comboBinding.OwnerSourceResultId,
                    System.StringComparison.Ordinal))
                {
                    return false;
                }

                effectiveEntry = CreateComboAttackVisualEntry(sourceEntry, comboBinding);
                preset = comboPreset;
                return true;
            }

            effectiveEntry = sourceEntry;
            preset = ResolvePreset(visualProjection, sourceEntry);
            graphicOverridePreset = ResolveGraphicOverridePreset(sourceEntry);
            return preset != null;
        }

        /// <summary>
        /// 解析发射根点使用的有效视觉条目。
        /// 组合技阶段无论实际 emit 来自主侧还是副侧，都统一复用组合技视觉绑定的 Owner 最终位姿；
        /// 这不会改变绘制入口只绘制一次 Owner 条目的规则。
        /// </summary>
        internal static bool TryResolveLaunch(
            VisualExpressionProjection visualProjection,
            VisualResidentEntry sourceEntry,
            WeaponVisualStageSnapshot weaponStageSnapshot,
            out VisualResidentEntry effectiveEntry,
            out ExpressionVisualPresetDef preset,
            out ExpressionVisualPresetDef graphicOverridePreset)
        {
            sourceEntry = ResolveLaunchSourceEntry(
                visualProjection,
                sourceEntry,
                weaponStageSnapshot);

            return TryResolve(
                visualProjection,
                sourceEntry,
                weaponStageSnapshot,
                out effectiveEntry,
                out preset,
                out graphicOverridePreset);
        }

        /// <summary>
        /// 把任意实际发射来源映射到当前组合技视觉的 Owner 来源条目。
        /// 非组合技阶段或 Owner 缺失时保留原来源条目。
        /// </summary>
        internal static VisualResidentEntry ResolveLaunchSourceEntry(
            VisualExpressionProjection visualProjection,
            VisualResidentEntry sourceEntry,
            WeaponVisualStageSnapshot weaponStageSnapshot)
        {
            ComboAttackVisualBinding comboBinding = ResolveComboAttackVisualBinding(
                visualProjection,
                weaponStageSnapshot);
            if (comboBinding == null)
            {
                return sourceEntry;
            }

            VisualResidentEntry ownerEntry = FindResidentEntry(
                visualProjection,
                comboBinding.OwnerSourceResultId);
            return ownerEntry ?? sourceEntry;
        }

        /// <summary>
        /// 按当前组合技宿主和动作阶段寻找组合技攻击视觉绑定。
        /// 待命阶段不消费该绑定，保证常驻视觉关系保持原样。
        /// </summary>
        internal static ComboAttackVisualBinding ResolveComboAttackVisualBinding(
            VisualExpressionProjection visualProjection,
            WeaponVisualStageSnapshot weaponStageSnapshot)
        {
            if (visualProjection?.ComboAttackVisualBindings == null
                || weaponStageSnapshot == null
                || weaponStageSnapshot.Stage == WeaponVisualActionStage.Idle
                || string.IsNullOrWhiteSpace(weaponStageSnapshot.HostResultId))
            {
                return null;
            }

            for (int index = 0; index < visualProjection.ComboAttackVisualBindings.Count; index++)
            {
                ComboAttackVisualBinding binding = visualProjection.ComboAttackVisualBindings[index];
                if (binding != null
                    && string.Equals(
                        binding.ComboResultId,
                        weaponStageSnapshot.HostResultId,
                        System.StringComparison.Ordinal))
                {
                    return binding;
                }
            }

            return null;
        }

        /// <summary>
        /// 按正式结果标识在常驻视觉投影中寻找来源条目。
        /// </summary>
        private static VisualResidentEntry FindResidentEntry(
            VisualExpressionProjection visualProjection,
            string resultId)
        {
            if (visualProjection?.ResidentEntries == null
                || string.IsNullOrWhiteSpace(resultId))
            {
                return null;
            }

            for (int index = 0; index < visualProjection.ResidentEntries.Count; index++)
            {
                VisualResidentEntry entry = visualProjection.ResidentEntries[index];
                if (entry != null
                    && string.Equals(entry.ResultId, resultId, System.StringComparison.Ordinal))
                {
                    return entry;
                }
            }

            return null;
        }

        /// <summary>
        /// 解析组合技攻击阶段视觉预设；缺失 Def 时返回空值并回退旧路径。
        /// </summary>
        private static ExpressionVisualPresetDef ResolveComboAttackPreset(
            ComboAttackVisualBinding binding)
        {
            return binding == null || string.IsNullOrWhiteSpace(binding.VisualPresetDefName)
                ? null
                : DefDatabase<ExpressionVisualPresetDef>.GetNamed(
                    binding.VisualPresetDefName,
                    false);
        }

        /// <summary>
        /// 为组合技阶段视觉构造一次性绘制条目。
        /// 结果身份改用组合结果，来源引用继续保留来源芯片，用于取色和阶段参与匹配。
        /// </summary>
        private static VisualResidentEntry CreateComboAttackVisualEntry(
            VisualResidentEntry sourceEntry,
            ComboAttackVisualBinding binding)
        {
            return new VisualResidentEntry
            {
                ResultId = binding.ComboResultId,
                SourceReference = sourceEntry.SourceReference,
                Side = sourceEntry.Side,
                SlotIndex = sourceEntry.SlotIndex,
                VisualPresetDefName = binding.VisualPresetDefName,
                VerbAttackRole = sourceEntry.VerbAttackRole,
                ForceSuppressHostEquipment = sourceEntry.ForceSuppressHostEquipment,
                VisualPriority = sourceEntry.VisualPriority
            };
        }

        /// <summary>
        /// 按当前视觉关系解析普通条目应使用的视觉预设。
        /// </summary>
        private static ExpressionVisualPresetDef ResolvePreset(
            VisualExpressionProjection visualProjection,
            VisualResidentEntry entry)
        {
            if (entry == null)
            {
                return null;
            }

            string presetDefName = visualProjection != null
                && visualProjection.RelationKind != VisualExpressionRelationKind.SingleSide
                && !string.IsNullOrWhiteSpace(entry.CompositeVisualPresetDefName)
                    ? entry.CompositeVisualPresetDefName
                    : entry.VisualPresetDefName;
            return string.IsNullOrWhiteSpace(presetDefName)
                ? null
                : DefDatabase<ExpressionVisualPresetDef>.GetNamed(presetDefName, false);
        }

        /// <summary>
        /// 解析当前普通条目的视觉图层局部覆盖预设。
        /// </summary>
        private static ExpressionVisualPresetDef ResolveGraphicOverridePreset(
            VisualResidentEntry entry)
        {
            return entry == null || string.IsNullOrWhiteSpace(entry.VisualGraphicOverrideDefName)
                ? null
                : DefDatabase<ExpressionVisualPresetDef>.GetNamed(
                    entry.VisualGraphicOverrideDefName,
                    false);
        }
    }
}
