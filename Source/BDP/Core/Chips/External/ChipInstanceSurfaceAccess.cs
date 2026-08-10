using System.Collections.Generic;
using Verse;

namespace BDP.Core.Chips
{
    /// <summary>
    /// Core 读取芯片实例动态定义与来源身份的唯一中性入口。
    /// </summary>
    public static class ChipInstanceSurfaceAccess
    {
        /// <summary>
        /// 遍历实例组件并读取第一份有效动态芯片定义。
        /// </summary>
        public static bool TryGetDefinition(Thing thing, out ChipDefinitionConfig definition)
        {
            definition = null;
            ThingWithComps thingWithComps = thing as ThingWithComps;
            if (thingWithComps?.AllComps == null)
            {
                return false;
            }

            for (int index = 0; index < thingWithComps.AllComps.Count; index++)
            {
                IChipInstanceDefinitionProvider provider =
                    thingWithComps.AllComps[index] as IChipInstanceDefinitionProvider;
                if (provider != null
                    && provider.TryGetChipDefinition(out definition)
                    && definition != null)
                {
                    return true;
                }
            }

            definition = null;
            return false;
        }

        /// <summary>
        /// 遍历实例组件并复制第一份来源身份；不存在时返回空快照。
        /// </summary>
        public static ChipSourceReferenceSnapshot ReadSourceReference(Thing thing)
        {
            ThingWithComps thingWithComps = thing as ThingWithComps;
            if (thingWithComps?.AllComps != null)
            {
                for (int index = 0; index < thingWithComps.AllComps.Count; index++)
                {
                    IChipSourceReferenceProvider provider =
                        thingWithComps.AllComps[index] as IChipSourceReferenceProvider;
                    if (provider != null)
                    {
                        return new ChipSourceReferenceSnapshot
                        {
                            OrderedSourceKeys = provider.OrderedSourceKeys != null
                                ? new List<string>(provider.OrderedSourceKeys).AsReadOnly()
                                : new List<string>().AsReadOnly(),
                            SourceVariantKey = provider.SourceVariantKey,
                            SourceVariantLabel = provider.SourceVariantLabel
                        };
                    }
                }
            }

            return new ChipSourceReferenceSnapshot
            {
                OrderedSourceKeys = new List<string>().AsReadOnly(),
                SourceVariantKey = null,
                SourceVariantLabel = null
            };
        }
    }
}
