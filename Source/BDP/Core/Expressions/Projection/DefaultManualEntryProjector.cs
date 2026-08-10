using System.Collections.Generic;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 第一版默认手动入口投影器。
    /// 当前先只立结构，不在未接主链时伪造手动入口。
    /// </summary>
    internal sealed class DefaultManualEntryProjector
    {
        /// <summary>
        /// 从正式总表生成手动入口投影结果。
        /// </summary>
        public ManualEntryProjection Build(ExpressionSnapshot snapshot)
        {
            List<ManualEntryProjectionGroup> groups = new List<ManualEntryProjectionGroup>();
            if (snapshot?.Results != null)
            {
                for (int i = 0; i < snapshot.Results.Count; i++)
                {
                    FormalExpressionResult result = snapshot.Results[i];
                    if (result == null
                        || result.ResultKind != ExpressionResultKind.Verb
                        || !result.IsAvailable
                        || !result.CanProject)
                    {
                        continue;
                    }

                    groups.Add(BuildGroup(result));
                }
            }

            return new ManualEntryProjection
            {
                Groups = groups
            };
        }

        /// <summary>
        /// 为一条正式结果构建最小手动入口组。
        /// </summary>
        private static ManualEntryProjectionGroup BuildGroup(FormalExpressionResult result)
        {
            string label = !string.IsNullOrWhiteSpace(result.DisplayLabel) ? result.DisplayLabel : result.Id;
            string aggregationKey = !string.IsNullOrWhiteSpace(result.ManualEntryAggregationKey)
                ? result.ManualEntryAggregationKey
                : result.Id;
            return new ManualEntryProjectionGroup
            {
                GroupId = aggregationKey + ":group",
                ResultId = result.Id,
                DisplayLabel = label,
                ManualEntryIconTexPath = result.ManualEntryIconTexPath,
                Items = new List<ManualEntryProjectionItem>
                {
                    new ManualEntryProjectionItem
                    {
                        ItemId = aggregationKey + ":primary",
                        ResultId = result.Id,
                        DisplayLabel = label,
                        ManualEntryIconTexPath = result.ManualEntryIconTexPath,
                        IsPrimary = true,
                        ResultKind = result.ResultKind,
                        WeaponMode = result.WeaponMode
                    }
                }
            };
        }
    }
}
