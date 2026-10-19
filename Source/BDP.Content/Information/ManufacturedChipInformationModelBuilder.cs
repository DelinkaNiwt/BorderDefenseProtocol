using System.Collections.Generic;
using BDP.Content.Assembly.ChipManufacturing.Model;
using BDP.Content.Assembly.ChipManufacturing.Resolution;
using BDP.Content.Information.Combos;
using BDP.Core.Expressions;
using BDP.Core.Information;
using BDP.Core.Chips;

namespace BDP.Content.Information
{
    /// <summary>
    /// 从一份合法成品组合解析结果构建统一芯片信息模型。
    /// 本类不持有窗口状态，也不负责把模型映射成具体界面。
    /// </summary>
    public static class ManufacturedChipInformationModelBuilder
    {
        /// <summary>构建当前成品的基础值、最终值、描述与来源链模型。</summary>
        public static ChipInformationModel Build(
            ChipCombinationResolution resolution)
        {
            return Build(resolution, null, null, null);
        }

        /// <summary>构建芯片静态信息并可选关联已发布的当前组合结果。</summary>
        internal static ChipInformationModel Build(
            ChipCombinationResolution resolution,
            string chipThingId,
            ChipInformationRuntimeState runtime,
            ExpressionPublishedProjectionSnapshot publishedProjection)
        {
            return BuildCore(
                resolution,
                chipThingId,
                runtime,
                publishedProjection,
                true);
        }

        /// <summary>构建芯片详情专用模型，只收集功能模板说明。</summary>
        internal static ChipInformationModel BuildForDetail(
            ChipCombinationResolution resolution)
        {
            return BuildForDetail(resolution, null, null, null);
        }

        /// <summary>构建详情专用模型并关联实例与已发布表达快照。</summary>
        internal static ChipInformationModel BuildForDetail(
            ChipCombinationResolution resolution,
            string chipThingId,
            ChipInformationRuntimeState runtime,
            ExpressionPublishedProjectionSnapshot publishedProjection)
        {
            return BuildCore(
                resolution,
                chipThingId,
                runtime,
                publishedProjection,
                false);
        }

        /// <summary>按消费界面决定是否附带武装构型说明，统一构建信息模型。</summary>
        private static ChipInformationModel BuildCore(
            ChipCombinationResolution resolution,
            string chipThingId,
            ChipInformationRuntimeState runtime,
            ExpressionPublishedProjectionSnapshot publishedProjection,
            bool includeArmamentFormDescription)
        {
            if (resolution == null)
            {
                return null;
            }

            ChipInformationIdentity identity = new ChipInformationIdentity(
                null,
                null,
                null,
                resolution.SourceProfessionKey);
            List<ChipInformationDescription> descriptions =
                CreateDescriptions(resolution, includeArmamentFormDescription);
            List<ChipInformationFactBlock> armamentFormBlocks =
                ChipArmamentFormInformationAdapter.CreateBlocks(resolution.ArmamentForm);
            List<string> sourceKeys = new List<string>();
            if (resolution.FunctionDefs != null)
            {
                for (int index = 0; index < resolution.FunctionDefs.Count; index++)
                {
                    string sourceKey = resolution.FunctionDefs[index]?.defName;
                    if (!string.IsNullOrWhiteSpace(sourceKey))
                    {
                        sourceKeys.Add(sourceKey);
                    }
                }
            }
            IReadOnlyList<ChipInformationExtensionReference> extensionReferences =
                ComboChipInformationReferenceProjector.Create(
                    new ChipSourceReferenceSnapshot
                    {
                        OrderedSourceKeys = sourceKeys.AsReadOnly(),
                        SourceProfessionKey = resolution.SourceProfessionKey,
                        SourceVariantKey = resolution.ArmamentForm?.defName
                    },
                    resolution.ResolvedConfig,
                    chipThingId,
                    runtime?.ActiveModeKey,
                    runtime?.ActiveStanceKey,
                    publishedProjection);
            ChipInformationRequest request = new ChipInformationRequest(
                identity,
                resolution.DeclaredConfig ?? resolution.ResolvedConfig,
                resolution.ResolvedConfig,
                runtime != null && runtime.HasRuntimeState
                    ? ChipInformationRuntimeInputAdapter.Create(
                        runtime.IsActive,
                        runtime.ActiveModeKey,
                        runtime.ActiveStanceKey)
                    : ChipInformationRuntimeInputAdapter.CreateStatic(),
                null,
                null,
                descriptions,
                armamentFormBlocks,
                extensionReferences,
                ChipArmamentFormInformationAdapter.CreateFeaturePropertySources(
                    resolution.ArmamentForm));
            return ChipInformationSurfaceAccess.Read(request);
        }

        /// <summary>读取功能模板描述，并按消费界面需要附带武装构型描述。</summary>
        private static List<ChipInformationDescription> CreateDescriptions(
            ChipCombinationResolution resolution,
            bool includeArmamentFormDescription)
        {
            List<ChipInformationDescription> result =
                new List<ChipInformationDescription>();
            if (resolution?.FunctionDefs != null)
            {
                for (int index = 0; index < resolution.FunctionDefs.Count; index++)
                {
                    if (resolution.FunctionDefs[index] == null)
                    {
                        continue;
                    }

                    result.Add(new ChipInformationDescription(
                        resolution.FunctionDefs[index].defName,
                        resolution.FunctionDefs[index].ResolvedLabel,
                        resolution.FunctionDefs[index].ResolvedDescription));
                }
            }

            if (includeArmamentFormDescription && resolution?.ArmamentForm != null)
            {
                result.Add(new ChipInformationDescription(
                    resolution.ArmamentForm.defName,
                    resolution.ArmamentForm.ResolvedLabel,
                    resolution.ArmamentForm.ResolvedDescription));
            }

            return result;
        }
    }
}
