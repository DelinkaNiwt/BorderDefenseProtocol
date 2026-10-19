using System;
using System.Collections.Generic;
using BDP.Core.Expressions;
using BDP.Core.AttackExecution;
using RimWorld;
using Verse;

namespace BDP.Core.Information
{
    /// <summary>
    /// 将表达系统内部正式结果快照适配为信息模型运行事实。
    /// 该适配器只读取已成立结果，不参与表达计算。
    /// </summary>
    internal static class ChipInformationExpressionSnapshotAdapter
    {
        /// <summary>创建一份运行时信息解析输入。</summary>
        internal static ChipInformationParseInput Create(ExpressionSnapshot snapshot)
        {
            return Create(snapshot, null);
        }

        /// <summary>创建指定激活芯片的运行时信息输入。</summary>
        internal static ChipInformationParseInput Create(
            ExpressionSnapshot snapshot,
            string chipThingId)
        {
            ChipInformationFacts facts = new ChipInformationFacts();
            List<ChipInformationProvenance> sources =
                new List<ChipInformationProvenance>();
            if (snapshot?.Results != null)
            {
                for (int index = 0; index < snapshot.Results.Count; index++)
                {
                    FormalExpressionResult result = snapshot.Results[index];
                    if (!string.IsNullOrWhiteSpace(chipThingId)
                        && !string.Equals(
                            result?.SourceReference?.ChipThingId,
                            chipThingId,
                            StringComparison.Ordinal))
                    {
                        continue;
                    }

                    AddResult(facts, sources, result, index);
                }
            }

            FormalExpressionResult current = snapshot?.CurrentExecuting;
            if (!string.IsNullOrWhiteSpace(chipThingId)
                && !string.Equals(
                    current?.SourceReference?.ChipThingId,
                    chipThingId,
                    StringComparison.Ordinal))
            {
                current = null;
            }

            return new ChipInformationParseInput(
                new ChipInformationIdentity(
                    chipThingId,
                    null,
                    null),
                facts,
                new ChipInformationRuntimeState(
                    true,
                    current != null,
                    current?.ModeKey,
                    null),
                sources);
        }

        /// <summary>把一条正式运行结果转换为表达事实。</summary>
        private static void AddResult(
            ChipInformationFacts facts,
            ICollection<ChipInformationProvenance> sources,
            FormalExpressionResult result,
            int fallbackIndex)
        {
            if (facts == null || result == null)
            {
                return;
            }

            string sourceKey = string.IsNullOrWhiteSpace(result.Id)
                ? "runtime.result." + fallbackIndex
                : result.Id;
            ChipInformationProvenance runtimeSource = new ChipInformationProvenance(
                sourceKey,
                ChipInformationSourceKind.RuntimeResult,
                ChipInformationContributionKind.Runtime,
                null,
                null,
                result.IsAvailable);
            sources.Add(runtimeSource);

            if (!string.IsNullOrWhiteSpace(result.SourceVariantKey))
            {
                sources.Add(new ChipInformationProvenance(
                    result.SourceVariantKey,
                    ChipInformationSourceKind.Variant,
                    ChipInformationContributionKind.Variant,
                    ChipInformationValue.FromEnum(result.SourceVariantKey),
                    null,
                    result.IsAvailable));
            }

            List<ChipInformationFactValue> fields = new List<ChipInformationFactValue>();
            AddField(fields, "displayLabel", "BDP_Info_Field_Expression", ChipInformationValueKind.Text,
                ChipInformationValue.FromText(result.DisplayLabel), runtimeSource);
            AddField(fields, "available", "BDP_Info_Field_Available", ChipInformationValueKind.Boolean,
                ChipInformationValue.FromBoolean(result.IsAvailable), runtimeSource);
            AddField(fields, "canProject", "BDP_Info_Field_CanProject", ChipInformationValueKind.Boolean,
                ChipInformationValue.FromBoolean(result.CanProject), runtimeSource);
            AddField(fields, "role", "BDP_Info_Field_Role", ChipInformationValueKind.Enum,
                StringValue(result.RoleKey), runtimeSource);
            AddField(fields, "variant", "BDP_Info_Field_Variant", ChipInformationValueKind.Enum,
                StringValue(result.SourceVariantKey), new ChipInformationProvenance(
                    result.SourceVariantKey,
                    ChipInformationSourceKind.Variant,
                    ChipInformationContributionKind.Variant,
                    StringValue(result.SourceVariantKey),
                    null,
                    result.IsAvailable));
            AddField(fields, "weaponMode", "BDP_Info_Field_WeaponMode", ChipInformationValueKind.Enum,
                ChipInformationKnownValueFactory.FromWeaponMode(result.WeaponMode.ToString()), runtimeSource);
            AddField(fields, "abilityDef", "BDP_Info_Field_Ability", ChipInformationValueKind.Enum,
                ChipInformationValue.FromDefReference(typeof(AbilityDef), result.AbilityDefName), runtimeSource);
            AddField(fields, "hediffDef", "BDP_Info_Field_Hediff", ChipInformationValueKind.Enum,
                ChipInformationValue.FromDefReference(typeof(HediffDef), result.HediffDefName), runtimeSource);
            AddField(fields, "hediffApplyMode", "BDP_Info_Field_HediffApplyMode", ChipInformationValueKind.Enum,
                ChipInformationKnownValueFactory.FromHediffApplyMode(result.HediffApplyModeKey), runtimeSource);
            AddField(fields, "hediffControlMode", "BDP_Info_Field_HediffControlMode", ChipInformationValueKind.Enum,
                ChipInformationKnownValueFactory.FromHediffControlMode(result.HediffControlMode), runtimeSource);
            AddField(fields, "passiveKey", "BDP_Info_Field_Passive", ChipInformationValueKind.Enum,
                StringValue(result.PassiveIdentityKey), runtimeSource);
            AddExposedFields(fields, result, runtimeSource);
            facts.Expressions.Add(new ChipInformationExpressionFact(
                sourceKey,
                "BDP_Info_Field_Expression",
                GetChannelKey(result),
                sourceKey,
                ChipInformationSourceKind.RuntimeResult,
                result.WeaponMode.ToString(),
                result.ModeKey,
                null,
                result.IsAvailable,
                fields,
                null,
                null));
        }

        /// <summary>提取运行时已发布的附加数据。</summary>
        private static void AddExposedFields(
            ICollection<ChipInformationFactValue> fields,
            FormalExpressionResult result,
            ChipInformationProvenance source)
        {
            if (result.ExposedData == null)
            {
                return;
            }

            for (int index = 0; index < result.ExposedData.Count; index++)
            {
                PassiveExpressionExposedDatum datum = result.ExposedData[index];
                if (datum == null || string.IsNullOrWhiteSpace(datum.Key))
                {
                    continue;
                }

                AddField(fields, "exposed." + datum.Key, "BDP_Info_Field_ExposedData",
                    ChipInformationValueKind.Text, ChipInformationValue.FromText(datum.Value), source);
            }
        }

        /// <summary>按正式结果通道取得中性通道键。</summary>
        private static string GetChannelKey(FormalExpressionResult result)
        {
            switch (result.OutputChannel)
            {
                case ExpressionOutputChannel.Ability:
                    return "ability";
                case ExpressionOutputChannel.Hediff:
                    return "hediff";
                case ExpressionOutputChannel.Passive:
                    return "passive";
                default:
                    return result.WeaponMode == WeaponExpressionMode.Melee
                        ? "melee"
                        : "ranged";
            }
        }

        /// <summary>添加运行时事实字段。</summary>
        private static void AddField(
            ICollection<ChipInformationFactValue> fields,
            string fieldKey,
            string labelKey,
            ChipInformationValueKind valueKind,
            ChipInformationValue value,
            ChipInformationProvenance source)
        {
            if (value == null || source == null)
            {
                return;
            }

            fields.Add(new ChipInformationFactValue(
                fieldKey,
                labelKey,
                valueKind,
                value,
                null,
                new[]
                {
                    new ChipInformationProvenance(
                        source.SourceKey,
                        source.SourceKind,
                        source.ContributionKind,
                        value,
                        source.Operation,
                        source.Applied)
                }));
        }

        /// <summary>把非空文本转换为枚举值。</summary>
        private static ChipInformationValue StringValue(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : ChipInformationValue.FromEnum(value);
        }
    }
}
