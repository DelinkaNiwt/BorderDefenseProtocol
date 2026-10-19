using BDP.Core.Chips;
using Verse;

namespace BDP.Core.Information
{
    /// <summary>
    /// 将成品或运行中芯片实例转换为统一请求。
    /// 它只读取中性实例表面，不在装配 UI 中解释实现类型。
    /// </summary>
    public static class ChipInformationInstanceRequestAdapter
    {
        /// <summary>尝试为实例构造声明配置、有效配置和运行时状态请求。</summary>
        public static bool TryCreate(
            Thing chip,
            ChipInformationRuntimeState runtime,
            string currentModeKey,
            string currentStanceKey,
            out ChipInformationRequest request)
        {
            request = null;
            if (chip == null
                || !ChipInstanceSurfaceAccess.TryGetDefinition(chip, out ChipDefinitionConfig effectiveConfig)
                || effectiveConfig == null)
            {
                return false;
            }

            ChipDefinitionConfig declaredConfig;
            if (!ChipInstanceSurfaceAccess.TryGetDeclaredDefinition(chip, out declaredConfig)
                || declaredConfig == null)
            {
                declaredConfig = effectiveConfig;
            }

            ChipSourceReferenceSnapshot source = ChipInstanceSurfaceAccess.ReadSourceReference(chip);
            ChipInformationIdentity identity = new ChipInformationIdentity(
                chip.ThingID,
                chip.def?.defName,
                null,
                source?.SourceProfessionKey);
            request = new ChipInformationRequest(
                identity,
                declaredConfig,
                effectiveConfig,
                runtime ?? ChipInformationRuntimeInputAdapter.CreateStatic(),
                currentModeKey,
                currentStanceKey);
            return true;
        }
    }
}
