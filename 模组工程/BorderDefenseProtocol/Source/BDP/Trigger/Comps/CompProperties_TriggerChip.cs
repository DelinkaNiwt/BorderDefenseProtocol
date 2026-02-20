using System;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 芯片物品的CompProperties——XML可配置参数。
    /// </summary>
    public class CompProperties_TriggerChip : CompProperties
    {
        /// <summary>建议槽位侧（Left/Right/Either）。</summary>
        public SlotSide preferredSide = SlotSide.Left;

        /// <summary>激活时一次性Trion消耗。</summary>
        public float activationCost = 0f;

        /// <summary>
        /// IChipEffect实现类（XML中填写全限定类名，如"BDP.Trigger.WeaponChipEffect"）。
        /// 实例化方式：Activator.CreateInstance(chipEffectClass)，要求无参构造函数。
        /// </summary>
        public Type chipEffectClass;

        public CompProperties_TriggerChip()
        {
            compClass = typeof(TriggerChipComp);
        }
    }
}
