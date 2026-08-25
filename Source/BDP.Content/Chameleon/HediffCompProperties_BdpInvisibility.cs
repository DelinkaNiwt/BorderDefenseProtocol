using Verse;

namespace BDP.Content.Chameleon
{
    /// <summary>
    /// 变色龙隐身组件配置。
    /// 继承原版通用隐身配置，不引用任何 DLC（下载内容）程序集。
    /// </summary>
    public sealed class HediffCompProperties_BdpInvisibility : HediffCompProperties_Invisibility
    {
        /// <summary>
        /// 将配置绑定到 BDP 自己的运行时隐身组件。
        /// </summary>
        public HediffCompProperties_BdpInvisibility()
        {
            compClass = typeof(HediffComp_BdpInvisibility);
        }
    }
}
