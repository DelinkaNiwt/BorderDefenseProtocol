using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BDP.Core.Expressions;
using BDP.Core.Trigger.Runtime;
using Verse;

namespace BDP.Core.AttackExecution
{
    /// <summary>来源条件刷新时判断攻击是否仍是原攻击；不解释任何业务条件。</summary>
    internal static class AttackSessionContinuity
    {
        /// <summary>
        /// 比较正式身份、槽位和配置，生成可迁移版本的结果集合。
        /// 只在来源条件发布边界使用，切换芯片等普通换版仍按既有规则中断。
        /// </summary>
        internal static HashSet<string> GetUnchangedResultIds(
            TriggerCombatProjectionState before, TriggerCombatProjectionState after)
        {
            HashSet<string> unchanged = new HashSet<string>();
            if (before?.ResultIndex == null || after?.ResultIndex == null) return unchanged;
            foreach (string id in before.ResultIndex.Keys)
            {
                if (SameResult(id, before, after, new HashSet<string>())) unchanged.Add(id);
            }
            return unchanged;
        }

        /// <summary>旧令牌必须确实属于本人物、上一版和未变结果，禁止救活更早失效的会话。</summary>
        internal static bool CanMigrateToken(AttackSessionToken token, int previousVersion,
            AttackSessionToken nextBindingToken, ISet<string> unchanged)
        {
            return token != null && token.IsValid && nextBindingToken != null
                && previousVersion > 0 && token.ProjectionVersion == previousVersion
                && token.OwnerPawnThingId == nextBindingToken.OwnerPawnThingId
                && unchanged != null && unchanged.Contains(token.ResultId);
        }

        /// <summary>复合攻击还要逐侧比较来源；编号相同不代表攻击配置没有改变。</summary>
        private static bool SameResult(string id, TriggerCombatProjectionState before,
            TriggerCombatProjectionState after, HashSet<string> visiting)
        {
            if (!before.ResultIndex.TryGetValue(id, out FormalExpressionResult oldResult)
                || !after.ResultIndex.TryGetValue(id, out FormalExpressionResult newResult)
                || oldResult == null || newResult == null || !oldResult.IsAvailable || !newResult.IsAvailable
                || oldResult.CanProject != newResult.CanProject
                || (oldResult.UseRequirementCheck != null && !oldResult.UseRequirementCheck.Satisfied)
                || (newResult.UseRequirementCheck != null && !newResult.UseRequirementCheck.Satisfied)
                || !visiting.Add(id)) return false;
            bool oldHasSlot = before.ResultIdToFormalSlot != null
                && before.ResultIdToFormalSlot.ContainsKey(id);
            bool newHasSlot = after.ResultIdToFormalSlot != null
                && after.ResultIdToFormalSlot.ContainsKey(id);
            if (oldHasSlot != newHasSlot || (oldHasSlot
                && before.ResultIdToFormalSlot[id] != after.ResultIdToFormalSlot[id])) return false;

            if (oldResult.Id != newResult.Id || oldResult.OutputChannel != newResult.OutputChannel
                || oldResult.WeaponMode != newResult.WeaponMode || oldResult.OriginKind != newResult.OriginKind
                || oldResult.CompositeKind != newResult.CompositeKind || oldResult.ComboDefName != newResult.ComboDefName
                || oldResult.SourceEntryId != newResult.SourceEntryId || oldResult.SourceVariantKey != newResult.SourceVariantKey
                || oldResult.ModeKey != newResult.ModeKey || oldResult.VerbAttackRole != newResult.VerbAttackRole
                || oldResult.ExecutionSlotKey != newResult.ExecutionSlotKey
                || !SameConfiguration(oldResult.SourceReference, newResult.SourceReference)
                || !SameConfiguration(oldResult.ResolvedVerbSpec, newResult.ResolvedVerbSpec)
                || !SameConfiguration(oldResult.VerbProps, newResult.VerbProps)
                || !SameConfiguration(oldResult.ExecutionStyle, newResult.ExecutionStyle)
                || !SameConfiguration(oldResult.Trion, newResult.Trion)
                || !SameConfiguration(oldResult.DeclaredTools, newResult.DeclaredTools)
                || !SameConfiguration(oldResult.DeclaredMeleeToolSurfaces, newResult.DeclaredMeleeToolSurfaces)
                || oldResult.Tool != newResult.Tool || oldResult.Maneuver != newResult.Maneuver
                || !SameConfiguration(oldResult.RangedModules, newResult.RangedModules)
                || !SameConfiguration(oldResult.HitPartPolicy, newResult.HitPartPolicy)
                || !SameConfiguration(oldResult.DeclaredPermissionKeys, newResult.DeclaredPermissionKeys)
                || !SameConfiguration(oldResult.Tags, newResult.Tags)) return false;

            CompositeExpressionReference oldReference = null;
            CompositeExpressionReference newReference = null;
            before.CompositeReferenceIndex?.TryGetValue(id, out oldReference);
            after.CompositeReferenceIndex?.TryGetValue(id, out newReference);
            if (!SameConfiguration(oldReference, newReference)) return false;
            if (oldReference?.SourceResultIds != null)
            {
                foreach (string sourceId in oldReference.SourceResultIds)
                {
                    if (!SameResult(sourceId, before, after, visiting)) return false;
                }
            }
            visiting.Remove(id);
            return true;
        }

        /// <summary>
        /// 比较已复制的声明配置，避免把新副本误当成新攻击。
        /// 模块配置沿用既有深复制的实例字段语义；Def、Tool、Thing 和 Unity 对象只认同一引用。
        /// </summary>
        private static bool SameConfiguration(object left, object right)
        {
            return SameConfiguration(left, right, new List<KeyValuePair<object, object>>(), 0);
        }

        /// <summary>递归比较配置字段和有序列表；循环配对校验防止错误等同，超深配置保守中断。</summary>
        private static bool SameConfiguration(object left, object right,
            List<KeyValuePair<object, object>> visited, int depth)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left == null || right == null || left.GetType() != right.GetType() || depth > 32) return false;
            Type type = left.GetType();
            if (type.IsValueType || type == typeof(string)) return left.Equals(right);
            if (left is Def || left is Tool || left is Thing || left is Type || left is UnityEngine.Object) return false;
            foreach (KeyValuePair<object, object> pair in visited)
            {
                if (ReferenceEquals(pair.Key, left)) return ReferenceEquals(pair.Value, right);
                if (ReferenceEquals(pair.Value, right)) return false;
            }
            visited.Add(new KeyValuePair<object, object>(left, right));
            if (left is IList leftList && right is IList rightList)
            {
                if (leftList.Count != rightList.Count) return false;
                for (int i = 0; i < leftList.Count; i++)
                    if (!SameConfiguration(leftList[i], rightList[i], visited, depth + 1)) return false;
                return true;
            }
            // 原版 VerbProperties 只比较作者可声明的公开字段，不把私有惰性缓存当作配置。
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly;
            if (!(left is VerbProperties)) flags |= BindingFlags.NonPublic;
            for (Type current = type; current != null && current != typeof(object); current = current.BaseType)
            {
                foreach (FieldInfo field in current.GetFields(flags))
                {
                    if (!SameConfiguration(field.GetValue(left), field.GetValue(right), visited, depth + 1)) return false;
                }
            }
            return true;
        }
    }
}
