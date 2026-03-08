namespace BDP.Trigger
{
    /// <summary>
    /// 射击模式Comp属性（v9.0 FireMode系统）。
    /// 挂载到远程武器芯片ThingDef的comps列表中，
    /// 使芯片拥有可调节的伤害/速度/连射数预算分配能力。
    /// </summary>
    public class CompProperties_FireMode : Verse.CompProperties
    {
        public CompProperties_FireMode()
        {
            compClass = typeof(CompFireMode);
        }
    }
}
