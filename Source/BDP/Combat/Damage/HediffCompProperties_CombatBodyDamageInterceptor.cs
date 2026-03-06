using Verse;

namespace BDP.Combat
{
    /// <summary>
    /// 战斗体伤害拦截器的属性定义。
    /// 用于在Hediff XML定义中声明此Comp。
    /// </summary>
    public class HediffCompProperties_CombatBodyDamageInterceptor : HediffCompProperties
    {
        public HediffCompProperties_CombatBodyDamageInterceptor()
        {
            compClass = typeof(HediffComp_CombatBodyDamageInterceptor);
        }
    }
}
