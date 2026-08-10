using System.Collections.Generic;
using BDP.Core.CombatModel;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 默认说明投影器。
    /// 当前先把正式结果总表翻译成最小可读文本，
    /// 不在这里反查内部计算过程。
    /// </summary>
    internal sealed class DefaultExpressionInfoProjector
    {
        /// <summary>
        /// 从正式总表生成说明读取结果。
        /// </summary>
        public ExpressionInfoProjection Build(ExpressionSnapshot snapshot)
        {
            List<string> lines = new List<string>();
            List<ExpressionInfoProjectionEntry> entries = new List<ExpressionInfoProjectionEntry>();
            Dictionary<string, ExpressionPublicationEntry> publicationIndex = BuildPublicationIndex(snapshot);
            if (snapshot == null)
            {
                return new ExpressionInfoProjection
                {
                    Lines = lines,
                    Entries = entries
                };
            }

            lines.Add(snapshot.HasSpecialWeaponOverride ? "特殊侧武器拦截：开启" : "特殊侧武器拦截：关闭");

            if (snapshot.PrimaryRanged != null)
            {
                lines.Add("远程默认主攻击：" + GetDisplayLabel(snapshot.PrimaryRanged));
            }

            if (snapshot.PrimaryMelee != null)
            {
                lines.Add("近战默认主攻击：" + GetDisplayLabel(snapshot.PrimaryMelee));
            }

            if (snapshot.CurrentExecuting != null)
            {
                lines.Add("当前执行表达：" + GetDisplayLabel(snapshot.CurrentExecuting));
            }

            lines.Add(BuildSummaryLine(snapshot));

            if (snapshot.Results != null)
            {
                for (int i = 0; i < snapshot.Results.Count; i++)
                {
                    FormalExpressionResult result = snapshot.Results[i];
                    if (result == null)
                    {
                        continue;
                    }

                    entries.Add(BuildEntry(result, snapshot, publicationIndex));
                    lines.Add(BuildResultLine(result));
                }
            }

            return new ExpressionInfoProjection
            {
                Lines = lines,
                Entries = entries,
                PrimaryRangedResultId = snapshot.PrimaryRanged?.Id,
                PrimaryMeleeResultId = snapshot.PrimaryMelee?.Id,
                CurrentExecutingResultId = snapshot.CurrentExecuting?.Id,
                HasSpecialWeaponOverride = snapshot.HasSpecialWeaponOverride
            };
        }

        /// <summary>
        /// 为单条正式结果构建结构化说明条目。
        /// </summary>
        private static ExpressionInfoProjectionEntry BuildEntry(
            FormalExpressionResult result,
            ExpressionSnapshot snapshot,
            Dictionary<string, ExpressionPublicationEntry> publicationIndex)
        {
            ExpressionPublicationEntry publicationEntry = ResolvePublicationEntry(publicationIndex, result != null ? result.Id : null);
            return new ExpressionInfoProjectionEntry
            {
                ResultId = result.Id,
                DisplayLabel = result.DisplayLabel,
                ResultKind = result.ResultKind,
                OriginKind = result.OriginKind,
                CompositeKind = result.CompositeKind,
                RoleKey = result.RoleKey,
                ModeKey = result.ModeKey,
                IsModeDerived = !string.IsNullOrWhiteSpace(result.ModeKey),
                HasVerbProps = result.VerbProps != null,
                VerbClassName = result.VerbProps?.verbClass != null ? result.VerbProps.verbClass.FullName : null,
                AbilityDefName = result.AbilityDefName,
                HediffDefName = result.HediffDefName,
                HediffApplyModeKey = result.HediffApplyModeKey,
                PassiveKey = result.PassiveKey,
                RangedExecutionRhythm = result.ExecutionStyle?.Single != null
                    ? result.ExecutionStyle.Single.RangedRhythm
                    : RangedExecutionRhythm.None,
                MeleeExecutionRhythm = result.ExecutionStyle?.Single != null
                    ? result.ExecutionStyle.Single.MeleeRhythm
                    : MeleeExecutionRhythm.None,
                DualExecutionSchedule = result.ExecutionStyle?.Dual != null
                    ? result.ExecutionStyle.Dual.Schedule
                    : DualExecutionSchedule.None,
                ShotCount = result.VerbProps != null ? result.VerbProps.burstShotCount : 0,
                HitCount = result.ExecutionStyle?.Single != null ? result.ExecutionStyle.Single.meleeHitCount : 0,
                WeaponMode = result.WeaponMode,
                IsAvailable = result.IsAvailable,
                CanProject = result.CanProject,
                PublishedKey = publicationEntry != null ? publicationEntry.PublishedKey : null,
                IsPublished = publicationEntry != null && publicationEntry.IsPublished,
                SourceResultIds = publicationEntry != null ? publicationEntry.SourceResultIds : new List<string>(),
                IsPrimaryRanged = snapshot != null && snapshot.PrimaryRanged != null && snapshot.PrimaryRanged.Id == result.Id,
                IsPrimaryMelee = snapshot != null && snapshot.PrimaryMelee != null && snapshot.PrimaryMelee.Id == result.Id,
                IsCurrentExecuting = snapshot != null && snapshot.CurrentExecuting != null && snapshot.CurrentExecuting.Id == result.Id
            };
        }

        /// <summary>
        /// 为当前快照建立发布观察条目的按结果标识索引。
        /// </summary>
        private static Dictionary<string, ExpressionPublicationEntry> BuildPublicationIndex(ExpressionSnapshot snapshot)
        {
            Dictionary<string, ExpressionPublicationEntry> result =
                new Dictionary<string, ExpressionPublicationEntry>();
            if (snapshot?.PublicationSnapshot?.Entries == null)
            {
                return result;
            }

            for (int i = 0; i < snapshot.PublicationSnapshot.Entries.Count; i++)
            {
                ExpressionPublicationEntry entry = snapshot.PublicationSnapshot.Entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.ResultId) || result.ContainsKey(entry.ResultId))
                {
                    continue;
                }

                result.Add(entry.ResultId, entry);
            }

            return result;
        }

        /// <summary>
        /// 按结果标识解析一条发布观察条目。
        /// </summary>
        private static ExpressionPublicationEntry ResolvePublicationEntry(
            Dictionary<string, ExpressionPublicationEntry> publicationIndex,
            string resultId)
        {
            if (publicationIndex == null || string.IsNullOrWhiteSpace(resultId))
            {
                return null;
            }

            ExpressionPublicationEntry result;
            return publicationIndex.TryGetValue(resultId, out result) ? result : null;
        }

        /// <summary>
        /// 为单条正式结果构建最小说明文本。
        /// </summary>
        private static string BuildResultLine(FormalExpressionResult result)
        {
            string label = string.IsNullOrWhiteSpace(result.DisplayLabel) ? "(未命名表达)" : result.DisplayLabel;
            string role = string.IsNullOrWhiteSpace(result.RoleKey) ? "-" : result.RoleKey;
            string composite = result.CompositeKind != CompositeExpressionKind.None
                ? result.CompositeKind.ToString()
                : "-";
            string modeKey = string.IsNullOrWhiteSpace(result.ModeKey) ? "-" : result.ModeKey;
            string verbClassName = result.VerbProps?.verbClass != null ? result.VerbProps.verbClass.Name : "-";
            string abilityDefName = string.IsNullOrWhiteSpace(result.AbilityDefName) ? "-" : result.AbilityDefName;
            string hediffDefName = string.IsNullOrWhiteSpace(result.HediffDefName) ? "-" : result.HediffDefName;
            string hediffApplyModeKey = string.IsNullOrWhiteSpace(result.HediffApplyModeKey) ? "-" : result.HediffApplyModeKey;
            string passiveKey = string.IsNullOrWhiteSpace(result.PassiveKey) ? "-" : result.PassiveKey;
            string rangedRhythm = result.ExecutionStyle?.Single != null
                ? result.ExecutionStyle.Single.RangedRhythm.ToString()
                : RangedExecutionRhythm.None.ToString();
            string meleeRhythm = result.ExecutionStyle?.Single != null
                ? result.ExecutionStyle.Single.MeleeRhythm.ToString()
                : MeleeExecutionRhythm.None.ToString();
            string dualSchedule = result.ExecutionStyle?.Dual != null
                ? result.ExecutionStyle.Dual.Schedule.ToString()
                : DualExecutionSchedule.None.ToString();
            int shotCount = result.VerbProps != null ? result.VerbProps.burstShotCount : 0;
            int hitCount = result.ExecutionStyle?.Single != null ? result.ExecutionStyle.Single.meleeHitCount : 0;
            return label
                + " | kind=" + result.ResultKind
                + " | origin=" + result.OriginKind
                + " | composite=" + composite
                + " | role=" + role
                + " | modeKey=" + modeKey
                + " | verbClass=" + verbClassName
                + " | abilityDef=" + abilityDefName
                + " | hediffDef=" + hediffDefName
                + " | hediffApplyMode=" + hediffApplyModeKey
                + " | passiveKey=" + passiveKey
                + " | rangedRhythm=" + rangedRhythm
                + " | meleeRhythm=" + meleeRhythm
                + " | dualSchedule=" + dualSchedule
                + " | shotCount=" + shotCount
                + " | hitCount=" + hitCount
                + " | mode=" + result.WeaponMode;
        }

        /// <summary>
        /// 构建当前结果总表摘要行。
        /// </summary>
        private static string BuildSummaryLine(ExpressionSnapshot snapshot)
        {
            int total = 0;
            int verb = 0;
            int ability = 0;
            int hediff = 0;
            int passive = 0;
            int composite = 0;
            if (snapshot?.Results != null)
            {
                for (int i = 0; i < snapshot.Results.Count; i++)
                {
                    FormalExpressionResult result = snapshot.Results[i];
                    if (result == null)
                    {
                        continue;
                    }

                    total++;
                    if (result.CompositeKind != CompositeExpressionKind.None)
                    {
                        composite++;
                    }

                    switch (result.ResultKind)
                    {
                        case ExpressionResultKind.Verb:
                            verb++;
                            break;
                        case ExpressionResultKind.Ability:
                            ability++;
                            break;
                        case ExpressionResultKind.Hediff:
                            hediff++;
                            break;
                        case ExpressionResultKind.Passive:
                            passive++;
                            break;
                    }
                }
            }

            return "结果总表：total=" + total
                + " | verb=" + verb
                + " | ability=" + ability
                + " | hediff=" + hediff
                + " | passive=" + passive
                + " | composite=" + composite;
        }

        /// <summary>
        /// 读取一条结果当前应显示的最小名称。
        /// </summary>
        private static string GetDisplayLabel(FormalExpressionResult result)
        {
            return result != null && !string.IsNullOrWhiteSpace(result.DisplayLabel)
                ? result.DisplayLabel
                : "(未命名表达)";
        }
    }
}
