using Verse;

namespace Aldon.Energy
{
    /// <summary>
    /// 能量容器组件的属性定义
    /// 用于在Def中声明AldonEnergyComp组件
    /// </summary>
    public class AldonEnergyCompProperties : CompProperties
    {
        public AldonEnergyCompProperties()
        {
            compClass = typeof(AldonEnergyComp);
        }
    }
}
