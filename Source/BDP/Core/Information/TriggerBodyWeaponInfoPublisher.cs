using System;
using System.Collections.Generic;
using BDP.Core.Expressions;
using BDP.Core.Trigger;
using BDP.Core.Trigger.Runtime;
using BDP.Core.Information.Vanilla;
using BDP.Support.Diagnostics;
using RimWorld;
using Verse;

namespace BDP.Core.Information
{
    /// <summary>
    /// 读取触发体当前默认自动攻击的原版武器统计条目。
    /// 这是纯信息适配器，不参与攻击选择、攻击执行或表达重建。
    /// </summary>
    public static class TriggerBodyWeaponInfoPublisher
    {
        // 原版信息卡与外部显示适配器共用 TriggerBodyAutomaticAttackDisplayProvider 的只读快照契约；
        // 本发布器继续保留多候选条目拼接，因为原版信息卡允许同时展示多个表达结果。

        /// <summary>
        /// 尝试从当前已发布战斗投影构建原版武器统计条目。
        /// 没有有效默认主武器时返回 false，调用方应完整回退原始信息卡。
        /// </summary>
        public static bool TryBuild(
            CompTriggerBody triggerBody,
            out IReadOnlyList<StatDrawEntry> entries)
        {
            List<StatDrawEntry> result = new List<StatDrawEntry>();
            entries = result.AsReadOnly();
            if (triggerBody == null)
            {
                return false;
            }

            try
            {
                TriggerCombatProjectionState projection = triggerBody.PublishedCombatProjection;
                ExpressionSnapshot snapshot = projection?.Snapshot;
                if (snapshot == null)
                {
                    return false;
                }

                List<FormalExpressionResult> candidates = new List<FormalExpressionResult>();
                AddCandidate(candidates, projection, snapshot.PrimaryMelee);
                AddCandidate(candidates, projection, snapshot.PrimaryRanged);
                for (int index = 0; index < candidates.Count; index++)
                {
                    FormalExpressionResult candidate = candidates[index];
                    if (!IsDisplayableWeapon(candidate))
                    {
                        continue;
                    }

                    VanillaWeaponStatSurface surface;
                    string failureReason;
                    if (!VanillaWeaponStatSurfaceFactory.TryCreate(
                        candidate,
                        out surface,
                        out failureReason))
                    {
                        continue;
                    }

                    IReadOnlyList<StatDrawEntry> projectedEntries;
                    if (!VanillaWeaponStatSurfaceReader.TryRead(
                        surface,
                        out projectedEntries,
                        out failureReason)
                        || projectedEntries == null)
                    {
                        continue;
                    }

                    for (int statIndex = 0; statIndex < projectedEntries.Count; statIndex++)
                    {
                        StatDrawEntry entry = projectedEntries[statIndex];
                        if (entry != null)
                        {
                            result.Add(entry);
                        }
                    }
                }

                entries = result.AsReadOnly();
                return result.Count > 0;
            }
            catch (Exception exception)
            {
                BdpDiagnostics.Once(
                    "trigger.info_card.dynamic_weapon_projection",
                    "触发体动态武器信息读取失败：" + exception.GetType().Name);
                entries = new List<StatDrawEntry>().AsReadOnly();
                return false;
            }
        }

        /// <summary>按结果标识去重并只接受可用 Verb 武器结果。</summary>
        private static void AddCandidate(
            IList<FormalExpressionResult> candidates,
            TriggerCombatProjectionState projection,
            FormalExpressionResult candidate)
        {
            if (!IsDisplayableWeapon(candidate))
            {
                return;
            }

            AddCandidateOnce(candidates, candidate);
            if (projection?.CompositeReferenceIndex == null
                || projection.ResultIndex == null
                || !projection.CompositeReferenceIndex.TryGetValue(
                    candidate.Id,
                    out CompositeExpressionReference composite)
                || composite?.SourceResultIds == null)
            {
                return;
            }

            for (int index = 0; index < composite.SourceResultIds.Count; index++)
            {
                string sourceResultId = composite.SourceResultIds[index];
                if (string.IsNullOrWhiteSpace(sourceResultId)
                    || !projection.ResultIndex.TryGetValue(
                        sourceResultId,
                        out FormalExpressionResult sourceResult))
                {
                    continue;
                }

                AddCandidateOnce(candidates, sourceResult);
            }
        }

        /// <summary>加入一条未重复的显示候选结果。</summary>
        private static void AddCandidateOnce(
            IList<FormalExpressionResult> candidates,
            FormalExpressionResult candidate)
        {
            if (!IsDisplayableWeapon(candidate))
            {
                return;
            }

            for (int index = 0; index < candidates.Count; index++)
            {
                if (string.Equals(candidates[index]?.Id, candidate.Id, StringComparison.Ordinal))
                {
                    return;
                }
            }

            candidates.Add(candidate);
        }

        /// <summary>判断正式结果是否具备可读的原版武器表面。</summary>
        private static bool IsDisplayableWeapon(FormalExpressionResult result)
        {
            return result != null
                && result.IsAvailable
                && result.CanProject
                && result.OutputChannel == ExpressionOutputChannel.Verb
                && (result.WeaponMode == WeaponExpressionMode.Melee
                    || result.WeaponMode == WeaponExpressionMode.Ranged);
        }
    }
}
