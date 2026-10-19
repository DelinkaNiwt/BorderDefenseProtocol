using RimWorld;

namespace BDP.Content.Trigger.Black.Formation
{
    /// <summary>黑触发器使用原版 Biocode（生物编码）组件的配置。</summary>
    public sealed class CompProperties_BlackTriggerBiocodable : CompProperties_Biocodable
    {
        /// <summary>构造绑定组件类型并默认装备即尝试绑定。</summary>
        public CompProperties_BlackTriggerBiocodable()
        {
            compClass = typeof(CompBlackTriggerBiocodable);
            biocodeOnEquip = true;
        }
    }
}
