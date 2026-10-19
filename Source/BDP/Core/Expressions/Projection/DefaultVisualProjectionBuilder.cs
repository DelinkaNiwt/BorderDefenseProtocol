using System.Collections.Generic;
using System;
using BDP.Core.AttackExecution;
using BDP.Core.Trigger;
using Verse;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 第一版默认视觉投影器。
    /// 当前先只返回最小视觉结果对象，不在未接视觉主链前伪造视觉关系。
    /// </summary>
    internal sealed class DefaultVisualProjectionBuilder
    {
        /// <summary>
        /// 从正式总表生成视觉读取结果。
        /// </summary>
        public VisualExpressionProjection Build(ExpressionSnapshot snapshot)
        {
            List<ComboAttackVisualBinding> comboAttackVisualBindings =
                CollectComboAttackVisualBindings(snapshot);
            List<VisualResidentEntry> residentEntries = CollectResidentEntries(
                snapshot,
                comboAttackVisualBindings);
            int activeWeaponChipInstanceCount = CountActiveWeaponChipInstances(snapshot);
            VisualExpressionRelationKind relationKind = ResolveRelationKind(
                snapshot,
                residentEntries,
                activeWeaponChipInstanceCount);
            return new VisualExpressionProjection
            {
                RelationKind = relationKind,
                ResidentEntries = residentEntries,
                ComboAttackVisualBindings = comboAttackVisualBindings,
                ActiveWeaponChipInstanceCount = activeWeaponChipInstanceCount,
                HostEquipmentRenderMode = ResolveHostEquipmentRenderMode(
                    residentEntries,
                    activeWeaponChipInstanceCount,
                    relationKind,
                    comboAttackVisualBindings),
                ExecutionFocusPolicy = ResolveExecutionFocusPolicy(residentEntries, activeWeaponChipInstanceCount),
                MuzzleFollowPolicy = ResolveMuzzleFollowPolicy(residentEntries, activeWeaponChipInstanceCount)
            };
        }

        /// <summary>
        /// 收集当前可进入常驻视觉读取的结果条目。
        /// Resident 只保留单侧结果；Dual/Combo 的复合表象通过每条单侧结果携带的 CompositeVisualPresetDefName 解释。
        /// 组合技专用阶段预设即使没有单侧待命预设，也要为其归属来源补一个阶段绘制候选；
        /// 该候选在 Idle（待命）阶段不会自行产生绘制。
        /// </summary>
        private static List<VisualResidentEntry> CollectResidentEntries(
            ExpressionSnapshot snapshot,
            IReadOnlyList<ComboAttackVisualBinding> comboAttackVisualBindings)
        {
            List<VisualResidentEntry> result = new List<VisualResidentEntry>();
            if (snapshot?.Results == null)
            {
                return result;
            }

            for (int i = 0; i < snapshot.Results.Count; i++)
            {
                FormalExpressionResult entry = snapshot.Results[i];
                if (entry == null
                    || entry.CompositeKind != CompositeExpressionKind.None
                    || !entry.IsAvailable
                    || !entry.CanProject
                    || string.IsNullOrWhiteSpace(entry.Id))
                {
                    continue;
                }

                string moduleVisualPresetDefName = ResolveModuleWeaponVisualPresetDefName(entry);
                if (string.IsNullOrWhiteSpace(entry.VisualPresetDefName)
                    && string.IsNullOrWhiteSpace(entry.VisualGraphicOverrideDefName)
                    && string.IsNullOrWhiteSpace(entry.CompositeVisualPresetDefName)
                    && !entry.ForceSuppressHostEquipment
                    && string.IsNullOrWhiteSpace(moduleVisualPresetDefName))
                {
                    continue;
                }

                result.Add(new VisualResidentEntry
                {
                    ResultId = entry.Id,
                    SourceReference = entry.SourceReference,
                    Side = entry.SourceReference != null ? entry.SourceReference.Side : TriggerSide.Main,
                    SlotIndex = entry.SourceReference != null ? entry.SourceReference.SlotIndex : -1,
                    VisualPresetDefName = string.IsNullOrWhiteSpace(moduleVisualPresetDefName)
                        ? entry.VisualPresetDefName
                        : moduleVisualPresetDefName,
                    VisualGraphicOverrideDefName = entry.VisualGraphicOverrideDefName,
                    CompositeVisualPresetDefName = entry.CompositeVisualPresetDefName,
                    VerbAttackRole = entry.VerbAttackRole,
                    ForceSuppressHostEquipment = entry.ForceSuppressHostEquipment,
                    VisualPriority = entry.VisualPriority
                });
            }

            AddComboAttackVisualOwnerEntries(
                snapshot,
                result,
                comboAttackVisualBindings);
            result.Sort(CompareResidentEntries);
            return result;
        }

        /// <summary>
        /// 为只有组合技阶段视觉预设的来源结果补充一个最小常驻条目。
        /// 它只负责让阶段绘制链找到来源；待命阶段因没有普通视觉预设仍会自然跳过。
        /// </summary>
        private static void AddComboAttackVisualOwnerEntries(
            ExpressionSnapshot snapshot,
            List<VisualResidentEntry> residentEntries,
            IReadOnlyList<ComboAttackVisualBinding> comboAttackVisualBindings)
        {
            if (snapshot?.Results == null
                || residentEntries == null
                || comboAttackVisualBindings == null
                || comboAttackVisualBindings.Count == 0)
            {
                return;
            }

            HashSet<string> residentResultIds = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < residentEntries.Count; index++)
            {
                VisualResidentEntry residentEntry = residentEntries[index];
                if (!string.IsNullOrWhiteSpace(residentEntry?.ResultId))
                {
                    residentResultIds.Add(residentEntry.ResultId);
                }
            }

            for (int bindingIndex = 0; bindingIndex < comboAttackVisualBindings.Count; bindingIndex++)
            {
                ComboAttackVisualBinding binding = comboAttackVisualBindings[bindingIndex];
                if (binding == null
                    || string.IsNullOrWhiteSpace(binding.OwnerSourceResultId)
                    || residentResultIds.Contains(binding.OwnerSourceResultId))
                {
                    continue;
                }

                FormalExpressionResult ownerResult = FindResultById(
                    snapshot.Results,
                    binding.OwnerSourceResultId);
                if (ownerResult == null
                    || ownerResult.CompositeKind != CompositeExpressionKind.None
                    || !ownerResult.IsAvailable
                    || !ownerResult.CanProject
                    || ownerResult.SourceReference == null)
                {
                    continue;
                }

                residentEntries.Add(new VisualResidentEntry
                {
                    ResultId = ownerResult.Id,
                    SourceReference = ownerResult.SourceReference,
                    Side = ownerResult.SourceReference.Side,
                    SlotIndex = ownerResult.SourceReference.SlotIndex,
                    VisualPresetDefName = ownerResult.VisualPresetDefName,
                    VisualGraphicOverrideDefName = ownerResult.VisualGraphicOverrideDefName,
                    CompositeVisualPresetDefName = ownerResult.CompositeVisualPresetDefName,
                    VerbAttackRole = ownerResult.VerbAttackRole,
                    ForceSuppressHostEquipment = ownerResult.ForceSuppressHostEquipment,
                    VisualPriority = ownerResult.VisualPriority
                });
                residentResultIds.Add(ownerResult.Id);
            }
        }

        /// <summary>
        /// 按结果标识寻找正式结果。
        /// </summary>
        private static FormalExpressionResult FindResultById(
            IReadOnlyList<FormalExpressionResult> results,
            string resultId)
        {
            if (results == null || string.IsNullOrWhiteSpace(resultId))
            {
                return null;
            }

            for (int index = 0; index < results.Count; index++)
            {
                FormalExpressionResult result = results[index];
                if (result != null
                    && string.Equals(result.Id, resultId, StringComparison.Ordinal))
                {
                    return result;
                }
            }

            return null;
        }

        /// <summary>
        /// 从当前已激活的远程模块中读取最后一条显式武器贴图覆盖。
        /// 模块只提供配置，实际绘制仍由既有 DrawEquipmentAiming（瞄准装备绘制）入口完成。
        /// </summary>
        private static string ResolveModuleWeaponVisualPresetDefName(FormalExpressionResult entry)
        {
            if (entry?.RangedModules == null)
            {
                return null;
            }

            string result = null;
            for (int i = 0; i < entry.RangedModules.Count; i++)
            {
                RangedModuleMountConfig mount = entry.RangedModules[i];
                if (mount == null
                    || !mount.enabled
                    || !(mount.config is IRangedModuleWeaponVisualOverride visualOverride)
                    || string.IsNullOrWhiteSpace(visualOverride.WeaponVisualPresetDefName))
                {
                    continue;
                }

                result = visualOverride.WeaponVisualPresetDefName;
            }

            return result;
        }

        /// <summary>
        /// 解析当前视觉关系类型。
        /// 显式 Combo（组合）优先；否则按正式 DualWeapon（双武器）结果或实际武器芯片实例数量回退。
        /// </summary>
        private static VisualExpressionRelationKind ResolveRelationKind(
            ExpressionSnapshot snapshot,
            List<VisualResidentEntry> residentEntries,
            int activeWeaponChipInstanceCount)
        {
            bool hasAvailableDualWeaponResult = false;
            if (snapshot?.Results != null)
            {
                for (int i = 0; i < snapshot.Results.Count; i++)
                {
                    FormalExpressionResult result = snapshot.Results[i];
                    if (result == null
                        || !result.IsAvailable
                        || (result.UseRequirementCheck != null
                            && !result.UseRequirementCheck.Satisfied))
                    {
                        continue;
                    }

                    if (result.CompositeKind == CompositeExpressionKind.Combo
                        && HasDeclaredComboVisualRelation(result))
                    {
                        return VisualExpressionRelationKind.Combo;
                    }

                    if (result.CompositeKind == CompositeExpressionKind.DualWeapon)
                    {
                        hasAvailableDualWeaponResult = true;
                    }
                }
            }

            if (hasAvailableDualWeaponResult || activeWeaponChipInstanceCount >= 2)
            {
                return VisualExpressionRelationKind.DualWeapon;
            }

            return HasNormalVisualDeclaration(residentEntries)
                ? VisualExpressionRelationKind.SingleSide
                : VisualExpressionRelationKind.None;
        }

        /// <summary>
        /// 判断 Combo（组合）结果是否显式声明了手持视觉关系。
        /// 未声明时必须继续回退到实际武器实例数量，不能遮蔽双武器视觉。
        /// </summary>
        private static bool HasDeclaredComboVisualRelation(FormalExpressionResult result)
        {
            return result != null
                && (!string.IsNullOrWhiteSpace(result.VisualPresetDefName)
                    || !string.IsNullOrWhiteSpace(result.CompositeVisualPresetDefName));
        }

        /// <summary>
        /// 统计当前正式表达中的激活武器芯片实例数量。
        /// 多个 Verb 来自同一枚芯片时只算一个实例，避免把单芯片主副攻击误判成双武器。
        /// </summary>
        private static int CountActiveWeaponChipInstances(ExpressionSnapshot snapshot)
        {
            HashSet<string> keys = new HashSet<string>();
            if (snapshot?.Results == null)
            {
                return 0;
            }

            for (int i = 0; i < snapshot.Results.Count; i++)
            {
                FormalExpressionResult result = snapshot.Results[i];
                if (result == null
                    || result.OutputChannel != ExpressionOutputChannel.Verb
                    || result.CompositeKind != CompositeExpressionKind.None
                    || result.WeaponMode == WeaponExpressionMode.None
                    || !result.IsAvailable
                    || !result.CanProject)
                {
                    continue;
                }

                string key = ExpressionSourceReferenceMatcher.BuildChipInstanceKey(result.SourceReference);
                if (!string.IsNullOrWhiteSpace(key))
                {
                    keys.Add(key);
                }
            }

            return keys.Count;
        }

        /// <summary>
        /// 解析当前宿主装备贴图绘制策略。
        /// 无显式姿态的单武器只替换贴图；显式姿态单武器与多武器使用完整替换。
        /// 多武器作者显式强压时升级到 Suppress。
        /// </summary>
        private static HostEquipmentRenderMode ResolveHostEquipmentRenderMode(
            List<VisualResidentEntry> residentEntries,
            int activeWeaponChipInstanceCount,
            VisualExpressionRelationKind relationKind,
            IReadOnlyList<ComboAttackVisualBinding> comboAttackVisualBindings)
        {
            if (residentEntries == null || residentEntries.Count == 0)
            {
                return HostEquipmentRenderMode.Keep;
            }

            // 组合技阶段预设必须进入完整常驻绘制入口；否则单芯片或无待命预设时，
            // ReplaceTextureOnly（仅替换贴图）会绕过 DrawResidentEntries（常驻条目绘制）。
            if (comboAttackVisualBindings != null && comboAttackVisualBindings.Count > 0)
            {
                return HostEquipmentRenderMode.Replace;
            }

            if (activeWeaponChipInstanceCount == 1)
            {
                return HasGraphicOverride(residentEntries)
                    || HasExplicitPose(residentEntries, relationKind)
                    ? HostEquipmentRenderMode.Replace
                    : HostEquipmentRenderMode.ReplaceTextureOnly;
            }

            for (int i = 0; i < residentEntries.Count; i++)
            {
                if (residentEntries[i] != null && residentEntries[i].ForceSuppressHostEquipment)
                {
                    return HostEquipmentRenderMode.Suppress;
                }
            }

            return HostEquipmentRenderMode.Replace;
        }

        /// <summary>
        /// 判断常驻条目是否声明了待命或复合阶段的普通视觉内容。
        /// 组合技专用阶段候选不计入这里，避免改变没有待命视觉时的关系回退。
        /// </summary>
        private static bool HasNormalVisualDeclaration(
            List<VisualResidentEntry> residentEntries)
        {
            if (residentEntries == null)
            {
                return false;
            }

            for (int index = 0; index < residentEntries.Count; index++)
            {
                VisualResidentEntry entry = residentEntries[index];
                if (entry != null
                    && (!string.IsNullOrWhiteSpace(entry.VisualPresetDefName)
                        || !string.IsNullOrWhiteSpace(entry.VisualGraphicOverrideDefName)
                        || !string.IsNullOrWhiteSpace(entry.CompositeVisualPresetDefName)
                        || entry.ForceSuppressHostEquipment))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 判断当前单武器最终使用的任一视觉预设是否显式声明手持姿态。
        /// </summary>
        private static bool HasExplicitPose(
            List<VisualResidentEntry> residentEntries,
            VisualExpressionRelationKind relationKind)
        {
            for (int i = 0; i < residentEntries.Count; i++)
            {
                string presetDefName = ResolveVisualPresetDefName(residentEntries[i], relationKind);
                if (string.IsNullOrWhiteSpace(presetDefName))
                {
                    continue;
                }

                ExpressionVisualPresetDef preset =
                    DefDatabase<ExpressionVisualPresetDef>.GetNamed(presetDefName, false);
                if (preset != null && preset.HasExplicitPose)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 从正式 Combo 结果和复合来源引用构建攻击阶段视觉绑定。
        /// 该绑定不参与待命视觉关系判断，避免单纯声明阶段视觉就改变双武器待命外观。
        /// </summary>
        private static List<ComboAttackVisualBinding> CollectComboAttackVisualBindings(
            ExpressionSnapshot snapshot)
        {
            List<ComboAttackVisualBinding> result = new List<ComboAttackVisualBinding>();
            if (snapshot?.Results == null || snapshot.CompositeReferences == null)
            {
                return result;
            }

            for (int resultIndex = 0; resultIndex < snapshot.Results.Count; resultIndex++)
            {
                FormalExpressionResult comboResult = snapshot.Results[resultIndex];
                if (comboResult == null
                    || comboResult.CompositeKind != CompositeExpressionKind.Combo
                    || !comboResult.IsAvailable
                    || !comboResult.CanProject
                    || (comboResult.UseRequirementCheck != null
                        && !comboResult.UseRequirementCheck.Satisfied)
                    || string.IsNullOrWhiteSpace(comboResult.Id)
                    || string.IsNullOrWhiteSpace(comboResult.ComboAttackVisualPresetDefName))
                {
                    continue;
                }

                CompositeExpressionReference reference = FindCompositeReference(
                    snapshot.CompositeReferences,
                    comboResult.Id);
                string ownerSourceResultId = ResolveComboVisualOwnerResultId(reference);
                if (reference == null || string.IsNullOrWhiteSpace(ownerSourceResultId))
                {
                    continue;
                }

                result.Add(new ComboAttackVisualBinding
                {
                    ComboResultId = comboResult.Id,
                    VisualPresetDefName = comboResult.ComboAttackVisualPresetDefName,
                    OwnerSourceResultId = ownerSourceResultId
                });
            }

            return result;
        }

        /// <summary>
        /// 按组合结果标识寻找其来源引用。
        /// </summary>
        private static CompositeExpressionReference FindCompositeReference(
            IReadOnlyList<CompositeExpressionReference> references,
            string compositeId)
        {
            for (int index = 0; index < references.Count; index++)
            {
                CompositeExpressionReference reference = references[index];
                if (reference != null
                    && string.Equals(reference.CompositeId, compositeId, System.StringComparison.Ordinal)
                    && reference.CompositeKind == CompositeExpressionKind.Combo)
                {
                    return reference;
                }
            }

            return null;
        }

        /// <summary>
        /// 解析组合技视觉的稳定绘制归属；优先使用主侧来源，缺失时回退来源列表首项。
        /// </summary>
        private static string ResolveComboVisualOwnerResultId(
            CompositeExpressionReference reference)
        {
            if (reference == null)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(reference.MainSourceResultId))
            {
                return reference.MainSourceResultId;
            }

            return reference.SourceResultIds != null && reference.SourceResultIds.Count > 0
                ? reference.SourceResultIds[0]
                : null;
        }

        /// <summary>
        /// 判断当前单武器是否声明了视觉图层局部覆盖。
        /// 局部覆盖必须进入完整姿态解析，不能退回宿主装备的贴图替换模式。
        /// </summary>
        private static bool HasGraphicOverride(List<VisualResidentEntry> residentEntries)
        {
            for (int i = 0; i < residentEntries.Count; i++)
            {
                if (residentEntries[i] != null
                    && !string.IsNullOrWhiteSpace(residentEntries[i].VisualGraphicOverrideDefName))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 按当前视觉关系解析条目最终使用的视觉预设名称。
        /// </summary>
        private static string ResolveVisualPresetDefName(
            VisualResidentEntry entry,
            VisualExpressionRelationKind relationKind)
        {
            if (entry == null)
            {
                return null;
            }

            if (relationKind != VisualExpressionRelationKind.SingleSide
                && !string.IsNullOrWhiteSpace(entry.CompositeVisualPresetDefName))
            {
                return entry.CompositeVisualPresetDefName;
            }

            return entry.VisualPresetDefName;
        }

        /// <summary>
        /// 解析视觉执行焦点读取策略。
        /// 单武器贴图替换不读取执行焦点，避免进入双武器视觉处理。
        /// </summary>
        private static VisualExecutionFocusPolicy ResolveExecutionFocusPolicy(
            List<VisualResidentEntry> residentEntries,
            int activeWeaponChipInstanceCount)
        {
            if (residentEntries == null || residentEntries.Count == 0 || activeWeaponChipInstanceCount == 1)
            {
                return VisualExecutionFocusPolicy.None;
            }

            return VisualExecutionFocusPolicy.CastResult;
        }

        /// <summary>
        /// 解析枪口跟随读取策略。
        /// 单武器贴图替换不发布视觉枪口跟随，发射点继续服从原版/执行层基线。
        /// </summary>
        private static VisualMuzzleFollowPolicy ResolveMuzzleFollowPolicy(
            List<VisualResidentEntry> residentEntries,
            int activeWeaponChipInstanceCount)
        {
            if (residentEntries == null || residentEntries.Count == 0 || activeWeaponChipInstanceCount == 1)
            {
                return VisualMuzzleFollowPolicy.None;
            }

            return VisualMuzzleFollowPolicy.EmitSourceResult;
        }

        /// <summary>
        /// 对常驻视觉条目按优先级和稳定标识排序。
        /// </summary>
        private static int CompareResidentEntries(VisualResidentEntry left, VisualResidentEntry right)
        {
            int leftPriority = left != null ? left.VisualPriority : 0;
            int rightPriority = right != null ? right.VisualPriority : 0;
            if (leftPriority != rightPriority)
            {
                return leftPriority.CompareTo(rightPriority);
            }

            string leftId = left != null ? left.ResultId : null;
            string rightId = right != null ? right.ResultId : null;
            return string.CompareOrdinal(leftId, rightId);
        }
    }
}
