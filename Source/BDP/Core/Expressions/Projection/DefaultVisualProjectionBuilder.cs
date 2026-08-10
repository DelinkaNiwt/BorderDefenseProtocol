using System.Collections.Generic;
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
            List<VisualResidentEntry> residentEntries = CollectResidentEntries(snapshot);
            int activeWeaponChipInstanceCount = CountActiveWeaponChipInstances(snapshot);
            VisualExpressionRelationKind relationKind = ResolveRelationKind(snapshot, residentEntries);
            return new VisualExpressionProjection
            {
                RelationKind = relationKind,
                ResidentEntries = residentEntries,
                ActiveWeaponChipInstanceCount = activeWeaponChipInstanceCount,
                HostEquipmentRenderMode = ResolveHostEquipmentRenderMode(
                    residentEntries,
                    activeWeaponChipInstanceCount,
                    relationKind),
                ExecutionFocusPolicy = ResolveExecutionFocusPolicy(residentEntries, activeWeaponChipInstanceCount),
                MuzzleFollowPolicy = ResolveMuzzleFollowPolicy(residentEntries, activeWeaponChipInstanceCount)
            };
        }

        /// <summary>
        /// 收集当前可进入常驻视觉读取的结果条目。
        /// Resident 只保留单侧结果；Dual/Combo 的复合表象通过每条单侧结果携带的 CompositeVisualPresetDefName 解释。
        /// </summary>
        private static List<VisualResidentEntry> CollectResidentEntries(ExpressionSnapshot snapshot)
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
                    || entry.ResultKind != ExpressionResultKind.Verb
                    || entry.CompositeKind != CompositeExpressionKind.None
                    || !entry.IsAvailable
                    || !entry.CanProject
                    || string.IsNullOrWhiteSpace(entry.Id))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(entry.VisualPresetDefName)
                    && string.IsNullOrWhiteSpace(entry.CompositeVisualPresetDefName)
                    && !entry.ForceSuppressHostEquipment)
                {
                    continue;
                }

                result.Add(new VisualResidentEntry
                {
                    ResultId = entry.Id,
                    SourceReference = entry.SourceReference,
                    Side = entry.SourceReference != null ? entry.SourceReference.Side : TriggerSide.Main,
                    SlotIndex = entry.SourceReference != null ? entry.SourceReference.SlotIndex : -1,
                    VisualPresetDefName = entry.VisualPresetDefName,
                    CompositeVisualPresetDefName = entry.CompositeVisualPresetDefName,
                    VerbAttackRole = entry.VerbAttackRole,
                    ForceSuppressHostEquipment = entry.ForceSuppressHostEquipment,
                    VisualPriority = entry.VisualPriority
                });
            }

            result.Sort(CompareResidentEntries);
            return result;
        }

        /// <summary>
        /// 解析当前视觉关系类型。
        /// 第一版只基于正式总表里已经成立的高层结果与常驻结果数量判断。
        /// </summary>
        private static VisualExpressionRelationKind ResolveRelationKind(
            ExpressionSnapshot snapshot,
            List<VisualResidentEntry> residentEntries)
        {
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

                    if (result.CompositeKind == CompositeExpressionKind.Combo)
                    {
                        return VisualExpressionRelationKind.Combo;
                    }

                    if (result.CompositeKind == CompositeExpressionKind.DualWeapon)
                    {
                        return VisualExpressionRelationKind.DualWeapon;
                    }
                }
            }

            return residentEntries != null && residentEntries.Count > 0
                ? VisualExpressionRelationKind.SingleSide
                : VisualExpressionRelationKind.None;
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
                    || result.ResultKind != ExpressionResultKind.Verb
                    || result.CompositeKind != CompositeExpressionKind.None
                    || result.WeaponMode == WeaponExpressionMode.None
                    || !result.IsAvailable
                    || !result.CanProject)
                {
                    continue;
                }

                string key = BuildWeaponChipInstanceKey(result.SourceReference);
                if (!string.IsNullOrWhiteSpace(key))
                {
                    keys.Add(key);
                }
            }

            return keys.Count;
        }

        /// <summary>
        /// 为武器结果构建来源芯片实例键。
        /// 优先使用 ThingID，缺失时回退到槽位坐标，保证读档恢复边界仍能稳定判断。
        /// </summary>
        private static string BuildWeaponChipInstanceKey(ExpressionSourceReference sourceReference)
        {
            if (sourceReference == null)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(sourceReference.ChipThingId))
            {
                return "thing:" + sourceReference.ChipThingId;
            }

            string chipDefName = !string.IsNullOrWhiteSpace(sourceReference.ChipDefName)
                ? sourceReference.ChipDefName
                : "unknown";
            return "slot:" + sourceReference.Side + ":" + sourceReference.SlotIndex + ":" + chipDefName;
        }

        /// <summary>
        /// 解析当前宿主装备贴图绘制策略。
        /// 无显式姿态的单武器只替换贴图；显式姿态单武器与多武器使用完整替换。
        /// 多武器作者显式强压时升级到 Suppress。
        /// </summary>
        private static HostEquipmentRenderMode ResolveHostEquipmentRenderMode(
            List<VisualResidentEntry> residentEntries,
            int activeWeaponChipInstanceCount,
            VisualExpressionRelationKind relationKind)
        {
            if (residentEntries == null || residentEntries.Count == 0)
            {
                return HostEquipmentRenderMode.Keep;
            }

            if (activeWeaponChipInstanceCount == 1)
            {
                return HasExplicitPose(residentEntries, relationKind)
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
