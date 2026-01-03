using Verse;

namespace Aldon.Energy
{
    /// <summary>
    /// 权限组件的属性定义
    /// 用于在Def中声明AldonPermissionComp组件
    /// </summary>
    public class AldonPermissionCompProperties : CompProperties
    {
        public AldonPermissionCompProperties()
        {
            compClass = typeof(AldonPermissionComp);
        }
    }
}
