using System.Collections.Generic;
using Verse;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 第一版默认宿主总同步器。
    /// 旧 Verb 宿主链已拆除后，这里只保留 Ability 和 Hediff 两条同步通道。
    /// </summary>
    internal sealed class DefaultExpressionHostSynchronizer
    {
        /// <summary>
        /// Ability 宿主同步器。
        /// </summary>
        private readonly DefaultExpressionAbilityHostSynchronizer abilitySynchronizer;

        /// <summary>
        /// Hediff 宿主同步器。
        /// </summary>
        private readonly DefaultExpressionHediffHostSynchronizer hediffSynchronizer;

        /// <summary>
        /// 用指定子同步器构造总同步器。
        /// </summary>
        public DefaultExpressionHostSynchronizer(
            DefaultExpressionAbilityHostSynchronizer abilitySynchronizer,
            DefaultExpressionHediffHostSynchronizer hediffSynchronizer)
        {
            this.abilitySynchronizer = abilitySynchronizer;
            this.hediffSynchronizer = hediffSynchronizer;
        }

        /// <summary>
        /// 按当前正式总表同步全部已实现的宿主通道。
        /// </summary>
        public void Sync(Pawn pawn, ExpressionSnapshot snapshot)
        {
            if (pawn == null)
            {
                return;
            }

            abilitySynchronizer?.Sync(pawn, snapshot);
            hediffSynchronizer?.Sync(pawn, snapshot);
        }

        /// <summary>
        /// 为当前正式总表构建一份旁路发布观察快照。
        /// 它只服务说明和排查，不改变同步逻辑本身。
        /// </summary>
        public ExpressionPublicationSnapshot BuildPublicationSnapshot(ExpressionSnapshot snapshot)
        {
            List<ExpressionPublicationEntry> entries = new List<ExpressionPublicationEntry>();
            AppendVerbPublicationEntries(snapshot, entries);
            abilitySynchronizer?.AppendPublicationEntries(snapshot, entries);
            hediffSynchronizer?.AppendPublicationEntries(snapshot, entries);
            AppendPassivePublicationEntries(snapshot, entries);
            return new ExpressionPublicationSnapshot
            {
                Entries = entries
            };
        }

        /// <summary>
        /// 解析一条结果在复合引用表里的来源结果标识列表。
        /// </summary>
        internal static IReadOnlyList<string> ResolveSourceResultIds(ExpressionSnapshot snapshot, string resultId)
        {
            if (snapshot?.CompositeReferences == null || string.IsNullOrWhiteSpace(resultId))
            {
                return new List<string>();
            }

            for (int i = 0; i < snapshot.CompositeReferences.Count; i++)
            {
                CompositeExpressionReference reference = snapshot.CompositeReferences[i];
                if (reference == null || reference.CompositeId != resultId)
                {
                    continue;
                }

                return reference.SourceResultIds ?? new List<string>();
            }

            return new List<string>();
        }

        /// <summary>
        /// 为 Verb 结果追加发布观察条目。
        /// </summary>
        private static void AppendVerbPublicationEntries(
            ExpressionSnapshot snapshot,
            List<ExpressionPublicationEntry> entries)
        {
            if (entries == null || snapshot?.Results == null)
            {
                return;
            }

            for (int i = 0; i < snapshot.Results.Count; i++)
            {
                FormalExpressionResult result = snapshot.Results[i];
                if (result == null || result.ResultKind != ExpressionResultKind.Verb)
                {
                    continue;
                }

                entries.Add(new ExpressionPublicationEntry
                {
                    ResultId = result.Id,
                    ResultKind = result.ResultKind,
                    PublishedKey = result.ExecutionSlotKey,
                    IsPublished = result.IsAvailable
                        && (result.UseRequirementCheck == null
                            || result.UseRequirementCheck.Satisfied)
                        && result.CanProject
                        && !string.IsNullOrWhiteSpace(result.ExecutionSlotKey),
                    SourceResultIds = ResolveSourceResultIds(snapshot, result.Id)
                });
            }
        }

        /// <summary>
        /// 为 Passive 结果追加发布观察条目。
        /// </summary>
        private static void AppendPassivePublicationEntries(
            ExpressionSnapshot snapshot,
            List<ExpressionPublicationEntry> entries)
        {
            if (entries == null || snapshot?.Results == null)
            {
                return;
            }

            for (int i = 0; i < snapshot.Results.Count; i++)
            {
                FormalExpressionResult result = snapshot.Results[i];
                if (result == null || result.ResultKind != ExpressionResultKind.Passive)
                {
                    continue;
                }

                entries.Add(new ExpressionPublicationEntry
                {
                    ResultId = result.Id,
                    ResultKind = result.ResultKind,
                    PublishedKey = result.PassiveKey,
                    IsPublished = result.IsAvailable
                        && (result.UseRequirementCheck == null
                            || result.UseRequirementCheck.Satisfied)
                        && !string.IsNullOrWhiteSpace(result.PassiveKey),
                    SourceResultIds = ResolveSourceResultIds(snapshot, result.Id)
                });
            }
        }
    }
}
