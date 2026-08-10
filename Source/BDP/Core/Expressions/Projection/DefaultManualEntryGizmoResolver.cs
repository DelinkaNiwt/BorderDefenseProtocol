using System.Collections.Generic;
using BDP.Core.AttackExecution;
using BDP.Core.Combos;
using BDP.Core.Requirements;
using BDP.Core.Trigger.Runtime;
using UnityEngine;
using Verse;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 第一版默认手动入口按钮解析器。
    /// 当前按“每组一个主入口按钮”翻译，不提前扩展副入口和复杂分组行为。
    /// </summary>
    internal sealed class DefaultManualEntryGizmoResolver
    {
        /// <summary>
        /// 为指定 Pawn 和手动入口投影解析按钮集合。
        /// 当前版本按新宿主层对应的 targetingSource 生成手动按钮。
        /// </summary>
        public IEnumerable<Gizmo> Resolve(Pawn pawn, ManualEntryProjection projection)
        {
            if (pawn == null || projection?.Groups == null)
            {
                yield break;
            }

            IExpressionReader reader = ExpressionSurfaceAccess.ResolveReader(pawn);
            TriggerCombatProjectionState combatProjection = reader?.GetCombatProjection(pawn);
            if (combatProjection?.ResultIndex == null)
            {
                yield break;
            }

            for (int i = 0; i < projection.Groups.Count; i++)
            {
                ManualEntryProjectionGroup group = projection.Groups[i];
                ManualEntryProjectionItem primaryItem = FindPrimaryItem(group);
                FormalExpressionResult result = FindResult(combatProjection, primaryItem?.ResultId ?? group?.ResultId);
                if (result == null)
                {
                    continue;
                }

                Gizmo command = BuildCommand(pawn, group, primaryItem, result);
                if (command != null)
                {
                    yield return command;
                }
            }
        }

        /// <summary>
        /// 从当前已发布战斗投影里查找指定结果。
        /// </summary>
        private static FormalExpressionResult FindResult(TriggerCombatProjectionState combatProjection, string resultId)
        {
            if (combatProjection?.ResultIndex == null || string.IsNullOrWhiteSpace(resultId))
            {
                return null;
            }

            FormalExpressionResult result;
            return combatProjection.ResultIndex.TryGetValue(resultId, out result) ? result : null;
        }

        /// <summary>
        /// 从入口组里读取当前主入口项。
        /// </summary>
        private static ManualEntryProjectionItem FindPrimaryItem(ManualEntryProjectionGroup group)
        {
            if (group?.Items == null || group.Items.Count == 0)
            {
                return null;
            }

            for (int i = 0; i < group.Items.Count; i++)
            {
                ManualEntryProjectionItem item = group.Items[i];
                if (item != null && item.IsPrimary)
                {
                    return item;
                }
            }

            return group.Items[0];
        }

        /// <summary>
        /// 为当前入口组构建最小命令按钮。
        /// 按钮本身不直接执行攻击，只负责启动正式目标选择并把结果送进 AttackExecution。
        /// </summary>
        private static Gizmo BuildCommand(
            Pawn pawn,
            ManualEntryProjectionGroup group,
            ManualEntryProjectionItem item,
            FormalExpressionResult result)
        {
            if (pawn == null || item == null || result == null)
            {
                return null;
            }

            ManualEntryRecord manualEntryRecord = BuildManualEntryRecord(pawn, group, item, result);
            if (manualEntryRecord == null || manualEntryRecord.Hidden)
            {
                return null;
            }

            AttackExecutionTargetingSource targetingSource = AttackExecutionSurfaceAccess.CreateTargetingSource(
                pawn,
                item.ResultId,
                AttackExecutionReason.Manual,
                AttackDispatchIntent.ForceTargetOrder,
                manualEntryRecord.ModuleSession);
            if (targetingSource == null)
            {
                return null;
            }

            string rawLabel = !string.IsNullOrWhiteSpace(item.DisplayLabel)
                ? item.DisplayLabel
                : !string.IsNullOrWhiteSpace(group?.DisplayLabel) ? group.DisplayLabel : GetDisplayLabel(result);
            string label = BuildCommandLabel(result, !string.IsNullOrWhiteSpace(manualEntryRecord.Label) ? manualEntryRecord.Label : rawLabel);
            string description = !string.IsNullOrWhiteSpace(manualEntryRecord.Description)
                ? manualEntryRecord.Description
                : GetDisplayLabel(result);
            Texture2D iconTexture = ResolveIconTexture(item, group, result, targetingSource, manualEntryRecord);
            string manualEntryGroupId = !string.IsNullOrWhiteSpace(group?.GroupId) ? group.GroupId : item.ResultId;
            Command_BdpManualEntryTarget command = new Command_BdpManualEntryTarget(
                label,
                description,
                iconTexture,
                targetingSource,
                manualEntryGroupId);
            PawnRequirementCheckResult requirementCheck =
                ComboUseRequirementService.Instance.Evaluate(pawn, result.ComboDefName);
            if (!string.IsNullOrWhiteSpace(result.ComboDefName) && !requirementCheck.Satisfied)
            {
                string disabledReason =
                    ComboUseRequirementService.Instance.BuildFailureText(requirementCheck);
                command.DisableForUseRequirements(disabledReason);
            }

            return command;
        }

        /// <summary>
        /// 构建当前手动入口阶段记录，并允许模块在最终发出按钮前参与调整。
        /// </summary>
        private static ManualEntryRecord BuildManualEntryRecord(
            Pawn pawn,
            ManualEntryProjectionGroup group,
            ManualEntryProjectionItem item,
            FormalExpressionResult result)
        {
            RangedAttackModuleRuntimeHost runtimeHost = AttackExecutionSurfaceAccess.ResolveRangedModuleRuntimeHost(pawn);
            RangedAttackModuleSession moduleSession = runtimeHost != null ? runtimeHost.CreateSession(pawn, result) : null;
            ManualEntryRecord record = new ManualEntryRecord
            {
                Pawn = pawn,
                Result = result,
                ModuleSession = moduleSession,
                GroupId = group != null ? group.GroupId : null,
                ResultId = item != null ? item.ResultId : null,
                Label = item != null ? item.DisplayLabel : null,
                Description = result != null ? GetDisplayLabel(result) : null,
                ManualEntryIconTexPath = item != null && !string.IsNullOrWhiteSpace(item.ManualEntryIconTexPath)
                    ? item.ManualEntryIconTexPath
                    : group != null ? group.ManualEntryIconTexPath : null
            };

            if (moduleSession?.GetManualEntryModules() != null)
            {
                IReadOnlyList<IManualEntryStageModule> modules = moduleSession.GetManualEntryModules();
                for (int i = 0; i < modules.Count; i++)
                {
                    record.CurrentRuntime = modules[i] as IRangedAttackModuleRuntime;
                    modules[i]?.Contribute(record);
                    record.CurrentRuntime = null;
                }
            }

            if (record.Stop.IsRequested)
            {
                record.Hidden = true;
            }

            if (moduleSession != null)
            {
                RangedStageAddonDispatcher.Execute(
                    moduleSession.GetAddonModules(),
                    new RangedStageAddonContext(
                        RangedStageKind.ManualEntry,
                        pawn,
                        pawn != null ? pawn.Map : null,
                        null,
                        record.ResultId,
                        -1,
                        null,
                        pawn,
                        pawn?.equipment?.Primary ?? (Thing)pawn,
                        LocalTargetInfo.Invalid,
                        LocalTargetInfo.Invalid,
                        default,
                        null,
                        default,
                        result != null ? result.SemanticContext : null,
                        moduleSession.AttackContext?.ToSnapshot()));
            }

            return record;
        }

        /// <summary>
        /// 构建当前按钮的紧凑显示名。
        /// 双持复合入口显示"双持·xxx"，有枪械类别时追加"(XX型)"。
        /// fallbackLabel 可能已含"双持·"前缀（来自 DualWeapon 的 DisplayLabel），
        /// 这里统一剥离后再重建，避免"双持·双持·XX"冗余。
        /// </summary>
        private static string BuildCommandLabel(FormalExpressionResult result, string fallbackLabel)
        {
            string variantSuffix = ResolveSourceVariantLabelSuffix(result);
            string rawAction = !string.IsNullOrWhiteSpace(fallbackLabel) ? fallbackLabel : "(未命名表达)";

            bool isDual = result != null && result.CompositeKind == CompositeExpressionKind.DualWeapon;
            if (isDual && rawAction.StartsWith("双持·"))
            {
                rawAction = rawAction.Substring("双持·".Length);
            }

            string fullAction = variantSuffix != null ? rawAction + variantSuffix : rawAction;

            string label = isDual ? "双持·" + fullAction : fullAction;

            return label;
        }

        /// <summary>
        /// 从正式结果读取来源变体标签后缀。
        /// </summary>
        private static string ResolveSourceVariantLabelSuffix(FormalExpressionResult result)
        {
            string label = result?.SourceVariantLabel;
            if (string.IsNullOrWhiteSpace(label))
            {
                return null;
            }

            return "[" + label + "]";
        }

        /// <summary>
        /// 解析当前手动入口按钮应显示的图标。
        /// 优先读取显式贴图路径；未声明时再回退到真实宿主 Verb 的图标。
        /// </summary>
        private static Texture2D ResolveIconTexture(
            ManualEntryProjectionItem item,
            ManualEntryProjectionGroup group,
            FormalExpressionResult result,
            AttackExecutionTargetingSource targetingSource,
            ManualEntryRecord manualEntryRecord)
        {
            string explicitTexPath = manualEntryRecord != null && !string.IsNullOrWhiteSpace(manualEntryRecord.ManualEntryIconTexPath)
                ? manualEntryRecord.ManualEntryIconTexPath
                : !string.IsNullOrWhiteSpace(item?.ManualEntryIconTexPath)
                    ? item.ManualEntryIconTexPath
                    : group != null ? group.ManualEntryIconTexPath : null;
            if (string.IsNullOrWhiteSpace(explicitTexPath))
            {
                explicitTexPath = ResolveCompositeIconTexPath(result);
            }

            if (!string.IsNullOrWhiteSpace(explicitTexPath))
            {
                Texture2D explicitTexture = ContentFinder<Texture2D>.Get(explicitTexPath, false);
                if (explicitTexture != null)
                {
                    return explicitTexture;
                }
            }

            return targetingSource != null && targetingSource.UIIcon != null
                ? targetingSource.UIIcon
                : BaseContent.BadTex;
        }

        /// <summary>
        /// 解析复合表达入口当前应使用的显式贴图路径。
        /// 只有 Dual / Combo 这类没有单一芯片物品归属的入口才消费这层定义。
        /// </summary>
        private static string ResolveCompositeIconTexPath(FormalExpressionResult result)
        {
            if (result == null || result.CompositeKind == CompositeExpressionKind.None)
            {
                return null;
            }

            ExpressionCompositePresentationDef bestMatch = null;
            int bestScore = -1;
            foreach (ExpressionCompositePresentationDef definition in DefDatabase<ExpressionCompositePresentationDef>.AllDefsListForReading)
            {
                if (!IsCompositePresentationMatch(definition, result))
                {
                    continue;
                }

                int score = ScoreCompositePresentation(definition, result);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestMatch = definition;
                }
            }

            return bestMatch != null ? bestMatch.ManualEntryIconTexPath : null;
        }

        /// <summary>
        /// 判断指定复合表现定义是否命中当前结果。
        /// </summary>
        private static bool IsCompositePresentationMatch(
            ExpressionCompositePresentationDef definition,
            FormalExpressionResult result)
        {
            if (definition == null
                || result == null
                || string.IsNullOrWhiteSpace(definition.ManualEntryIconTexPath))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(definition.CompositeKind)
                && definition.CompositeKind != result.CompositeKind.ToString())
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(definition.WeaponMode)
                && definition.WeaponMode != result.WeaponMode.ToString())
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(definition.ComboDefName)
                && definition.ComboDefName != result.ComboDefName)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 为多个命中的复合表现定义计算优先级。
        /// 越具体的定义优先级越高。
        /// </summary>
        private static int ScoreCompositePresentation(
            ExpressionCompositePresentationDef definition,
            FormalExpressionResult result)
        {
            int score = 0;
            if (definition == null || result == null)
            {
                return score;
            }

            if (!string.IsNullOrWhiteSpace(definition.CompositeKind)
                && definition.CompositeKind == result.CompositeKind.ToString())
            {
                score += 4;
            }

            if (!string.IsNullOrWhiteSpace(definition.WeaponMode)
                && definition.WeaponMode == result.WeaponMode.ToString())
            {
                score += 2;
            }

            if (!string.IsNullOrWhiteSpace(definition.ComboDefName)
                && definition.ComboDefName == result.ComboDefName)
            {
                score += 8;
            }

            return score;
        }

        /// <summary>
        /// 读取结果显示名。
        /// </summary>
        private static string GetDisplayLabel(FormalExpressionResult result)
        {
            return result != null && !string.IsNullOrWhiteSpace(result.DisplayLabel)
                ? result.DisplayLabel
                : "(未命名表达)";
        }
    }
}
