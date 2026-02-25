using System;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 芯片物品上的ThingComp——持有IChipEffect实现实例。
    /// 芯片本身无状态，效果实例由此Comp懒加载创建。
    /// </summary>
    public class TriggerChipComp : ThingComp
    {
        private IChipEffect effectInstance;

        public CompProperties_TriggerChip Props => (CompProperties_TriggerChip)props;

        /// <summary>
        /// 获取IChipEffect实例（懒加载）。
        /// 通过Activator.CreateInstance(chipEffectClass)创建，要求无参构造函数。
        /// </summary>
        public IChipEffect GetEffect()
        {
            if (effectInstance != null) return effectInstance;

            if (Props.chipEffectClass == null)
            {
                Log.Error($"[BDP] TriggerChipComp on {parent.def.defName}: chipEffectClass未配置");
                return null;
            }

            try
            {
                effectInstance = (IChipEffect)Activator.CreateInstance(Props.chipEffectClass);
            }
            catch (Exception e)
            {
                Log.Error($"[BDP] 无法实例化IChipEffect {Props.chipEffectClass}: {e.Message}");
            }

            return effectInstance;
        }
    }
}
